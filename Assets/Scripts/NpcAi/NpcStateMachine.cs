using System;
using System.Collections.Generic;
using Events;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NpcAi
{
    public class NpcStateMachine : MonoBehaviour
    {
        [Serializable]
        public class NamedUnityEvent
        {
            public string id;
            public UnityEvent onRaised = new UnityEvent();
        }

        [SerializeField] NpcStateMachineGraph graph;
        [SerializeField] bool playOnEnable = true;
        [SerializeField] NpcStateActor actor;

        [Header("Actions")]
        [SerializeField] List<NamedUnityEvent> actionEvents = new List<NamedUnityEvent>
        {
            new NamedUnityEvent { id = "Spotted" }
        };

        [Header("Lifecycle")]
        [SerializeField] UnityEvent onStarted = new UnityEvent();
        [SerializeField] UnityEvent onCompleted = new UnityEvent();
        [SerializeField] UnityEvent onFailed = new UnityEvent();
        [SerializeField] UnityEvent onInterrupted = new UnityEvent();

        NpcStateMachineRunner _runner;
        bool _started;
        bool _spotted;

        public NpcStateMachineGraph Graph => graph;
        public NpcStateActor Actor => actor;
        public bool IsPlaying => _runner != null && _runner.IsPlaying;
        public NpcStateMachineNodeData CurrentNode => _runner != null ? _runner.Current : null;
        public NpcStateMachineOutcome LastOutcome => _runner != null ? _runner.LastOutcome : NpcStateMachineOutcome.None;
        public bool ConsumeSpotted()
        {
            bool value = _spotted;
            _spotted = false;
            return value;
        }

        public event Action<NpcStateMachineOutcome> Completed;

        void Awake()
        {
            if (actor == null)
                actor = GetComponent<NpcStateActor>();

            _runner = new NpcStateMachineRunner(this);
            _runner.Completed += HandleFinished;
            if (actor != null)
                actor.Completed += HandleActorCompleted;
        }

        void Start()
        {
            _started = true;
            if (playOnEnable)
                Play();
        }

        void OnEnable()
        {
            if (_started && playOnEnable && !IsPlaying)
                Play();
        }

        void OnDisable()
        {
            if (_runner != null && _runner.IsPlaying)
                StopInternal(NpcStateMachineOutcome.Interrupted, true);
        }

        void OnDestroy()
        {
            if (actor != null)
                actor.Completed -= HandleActorCompleted;

            if (_runner == null)
                return;

            _runner.Completed -= HandleFinished;
            _runner.Stop(NpcStateMachineOutcome.Interrupted, false);
        }

        public void Play()
        {
            Play(graph);
        }

        public void Play(NpcStateMachineGraph machineGraph)
        {
            if (machineGraph == null)
            {
                Debug.LogWarning($"[NpcStateMachine] No NpcStateMachineGraph assigned on {gameObject.name}.");
                return;
            }

            if (actor == null)
                actor = GetComponent<NpcStateActor>();

            if (_runner == null)
            {
                _runner = new NpcStateMachineRunner(this);
                _runner.Completed += HandleFinished;
            }

            graph = machineGraph;
            _spotted = false;
            if (_runner.IsPlaying)
                _runner.Stop(NpcStateMachineOutcome.Interrupted, false);
            if (actor != null && actor.IsPlaying)
                actor.Stop();

            DisableLocomotion();
            onStarted?.Invoke();
            _runner.Start(machineGraph);
        }

        public void Stop()
        {
            StopInternal(NpcStateMachineOutcome.Interrupted, true);
        }

        public void NotifySpotted()
        {
            NpcStateMachineNodeData node = CurrentNode;
            if (node == null)
                return;

            TryInterrupt(node.id);
        }

        public void InterruptCurrent()
        {
            NpcStateMachineNodeData node = CurrentNode;
            if (node == null)
                return;

            TryInterrupt(node.id);
        }

        public bool TryInterrupt(string nodeIdOrName)
        {
            if (!IsPlaying || _spotted || string.IsNullOrEmpty(nodeIdOrName))
                return false;

            NpcStateMachineNodeData node = CurrentNode;
            if (node == null || node.kind != NpcStateMachineNodeKind.State)
                return false;

            if (!node.MatchesKey(nodeIdOrName))
                return false;

            if (graph != null && string.IsNullOrEmpty(graph.FindNext(node.id, 2)))
                return false;

            _spotted = true;
            if (actor != null && actor.IsPlaying)
                actor.Stop();

            return true;
        }

        public void PlayState(NpcStateGraph stateGraph)
        {
            if (actor == null)
            {
                Debug.LogWarning($"[NpcStateMachine] No NpcStateActor on {gameObject.name}.");
                return;
            }

            DisableLocomotion();
            actor.Play(stateGraph);
        }

        public void RaiseAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId) || actionEvents == null)
                return;

            for (int i = 0; i < actionEvents.Count; i++)
            {
                NamedUnityEvent entry = actionEvents[i];
                if (entry == null || entry.id != actionId)
                    continue;

                entry.onRaised?.Invoke();
                return;
            }

            Debug.LogWarning($"[NpcStateMachine] No action event '{actionId}' on {gameObject.name}.");
        }

        public void SetGraph(NpcStateMachineGraph machineGraph)
        {
            graph = machineGraph;
        }

        public UnityEvent EnsureAction(string actionId)
        {
            if (actionEvents == null)
                actionEvents = new List<NamedUnityEvent>();

            for (int i = 0; i < actionEvents.Count; i++)
            {
                if (actionEvents[i] != null && actionEvents[i].id == actionId)
                    return actionEvents[i].onRaised;
            }

            NamedUnityEvent created = new NamedUnityEvent { id = actionId };
            actionEvents.Add(created);
            return created.onRaised;
        }

        void DisableLocomotion()
        {
            SerumActionBridge bridge = actor != null ? actor.Bridge : GetComponent<SerumActionBridge>();
            bridge?.DisableLocomotion();
        }

        void StopInternal(NpcStateMachineOutcome outcome, bool notify)
        {
            if (_runner == null || !_runner.IsPlaying)
                return;

            _spotted = false;
            _runner.Stop(outcome, false);
            if (actor != null && actor.IsPlaying)
                actor.Stop();

            if (notify)
                HandleFinished(outcome);
        }

        void HandleActorCompleted(NpcStateOutcome outcome)
        {
            _runner?.HandleActorCompleted(outcome);
        }

        void HandleFinished(NpcStateMachineOutcome outcome)
        {
            Completed?.Invoke(outcome);
            switch (outcome)
            {
                case NpcStateMachineOutcome.Completed:
                    onCompleted?.Invoke();
                    break;
                case NpcStateMachineOutcome.Failed:
                    onFailed?.Invoke();
                    break;
                case NpcStateMachineOutcome.Interrupted:
                    onInterrupted?.Invoke();
                    break;
            }
        }

#if UNITY_EDITOR
        static readonly Color OverlayIdleBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        static readonly Color OverlayActiveBg = new Color(0.15f, 0.85f, 0.28f, 1f);
        static readonly Color OverlayIdleText = new Color(0.75f, 0.75f, 0.75f, 1f);
        Rect _overlayRect;
        bool _overlayRectInit;
        GUIStyle _overlayKeyStyle;

        void OnGUI()
        {
            if (!EditorPrefs.GetBool(NpcStateActor.OverlayPrefsKey, false))
                return;

            if (!_overlayRectInit)
            {
                float y = 12f + (Mathf.Abs(GetInstanceID()) % 8) * 28f;
                _overlayRect = new Rect(280f, y, 280f, 260f);
                _overlayRectInit = true;
            }

            _overlayRect = GUI.Window(GetInstanceID(), _overlayRect, DrawOverlay, $"NPC Brain  {gameObject.name}");
        }

        void DrawOverlay(int windowId)
        {
            DrawRow(IsPlaying ? "Playing" : "Idle", IsPlaying, 260f);

            string graphName = graph != null ? graph.name : "—";
            DrawRow($"Graph  {graphName}", graph != null, 260f);

            NpcStateMachineNodeData node = CurrentNode;
            if (IsPlaying && node != null)
            {
                DrawRow($"Node  {node.kind}", true, 260f);
                if (node.kind == NpcStateMachineNodeKind.State)
                    DrawRow($"State  {node.DisplayName}", true, 260f);
                else if (node.kind == NpcStateMachineNodeKind.Action)
                {
                    DrawRow($"Action  {node.actionId}", true, 260f);
                }
            }
            else
            {
                DrawRow("Node  —", false, 260f);
            }

            DrawRow($"Spotted  {_spotted}", _spotted, 260f);
            DrawRow($"Outcome  {LastOutcome}", LastOutcome == NpcStateMachineOutcome.Completed, 260f);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))
                Play();
            if (GUILayout.Button("Stop"))
                Stop();
            if (GUILayout.Button("Spot"))
                NotifySpotted();
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
