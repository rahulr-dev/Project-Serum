using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace QTE.Editor
{
    public class QTEGraphWindow : EditorWindow
    {
        QTEGraphView _graphView;
        QTEGraph _graph;
        List<QTEGraph> _assets = new List<QTEGraph>();
        Vector2 _listScroll;
        string _filter = "";
        QTESearchWindow _search;
        SerializedObject _serializedGraph;

        [MenuItem("Serum/QTE Editor", false, 21)]
        public static void Open()
        {
            QTEGraphWindow window = GetWindow<QTEGraphWindow>();
            window.titleContent = new GUIContent("QTE Editor");
            window.minSize = new Vector2(900f, 500f);
        }

        public static void OpenGraph(QTEGraph graph)
        {
            Open();
            GetWindow<QTEGraphWindow>().Load(graph);
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

            _graphView = new QTEGraphView { style = { flexGrow = 1 } };
            _graphView.RegisterCallback<KeyDownEvent>(OnGraphKeyDown);
            _graphView.nodeCreationRequest = OnNodeCreationRequest;
            right.Add(_graphView);

            IMGUIContainer inspector = new IMGUIContainer(DrawInspector);
            inspector.style.height = 180;
            right.Add(inspector);

            _search = CreateInstance<QTESearchWindow>();
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
            EditorGUILayout.LabelField("QTE Graphs", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh"))
                RefreshAssets();
            if (GUILayout.Button("New Graph"))
                CreateGraphAsset();

            _filter = EditorGUILayout.TextField("Filter", _filter);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            for (int i = 0; i < _assets.Count; i++)
            {
                QTEGraph asset = _assets[i];
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
                EditorGUILayout.PropertyField(_serializedGraph.FindProperty("defaultOutcome"));
                _serializedGraph.ApplyModifiedProperties();
            }
        }

        void DrawInspector()
        {
            if (_graphView == null || _graph == null)
                return;

            QTENodeView selected = null;
            foreach (GraphElement element in _graphView.selection)
            {
                if (element is QTENodeView view)
                {
                    selected = view;
                    break;
                }
            }

            if (selected == null)
            {
                EditorGUILayout.HelpBox("Select a node to edit serialized fields (Action target GameObject, Sequence child IDs).", MessageType.Info);
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

                if (selected.Data.kind == QTENodeKind.Action)
                    QTEActionInspector.DrawExecuteTarget(node.FindPropertyRelative("executeHandlerId"));
                else if (selected.Data.kind == QTENodeKind.Sequence)
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("childNodeIds"), true);
                else if (selected.Data.kind == QTENodeKind.SequenceInput)
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("inputSequence"), true);
                else if (selected.Data.kind == QTENodeKind.Branch)
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("branchLabels"), true);

                so.ApplyModifiedProperties();
                break;
            }
        }

        void Load(QTEGraph graph)
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
                if (element is QTENodeView view)
                    view.SyncPosition();
            }

            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssets();
        }

        void RefreshAssets()
        {
            _assets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:QTEGraph");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                QTEGraph graph = AssetDatabase.LoadAssetAtPath<QTEGraph>(path);
                if (graph != null)
                    _assets.Add(graph);
            }
        }

        void CreateGraphAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("New QTE Graph", "QTEGraph", "asset", "Choose a save location");
            if (string.IsNullOrEmpty(path))
                return;

            QTEGraph graph = CreateInstance<QTEGraph>();
            graph.nodes.Add(new QTENodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = QTENodeKind.Start,
                position = new Vector2(80f, 160f)
            });
            graph.nodes.Add(new QTENodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = QTENodeKind.End,
                position = new Vector2(520f, 160f),
                endOutcome = QTEOutcome.Success
            });
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            RefreshAssets();
            Load(graph);
        }
    }
}
