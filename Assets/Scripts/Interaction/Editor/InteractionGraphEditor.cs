using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace InteractionSystem.Editor
{
    public class InteractionGraphEditor : EditorWindow
    {
        private InteractionSequenceSO targetSequence;
        private InteractionNode draggingNode = null;
        private Vector2 dragOffset;
        private InteractionNode connectingFromNode = null;

        private const float NodeWidth = 270f;
        private const float PortVisualRadius = 7f;
        private const float PortHitboxRadius = 28f; // Generous 56px hitbox for effortless connection
        private const string DefaultAssetPath = "Assets/Interaction/Sequences/InteractionSequenceSO.asset";

        private readonly Color inputPortColor = new Color(0.95f, 0.3f, 0.3f, 1f);
        private readonly Color outputPortColor = new Color(0.3f, 0.85f, 0.4f, 1f);
        private readonly Color activeWireColor = new Color(1f, 0.85f, 0.2f, 1f);
        private readonly Color connectionWireColor = new Color(0.35f, 0.75f, 1f, 1f);
        private readonly Color validTargetHighlightColor = new Color(0.3f, 1f, 0.5f, 0.95f);

        private string hoverTooltipText = "";

        // Object Picker Tracking
        private InteractionAction activePickerAction = null;
        private int activePickerControlID = -1;

        [MenuItem("Serum/Interaction/Interaction Graph", false, 10)]
        public static void OpenWindow()
        {
            InteractionGraphEditor window = GetWindow<InteractionGraphEditor>("Interaction Graph");
            window.minSize = new Vector2(800, 560);
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
            hoverTooltipText = "";

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

            // Handle Drag & Drop of assets onto Invoke Event nodes
            HandleDragAndDrop(e);

            // Handle Object Picker Command
            HandleObjectPickerCommand(e);

            // Handle connection and node header drag events
            HandleEvents(e);

            // Render graph elements
            DrawConnections();
            DrawNodes(e);
            DrawConnectingWire(e);

            // Render bottom tooltip banner
            DrawTooltipBanner();

            if (connectingFromNode != null || draggingNode != null || e.type == EventType.MouseMove)
            {
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Sequence Asset:", GUILayout.Width(100));
            targetSequence = (InteractionSequenceSO)EditorGUILayout.ObjectField(targetSequence, typeof(InteractionSequenceSO), false, GUILayout.Width(260));

            if (GUILayout.Button("Create New Asset", EditorStyles.toolbarButton))
            {
                CreateNewSequenceAsset();
            }

            if (targetSequence != null)
            {
                if (GUILayout.Button("Reset Flow (Start → Invoke Event → Wait → End)", EditorStyles.toolbarButton))
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

            for (int i = targetSequence.Nodes.Count - 1; i >= 0; i--)
            {
                InteractionNode node = targetSequence.Nodes[i];
                if (node == null) continue;

                Rect nodeRect = GetNodeRect(node);
                Color headerColor = GetNodeHeaderColor(node.NodeType);
                Color bodyColor = new Color(0.18f, 0.18f, 0.20f, 0.98f);

                // Check node hover for tooltip
                if (nodeRect.Contains(e.mousePosition))
                {
                    hoverTooltipText = GetNodeTooltip(node.NodeType);
                }

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

                // Delete X button in upper right corner of node header
                Rect deleteBtnRect = new Rect(nodeRect.xMax - 26f, nodeRect.y + 2f, 22f, 20f);
                if (GUI.Button(deleteBtnRect, "×", EditorStyles.miniButton))
                {
                    DeleteNode(node);
                    GUIUtility.ExitGUI();
                    return;
                }

                // Outline
                Handles.DrawSolidRectangleWithOutline(nodeRect, Color.clear, new Color(0.1f, 0.1f, 0.1f, 0.85f));

                // ─── Node Body Content for WAIT ──────────────────────────────
                if (node.NodeType == InteractionNodeType.Wait)
                {
                    Rect contentRect = new Rect(nodeRect.x + 10f, nodeRect.y + 28f, nodeRect.width - 20f, nodeRect.height - 32f);
                    GUILayout.BeginArea(contentRect);
                    EditorGUILayout.LabelField("Pause Sequence", EditorStyles.miniBoldLabel);
                    EditorGUI.BeginChangeCheck();
                    float duration = EditorGUILayout.FloatField("Duration (sec):", node.WaitDuration);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetSequence, "Change Wait Duration");
                        node.WaitDuration = Mathf.Max(0f, duration);
                        SaveAsset();
                    }
                    GUILayout.EndArea();
                }

                // ─── Node Body Content for INVOKE EVENT ─────────────────────
                if (node.NodeType == InteractionNodeType.InvokeEvent)
                {
                    Rect contentRect = new Rect(nodeRect.x + 8f, nodeRect.y + 28f, nodeRect.width - 16f, nodeRect.height - 32f);
                    GUILayout.BeginArea(contentRect);

                    EditorGUILayout.Space(2);

                    // Optional Switch/InvokeEvent Component Field
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    InvokeEvent newEvt = (InvokeEvent)EditorGUILayout.ObjectField(new GUIContent("Target Component", "Optional InvokeEvent component on GameObject"), node.InvokeEvent, typeof(InvokeEvent), true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetSequence, "Change Node InvokeEvent Component");
                        node.InvokeEvent = newEvt;
                        SaveAsset();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Configured Actions:", EditorStyles.miniBoldLabel);

                    if (node.Actions == null || node.Actions.Count == 0)
                    {
                        // Drop helper box
                        Rect dropBoxRect = EditorGUILayout.GetControlRect(false, 46f);
                        EditorGUI.DrawRect(dropBoxRect, new Color(0.12f, 0.12f, 0.14f, 0.9f));
                        Handles.DrawSolidRectangleWithOutline(dropBoxRect, Color.clear, new Color(0.4f, 0.4f, 0.45f, 0.5f));

                        GUIStyle dropStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            wordWrap = true,
                            normal = { textColor = new Color(0.75f, 0.75f, 0.8f) }
                        };
                        GUI.Label(dropBoxRect, "Drag an asset or component here to add an action", dropStyle);

                        if (dropBoxRect.Contains(e.mousePosition))
                        {
                            hoverTooltipText = "Drop a supported component or asset here.";
                        }
                    }
                    else
                    {
                        // Action List
                        for (int aIdx = 0; aIdx < node.Actions.Count; aIdx++)
                        {
                            InteractionAction act = node.Actions[aIdx];
                            if (act == null) continue;

                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            EditorGUILayout.BeginHorizontal();

                            // Target Object Field
                            EditorGUI.BeginChangeCheck();
                            UnityEngine.Object newTarget = EditorGUILayout.ObjectField(act.TargetObject, typeof(UnityEngine.Object), true, GUILayout.Width(110));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(targetSequence, "Edit Action Target");
                                act.TargetObject = newTarget;
                                SaveAsset();
                            }

                            // Object Picker Button ⊙
                            if (GUILayout.Button(new GUIContent("⊙", "Click to open Object Picker"), EditorStyles.miniButton, GUILayout.Width(22), GUILayout.Height(18)))
                            {
                                activePickerAction = act;
                                int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
                                activePickerControlID = controlID;
                                EditorGUIUtility.ShowObjectPicker<UnityEngine.Object>(act.TargetObject, true, "", controlID);
                                GUIUtility.ExitGUI();
                                return;
                            }

                            // Action Type Dropdown
                            EditorGUI.BeginChangeCheck();
                            InteractionActionType newType = (InteractionActionType)EditorGUILayout.EnumPopup(act.ActionType, GUILayout.Width(85));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(targetSequence, "Edit Action Type");
                                act.ActionType = newType;
                                SaveAsset();
                            }

                            // Action Delete Button ×
                            if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(18)))
                            {
                                Undo.RecordObject(targetSequence, "Delete Action");
                                node.Actions.RemoveAt(aIdx);
                                SaveAsset();
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.EndVertical();
                                break;
                            }

                            EditorGUILayout.EndHorizontal();

                            // Show string parameter field if action requires it
                            if (act.ActionType == InteractionActionType.AnimatorSetTrigger || act.ActionType == InteractionActionType.AnimatorPlayState || act.ActionType == InteractionActionType.AnimationPlay)
                            {
                                EditorGUI.BeginChangeCheck();
                                string newParam = EditorGUILayout.TextField("State/Trigger:", act.StringParam);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(targetSequence, "Change Action Param");
                                    act.StringParam = newParam;
                                    SaveAsset();
                                }
                            }

                            EditorGUILayout.EndVertical();
                        }
                    }

                    EditorGUILayout.Space(4);

                    // + Add Action Button
                    Rect addBtnRect = EditorGUILayout.GetControlRect(false, 24f);
                    if (GUI.Button(addBtnRect, "+ Add Action", EditorStyles.miniButton))
                    {
                        Undo.RecordObject(targetSequence, "Add Action");
                        if (node.Actions == null) node.Actions = new List<InteractionAction>();
                        node.Actions.Add(new InteractionAction(null, InteractionActionType.CustomUnityEvent));
                        SaveAsset();
                        Repaint();
                    }

                    if (addBtnRect.Contains(e.mousePosition))
                    {
                        hoverTooltipText = "Add another action to execute from this Invoke Event.";
                    }

                    GUILayout.EndArea();
                }

                // ─── Input Port (Top Center) ──────────────────────────────
                if (node.NodeType == InteractionNodeType.InvokeEvent || node.NodeType == InteractionNodeType.Wait || node.NodeType == InteractionNodeType.End)
                {
                    Vector2 inPortPos = GetInputPortPosition(node);
                    bool isHovered = Vector2.Distance(e.mousePosition, inPortPos) <= PortHitboxRadius;
                    bool isValidTarget = connectingFromNode != null && connectingFromNode != node && IsValidConnection(connectingFromNode, node);

                    if (isHovered)
                    {
                        hoverTooltipText = "Release a connection here to connect the previous step.";
                    }

                    DrawPortHandle(inPortPos, inputPortColor, isHovered, isValidTarget);
                }

                // ─── Output Port (Bottom Center) ──────────────────────────
                if (node.NodeType == InteractionNodeType.Start || node.NodeType == InteractionNodeType.InvokeEvent || node.NodeType == InteractionNodeType.Wait)
                {
                    Vector2 outPortPos = GetOutputPortPosition(node);
                    bool isHovered = Vector2.Distance(e.mousePosition, outPortPos) <= PortHitboxRadius;

                    if (isHovered)
                    {
                        hoverTooltipText = "Drag from this port to connect the next step.";
                    }

                    DrawPortHandle(outPortPos, outputPortColor, isHovered, false);
                }
            }
        }

        private void HandleObjectPickerCommand(Event e)
        {
            if (e.type == EventType.ExecuteCommand && e.commandName == "ObjectSelectorUpdated")
            {
                if (activePickerAction != null)
                {
                    UnityEngine.Object picked = EditorGUIUtility.GetObjectPickerObject();
                    Undo.RecordObject(targetSequence, "Pick Action Target");
                    activePickerAction.TargetObject = picked;
                    SaveAsset();
                    Repaint();
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

        private void DrawTooltipBanner()
        {
            if (string.IsNullOrEmpty(hoverTooltipText)) return;

            Rect tooltipRect = new Rect(10f, position.height - 30f, position.width - 20f, 24f);
            EditorGUI.DrawRect(tooltipRect, new Color(0.1f, 0.1f, 0.12f, 0.95f));
            Handles.DrawSolidRectangleWithOutline(tooltipRect, Color.clear, new Color(0.3f, 0.6f, 0.9f, 0.7f));

            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) },
                padding = new RectOffset(8, 0, 0, 0)
            };
            GUI.Label(tooltipRect, hoverTooltipText, style);
        }

        private void HandleDragAndDrop(Event e)
        {
            if (targetSequence == null || targetSequence.Nodes == null) return;

            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
                InteractionNode targetNode = GetNodeAtPosition(e.mousePosition);
                if (targetNode != null && targetNode.NodeType == InteractionNodeType.InvokeEvent)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        Undo.RecordObject(targetSequence, "Drag & Drop Actions");

                        foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                        {
                            if (obj == null) continue;

                            InteractionAction action = CreateActionFromObject(obj);
                            if (action != null)
                            {
                                if (targetNode.Actions == null) targetNode.Actions = new List<InteractionAction>();
                                targetNode.Actions.Add(action);
                            }
                        }

                        SaveAsset();
                        e.Use();
                    }
                }
            }
        }

        private InteractionAction CreateActionFromObject(UnityEngine.Object obj)
        {
            if (obj is AnimationClip clip)
            {
                return new InteractionAction(clip, InteractionActionType.AnimationPlay) { StringParam = clip.name };
            }
            if (obj is Animator anim)
            {
                return new InteractionAction(anim, InteractionActionType.AnimatorSetTrigger) { StringParam = "Play" };
            }
            if (obj is ParticleSystem ps)
            {
                return new InteractionAction(ps, InteractionActionType.ParticlePlay);
            }
            if (obj is AudioSource audio)
            {
                return new InteractionAction(audio, InteractionActionType.AudioPlay);
            }
            if (obj is PlayableDirector director)
            {
                return new InteractionAction(director, InteractionActionType.TimelinePlay);
            }
            if (obj is Door door)
            {
                return new InteractionAction(door, InteractionActionType.DoorOpen);
            }
            if (obj is GameObject go)
            {
                Door d = go.GetComponent<Door>();
                if (d != null) return new InteractionAction(d, InteractionActionType.DoorOpen);

                Animator a = go.GetComponent<Animator>();
                if (a != null) return new InteractionAction(a, InteractionActionType.AnimatorSetTrigger) { StringParam = "Play" };

                ParticleSystem p = go.GetComponent<ParticleSystem>();
                if (p != null) return new InteractionAction(p, InteractionActionType.ParticlePlay);

                AudioSource aud = go.GetComponent<AudioSource>();
                if (aud != null) return new InteractionAction(aud, InteractionActionType.AudioPlay);

                PlayableDirector dir = go.GetComponent<PlayableDirector>();
                if (dir != null) return new InteractionAction(dir, InteractionActionType.TimelinePlay);

                return new InteractionAction(go, InteractionActionType.CustomUnityEvent);
            }

            string typeName = obj.GetType().Name;
            if (typeName.Contains("QTE"))
            {
                return new InteractionAction(obj, InteractionActionType.QTEStart);
            }
            if (typeName.Contains("Dialogue"))
            {
                return new InteractionAction(obj, InteractionActionType.DialogueStart);
            }

            return new InteractionAction(obj, InteractionActionType.CustomUnityEvent);
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

                        // 2. Check if clicking specifically on a Node Header Drag Area (header bar excluding X button)
                        InteractionNode clickedNode = GetNodeAtHeaderPosition(e.mousePosition);
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

        private InteractionNode GetNodeAtHeaderPosition(Vector2 position)
        {
            if (targetSequence == null || targetSequence.Nodes == null) return null;
            for (int i = targetSequence.Nodes.Count - 1; i >= 0; i--)
            {
                InteractionNode node = targetSequence.Nodes[i];
                if (node == null) continue;

                // Header drag area is top 24px of node, excluding the X delete button on the right
                Rect headerDragRect = new Rect(node.EditorX, node.EditorY, NodeWidth - 30f, 24f);
                if (headerDragRect.Contains(position))
                {
                    return node;
                }
            }
            return null;
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

            menu.AddItem(new GUIContent("Add Invoke Event"), false, () => AddNode(InteractionNodeType.InvokeEvent, mousePosition));
            menu.AddItem(new GUIContent("Add Wait"), false, () => AddNode(InteractionNodeType.Wait, mousePosition));

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
            InteractionNode invokeNode = new InteractionNode(InteractionNodeType.InvokeEvent, new Vector2(250, 160));
            InteractionNode waitNode = new InteractionNode(InteractionNodeType.Wait, new Vector2(250, 360));
            InteractionNode endNode = new InteractionNode(InteractionNodeType.End, new Vector2(250, 480));

            targetSequence.Nodes.Add(startNode);
            targetSequence.Nodes.Add(invokeNode);
            targetSequence.Nodes.Add(waitNode);
            targetSequence.Nodes.Add(endNode);

            targetSequence.Edges.Add(new InteractionEdge(startNode.ID, invokeNode.ID));
            targetSequence.Edges.Add(new InteractionEdge(invokeNode.ID, waitNode.ID));
            targetSequence.Edges.Add(new InteractionEdge(waitNode.ID, endNode.ID));

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
            float height;
            switch (node.NodeType)
            {
                case InteractionNodeType.Start:
                case InteractionNodeType.End:
                    height = 52f;
                    break;
                case InteractionNodeType.Wait:
                    height = 80f;
                    break;
                case InteractionNodeType.InvokeEvent:
                    int actionCount = node.Actions != null ? node.Actions.Count : 0;
                    if (actionCount == 0)
                    {
                        height = 145f;
                    }
                    else
                    {
                        height = 95f + (actionCount * 46f);
                    }
                    break;
                default:
                    height = 80f;
                    break;
            }
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

                if (node.NodeType == InteractionNodeType.InvokeEvent || node.NodeType == InteractionNodeType.Wait || node.NodeType == InteractionNodeType.End)
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

                if (node.NodeType == InteractionNodeType.Start || node.NodeType == InteractionNodeType.InvokeEvent || node.NodeType == InteractionNodeType.Wait)
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
            if (targetSequence == null || targetSequence.Nodes == null) return null;
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
                case InteractionNodeType.InvokeEvent:
                    return new Color(0.2f, 0.5f, 0.85f);
                case InteractionNodeType.Wait:
                    return new Color(0.95f, 0.65f, 0.15f);
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
                case InteractionNodeType.InvokeEvent:
                    return "INVOKE EVENT";
                case InteractionNodeType.Wait:
                    return "WAIT";
                case InteractionNodeType.End:
                    return "END";
                default:
                    return "NODE";
            }
        }

        private string GetNodeTooltip(InteractionNodeType type)
        {
            switch (type)
            {
                case InteractionNodeType.Start:
                    return "START: Begins this interaction sequence.";
                case InteractionNodeType.InvokeEvent:
                    return "INVOKE EVENT: Triggers one or more configured actions at this point in the interaction sequence.";
                case InteractionNodeType.Wait:
                    return "WAIT: Pauses the interaction sequence for the specified duration before continuing.";
                case InteractionNodeType.End:
                    return "END: Ends execution of this interaction sequence branch.";
                default:
                    return "";
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
