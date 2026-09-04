using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InteractionSystem.Editor
{
    public class InteractionGraphEditor : EditorWindow
    {
        private InteractionSequenceSO targetSequence;
        private InteractionNode draggingNode = null;
        private Vector2 dragOffset;
        private InteractionNode connectingFromNode = null;

        private const float NodeWidth = 220f;
        private const float NodeHeightStartEnd = 52f;
        private const float NodeHeightSwitch = 90f;
        private const float PortVisualRadius = 7f;
        private const float PortHitboxRadius = 24f; // Generous 48px hitbox for effortless connection
        private const string DefaultAssetPath = "Assets/Interaction/Sequences/InteractionSequenceSO.asset";

        private readonly Color inputPortColor = new Color(0.95f, 0.3f, 0.3f, 1f);
        private readonly Color outputPortColor = new Color(0.3f, 0.85f, 0.4f, 1f);
        private readonly Color activeWireColor = new Color(1f, 0.85f, 0.2f, 1f);
        private readonly Color connectionWireColor = new Color(0.35f, 0.75f, 1f, 1f);
        private readonly Color validTargetHighlightColor = new Color(0.3f, 1f, 0.5f, 0.95f);

        [MenuItem("Interaction/Interaction Graph")]
        public static void OpenWindow()
        {
            InteractionGraphEditor window = GetWindow<InteractionGraphEditor>("Interaction Graph");
            window.minSize = new Vector2(750, 520);
            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            TryFindSequence();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            TryFindSequence();
            Repaint();
        }

        private void TryFindSequence()
        {
            if (targetSequence != null) return;

            if (Selection.activeObject is InteractionSequenceSO so)
            {
                targetSequence = so;
                return;
            }

            if (Selection.activeGameObject != null)
            {
                InteractionGraph graph = Selection.activeGameObject.GetComponent<InteractionGraph>();
                if (graph != null && graph.Sequence != null)
                {
                    targetSequence = graph.Sequence;
                    return;
                }
            }

            if (File.Exists(DefaultAssetPath))
            {
                targetSequence = AssetDatabase.LoadAssetAtPath<InteractionSequenceSO>(DefaultAssetPath);
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (targetSequence == null)
            {
                EditorGUILayout.HelpBox("Please select or create an InteractionSequenceSO asset.", MessageType.Info);
                if (GUILayout.Button("Create Default InteractionSequenceSO Asset", GUILayout.Height(32)))
                {
                    CreateOrLoadDefaultAsset();
                }
                return;
            }

            DrawBackgroundGrid(20f, 0.15f, Color.gray);
            DrawBackgroundGrid(100f, 0.35f, Color.gray);

            Event e = Event.current;

            // Handle connection and node events
            HandleEvents(e);

            // Render graph elements
            DrawConnections();
            DrawNodes(e);
            DrawConnectingWire(e);

            // Repaint continuously when interacting for 60fps responsive feel
            if (connectingFromNode != null || draggingNode != null || e.type == EventType.MouseMove)
            {
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Sequence Asset:", GUILayout.Width(100));
            targetSequence = (InteractionSequenceSO)EditorGUILayout.ObjectField(targetSequence, typeof(InteractionSequenceSO), false, GUILayout.Width(250));

            if (GUILayout.Button("Create New Asset", EditorStyles.toolbarButton))
            {
                CreateNewSequenceAsset();
            }

            if (targetSequence != null)
            {
                if (GUILayout.Button("Reset Flow (Start -> SwitchEvent -> End)", EditorStyles.toolbarButton))
                {
                    ResetToDefaultFlow();
                }

                if (GUILayout.Button("Save Asset", EditorStyles.toolbarButton))
                {
                    SaveAsset();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBackgroundGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(new Vector3(gridSpacing * i, 0f, 0f), new Vector3(gridSpacing * i, position.height, 0f));
            }

            for (int j = 0; j < heightDivs; j++)
            {
                Handles.DrawLine(new Vector3(0f, gridSpacing * j, 0f), new Vector3(position.width, gridSpacing * j, 0f));
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawNodes(Event e)
        {
            if (targetSequence.Nodes == null) return;

            for (int i = 0; i < targetSequence.Nodes.Count; i++)
            {
                InteractionNode node = targetSequence.Nodes[i];
                if (node == null) continue;

                Rect nodeRect = GetNodeRect(node);
                Color headerColor = GetNodeHeaderColor(node.NodeType);
                Color bodyColor = new Color(0.18f, 0.18f, 0.20f, 0.98f);

                // Main node body background
                EditorGUI.DrawRect(nodeRect, bodyColor);

                // Header bar
                Rect headerRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, 24f);
                EditorGUI.DrawRect(headerRect, headerColor);

                // Header Title
                GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.white },
                    padding = new RectOffset(10, 0, 0, 0)
                };
                GUI.Label(headerRect, GetNodeTitle(node.NodeType), headerStyle);

                // Close / Delete button in header
                Rect deleteBtnRect = new Rect(nodeRect.xMax - 22f, nodeRect.y + 3f, 18f, 18f);
                if (GUI.Button(deleteBtnRect, "×", EditorStyles.miniButton))
                {
                    DeleteNode(node);
                    GUIUtility.ExitGUI();
                }

                // Outline
                Handles.DrawSolidRectangleWithOutline(nodeRect, Color.clear, new Color(0.1f, 0.1f, 0.1f, 0.85f));

                // Node Body Content for SwitchEvent
                if (node.NodeType == InteractionNodeType.SwitchEvent)
                {
                    Rect contentRect = new Rect(nodeRect.x + 8f, nodeRect.y + 28f, nodeRect.width - 16f, nodeRect.height - 32f);
                    GUILayout.BeginArea(contentRect);
                    EditorGUILayout.LabelField("Switch Event Component:", EditorStyles.miniLabel);
                    EditorGUI.BeginChangeCheck();
                    SwitchEvent newEvt = (SwitchEvent)EditorGUILayout.ObjectField(node.SwitchEvent, typeof(SwitchEvent), true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetSequence, "Change Node SwitchEvent");
                        node.SwitchEvent = newEvt;
                        SaveAsset();
                    }
                    GUILayout.EndArea();
                }

                // ─── Input Port (Top Center) ──────────────────────────────
                if (node.NodeType == InteractionNodeType.SwitchEvent || node.NodeType == InteractionNodeType.End)
                {
                    Vector2 inPortPos = GetInputPortPosition(node);
                    bool isHovered = Vector2.Distance(e.mousePosition, inPortPos) <= PortHitboxRadius;
                    bool isValidTarget = connectingFromNode != null && connectingFromNode != node && IsValidConnection(connectingFromNode, node);

                    DrawPortHandle(inPortPos, inputPortColor, isHovered, isValidTarget);
                }

                // ─── Output Port (Bottom Center) ──────────────────────────
                if (node.NodeType == InteractionNodeType.Start || node.NodeType == InteractionNodeType.SwitchEvent)
                {
                    Vector2 outPortPos = GetOutputPortPosition(node);
                    bool isHovered = Vector2.Distance(e.mousePosition, outPortPos) <= PortHitboxRadius;

                    DrawPortHandle(outPortPos, outputPortColor, isHovered, false);
                }
            }
        }

        private void DrawPortHandle(Vector2 position, Color color, bool isHovered, bool isValidTarget)
        {
            Handles.BeginGUI();

            // Highlight ring if valid target or hovered
            if (isValidTarget || isHovered)
            {
                Handles.color = isHovered ? Color.white : validTargetHighlightColor;
                Handles.DrawWireDisc(position, Vector3.forward, PortVisualRadius + 4f);
            }

            // Outer dark ring
            Handles.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            Handles.DrawSolidDisc(position, Vector3.forward, PortVisualRadius + 2f);

            // Colored port center
            Handles.color = isHovered ? Color.Lerp(color, Color.white, 0.35f) : color;
            Handles.DrawSolidDisc(position, Vector3.forward, PortVisualRadius);

            // Subtle white border
            Handles.color = Color.white;
            Handles.DrawWireDisc(position, Vector3.forward, PortVisualRadius);

            Handles.EndGUI();
        }

        private void DrawConnections()
        {
            if (targetSequence.Edges == null || targetSequence.Nodes == null) return;

            Handles.BeginGUI();
            for (int i = targetSequence.Edges.Count - 1; i >= 0; i--)
            {
                InteractionEdge edge = targetSequence.Edges[i];
                InteractionNode fromNode = targetSequence.GetNodeByID(edge.FromNodeID);
                InteractionNode toNode = targetSequence.GetNodeByID(edge.ToNodeID);

                if (fromNode == null || toNode == null)
                {
                    targetSequence.Edges.RemoveAt(i);
                    SaveAsset();
                    continue;
                }

                Vector2 startPos = GetOutputPortPosition(fromNode);
                Vector2 endPos = GetInputPortPosition(toNode);

                Vector2 startTan = startPos + Vector2.up * 45f;
                Vector2 endTan = endPos - Vector2.up * 45f;

                // Draw Directed Bézier Wire
                Handles.DrawBezier(startPos, endPos, startTan, endTan, connectionWireColor, null, 3.5f);

                // Small delete button at curve midpoint
                Vector2 midPoint = (startPos + endPos) * 0.5f;
                Rect delRect = new Rect(midPoint.x - 9f, midPoint.y - 9f, 18f, 18f);
                if (GUI.Button(delRect, "×", EditorStyles.miniButton))
                {
                    Undo.RecordObject(targetSequence, "Delete Edge");
                    targetSequence.Edges.RemoveAt(i);
                    SaveAsset();
                    GUIUtility.ExitGUI();
                }
            }
            Handles.EndGUI();
        }

        private void DrawConnectingWire(Event e)
        {
            if (connectingFromNode == null) return;

            Vector2 startPos = GetOutputPortPosition(connectingFromNode);

            // Check if mouse is hovering over a valid input target port for visual snapping
            InteractionNode hoveredTarget = GetInputPortNodeAtPosition(e.mousePosition);
            Vector2 endPos;
            if (hoveredTarget != null && IsValidConnection(connectingFromNode, hoveredTarget))
            {
                endPos = GetInputPortPosition(hoveredTarget);
            }
            else
            {
                endPos = e.mousePosition;
            }

            Handles.BeginGUI();
            Vector2 startTan = startPos + Vector2.up * 45f;
            Vector2 endTan = endPos - Vector2.up * 45f;
            Handles.DrawBezier(startPos, endPos, startTan, endTan, activeWireColor, null, 3.5f);
            Handles.EndGUI();
        }

        private void HandleEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0) // Left click
                    {
                        // 1. Check if clicking on an OUTPUT port to begin connection
                        InteractionNode outNode = GetOutputPortNodeAtPosition(e.mousePosition);
                        if (outNode != null)
                        {
                            connectingFromNode = outNode;
                            draggingNode = null;
                            e.Use();
                            return;
                        }

                        // 2. Check if clicking on a node body to drag node
                        InteractionNode clickedNode = GetNodeAtPosition(e.mousePosition);
                        if (clickedNode != null)
                        {
                            draggingNode = clickedNode;
                            dragOffset = new Vector2(clickedNode.EditorX, clickedNode.EditorY) - e.mousePosition;
                            e.Use();
                            return;
                        }

                        // 3. Clicked empty background cancels any pending wire
                        if (connectingFromNode != null)
                        {
                            connectingFromNode = null;
                            e.Use();
                            return;
                        }
                    }
                    else if (e.button == 1) // Right click context menu
                    {
                        if (connectingFromNode != null)
                        {
                            connectingFromNode = null;
                            e.Use();
                        }
                        else
                        {
                            ShowContextMenu(e.mousePosition);
                            e.Use();
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0)
                    {
                        if (draggingNode != null)
                        {
                            Undo.RecordObject(targetSequence, "Move Node");
                            Vector2 newPos = e.mousePosition + dragOffset;
                            draggingNode.Position = newPos;
                            EditorUtility.SetDirty(targetSequence);
                            e.Use();
                        }
                        else if (connectingFromNode != null)
                        {
                            // Active wire drag
                            e.Use();
                        }
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0)
                    {
                        if (connectingFromNode != null)
                        {
                            // Check if mouse was released over a valid target input port
                            InteractionNode targetInputNode = GetInputPortNodeAtPosition(e.mousePosition);
                            if (targetInputNode != null && IsValidConnection(connectingFromNode, targetInputNode))
                            {
                                TryConnectNodes(connectingFromNode, targetInputNode);
                            }

                            connectingFromNode = null;
                            e.Use();
                            return;
                        }

                        if (draggingNode != null)
                        {
                            draggingNode = null;
                            SaveAsset();
                            e.Use();
                            return;
                        }
                    }
                    break;
            }
        }

        private void ShowContextMenu(Vector2 mousePosition)
        {
            GenericMenu menu = new GenericMenu();

            bool hasStart = targetSequence.Nodes.Exists(n => n.NodeType == InteractionNodeType.Start);
            bool hasEnd = targetSequence.Nodes.Exists(n => n.NodeType == InteractionNodeType.End);

            if (!hasStart)
            {
                menu.AddItem(new GUIContent("Add Start"), false, () => AddNode(InteractionNodeType.Start, mousePosition));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Add Start (Only 1 allowed)"));
            }

            menu.AddItem(new GUIContent("Add Switch Event"), false, () => AddNode(InteractionNodeType.SwitchEvent, mousePosition));

            if (!hasEnd)
            {
                menu.AddItem(new GUIContent("Add End"), false, () => AddNode(InteractionNodeType.End, mousePosition));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Add End (Only 1 allowed)"));
            }

            menu.ShowAsContext();
        }

        private void AddNode(InteractionNodeType type, Vector2 position)
        {
            Undo.RecordObject(targetSequence, "Add Node");
            InteractionNode newNode = new InteractionNode(type, position);
            targetSequence.Nodes.Add(newNode);
            SaveAsset();
        }

        private void DeleteNode(InteractionNode node)
        {
            Undo.RecordObject(targetSequence, "Delete Node");
            targetSequence.Edges.RemoveAll(e => e.FromNodeID == node.ID || e.ToNodeID == node.ID);
            targetSequence.Nodes.Remove(node);
            if (connectingFromNode == node) connectingFromNode = null;
            SaveAsset();
        }

        private bool IsValidConnection(InteractionNode from, InteractionNode to)
        {
            if (from == null || to == null) return false;
            if (from == to) return false;
            if (from.NodeType == InteractionNodeType.End) return false;
            if (to.NodeType == InteractionNodeType.Start) return false;
            if (targetSequence.Edges.Exists(e => e.FromNodeID == from.ID && e.ToNodeID == to.ID)) return false;
            return true;
        }

        private void TryConnectNodes(InteractionNode from, InteractionNode to)
        {
            if (!IsValidConnection(from, to)) return;

            Undo.RecordObject(targetSequence, "Add Edge");
            targetSequence.Edges.Add(new InteractionEdge(from.ID, to.ID));
            SaveAsset();
        }

        private void ResetToDefaultFlow()
        {
            Undo.RecordObject(targetSequence, "Reset Flow");
            targetSequence.Nodes.Clear();
            targetSequence.Edges.Clear();

            InteractionNode startNode = new InteractionNode(InteractionNodeType.Start, new Vector2(250, 60));
            InteractionNode switchNode = new InteractionNode(InteractionNodeType.SwitchEvent, new Vector2(250, 160));
            InteractionNode endNode = new InteractionNode(InteractionNodeType.End, new Vector2(250, 290));

            targetSequence.Nodes.Add(startNode);
            targetSequence.Nodes.Add(switchNode);
            targetSequence.Nodes.Add(endNode);

            targetSequence.Edges.Add(new InteractionEdge(startNode.ID, switchNode.ID));
            targetSequence.Edges.Add(new InteractionEdge(switchNode.ID, endNode.ID));

            SaveAsset();
        }

        private void CreateNewSequenceAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Interaction Sequence", "NewInteractionSequence", "asset", "Save Interaction Sequence Asset");
            if (!string.IsNullOrEmpty(path))
            {
                InteractionSequenceSO newSO = CreateInstance<InteractionSequenceSO>();
                AssetDatabase.CreateAsset(newSO, path);
                targetSequence = newSO;
                ResetToDefaultFlow();
            }
        }

        private void CreateOrLoadDefaultAsset()
        {
            if (!Directory.Exists("Assets/Interaction/Sequences"))
            {
                Directory.CreateDirectory("Assets/Interaction/Sequences");
                AssetDatabase.Refresh();
            }

            if (File.Exists(DefaultAssetPath))
            {
                targetSequence = AssetDatabase.LoadAssetAtPath<InteractionSequenceSO>(DefaultAssetPath);
            }
            else
            {
                InteractionSequenceSO newSO = CreateInstance<InteractionSequenceSO>();
                AssetDatabase.CreateAsset(newSO, DefaultAssetPath);
                targetSequence = newSO;
                ResetToDefaultFlow();
            }
        }

        private Rect GetNodeRect(InteractionNode node)
        {
            float height = (node.NodeType == InteractionNodeType.SwitchEvent) ? NodeHeightSwitch : NodeHeightStartEnd;
            return new Rect(node.EditorX, node.EditorY, NodeWidth, height);
        }

        private Vector2 GetInputPortPosition(InteractionNode node)
        {
            Rect r = GetNodeRect(node);
            return new Vector2(r.center.x, r.yMin);
        }

        private Vector2 GetOutputPortPosition(InteractionNode node)
        {
            Rect r = GetNodeRect(node);
            return new Vector2(r.center.x, r.yMax);
        }

        private InteractionNode GetInputPortNodeAtPosition(Vector2 mousePos)
        {
            if (targetSequence == null || targetSequence.Nodes == null) return null;

            for (int i = 0; i < targetSequence.Nodes.Count; i++)
            {
                InteractionNode node = targetSequence.Nodes[i];
                if (node == null) continue;

                // Only SwitchEvent and End nodes have input ports
                if (node.NodeType == InteractionNodeType.SwitchEvent || node.NodeType == InteractionNodeType.End)
                {
                    Vector2 inPortPos = GetInputPortPosition(node);
                    if (Vector2.Distance(mousePos, inPortPos) <= PortHitboxRadius)
                    {
                        return node;
                    }
                }
            }
            return null;
        }

        private InteractionNode GetOutputPortNodeAtPosition(Vector2 mousePos)
        {
            if (targetSequence == null || targetSequence.Nodes == null) return null;

            for (int i = 0; i < targetSequence.Nodes.Count; i++)
            {
                InteractionNode node = targetSequence.Nodes[i];
                if (node == null) continue;

                // Only Start and SwitchEvent nodes have output ports
                if (node.NodeType == InteractionNodeType.Start || node.NodeType == InteractionNodeType.SwitchEvent)
                {
                    Vector2 outPortPos = GetOutputPortPosition(node);
                    if (Vector2.Distance(mousePos, outPortPos) <= PortHitboxRadius)
                    {
                        return node;
                    }
                }
            }
            return null;
        }

        private InteractionNode GetNodeAtPosition(Vector2 position)
        {
            if (targetSequence.Nodes == null) return null;
            for (int i = targetSequence.Nodes.Count - 1; i >= 0; i--)
            {
                if (GetNodeRect(targetSequence.Nodes[i]).Contains(position))
                {
                    return targetSequence.Nodes[i];
                }
            }
            return null;
        }

        private Color GetNodeHeaderColor(InteractionNodeType type)
        {
            switch (type)
            {
                case InteractionNodeType.Start:
                    return new Color(0.2f, 0.65f, 0.35f);
                case InteractionNodeType.SwitchEvent:
                    return new Color(0.2f, 0.5f, 0.85f);
                case InteractionNodeType.End:
                    return new Color(0.85f, 0.25f, 0.25f);
                default:
                    return Color.gray;
            }
        }

        private string GetNodeTitle(InteractionNodeType type)
        {
            switch (type)
            {
                case InteractionNodeType.Start:
                    return "START";
                case InteractionNodeType.SwitchEvent:
                    return "SWITCH EVENT";
                case InteractionNodeType.End:
                    return "END";
                default:
                    return "NODE";
            }
        }

        private void SaveAsset()
        {
            if (targetSequence != null)
            {
                EditorUtility.SetDirty(targetSequence);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
