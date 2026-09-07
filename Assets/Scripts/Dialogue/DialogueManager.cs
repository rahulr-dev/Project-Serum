using System;
using System.Collections.Generic;
using Game;
using Interaction;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        public event Action<DialogueGraph> OnDialogueStarted;
        public event Action<DialogueLineInfo> OnLineStarted;
        public event Action<string> OnLineTextUpdated;
        public event Action<IReadOnlyList<string>> OnChoicesPresented;
        public event Action<int> OnChoiceIndexChanged;
        public event Action OnDialogueEnded;

        public const string OverlayPrefsKey = "Serum.DialogueOverlay.Enabled";

        public bool IsPlaying => _runner != null && _runner.IsPlaying;
        public DialogueGraph CurrentGraph { get; private set; }

        readonly DialogueRunner _runner = new DialogueRunner();
        bool _ending;

#if UNITY_EDITOR
        static readonly Color OverlayIdleBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        static readonly Color OverlayActiveBg = new Color(0.15f, 0.85f, 0.28f, 1f);
        static readonly Color OverlayIdleText = new Color(0.75f, 0.75f, 0.75f, 1f);
        Rect _overlayRect = new Rect(460f, 12f, 240f, 300f);
        GUIStyle _overlayKeyStyle;
        DialogueGraph _overlayGraph;
        const int OverlayPickerId = 0x53444C47;
#endif

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _runner.OnLineStarted += HandleLineStarted;
            _runner.OnLineTextUpdated += HandleLineTextUpdated;
            _runner.OnChoicesPresented += HandleChoicesPresented;
            _runner.OnChoiceIndexChanged += HandleChoiceIndexChanged;
            _runner.OnFinished += HandleRunnerFinished;
        }

        void OnEnable()
        {
            InteractionManager.OnInteractStarted += HandleInteractStarted;
        }

        void OnDisable()
        {
            InteractionManager.OnInteractStarted -= HandleInteractStarted;
        }

        void OnDestroy()
        {
            InteractionManager.OnInteractStarted -= HandleInteractStarted;
            _runner.OnLineStarted -= HandleLineStarted;
            _runner.OnLineTextUpdated -= HandleLineTextUpdated;
            _runner.OnChoicesPresented -= HandleChoicesPresented;
            _runner.OnChoiceIndexChanged -= HandleChoiceIndexChanged;
            _runner.OnFinished -= HandleRunnerFinished;
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            _runner.Tick(Time.deltaTime);
        }

        public void StartDialogue(DialogueGraph graph)
        {
            if (graph == null)
                return;

            if (IsPlaying)
                StopDialogue();

            CurrentGraph = graph;
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.SetState(graph.playState);

            OnDialogueStarted?.Invoke(graph);
            _runner.Start(graph);
        }

        public void StopDialogue()
        {
            if (!IsPlaying && CurrentGraph == null)
                return;

            _runner.Stop(true);
        }

        public void SelectChoice(int index)
        {
            _runner.SelectChoice(index);
        }

        void HandleInteractStarted()
        {
            if (IsPlaying)
                _runner.HandleInteract();
        }

        void HandleLineStarted(DialogueLineInfo info)
        {
            OnLineStarted?.Invoke(info);
        }

        void HandleLineTextUpdated(string visible)
        {
            OnLineTextUpdated?.Invoke(visible);
        }

        void HandleChoicesPresented(IReadOnlyList<string> labels)
        {
            OnChoicesPresented?.Invoke(labels);
        }

        void HandleChoiceIndexChanged(int index)
        {
            OnChoiceIndexChanged?.Invoke(index);
        }

        void HandleRunnerFinished()
        {
            if (_ending)
                return;

            _ending = true;
            DialogueGraph graph = CurrentGraph;
            CurrentGraph = null;
            if (GameStateManager.Instance != null && graph != null)
                GameStateManager.Instance.SetState(graph.endState);

            OnDialogueEnded?.Invoke();
            _ending = false;
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (!EditorPrefs.GetBool(OverlayPrefsKey, false))
                return;

            Event current = Event.current;
            if (EditorGUIUtility.GetObjectPickerControlID() == OverlayPickerId)
            {
                DialogueGraph picked = EditorGUIUtility.GetObjectPickerObject() as DialogueGraph;
                if (picked != null)
                    _overlayGraph = picked;
            }
            else if (current.type == EventType.ExecuteCommand &&
                     (current.commandName == "ObjectSelectorUpdated" || current.commandName == "ObjectSelectorClosed"))
            {
                DialogueGraph picked = EditorGUIUtility.GetObjectPickerObject() as DialogueGraph;
                if (picked != null)
                    _overlayGraph = picked;
                current.Use();
            }

            _overlayRect = GUI.Window(GetInstanceID(), _overlayRect, DrawOverlay, "Dialogue Overlay");
        }

        void DrawOverlay(int windowId)
        {
            DrawRow(IsPlaying ? "Playing" : "Idle", IsPlaying, 220f);
            string graphName = CurrentGraph != null ? CurrentGraph.name : "—";
            DrawRow($"Graph  {graphName}", CurrentGraph != null, 220f);

            GameState play = CurrentGraph != null ? CurrentGraph.playState : (_overlayGraph != null ? _overlayGraph.playState : GameState.Dialogue);
            GameState end = CurrentGraph != null ? CurrentGraph.endState : (_overlayGraph != null ? _overlayGraph.endState : GameState.Gameplay);
            DrawRow($"Play state  {play}", false, 220f);
            DrawRow($"End state  {end}", false, 220f);

            string live = GameStateManager.Instance != null
                ? GameStateManager.Instance.CurrentState.ToString()
                : "no GSM";
            DrawRow($"Game state  {live}", true, 220f);

            GUILayout.Space(6);
            GUILayout.Label(_overlayGraph != null ? _overlayGraph.name : "None");
            if (GUILayout.Button("Select Graph"))
            {
                EditorGUIUtility.ShowObjectPicker<DialogueGraph>(_overlayGraph, false, "", OverlayPickerId);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start") && _overlayGraph != null)
                StartDialogue(_overlayGraph);
            if (GUILayout.Button("Stop"))
                StopDialogue();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        GUIStyle OverlayKeyStyle
        {
            get
            {
                if (_overlayKeyStyle == null)
                {
                    _overlayKeyStyle = new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        fontSize = 12
                    };
                    _overlayKeyStyle.normal.background = Texture2D.whiteTexture;
                    _overlayKeyStyle.hover.background = Texture2D.whiteTexture;
                    _overlayKeyStyle.active.background = Texture2D.whiteTexture;
                }

                return _overlayKeyStyle;
            }
        }

        void DrawRow(string label, bool active, float width)
        {
            Color previousBg = GUI.backgroundColor;
            GUI.backgroundColor = active ? OverlayActiveBg : OverlayIdleBg;
            OverlayKeyStyle.normal.textColor = active ? Color.black : OverlayIdleText;
            OverlayKeyStyle.hover.textColor = OverlayKeyStyle.normal.textColor;
            OverlayKeyStyle.active.textColor = OverlayKeyStyle.normal.textColor;
            GUILayout.Box(label, OverlayKeyStyle, GUILayout.Width(width), GUILayout.Height(22f));
            GUI.backgroundColor = previousBg;
        }
#endif
    }
}
