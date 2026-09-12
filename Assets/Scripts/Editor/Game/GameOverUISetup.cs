using Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class GameOverUISetup
    {
        const string Level1Path = "Assets/Scene/Level_01/Level 1.unity";
        const string PrefabPath = "Assets/Prefabs/GameOver_UI.prefab";
        const string RootName = "GameOver_UI";
        const string PanelName = "GameOver_Panel";
        const string ButtonName = "RestartFromCheckpoint_Button";

        [MenuItem("Serum/Game/Create Game Over UI In Scene", false, 40)]
        public static void CreateInActiveScene()
        {
            GameObject root = BuildInScene();
            SavePrefab(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log($"[Game] Game Over UI ready in '{SceneManager.GetActiveScene().name}'. Prefab saved to {PrefabPath}.", root);
        }

        [MenuItem("Serum/Game/Setup Game Over UI In Level 1", false, 41)]
        public static void SetupLevel1()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);
            GameObject root = BuildInScene();
            SavePrefab(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            Debug.Log($"[Game] Game Over UI set up in Level 1. Prefab saved to {PrefabPath}.", root);
        }

        /// <summary>
        /// Batch / -executeMethod entry point (no interactive save prompt).
        /// </summary>
        public static void SetupLevel1Batch()
        {
            Scene scene = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);
            GameObject root = BuildInScene();
            SavePrefab(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Game] Batch: Game Over UI set up in Level 1. Prefab saved to {PrefabPath}.");
        }

        static GameObject BuildInScene()
        {
            EnsureManager<GameStateManager>("GameStateManager");
            EnsureManager<SerumSceneManager>("SerumSceneManager");
            EnsureEventSystem();

            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            GameObject root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameOverUI));
            Undo.RegisterCreatedObjectUndo(root, "Create Game Over UI");

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject panel = CreateUiObject(PanelName, root.transform);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.75f);
            StretchFull(panel.GetComponent<RectTransform>());

            GameObject title = CreateUiObject("Title_Text", panel.transform);
            Text titleText = title.AddComponent<Text>();
            titleText.text = "Game Over";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontSize = 64;
            titleText.color = Color.white;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(800f, 120f);
            titleRect.anchoredPosition = new Vector2(0f, 80f);

            GameObject buttonGo = CreateUiObject(ButtonName, panel.transform);
            Image buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.85f, 0.2f, 0.2f, 1f);
            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(420f, 64f);
            buttonRect.anchoredPosition = new Vector2(0f, -40f);

            GameObject buttonLabel = CreateUiObject("Label", buttonGo.transform);
            Text labelText = buttonLabel.AddComponent<Text>();
            labelText.text = "Restart From Checkpoint";
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.fontSize = 28;
            labelText.color = Color.white;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            StretchFull(buttonLabel.GetComponent<RectTransform>());

            GameOverUI ui = root.GetComponent<GameOverUI>();
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("rootPanel").objectReferenceValue = panel;
            so.FindProperty("restartButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return root;
        }

        static void SavePrefab(GameObject root)
        {
            EnsureFolder("Assets/Prefabs");
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        }

        static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void EnsureManager<T>(string objectName) where T : Component
        {
            T existing = Object.FindFirstObjectByType<T>();
            if (existing != null)
                return;

            GameObject go = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(go, $"Create {objectName}");
            go.AddComponent<T>();
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
