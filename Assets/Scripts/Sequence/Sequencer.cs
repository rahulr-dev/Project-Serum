using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SequenceSystem
{
    public class Sequencer : MonoBehaviour
    {
        [Serializable]
        class NamedUnityEvent
        {
            public string id;
            public UnityEvent onRaised = new UnityEvent();
        }

        [Serializable]
        class NamedCondition
        {
            public string key;
            public bool value;
        }

        [SerializeField] SequenceGraph graph;
        [SerializeField] bool playOnEnable = true;

        [Header("Condition")]
        public bool canAdvance;
        public bool cancelSequence;
        [Tooltip("Optional flags keyed like Condition nodes (e.g. Bell1). If the current Condition key matches and value is true, the graph continues. Prefer TryAdvance from bells unless you need sticky flags.")]
        [SerializeField] List<NamedCondition> namedConditions = new List<NamedCondition>();

        [Header("Actions")]
        [SerializeField] List<NamedUnityEvent> actionEvents = new List<NamedUnityEvent>();

        [Header("Lifecycle")]
        [SerializeField] UnityEvent onStarted = new UnityEvent();
        [SerializeField] UnityEvent onContinued = new UnityEvent();
        [SerializeField] UnityEvent onCancelled = new UnityEvent();
        [SerializeField] UnityEvent onCompleted = new UnityEvent();

        SequenceRunner _runner;
        bool _appliedPlayState;

        public const string OverlayPrefsKey = "Serum.SequenceOverlay.Enabled";

        public bool IsPlaying => _runner != null && _runner.IsPlaying;
        public SequenceGraph Graph => graph;
        public SequenceNodeData CurrentNode => _runner != null ? _runner.Current : null;
        public bool IsWaitingOnCondition => _runner != null && _runner.IsWaitingOnCondition;
        public SequenceOutcome LastOutcome => _runner != null ? _runner.LastOutcome : SequenceOutcome.None;

#if UNITY_EDITOR
        static readonly Color OverlayIdleBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        static readonly Color OverlayActiveBg = new Color(0.15f, 0.85f, 0.28f, 1f);
        static readonly Color OverlayIdleText = new Color(0.75f, 0.75f, 0.75f, 1f);
        Rect _overlayRect;
        bool _overlayRectInit;
        GUIStyle _overlayKeyStyle;
        string _overlayAdvanceKey = "Bell1";
#endif

        public bool ShouldAdvance
        {
            get
            {
                if (canAdvance)
                    return true;

                SequenceNodeData node = CurrentNode;
                if (node == null || node.kind != SequenceNodeKind.Condition)
                    return false;

                return GetNamedCondition(node.conditionKey);
            }
        }

        public bool ShouldCancel => cancelSequence;

        void Awake()
        {
            _runner = new SequenceRunner(this);
            _runner.OnFinished += HandleFinished;
        }

        void OnEnable()
        {
            if (playOnEnable)
                EnsurePlaying();
        }

        void OnDestroy()
        {
            if (_runner != null)
                _runner.OnFinished -= HandleFinished;
        }

        void Update()
        {
            _runner?.Tick();
        }

        void EnsurePlaying()
        {
            if (IsPlaying)
                return;

            StartSequence();
        }

        public void StartSequence()
        {
            if (graph == null)
            {
                Debug.LogWarning($"[Sequencer] No SequenceGraph assigned on {gameObject.name}.");
                return;
            }

            if (_runner == null)
            {
                _runner = new SequenceRunner(this);
                _runner.OnFinished += HandleFinished;
            }

            if (_runner.IsPlaying)
                _runner.Stop(SequenceOutcome.Stopped, false);

            ApplyPlayState();
            onStarted?.Invoke();
            _runner.Start(graph);
        }

        public void StopSequence()
        {
            if (_runner == null || !_runner.IsPlaying)
                return;

            _runner.Stop(SequenceOutcome.Stopped, true);
        }

        public void Continue()
        {
            EnsurePlaying();
            canAdvance = true;
            _runner?.Continue();
        }

        public void Cancel()
        {
            EnsurePlaying();
            cancelSequence = true;
            _runner?.Cancel();
        }

        public void SetCanAdvance(bool value)
        {
            canAdvance = value;
        }

        public void TryAdvance(string key)
        {
            EnsurePlaying();
            _runner?.TryAdvance(key);
        }

        public void SetNamedCondition(string key, bool value)
        {
            if (string.IsNullOrEmpty(key) || namedConditions == null)
                return;

            for (int i = 0; i < namedConditions.Count; i++)
            {
                NamedCondition entry = namedConditions[i];
                if (entry == null || entry.key != key)
                    continue;

                entry.value = value;
                return;
            }

            namedConditions.Add(new NamedCondition { key = key, value = value });
        }

        public void ClearConditions()
        {
            canAdvance = false;
            cancelSequence = false;
            if (namedConditions == null)
                return;

            for (int i = 0; i < namedConditions.Count; i++)
            {
                if (namedConditions[i] != null)
                    namedConditions[i].value = false;
            }
        }

        public void ConsumeConditionFlags()
        {
            canAdvance = false;
            cancelSequence = false;
        }

        public void PrepareConditionWait()
        {
            canAdvance = false;
            cancelSequence = false;
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

            Debug.LogWarning($"[Sequencer] No action event '{actionId}' on {gameObject.name}.");
        }

        public void NotifyContinued()
        {
            onContinued?.Invoke();
        }

        public void NotifyCancelled()
        {
            onCancelled?.Invoke();
        }

        bool GetNamedCondition(string key)
        {
            if (string.IsNullOrEmpty(key) || namedConditions == null)
                return false;

            for (int i = 0; i < namedConditions.Count; i++)
            {
                NamedCondition entry = namedConditions[i];
                if (entry != null && entry.key == key && entry.value)
                    return true;
            }

            return false;
        }

        void ApplyPlayState()
        {
            _appliedPlayState = false;
            if (graph == null || GameStateManager.Instance == null)
                return;

            if (IsGameplayLike(graph.playState))
                return;

            GameStateManager.Instance.SetState(graph.playState);
            _appliedPlayState = true;
        }

        void HandleFinished(SequenceOutcome outcome)
        {
            RestoreEndState();
            if (outcome == SequenceOutcome.Completed)
                onCompleted?.Invoke();
        }

        void RestoreEndState()
        {
            if (!_appliedPlayState || graph == null || GameStateManager.Instance == null)
            {
                _appliedPlayState = false;
                return;
            }

            if (GameStateManager.Instance.CurrentState == graph.playState)
                GameStateManager.Instance.SetState(graph.endState);

            _appliedPlayState = false;
        }

        static bool IsGameplayLike(GameState state)
        {
            return state == GameState.Gameplay ||
                   state == GameState.GameplayNoJump ||
                   state == GameState.GameplayStealth ||
                   state == GameState.GameplayDialogue ||
                   state == GameState.GameplayPushing;
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (!EditorPrefs.GetBool(OverlayPrefsKey, false))
                return;

            if (!_overlayRectInit)
            {
                float y = 12f + (Mathf.Abs(GetInstanceID()) % 8) * 28f;
                _overlayRect = new Rect(680f, y, 260f, 360f);
                _overlayRectInit = true;
            }

            _overlayRect = GUI.Window(GetInstanceID(), _overlayRect, DrawOverlay, $"Sequence  {gameObject.name}");
        }

        void DrawOverlay(int windowId)
        {
            DrawRow(IsPlaying ? "Playing" : "Idle", IsPlaying, 240f);

            string graphName = graph != null ? graph.name : "—";
            DrawRow($"Graph  {graphName}", graph != null, 240f);

            SequenceNodeData node = CurrentNode;
            if (IsPlaying && node != null)
            {
                DrawRow($"Node  {node.kind}", true, 240f);
                if (node.kind == SequenceNodeKind.Condition)
                    DrawRow($"Wait key  {node.conditionKey}", IsWaitingOnCondition, 240f);
                else if (node.kind == SequenceNodeKind.Action)
                    DrawRow($"Action  {node.actionId}", true, 240f);
            }
            else
            {
                DrawRow("Node  —", false, 240f);
            }

            DrawRow($"canAdvance  {canAdvance}", canAdvance, 240f);
            DrawRow($"cancelSequence  {cancelSequence}", cancelSequence, 240f);
            DrawRow($"Outcome  {LastOutcome}", LastOutcome == SequenceOutcome.Completed, 240f);

            GUILayout.Space(6);
            GUILayout.Label("TryAdvance");
            _overlayAdvanceKey = GUILayout.TextField(_overlayAdvanceKey ?? "");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Advance") && !string.IsNullOrEmpty(_overlayAdvanceKey))
                TryAdvance(_overlayAdvanceKey);
            if (GUILayout.Button("Continue"))
                Continue();
            if (GUILayout.Button("Cancel"))
                Cancel();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restart"))
                StartSequence();
            if (GUILayout.Button("Stop"))
                StopSequence();
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
