using System;
using Events;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NpcAi
{
    public class NpcStateActor : MonoBehaviour, INpcStateContext
    {
        [SerializeField] NpcStateGraph graph;
        [SerializeField] bool playOnEnable;
        [SerializeField] SerumActionBridge actionBridge;

        [Header("Lifecycle")]
        [SerializeField] UnityEvent onStarted = new UnityEvent();
        [SerializeField] UnityEvent onCompleted = new UnityEvent();
        [SerializeField] UnityEvent onFailed = new UnityEvent();
        [SerializeField] UnityEvent onInterrupted = new UnityEvent();

        NpcStateRunner _runner;

        public const string OverlayPrefsKey = "Serum.NpcStateOverlay.Enabled";

        public SerumActionBridge Bridge => actionBridge;
        public bool IsPlaying => _runner != null && _runner.IsPlaying;
        public NpcStateGraph Graph => graph;
        public NpcStateNodeData CurrentNode => _runner != null ? _runner.Current : null;
        public NpcStateOutcome LastOutcome => _runner != null ? _runner.LastOutcome : NpcStateOutcome.None;

        public event Action<NpcStateOutcome> Completed;

#if UNITY_EDITOR
        static readonly Color OverlayIdleBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        static readonly Color OverlayActiveBg = new Color(0.15f, 0.85f, 0.28f, 1f);
        static readonly Color OverlayIdleText = new Color(0.75f, 0.75f, 0.75f, 1f);
        Rect _overlayRect;
        bool _overlayRectInit;
        GUIStyle _overlayKeyStyle;
#endif

        void Awake()
        {
            if (actionBridge == null)
                actionBridge = GetComponent<SerumActionBridge>();

            _runner = new NpcStateRunner(this);
            _runner.Completed += HandleFinished;
        }

        void OnEnable()
        {
            if (playOnEnable)
                Play();
        }

        void OnDisable()
        {
            if (_runner != null && _runner.IsPlaying)
            {
                actionBridge?.StopSmoothMotion();
                _runner.Stop(NpcStateOutcome.Interrupted, true);
            }
        }

        void OnDestroy()
        {
            if (_runner == null)
                return;

            _runner.Completed -= HandleFinished;
            _runner.Stop(NpcStateOutcome.Interrupted, false);
        }

        void Update()
        {
            _runner?.Tick(Time.deltaTime);
        }

        public void Play()
        {
            Play(graph);
        }

        public void Play(NpcStateGraph stateGraph)
        {
            if (stateGraph == null)
            {
                Debug.LogWarning($"[NpcStateActor] No NpcStateGraph assigned on {gameObject.name}.");
                return;
            }

            if (actionBridge == null)
                actionBridge = GetComponent<SerumActionBridge>();

            if (_runner == null)
            {
                _runner = new NpcStateRunner(this);
                _runner.Completed += HandleFinished;
            }

            graph = stateGraph;
            actionBridge?.StopSmoothMotion();
            onStarted?.Invoke();
            _runner.Start(stateGraph);
        }

        public void Stop()
        {
            if (_runner == null || !_runner.IsPlaying)
                return;

            actionBridge?.StopSmoothMotion();
            _runner.Stop(NpcStateOutcome.Interrupted, true);
        }

        public void ExecuteSceneAction(string handlerId)
        {
            if (string.IsNullOrEmpty(handlerId))
                return;

            NpcSceneActionHandler handler = NpcSceneActionHandler.FindById(handlerId);
            if (handler == null)
            {
                Debug.LogWarning($"[NpcStateActor] SceneAction could not find handler '{handlerId}' on {gameObject.name}.");
                return;
            }

            handler.Execute();
        }

        void HandleFinished(NpcStateOutcome outcome)
        {
            Completed?.Invoke(outcome);
            switch (outcome)
            {
                case NpcStateOutcome.Completed:
                    onCompleted?.Invoke();
                    break;
                case NpcStateOutcome.Failed:
                    onFailed?.Invoke();
                    break;
                case NpcStateOutcome.Interrupted:
                    onInterrupted?.Invoke();
                    break;
            }
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (!EditorPrefs.GetBool(OverlayPrefsKey, false))
                return;

            if (!_overlayRectInit)
            {
                float y = 12f + (Mathf.Abs(GetInstanceID()) % 8) * 28f;
                _overlayRect = new Rect(12f, y, 260f, 280f);
                _overlayRectInit = true;
            }

            _overlayRect = GUI.Window(GetInstanceID(), _overlayRect, DrawOverlay, $"NPC State  {gameObject.name}");
        }

        void DrawOverlay(int windowId)
        {
            DrawRow(IsPlaying ? "Playing" : "Idle", IsPlaying, 240f);

            string graphName = graph != null ? graph.name : "—";
            DrawRow($"Graph  {graphName}", graph != null, 240f);

            NpcStateNodeData node = CurrentNode;
            if (IsPlaying && node != null)
            {
                DrawRow($"Node  {node.kind}", true, 240f);
                if (node.kind == NpcStateNodeKind.CharacterAction)
                    DrawRow($"Action  {node.characterAction}", true, 240f);
                else if (node.kind == NpcStateNodeKind.WaitEvent || (_runner != null && _runner.IsWaitingOnEvent))
                    DrawRow($"Wait  {_runner.WaitEventId}", true, 240f);
                else if (node.kind == NpcStateNodeKind.Wait)
                    DrawRow($"Wait  {_runner.RemainingTime:0.00}s", true, 240f);
            }
            else
            {
                DrawRow("Node  —", false, 240f);
            }

            DrawRow($"Outcome  {LastOutcome}", LastOutcome == NpcStateOutcome.Completed, 240f);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))
                Play();
            if (GUILayout.Button("Stop"))
                Stop();
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
