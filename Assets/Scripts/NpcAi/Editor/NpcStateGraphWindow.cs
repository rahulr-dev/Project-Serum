using System.Collections.Generic;
using System.Linq;
using NpcAi;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NpcAi.Editor
{
    public class NpcStateGraphWindow : EditorWindow
    {
        const string DefaultFolder = "Assets/Data/NpcStates";

        NpcStateGraphView _graphView;
        NpcStateGraph _graph;
        List<NpcStateGraph> _assets = new List<NpcStateGraph>();
        Vector2 _listScroll;
        Vector2 _inspectorScroll;
        string _filter = "";
        NpcStateSearchWindow _search;

        [MenuItem("Serum/NPC State Editor", false, 24)]
        public static void Open()
        {
            NpcStateGraphWindow window = GetWindow<NpcStateGraphWindow>();
            window.titleContent = new GUIContent("NPC State Editor");
            window.minSize = new Vector2(900f, 500f);
        }

        public static void OpenGraph(NpcStateGraph graph)
        {
            Open();
            GetWindow<NpcStateGraphWindow>().Load(graph);
        }

        void OnEnable()
        {
            RefreshAssets();
            BuildUi();
            if (_graph != null)
                Load(_graph);
        }

        void OnDisable()
        {
            Save();
        }

        void BuildUi()
        {
            rootVisualElement.Clear();

            VisualElement root = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            rootVisualElement.Add(root);

            IMGUIContainer sidebar = new IMGUIContainer(DrawSidebar);
            sidebar.style.width = 240;
            sidebar.style.minWidth = 240;
            root.Add(sidebar);

            VisualElement right = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };
            root.Add(right);

            _graphView = new NpcStateGraphView { style = { flexGrow = 1 } };
            _graphView.RegisterCallback<KeyDownEvent>(OnGraphKeyDown);
            _graphView.nodeCreationRequest = OnNodeCreationRequest;
            right.Add(_graphView);

            IMGUIContainer inspector = new IMGUIContainer(DrawInspector);
            inspector.style.height = 240;
            inspector.style.minHeight = 140;
            right.Add(inspector);

            _search = CreateInstance<NpcStateSearchWindow>();
            _search.Init(_graphView);
        }

        void OnNodeCreationRequest(NodeCreationContext context)
        {
            if (_graphView == null)
                return;

            _search.SetGraphMouse(_graphView.LastContentMouse);
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _search);
        }

        void OnGraphKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.S && evt.ctrlKey)
            {
                Save();
                evt.StopPropagation();
            }
        }

        void DrawSidebar()
        {
            EditorGUILayout.LabelField("NPC State Graphs", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh"))
                RefreshAssets();
            if (GUILayout.Button("New Graph"))
                CreateGraphAsset();

            _filter = EditorGUILayout.TextField("Filter", _filter);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            for (int i = 0; i < _assets.Count; i++)
            {
                NpcStateGraph asset = _assets[i];
                if (asset == null)
                    continue;

                string name = asset.name;
                if (!string.IsNullOrEmpty(_filter) && name.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                EditorGUILayout.BeginHorizontal();
                bool selected = asset == _graph;
                if (GUILayout.Toggle(selected, name, "Button") && !selected)
                    Load(asset);
                if (GUILayout.Button("Ping", GUILayout.Width(44)))
                    EditorGUIUtility.PingObject(asset);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawInspector()
        {
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            if (_graphView == null || _graph == null)
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            NpcStateNodeView selected = null;
            foreach (GraphElement element in _graphView.selection)
            {
                if (element is NpcStateNodeView view)
                {
                    selected = view;
                    break;
                }
            }

            if (selected == null)
            {
                EditorGUILayout.HelpBox("Select a node to edit Character Action params or Scene Action targets.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            SerializedObject so = new SerializedObject(_graph);
            SerializedProperty nodes = so.FindProperty("nodes");
            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty node = nodes.GetArrayElementAtIndex(i);
                if (node.FindPropertyRelative("id").stringValue != selected.Data.id)
                    continue;

                so.Update();
                EditorGUILayout.LabelField($"{selected.Data.kind} — {selected.Data.id}", EditorStyles.boldLabel);

                switch (selected.Data.kind)
                {
                    case NpcStateNodeKind.CharacterAction:
                        NpcStateCharacterActionInspector.Draw(node);
                        selected.RefreshTitle();
                        break;
                    case NpcStateNodeKind.SceneAction:
                        NpcSceneActionInspector.DrawExecuteTarget(node.FindPropertyRelative("executeHandlerId"));
                        break;
                    case NpcStateNodeKind.Wait:
                        EditorGUILayout.PropertyField(node.FindPropertyRelative("duration"));
                        break;
                    case NpcStateNodeKind.WaitEvent:
                        EditorGUILayout.PropertyField(node.FindPropertyRelative("waitEventId"));
                        EditorGUILayout.PropertyField(node.FindPropertyRelative("duration"), new GUIContent("Timeout"));
                        selected.RefreshTitle();
                        break;
                    case NpcStateNodeKind.End:
                        EditorGUILayout.PropertyField(node.FindPropertyRelative("endOutcome"));
                        break;
                    case NpcStateNodeKind.RandomBranch:
                        DrawRandomBranch(node, selected);
                        break;
                }

                int portsBefore = selected.OutputPorts.Count;
                so.ApplyModifiedProperties();
                if (selected.Data.kind == NpcStateNodeKind.RandomBranch &&
                    selected.Data.GetBranchCount() != portsBefore)
                {
                    selected.RebuildRandomOutputs();
                    _graphView.SerializeEdges();
                }

                break;
            }

            EditorGUILayout.EndScrollView();
        }

        static void DrawRandomBranch(SerializedProperty node, NpcStateNodeView selected)
        {
            SerializedProperty countProp = node.FindPropertyRelative("branchCount");
            int current = selected.Data.GetBranchCount();
            if (countProp.intValue != current)
                countProp.intValue = current;

            int newCount = EditorGUILayout.IntSlider(
                "Branch Count",
                current,
                NpcStateNodeData.MinBranchCount,
                NpcStateNodeData.MaxBranchCount);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Pin") && current < NpcStateNodeData.MaxBranchCount)
                newCount = current + 1;
            if (GUILayout.Button("Remove Pin") && current > NpcStateNodeData.MinBranchCount)
                newCount = current - 1;
            EditorGUILayout.EndHorizontal();

            if (newCount != current)
                countProp.intValue = newCount;

            EditorGUILayout.HelpBox(
                "Each pin is an equally likely path. Unconnected pins still count and will complete the state.",
                MessageType.Info);
        }

        void Load(NpcStateGraph graph)
        {
            Save();
            _graph = graph;
            _graphView?.Populate(graph);
        }

        void Save()
        {
            if (_graphView == null || _graph == null)
                return;

            _graphView.SerializeEdges();
            foreach (GraphElement element in _graphView.graphElements.ToList())
            {
                if (element is NpcStateNodeView view)
                    view.SyncPosition();
            }

            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssets();
        }

        void RefreshAssets()
        {
            _assets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:NpcStateGraph");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                NpcStateGraph graph = AssetDatabase.LoadAssetAtPath<NpcStateGraph>(path);
                if (graph != null)
                    _assets.Add(graph);
            }
        }

        void CreateGraphAsset()
        {
            EnsureDefaultFolder();
            string path = EditorUtility.SaveFilePanelInProject(
                "New NPC State Graph",
                "NpcStateGraph",
                "asset",
                "Choose a save location",
                DefaultFolder);
            if (string.IsNullOrEmpty(path))
                return;

            NpcStateGraph graph = CreateInstance<NpcStateGraph>();
            graph.nodes.Add(new NpcStateNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = NpcStateNodeKind.Start,
                position = new Vector2(80f, 160f)
            });
            graph.nodes.Add(new NpcStateNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = NpcStateNodeKind.End,
                position = new Vector2(520f, 160f),
                endOutcome = NpcStateOutcome.Completed
            });
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            RefreshAssets();
            Load(graph);
        }

        static void EnsureDefaultFolder()
        {
            if (AssetDatabase.IsValidFolder(DefaultFolder))
                return;

            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");

            AssetDatabase.CreateFolder("Assets/Data", "NpcStates");
        }
    }
}
