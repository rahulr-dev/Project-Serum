using System;
using Events;
using UnityEngine;

namespace NpcAi
{
    public class NpcStateRunner
    {
        const int MaxEnterDepth = 64;

        readonly INpcStateContext _context;

        NpcStateGraph _graph;
        NpcStateNodeData _current;
        int _enterDepth;
        float _timer;
        bool _waitingOnEvent;
        string _waitEventId;
        bool _waitHasTimeout;
        SerumActionBridge _subscribedBridge;

        public bool IsPlaying { get; private set; }
        public NpcStateNodeData Current => _current;
        public NpcStateNodeKind CurrentNodeKind => _current != null ? _current.kind : NpcStateNodeKind.Start;
        public NpcStateOutcome LastOutcome { get; private set; } = NpcStateOutcome.None;
        public bool IsWaitingOnEvent => _waitingOnEvent;
        public string WaitEventId => _waitEventId;
        public float RemainingTime { get; private set; }

        public event Action<NpcStateOutcome> Completed;
        public event Action<NpcStateNodeData> NodeEntered;

        public NpcStateRunner(INpcStateContext context)
        {
            _context = context;
        }

        public void Start(NpcStateGraph graph)
        {
            Stop(NpcStateOutcome.Interrupted, false);
            _graph = graph;
            _enterDepth = 0;
            LastOutcome = NpcStateOutcome.None;
            IsPlaying = graph != null;
            if (graph == null)
            {
                Finish(NpcStateOutcome.Failed);
                return;
            }

            NpcStateNodeData start = graph.FindStart();
            if (start == null)
            {
                Debug.LogWarning("[NpcStateRunner] No Start node found.");
                Finish(NpcStateOutcome.Failed);
                return;
            }

            SubscribeBridge();
            Enter(start.id);
        }

        public void Stop(NpcStateOutcome outcome = NpcStateOutcome.Interrupted, bool notify = true)
        {
            bool wasPlaying = IsPlaying;
            ClearWait();
            UnsubscribeBridge();
            _current = null;
            IsPlaying = false;
            _graph = null;
            _enterDepth = 0;
            if (notify && wasPlaying)
            {
                LastOutcome = outcome;
                Completed?.Invoke(outcome);
            }
        }

        public void Tick(float deltaTime)
        {
            if (!IsPlaying || _current == null)
                return;

            if (_current.kind == NpcStateNodeKind.Wait)
            {
                TickWait(deltaTime);
                return;
            }

            if (!_waitingOnEvent || !_waitHasTimeout)
                return;

            _timer -= deltaTime;
            RemainingTime = Mathf.Max(0f, _timer);
            if (_timer > 0f)
                return;

            ResolveWait(false);
        }

        void TickWait(float deltaTime)
        {
            _timer -= deltaTime;
            RemainingTime = Mathf.Max(0f, _timer);
            if (_timer <= 0f)
                AdvanceFromNode(0);
        }

        void SubscribeBridge()
        {
            UnsubscribeBridge();
            SerumActionBridge bridge = _context != null ? _context.Bridge : null;
            if (bridge == null)
                return;

            _subscribedBridge = bridge;
            _subscribedBridge.NamedEventRaised += HandleNamedEvent;
        }

        void UnsubscribeBridge()
        {
            if (_subscribedBridge == null)
                return;

            _subscribedBridge.NamedEventRaised -= HandleNamedEvent;
            _subscribedBridge = null;
        }

        void HandleNamedEvent(string eventId)
        {
            if (!IsPlaying || !_waitingOnEvent)
                return;

            if (string.IsNullOrEmpty(eventId) || !string.Equals(eventId, _waitEventId, StringComparison.Ordinal))
                return;

            ResolveWait(true);
        }

        void ResolveWait(bool succeeded)
        {
            if (_current == null)
                return;

            NpcStateNodeData node = _current;
            ClearWait();

            if (succeeded)
            {
                AdvanceFromNode(0);
                return;
            }

            if (node.kind == NpcStateNodeKind.WaitEvent)
            {
                string timeoutId = _graph != null ? _graph.FindNext(node.id, 1) : null;
                if (!string.IsNullOrEmpty(timeoutId))
                {
                    Enter(timeoutId);
                    return;
                }
            }

            Finish(NpcStateOutcome.Failed);
        }

        void ClearWait()
        {
            _waitingOnEvent = false;
            _waitEventId = null;
            _waitHasTimeout = false;
            _timer = 0f;
            RemainingTime = 0f;
        }

        void AdvanceFromNode(int port)
        {
            if (_current == null || _graph == null)
            {
                Finish(NpcStateOutcome.Failed);
                return;
            }

            Enter(_graph.FindNext(_current.id, port));
        }

        void Enter(string nodeId)
        {
            if (!IsPlaying)
                return;

            if (_enterDepth++ > MaxEnterDepth)
            {
                Debug.LogWarning("[NpcStateRunner] Node chain is too deep; stopping.");
                Finish(NpcStateOutcome.Failed);
                return;
            }

            if (string.IsNullOrEmpty(nodeId) || _graph == null)
            {
                Finish(NpcStateOutcome.Completed);
                return;
            }

            NpcStateNodeData node = _graph.FindNode(nodeId);
            if (node == null)
            {
                Finish(NpcStateOutcome.Failed);
                return;
            }

            _current = node;
            ClearWait();
            NodeEntered?.Invoke(node);

            switch (node.kind)
            {
                case NpcStateNodeKind.Start:
                    Enter(_graph.FindNext(node.id, 0));
                    break;
                case NpcStateNodeKind.Wait:
                    _timer = Mathf.Max(0f, node.duration);
                    RemainingTime = _timer;
                    if (_timer <= 0f)
                    {
                        Enter(_graph.FindNext(node.id, 0));
                        break;
                    }

                    _enterDepth = 0;
                    break;
                case NpcStateNodeKind.CharacterAction:
                    EnterCharacterAction(node);
                    break;
                case NpcStateNodeKind.SceneAction:
                    _context?.ExecuteSceneAction(node.executeHandlerId);
                    Enter(_graph.FindNext(node.id, 0));
                    break;
                case NpcStateNodeKind.WaitEvent:
                    BeginWaitEvent(node.waitEventId, node.duration);
                    break;
                case NpcStateNodeKind.RandomBranch:
                    Enter(_graph.FindNext(node.id, UnityEngine.Random.Range(0, node.GetBranchCount())));
                    break;
                case NpcStateNodeKind.End:
                    Finish(node.endOutcome == NpcStateOutcome.Failed
                        ? NpcStateOutcome.Failed
                        : NpcStateOutcome.Completed);
                    break;
                default:
                    Enter(_graph.FindNext(node.id, 0));
                    break;
            }
        }

        void EnterCharacterAction(NpcStateNodeData node)
        {
            SerumActionBridge bridge = _context != null ? _context.Bridge : null;
            NpcStateCharacterActions.Execute(bridge, node);

            if (node.waitUntilDone &&
                NpcStateCharacterActions.TryGetCompletionEvent(node.characterAction, out string eventId) &&
                NpcStateCharacterActions.IsWaitingAfterExecute(bridge, node.characterAction))
            {
                BeginWaitEvent(eventId, 0f);
                return;
            }

            Enter(_graph.FindNext(node.id, 0));
        }

        void BeginWaitEvent(string eventId, float timeout)
        {
            _waitEventId = string.IsNullOrEmpty(eventId)
                ? NpcStateCharacterActions.ScriptedRunEnded
                : eventId;
            _waitingOnEvent = true;
            _enterDepth = 0;
            _waitHasTimeout = timeout > 0f;
            _timer = _waitHasTimeout ? timeout : 0f;
            RemainingTime = _timer;
        }

        void Finish(NpcStateOutcome outcome)
        {
            LastOutcome = outcome;
            ClearWait();
            UnsubscribeBridge();
            _current = null;
            IsPlaying = false;
            _graph = null;
            _enterDepth = 0;
            Completed?.Invoke(outcome);
        }
    }
}
