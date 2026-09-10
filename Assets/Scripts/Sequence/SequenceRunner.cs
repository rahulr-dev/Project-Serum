using System;
using UnityEngine;

namespace SequenceSystem
{
    public class SequenceRunner
    {
        readonly Sequencer _host;

        const int MaxEnterDepth = 64;

        SequenceGraph _graph;
        SequenceNodeData _current;
        bool _waitingCondition;
        int _enterDepth;

        public bool IsPlaying { get; private set; }
        public SequenceNodeData Current => _current;
        public bool IsWaitingOnCondition => _waitingCondition;
        public SequenceOutcome LastOutcome { get; private set; }

        public event Action<SequenceOutcome> OnFinished;

        public SequenceRunner(Sequencer host)
        {
            _host = host;
        }

        public void Start(SequenceGraph graph)
        {
            Stop(SequenceOutcome.Stopped, false);
            _graph = graph;
            _enterDepth = 0;
            LastOutcome = SequenceOutcome.None;
            IsPlaying = graph != null;
            if (graph == null)
            {
                Finish(SequenceOutcome.Stopped);
                return;
            }

            SequenceNodeData start = graph.FindStart();
            if (start == null)
            {
                Debug.LogWarning("[SequenceRunner] No Start node found.");
                Finish(SequenceOutcome.Stopped);
                return;
            }

            Enter(graph.FindNext(start.id, 0));
        }

        public void Stop(SequenceOutcome outcome = SequenceOutcome.Stopped, bool notify = true)
        {
            _waitingCondition = false;
            _current = null;
            IsPlaying = false;
            _graph = null;
            if (notify)
                OnFinished?.Invoke(outcome);
        }

        public void Tick()
        {
            if (!IsPlaying || !_waitingCondition || _host == null)
                return;

            if (_host.ShouldCancel)
            {
                ResolveCondition(false);
                return;
            }

            if (_host.ShouldAdvance)
                ResolveCondition(true);
        }

        public void Continue()
        {
            if (!IsPlaying || !_waitingCondition)
                return;

            ResolveCondition(true);
        }

        public void Cancel()
        {
            if (!IsPlaying || !_waitingCondition)
                return;

            ResolveCondition(false);
        }

        public void TryAdvance(string key)
        {
            if (!IsPlaying || !_waitingCondition || _current == null)
                return;

            string expected = string.IsNullOrEmpty(_current.conditionKey) ? "default" : _current.conditionKey;
            if (string.Equals(expected, key, StringComparison.Ordinal))
            {
                ResolveCondition(true);
                return;
            }

            if (_current.IgnoresKey(key))
                return;

            ResolveCondition(false);
        }

        void ResolveCondition(bool continuePath)
        {
            if (_current == null || _graph == null)
            {
                Finish(SequenceOutcome.Stopped);
                return;
            }

            SequenceNodeData node = _current;
            _waitingCondition = false;
            if (_host != null)
                _host.ConsumeConditionFlags();

            if (continuePath)
            {
                _host?.NotifyContinued();
                Enter(_graph.FindNext(node.id, 0));
                return;
            }

            _host?.NotifyCancelled();
            string cancelId = _graph.FindNext(node.id, 1);
            if (string.IsNullOrEmpty(cancelId))
            {
                Finish(SequenceOutcome.Cancelled);
                return;
            }

            Enter(cancelId);
        }

        void Enter(string nodeId)
        {
            if (!IsPlaying)
                return;

            if (_enterDepth++ > MaxEnterDepth)
            {
                Debug.LogWarning("[SequenceRunner] Node chain is too deep; stopping.");
                Finish(SequenceOutcome.Stopped);
                return;
            }

            if (string.IsNullOrEmpty(nodeId) || _graph == null)
            {
                Finish(SequenceOutcome.Completed);
                return;
            }

            SequenceNodeData node = _graph.FindNode(nodeId);
            if (node == null)
            {
                Finish(SequenceOutcome.Stopped);
                return;
            }

            _current = node;
            switch (node.kind)
            {
                case SequenceNodeKind.Start:
                    Enter(_graph.FindNext(node.id, 0));
                    break;

                case SequenceNodeKind.Action:
                    _host?.RaiseAction(node.actionId);
                    Enter(_graph.FindNext(node.id, 0));
                    break;

                case SequenceNodeKind.Condition:
                    _waitingCondition = true;
                    _enterDepth = 0;
                    _host?.PrepareConditionWait();
                    break;

                case SequenceNodeKind.ClearSequence:
                    _host?.ClearConditions();
                    Enter(_graph.FindNext(node.id, 0));
                    break;

                case SequenceNodeKind.End:
                    Finish(SequenceOutcome.Completed);
                    break;

                default:
                    Enter(_graph.FindNext(node.id, 0));
                    break;
            }
        }

        void Finish(SequenceOutcome outcome)
        {
            LastOutcome = outcome;
            _waitingCondition = false;
            _current = null;
            IsPlaying = false;
            _graph = null;
            OnFinished?.Invoke(outcome);
        }
    }
}
