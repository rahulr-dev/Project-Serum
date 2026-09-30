using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Serum.Editor
{
    public sealed class VertexCounterWindow : EditorWindow
    {
        [SerializeField] bool allScenes;
        [SerializeField] bool includeInactive = true;
        readonly List<Entry> entries = new List<Entry>();
        readonly HashSet<int> visited = new HashSet<int>();
        readonly HashSet<Mesh> uniqueMeshes = new HashSet<Mesh>();
        Vector2 scroll;
        long totalVertices;
        long uniqueVertices;
        double nextRefresh;

        struct Entry
        {
            public Object source;
            public Mesh mesh;
        }

        [MenuItem("Serum/Vertex Counter", false, 27)]
        public static void Open()
        {
            var window = GetWindow<VertexCounterWindow>("Vertex Counter");
            window.minSize = new Vector2(380, 260);
        }

        void OnEnable()
        {
            Selection.selectionChanged += Refresh;
            EditorApplication.hierarchyChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            Selection.selectionChanged -= Refresh;
            EditorApplication.hierarchyChanged -= Refresh;
        }

        void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup >= nextRefresh)
                Refresh();
        }

        void Refresh()
        {
            nextRefresh = EditorApplication.timeSinceStartup + 1;
            entries.Clear();
            visited.Clear();
            uniqueMeshes.Clear();
            totalVertices = 0;
            uniqueVertices = 0;

            if (allScenes)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;
                    foreach (var root in scene.GetRootGameObjects())
                        Collect(root);
                }
            }
            else
            {
                foreach (var selected in Selection.objects)
                {
                    if (selected is GameObject gameObject) Collect(gameObject);
                    else if (selected is Component component) Collect(component.gameObject);
                    else if (selected is Mesh mesh) Add(mesh, mesh);
                }
            }

            entries.Sort((a, b) => b.mesh.vertexCount.CompareTo(a.mesh.vertexCount));
            Repaint();
        }

        void Collect(GameObject root)
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                if (includeInactive || filter.gameObject.activeInHierarchy)
                    Add(filter, filter.sharedMesh);
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (includeInactive || renderer.gameObject.activeInHierarchy)
                    Add(renderer, renderer.sharedMesh);
        }

        void Add(Object source, Mesh mesh)
        {
            if (mesh == null || !visited.Add(source.GetInstanceID())) return;
            entries.Add(new Entry { source = source, mesh = mesh });
            totalVertices += mesh.vertexCount;
            if (uniqueMeshes.Add(mesh)) uniqueVertices += mesh.vertexCount;
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            allScenes = GUILayout.Toolbar(allScenes ? 1 : 0,
                new[] { "Selection + Children", "Loaded Scenes" }) == 1;
            includeInactive = EditorGUILayout.Toggle("Include inactive objects", includeInactive);
            if (EditorGUI.EndChangeCheck()) Refresh();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Total vertices", totalVertices.ToString("N0"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Unique mesh vertices", uniqueVertices.ToString("N0"));
            EditorGUILayout.LabelField("Mesh instances / unique meshes", $"{entries.Count:N0} / {uniqueMeshes.Count:N0}");
            EditorGUILayout.HelpBox("Totals count each mesh instance, including all LOD levels and disabled renderers. " +
                "Unique vertices count each shared mesh once. These are mesh data counts, not camera rendering statistics. " +
                "Terrain, particles and procedural draws are excluded.", MessageType.Info);

            if (GUILayout.Button("Refresh")) Refresh();
            if (entries.Count == 0)
                EditorGUILayout.HelpBox(allScenes ? "No meshes found in loaded scenes." :
                    "Select scene objects, prefabs, models or Mesh assets to inspect their vertices.", MessageType.Info);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var entry in entries)
            {
                if (entry.source == null || entry.mesh == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(entry.source, typeof(Object), true);
                GUILayout.Label(entry.mesh.vertexCount.ToString("N0"), GUILayout.Width(85));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
