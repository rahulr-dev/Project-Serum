using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Project.Editor
{
    /// <summary>
    /// Adds a project-scene picker to Unity's main toolbar menu.
    /// </summary>
    [InitializeOnLoad]
    static class SceneToolbarMenu
    {
        const string MenuRoot = "Scene/";
        static readonly List<string> RegisteredPaths = new List<string>();
        static readonly MethodInfo AddMenuItem = typeof(UnityEditor.Editor).Assembly
            .GetType("UnityEditor.Menu")
            ?.GetMethod("AddMenuItem", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        static readonly MethodInfo RemoveMenuItem = typeof(UnityEditor.Editor).Assembly
            .GetType("UnityEditor.Menu")
            ?.GetMethod("RemoveMenuItem", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        static SceneToolbarMenu()
        {
            EditorApplication.delayCall += Rebuild;
            EditorApplication.projectChanged += Rebuild;
            AssemblyReloadEvents.beforeAssemblyReload += RemoveRegisteredItems;
        }

        static void Rebuild()
        {
            RemoveRegisteredItems();

            if (AddMenuItem == null)
                return;

            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            Array.Sort(sceneGuids, CompareScenePaths);

            foreach (string sceneGuid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                if (string.IsNullOrEmpty(scenePath))
                    continue;

                string menuPath = MenuRoot + scenePath.Substring("Assets/".Length, scenePath.Length - "Assets/".Length - ".unity".Length);
                string capturedScenePath = scenePath;
                AddMenuItem.Invoke(null, new object[]
                {
                    menuPath,
                    string.Empty,
                    false,
                    0,
                    (Action)(() => OpenScene(capturedScenePath)),
                    (Func<bool>)(() => !EditorApplication.isPlayingOrWillChangePlaymode)
                });
                RegisteredPaths.Add(menuPath);
            }
        }

        static int CompareScenePaths(string leftGuid, string rightGuid)
        {
            return string.Compare(
                AssetDatabase.GUIDToAssetPath(leftGuid),
                AssetDatabase.GUIDToAssetPath(rightGuid),
                StringComparison.OrdinalIgnoreCase);
        }

        static void OpenScene(string scenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        static void RemoveRegisteredItems()
        {
            if (RemoveMenuItem != null)
            {
                foreach (string menuPath in RegisteredPaths)
                    RemoveMenuItem.Invoke(null, new object[] { menuPath });
            }

            RegisteredPaths.Clear();
        }
    }
}
