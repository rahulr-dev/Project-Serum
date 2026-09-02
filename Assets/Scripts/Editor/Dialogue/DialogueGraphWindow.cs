using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dialogue.Editor
{
    public class DialogueGraphWindow : EditorWindow
    {
        DialogueGraphView _graphView;
        DialogueGraph _graph;
        List<DialogueGraph> _assets = new List<DialogueGraph>();
        Vector2 _listScroll;
        string _filter = "";
        DialogueSearchWindow _search;
        SerializedObject _serializedGraph;

        [MenuItem("Serum/Dialogue Editor", false, 20)]
        public static void Open()
        {
            DialogueGraphWindow window = GetWindow<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Editor");
            window.minSize = new Vector2(900f, 500f);
        }

        public static void OpenGraph(DialogueGraph graph)
        {
            Open();
            GetWindow<DialogueGraphWindow>().Load(graph);
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

            _graphView = new DialogueGraphView { style = { flexGrow = 1 } };
            _graphView.RegisterCallback<KeyDownEvent>(OnGraphKeyDown);
            _graphView.nodeCreationRequest = OnNodeCreationRequest;
            right.Add(_graphView);

            IMGUIContainer inspector = new IMGUIContainer(DrawInspector);
            inspector.style.height = 140;
            right.Add(inspector);

            _search = CreateInstance<DialogueSearchWindow>();
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
            EditorGUILayout.LabelField("Dialogues", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh"))
                RefreshAssets();
            if (GUILayout.Button("New Graph"))
                CreateGraphAsset();

            _filter = EditorGUILayout.TextField("Filter", _filter);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            for (int i = 0; i < _assets.Count; i++)
            {
                DialogueGraph asset = _assets[i];
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
                EditorGUILayout.PropertyField(_serializedGraph.FindProperty("charsPerSecond"));
                EditorGUILayout.PropertyField(_serializedGraph.FindProperty("choiceRepeatDelay"));
                _serializedGraph.ApplyModifiedProperties();
            }
        }

        void DrawInspector()
        {
            if (_graphView == null)
                return;

            DialogueNodeView selected = null;
            foreach (GraphElement element in _graphView.selection)
            {
                if (element is DialogueNodeView view)
                {
                    selected = view;
                    break;
                }
            }

            if (selected == null || selected.Data.kind != DialogueNodeKind.Line)
            {
                EditorGUILayout.HelpBox("Select a Line node to edit On Start (UnityEvent).", MessageType.Info);
                return;
            }

            if (_graph == null)
                return;

            SerializedObject so = new SerializedObject(_graph);
            SerializedProperty nodes = so.FindProperty("nodes");
            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty node = nodes.GetArrayElementAtIndex(i);
                if (node.FindPropertyRelative("id").stringValue != selected.Data.id)
                    continue;

                so.Update();
                EditorGUILayout.LabelField($"On Start — {selected.Data.speaker}", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(node.FindPropertyRelative("onStart"));
                so.ApplyModifiedProperties();
                break;
            }
        }

        void Load(DialogueGraph graph)
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
                if (element is DialogueNodeView view)
                    view.SyncPosition();
            }

            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssets();
        }

        void RefreshAssets()
        {
            _assets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:DialogueGraph");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                DialogueGraph graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(path);
                if (graph != null)
                    _assets.Add(graph);
            }
        }

        void CreateGraphAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("New Dialogue Graph", "DialogueGraph", "asset", "Choose a save location");
            if (string.IsNullOrEmpty(path))
                return;

            DialogueGraph graph = CreateInstance<DialogueGraph>();
            graph.nodes.Add(new DialogueNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = DialogueNodeKind.Start,
                position = new Vector2(80f, 160f)
            });
            graph.nodes.Add(new DialogueNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = DialogueNodeKind.End,
                position = new Vector2(420f, 160f)
            });
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            RefreshAssets();
            Load(graph);
        }
    }
}
