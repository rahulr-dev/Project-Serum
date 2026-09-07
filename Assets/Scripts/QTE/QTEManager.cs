using System;
using Dialogue;
using Game;
using Interaction;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QTE
{
    [DefaultExecutionOrder(100)]
    public class QTEManager : MonoBehaviour
    {
        public static QTEManager Instance { get; private set; }

        public static event Action<QTEOutcome> OnQTECompleted;

        public event Action<QTEGraph> OnQTEStarted;
        public event Action<QTENodeData> OnNodeEntered;
        public event Action<string, float> OnPromptUpdated;
        public event Action<float> OnProgressUpdated;
        public event Action<int, int> OnSequenceStepUpdated;
        public event Action<QTEInputKind?, QTEInputKind?> OnRequiredInputUpdated;

        public const string OverlayPrefsKey = "Serum.QTEOverlay.Enabled";

        public bool IsRunning => _runner != null && _runner.IsRunning;
        public QTEGraph CurrentGraph { get; private set; }
        public QTEOutcome LastOutcome { get; private set; } = QTEOutcome.Cancelled;
        public QTENodeKind CurrentNodeKind => _runner.CurrentNodeKind;
        public QTEInputKind? CurrentRequiredInput => _runner.CurrentRequiredInput;
        public QTEInputKind? CurrentSequenceExpectedInput => _runner.CurrentSequenceExpectedInput;

        readonly QTERunner _runner = new QTERunner();
        bool _ending;

#if UNITY_EDITOR
        static readonly Color OverlayIdleBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        static readonly Color OverlayActiveBg = new Color(0.15f, 0.85f, 0.28f, 1f);
        static readonly Color OverlayIdleText = new Color(0.75f, 0.75f, 0.75f, 1f);
        Rect _overlayRect = new Rect(710f, 12f, 260f, 380f);
        GUIStyle _overlayKeyStyle;
        QTEGraph _overlayGraph;
        const int OverlayPickerId = 0x53515445;
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
            _runner.NodeEntered += HandleNodeEntered;
            _runner.PromptUpdated += HandlePromptUpdated;
            _runner.ProgressUpdated += HandleProgressUpdated;
            _runner.SequenceStepUpdated += HandleSequenceStepUpdated;
            _runner.RequiredInputUpdated += HandleRequiredInputUpdated;
            _runner.Completed += HandleRunnerCompleted;
            InteractionManager.OnQTEInputRegistered += HandleQTEInputRegistered;
        }

        void OnDestroy()
        {
            _runner.NodeEntered -= HandleNodeEntered;
            _runner.PromptUpdated -= HandlePromptUpdated;
            _runner.ProgressUpdated -= HandleProgressUpdated;
            _runner.SequenceStepUpdated -= HandleSequenceStepUpdated;
            _runner.RequiredInputUpdated -= HandleRequiredInputUpdated;
            _runner.Completed -= HandleRunnerCompleted;
            InteractionManager.OnQTEInputRegistered -= HandleQTEInputRegistered;
            if (Instance == this)
                Instance = null;
        }

        void LateUpdate()
        {
            if (IsRunning)
                _runner.Tick(Time.deltaTime);
        }

        public bool StartQTE(QTEGraph graph)
        {
            if (graph == null)
                return false;

            if (IsRunning)
                StopQTE();

            if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying)
                return false;

            CurrentGraph = graph;
            LastOutcome = QTEOutcome.Cancelled;
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.SetState(graph.playState);

            OnQTEStarted?.Invoke(graph);
            _runner.Start(graph);
            return _runner.IsRunning;
        }

        public void StopQTE()
        {
            if (!IsRunning && CurrentGraph == null)
                return;

            _runner.Stop(true);
        }

        public void CancelQTE()
        {
            if (!IsRunning)
                return;

            _runner.Cancel();
        }

        void HandleNodeEntered(QTENodeData node)
        {
            OnNodeEntered?.Invoke(node);
        }

        void HandlePromptUpdated(string prompt, float normalizedTime)
        {
            OnPromptUpdated?.Invoke(prompt, normalizedTime);
        }

        void HandleProgressUpdated(float progress)
        {
            OnProgressUpdated?.Invoke(progress);
        }

        void HandleSequenceStepUpdated(int current, int total)
        {
            OnSequenceStepUpdated?.Invoke(current, total);
        }

        void HandleRequiredInputUpdated(QTEInputKind? required, QTEInputKind? sequenceExpected)
        {
            OnRequiredInputUpdated?.Invoke(required, sequenceExpected);
        }

        void HandleQTEInputRegistered(QTEInputKind kind, string source)
        {
            string node = IsRunning ? _runner.CurrentNodeKind.ToString() : "idle";
            Debug.Log($"[QTEManager] Input registered: {kind} ({source}) | running={IsRunning} node={node}");
        }

        void HandleRunnerCompleted(QTEOutcome outcome)
        {
            if (_ending)
                return;

            _ending = true;
            QTEGraph graph = CurrentGraph;
            LastOutcome = outcome;
            CurrentGraph = null;

            if (GameStateManager.Instance != null && graph != null)
                GameStateManager.Instance.SetState(graph.endState);

            OnQTECompleted?.Invoke(outcome);
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
                QTEGraph picked = EditorGUIUtility.GetObjectPickerObject() as QTEGraph;
                if (picked != null)
                    _overlayGraph = picked;
            }
            else if (current.type == EventType.ExecuteCommand &&
                     (current.commandName == "ObjectSelectorUpdated" || current.commandName == "ObjectSelectorClosed"))
            {
                QTEGraph picked = EditorGUIUtility.GetObjectPickerObject() as QTEGraph;
                if (picked != null)
                    _overlayGraph = picked;
                current.Use();
            }

            _overlayRect = GUI.Window(GetInstanceID(), _overlayRect, DrawOverlay, "QTE Overlay");
        }

        void DrawOverlay(int windowId)
        {
            DrawRow(IsRunning ? "Running" : "Idle", IsRunning, 240f);
            string graphName = CurrentGraph != null ? CurrentGraph.name : "—";
            DrawRow($"Graph  {graphName}", CurrentGraph != null, 240f);

            GameState play = CurrentGraph != null ? CurrentGraph.playState : (_overlayGraph != null ? _overlayGraph.playState : GameState.QTE);
            GameState end = CurrentGraph != null ? CurrentGraph.endState : (_overlayGraph != null ? _overlayGraph.endState : GameState.Gameplay);
            DrawRow($"Play state  {play}", false, 240f);
            DrawRow($"End state  {end}", false, 240f);

            string live = GameStateManager.Instance != null
                ? GameStateManager.Instance.CurrentState.ToString()
                : "no GSM";
            DrawRow($"Game state  {live}", true, 240f);

            if (IsRunning)
            {
                DrawRow($"Node  {_runner.CurrentNodeKind}", true, 240f);
                DrawRow($"Prompt  {_runner.LastPrompt}", false, 240f);
                DrawRow($"Timer  {_runner.RemainingTime:0.0}s", false, 240f);
                DrawRow($"Progress  {_runner.Progress * 100f:0}%", false, 240f);
            }

            DrawRow($"Outcome  {LastOutcome}", !IsRunning && LastOutcome != QTEOutcome.Cancelled, 240f);

            GUILayout.Space(6);
            GUILayout.Label(_overlayGraph != null ? _overlayGraph.name : "None");
            if (GUILayout.Button("Select Graph"))
                EditorGUIUtility.ShowObjectPicker<QTEGraph>(_overlayGraph, false, "", OverlayPickerId);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start") && _overlayGraph != null)
                StartQTE(_overlayGraph);
            if (GUILayout.Button("Stop"))
                StopQTE();
            if (GUILayout.Button("Cancel"))
                CancelQTE();
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
