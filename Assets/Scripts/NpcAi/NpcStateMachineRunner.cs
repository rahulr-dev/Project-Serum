using System;
using UnityEngine;

namespace NpcAi
{
    public class NpcStateMachineRunner
    {
        const int MaxEnterDepth = 64;

        readonly NpcStateMachine _host;

        NpcStateMachineGraph _graph;
        NpcStateMachineNodeData _current;
        int _enterDepth;
        bool _waitingOnState;

        public bool IsPlaying { get; private set; }
        public NpcStateMachineNodeData Current => _current;
        public NpcStateMachineOutcome LastOutcome { get; private set; } = NpcStateMachineOutcome.None;
        public bool IsWaitingOnState => _waitingOnState;

        public event Action<NpcStateMachineOutcome> Completed;

        public NpcStateMachineRunner(NpcStateMachine host)
        {
            _host = host;
        }

        public void Start(NpcStateMachineGraph graph)
        {
            Stop(NpcStateMachineOutcome.Interrupted, false);
            _graph = graph;
            _enterDepth = 0;
            LastOutcome = NpcStateMachineOutcome.None;
            IsPlaying = graph != null;
            if (graph == null)
            {
                Finish(NpcStateMachineOutcome.Failed);
                return;
            }

            NpcStateMachineNodeData start = graph.FindStart();
            if (start == null)
            {
                Debug.LogWarning("[NpcStateMachineRunner] No Start node found.");
                Finish(NpcStateMachineOutcome.Failed);
                return;
            }

            Enter(graph.FindNext(start.id, 0));
        }

        public void Stop(NpcStateMachineOutcome outcome = NpcStateMachineOutcome.Interrupted, bool notify = true)
        {
            bool wasPlaying = IsPlaying;
            _waitingOnState = false;
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

        public void HandleActorCompleted(NpcStateOutcome outcome)
        {
            if (!IsPlaying || !_waitingOnState || _current == null)
                return;

            NpcStateMachineNodeData node = _current;
            _waitingOnState = false;

            switch (outcome)
            {
                case NpcStateOutcome.Completed:
                    Enter(_graph != null ? _graph.FindNext(node.id, 0) : null);
                    return;
                case NpcStateOutcome.Failed:
                    EnterOrFinish(node.id, 1, NpcStateMachineOutcome.Failed);
                    return;
                default:
                    if (_host != null && _host.ConsumeSpotted())
                    {
                        EnterOrFinish(node.id, 2, NpcStateMachineOutcome.Interrupted);
                        return;
                    }

                    Finish(NpcStateMachineOutcome.Interrupted);
                    return;
            }
        }

        void EnterOrFinish(string fromId, int port, NpcStateMachineOutcome fallback)
        {
            string nextId = _graph != null ? _graph.FindNext(fromId, port) : null;
            if (string.IsNullOrEmpty(nextId))
            {
                Finish(fallback);
                return;
            }

            Enter(nextId);
        }

        void Enter(string nodeId)
        {
            if (!IsPlaying)
                return;

            if (_enterDepth++ > MaxEnterDepth)
            {
                Debug.LogWarning("[NpcStateMachineRunner] Node chain is too deep; stopping.");
                Finish(NpcStateMachineOutcome.Failed);
                return;
            }

            if (string.IsNullOrEmpty(nodeId) || _graph == null)
            {
                Finish(NpcStateMachineOutcome.Completed);
                return;
            }

            NpcStateMachineNodeData node = _graph.FindNode(nodeId);
            if (node == null)
            {
                Finish(NpcStateMachineOutcome.Failed);
                return;
            }

            _current = node;
            _waitingOnState = false;

            switch (node.kind)
            {
                case NpcStateMachineNodeKind.Start:
                    Enter(_graph.FindNext(node.id, 0));
                    break;
                case NpcStateMachineNodeKind.Action:
                    _host?.RaiseAction(node.actionId);
                    Enter(_graph.FindNext(node.id, 0));
                    break;
                case NpcStateMachineNodeKind.State:
                    if (node.stateGraph == null)
                    {
                        Debug.LogWarning("[NpcStateMachineRunner] State node has no NpcStateGraph.");
                        Finish(NpcStateMachineOutcome.Failed);
                        return;
                    }

                    _waitingOnState = true;
                    _enterDepth = 0;
                    _host?.PlayState(node.stateGraph);
                    break;
                case NpcStateMachineNodeKind.End:
                    Finish(NpcStateMachineOutcome.Completed);
                    break;
                default:
                    Enter(_graph.FindNext(node.id, 0));
                    break;
            }
        }

        void Finish(NpcStateMachineOutcome outcome)
        {
            LastOutcome = outcome;
            _waitingOnState = false;
            _current = null;
            IsPlaying = false;
            _graph = null;
            _enterDepth = 0;
            Completed?.Invoke(outcome);
        }
    }
}
