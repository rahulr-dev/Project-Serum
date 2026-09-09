using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace InteractionSystem.Editor
{
    public class InteractionGraphWindow : EditorWindow
    {
        InteractionGraphView _graphView;
        InteractionGraph _graph;
        List<InteractionGraph> _assets = new List<InteractionGraph>();
        Vector2 _listScroll;
        Vector2 _inspectorScroll;
        string _filter = "";
        InteractionSearchWindow _search;
        SerializedObject _serializedGraph;

        [MenuItem("Serum/Interaction Editor", false, 22)]
        public static void Open()
        {
            InteractionGraphWindow window = GetWindow<InteractionGraphWindow>();
            window.titleContent = new GUIContent("Interaction Editor");
            window.minSize = new Vector2(900f, 500f);
        }

        public static void OpenGraph(InteractionGraph graph)
        {
            Open();
            GetWindow<InteractionGraphWindow>().Load(graph);
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

            _graphView = new InteractionGraphView { style = { flexGrow = 1 } };
            _graphView.RegisterCallback<KeyDownEvent>(OnGraphKeyDown);
            _graphView.nodeCreationRequest = OnNodeCreationRequest;
            right.Add(_graphView);

            IMGUIContainer inspector = new IMGUIContainer(DrawInspector);
            inspector.style.height = 240;
            inspector.style.minHeight = 140;
            right.Add(inspector);

            _search = CreateInstance<InteractionSearchWindow>();
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
            EditorGUILayout.LabelField("Interactions", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh"))
                RefreshAssets();
            if (GUILayout.Button("New Graph"))
                CreateGraphAsset();

            _filter = EditorGUILayout.TextField("Filter", _filter);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            for (int i = 0; i < _assets.Count; i++)
            {
                InteractionGraph asset = _assets[i];
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

            if (_graph != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Graph", EditorStyles.boldLabel);
                _serializedGraph ??= new SerializedObject(_graph);
                _serializedGraph.Update();
                EditorGUILayout.PropertyField(_serializedGraph.FindProperty("playState"));
                EditorGUILayout.PropertyField(_serializedGraph.FindProperty("endState"));
                _serializedGraph.ApplyModifiedProperties();
            }
        }

        void DrawInspector()
        {
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            if (_graphView == null || _graph == null)
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            InteractionNodeView selected = null;
            foreach (GraphElement element in _graphView.selection)
            {
                if (element is InteractionNodeView view)
                {
                    selected = view;
                    break;
                }
            }

            if (selected == null)
            {
                EditorGUILayout.HelpBox("Select an Action node to assign a scene GameObject UnityEvent.", MessageType.Info);
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
                EditorGUILayout.LabelField($"{selected.Data.kind}", EditorStyles.boldLabel);

                if (selected.Data.kind == InteractionNodeKind.Action)
                    InteractionActionInspector.DrawExecuteTarget(node.FindPropertyRelative("executeHandlerId"));
                else if (selected.Data.kind == InteractionNodeKind.Wait)
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("duration"));
                else
                    EditorGUILayout.HelpBox("This node has no extra fields.", MessageType.Info);

                so.ApplyModifiedProperties();
                break;
            }

            EditorGUILayout.EndScrollView();
        }

        void Load(InteractionGraph graph)
        {
            Save();
            _graph = graph;
            _serializedGraph = graph != null ? new SerializedObject(graph) : null;
            _graphView?.Populate(graph);
        }

        void Save()
        {
            if (_graphView == null || _graph == null)
                return;

            _graphView.SerializeEdges();
            foreach (GraphElement element in _graphView.graphElements.ToList())
            {
                if (element is InteractionNodeView view)
                    view.SyncPosition();
            }

            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssets();
        }

        void RefreshAssets()
        {
            _assets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:InteractionGraph");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                InteractionGraph graph = AssetDatabase.LoadAssetAtPath<InteractionGraph>(path);
                if (graph != null)
                    _assets.Add(graph);
            }
        }

        void CreateGraphAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("New Interaction Graph", "InteractionGraph", "asset", "Choose a save location");
            if (string.IsNullOrEmpty(path))
                return;

            InteractionGraph graph = CreateInstance<InteractionGraph>();
            graph.nodes.Add(new InteractionNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = InteractionNodeKind.Start,
                position = new Vector2(80f, 160f)
            });
            graph.nodes.Add(new InteractionNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = InteractionNodeKind.End,
                position = new Vector2(420f, 160f)
            });
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            RefreshAssets();
            Load(graph);
        }
    }
}
