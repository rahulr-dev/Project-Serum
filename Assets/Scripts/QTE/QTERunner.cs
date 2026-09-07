using System;
using Interaction;
using UnityEngine;

namespace QTE
{
    public class QTERunner
    {
        public event Action<QTEOutcome> Completed;
        public event Action<QTENodeData> NodeEntered;
        public event Action<string, float> PromptUpdated;
        public event Action<float> ProgressUpdated;
        public event Action<int, int> SequenceStepUpdated;
        public event Action<QTEInputKind?, QTEInputKind?> RequiredInputUpdated;

        public bool IsRunning { get; private set; }
        public QTEOutcome CurrentOutcome { get; private set; } = QTEOutcome.Success;
        public QTEResult LastNodeResult { get; private set; } = QTEResult.None;
        public QTENodeKind CurrentNodeKind => _current != null ? _current.kind : QTENodeKind.Start;
        public QTEInputKind? CurrentRequiredInput { get; private set; }
        public QTEInputKind? CurrentSequenceExpectedInput { get; private set; }
        public string LastPrompt { get; private set; } = "";
        public float RemainingTime { get; private set; }
        public float Progress { get; private set; }
        public QTEOutcome LastCompletedOutcome { get; private set; } = QTEOutcome.Cancelled;

        QTEGraph _graph;
        QTENodeData _current;
        float _timer;
        float _windowDuration;
        int _mashCount;
        int _sequenceStep;
        float _holdElapsed;
        bool _inputResolved;

        bool _inSequence;
        QTENodeData _sequenceNode;
        int _sequenceIndex;

        public void Start(QTEGraph graph)
        {
            Stop(false);
            _graph = graph;
            IsRunning = graph != null;
            CurrentOutcome = QTEOutcome.Success;
            LastNodeResult = QTEResult.None;
            LastCompletedOutcome = QTEOutcome.Cancelled;

            if (!IsRunning)
            {
                Completed?.Invoke(QTEOutcome.Cancelled);
                return;
            }

            QTENodeData start = graph.FindStart();
            if (start == null)
            {
                Finish(QTEOutcome.Failure);
                return;
            }

            Enter(start.id);
        }

        public void Stop(bool notifyFinished)
        {
            bool wasRunning = IsRunning;
            IsRunning = false;
            _graph = null;
            _current = null;
            _inSequence = false;
            _sequenceNode = null;
            ResetNodeState();
            if (notifyFinished && wasRunning)
                Completed?.Invoke(QTEOutcome.Cancelled);
        }

        public void Cancel()
        {
            if (!IsRunning)
                return;

            Finish(QTEOutcome.Cancelled);
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning || _current == null)
                return;

            switch (_current.kind)
            {
                case QTENodeKind.Wait:
                case QTENodeKind.Delay:
                    TickWait(deltaTime);
                    break;
                case QTENodeKind.InputPrompt:
                    TickInputPrompt(deltaTime);
                    break;
                case QTENodeKind.Hold:
                    TickHold(deltaTime);
                    break;
                case QTENodeKind.Mash:
                    TickMash(deltaTime);
                    break;
                case QTENodeKind.SequenceInput:
                    TickSequenceInput(deltaTime);
                    break;
            }
        }

        void TickWait(float deltaTime)
        {
            _timer -= deltaTime;
            RemainingTime = Mathf.Max(0f, _timer);
            if (_timer <= 0f)
                AdvanceFromNode(0);
        }

        void TickInputPrompt(float deltaTime)
        {
            if (_inputResolved)
                return;

            InteractionManager input = InteractionManager.Instance;
            if (input != null && input.WasInputPressedThisFrame(_current.requiredInput))
            {
                ResolveInputSuccess();
                return;
            }

            _timer -= deltaTime;
            RemainingTime = Mathf.Max(0f, _timer);
            float normalized = _windowDuration > 0f ? RemainingTime / _windowDuration : 0f;
            PromptUpdated?.Invoke(LastPrompt, normalized);

            if (_timer <= 0f)
                ResolveInputFailure(_current.failOnTimeout ? QTEResult.TimedOut : QTEResult.Failure);
        }

        void TickHold(float deltaTime)
        {
            if (_inputResolved)
                return;

            InteractionManager input = InteractionManager.Instance;
            bool held = input != null && input.IsInputHeld(_current.requiredInput);

            if (held)
            {
                _holdElapsed += deltaTime;
                Progress = _current.holdDuration > 0f
                    ? Mathf.Clamp01(_holdElapsed / _current.holdDuration)
                    : 1f;
                ProgressUpdated?.Invoke(Progress);

                if (_holdElapsed >= _current.holdDuration)
                {
                    ResolveInputSuccess();
                    return;
                }
            }

            _timer -= deltaTime;
            RemainingTime = Mathf.Max(0f, _timer);
            float normalized = _windowDuration > 0f ? RemainingTime / _windowDuration : 0f;
            PromptUpdated?.Invoke(LastPrompt, normalized);

            if (_timer <= 0f)
                ResolveInputFailure(QTEResult.TimedOut);
        }

        void TickMash(float deltaTime)
        {
            if (_inputResolved)
                return;

            InteractionManager input = InteractionManager.Instance;
            if (input != null)
            {
                int presses = input.CountInputPressesThisFrame(_current.requiredInput);
                if (presses > 0)
                {
                    _mashCount += presses;
                    Progress = _current.targetCount > 0
                        ? Mathf.Clamp01((float)_mashCount / _current.targetCount)
                        : 1f;
                    ProgressUpdated?.Invoke(Progress);

                    if (_mashCount >= _current.targetCount)
                    {
                        ResolveInputSuccess();
                        return;
                    }
                }
            }

            _timer -= deltaTime;
            RemainingTime = Mathf.Max(0f, _timer);
            float normalized = _windowDuration > 0f ? RemainingTime / _windowDuration : 0f;
            PromptUpdated?.Invoke(LastPrompt, normalized);

            if (_timer <= 0f)
                ResolveInputFailure(QTEResult.TimedOut);
        }

        void TickSequenceInput(float deltaTime)
        {
            if (_inputResolved)
                return;

            InteractionManager input = InteractionManager.Instance;
            if (input != null && _current.inputSequence != null && _current.inputSequence.Count > 0)
            {
                QTEInputKind expected = _current.inputSequence[_sequenceStep];
                if (input.WasInputPressedThisFrame(expected))
                {
                    _sequenceStep++;
                    SequenceStepUpdated?.Invoke(_sequenceStep, _current.inputSequence.Count);
                    UpdateSequenceExpectedInput();
                    _timer = _current.windowPerStep > 0f ? _current.windowPerStep : _current.totalWindow;

                    if (_sequenceStep >= _current.inputSequence.Count)
                    {
                        ResolveInputSuccess();
                        return;
                    }

                    return;
                }

                if (input.WasAnyWrongQTEInputPressed(_current.inputSequence, _sequenceStep))
                {
                    ResolveInputFailure(QTEResult.Failure);
                    return;
                }
            }

            _timer -= deltaTime;
            RemainingTime = Mathf.Max(0f, _timer);
            float normalized = _windowDuration > 0f ? RemainingTime / _windowDuration : 0f;
            PromptUpdated?.Invoke(LastPrompt, normalized);

            if (_timer <= 0f)
                ResolveInputFailure(QTEResult.TimedOut);
        }

        void ResolveInputSuccess()
        {
            _inputResolved = true;
            LastNodeResult = QTEResult.Success;
            AdvanceFromNode(0);
        }

        void ResolveInputFailure(QTEResult result)
        {
            _inputResolved = true;
            LastNodeResult = result;
            if (result == QTEResult.TimedOut)
                CurrentOutcome = QTEOutcome.TimedOut;
            else
                CurrentOutcome = QTEOutcome.Failure;

            AdvanceFromNode(1);
        }

        void AdvanceFromNode(int port)
        {
            if (_inSequence && _sequenceNode != null && _current != null &&
                _current.id != _sequenceNode.id && _sequenceNode.childNodeIds.Contains(_current.id))
            {
                if (port == 0)
                {
                    EnterNextSequenceChild();
                    return;
                }

                _inSequence = false;
                Enter(_graph.FindNext(_current.id, port));
                return;
            }

            if (_current == null)
                return;

            Enter(_graph.FindNext(_current.id, port));
        }

        void Enter(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                Finish(QTEOutcome.Failure);
                return;
            }

            QTENodeData node = _graph.FindNode(nodeId);
            if (node == null)
            {
                Finish(QTEOutcome.Failure);
                return;
            }

            _current = node;
            ResetNodeState();
            NodeEntered?.Invoke(node);

            switch (node.kind)
            {
                case QTENodeKind.Start:
                    Enter(_graph.FindNext(node.id, 0));
                    break;
                case QTENodeKind.Wait:
                case QTENodeKind.Delay:
                    _timer = Mathf.Max(0f, node.duration);
                    RemainingTime = _timer;
                    break;
                case QTENodeKind.Action:
                    QTEActionExecutor.Execute(node);
                    LastNodeResult = QTEResult.Success;
                    Enter(_graph.FindNext(node.id, 0));
                    break;
                case QTENodeKind.InputPrompt:
                case QTENodeKind.Hold:
                case QTENodeKind.Mash:
                    BeginInputNode(node);
                    break;
                case QTENodeKind.SequenceInput:
                    BeginSequenceInputNode(node);
                    break;
                case QTENodeKind.Sequence:
                    BeginSequence(node);
                    break;
                case QTENodeKind.Branch:
                    EnterBranch(node);
                    break;
                case QTENodeKind.End:
                    Finish(node.endOutcome);
                    break;
            }
        }

        void BeginInputNode(QTENodeData node)
        {
            LastPrompt = node.promptText ?? "";
            _windowDuration = Mathf.Max(0.01f, node.windowDuration);
            _timer = _windowDuration;
            RemainingTime = _timer;
            Progress = 0f;
            SetRequiredInput(node.requiredInput, null);
            PromptUpdated?.Invoke(LastPrompt, 1f);
            ProgressUpdated?.Invoke(0f);
        }

        void BeginSequenceInputNode(QTENodeData node)
        {
            LastPrompt = node.promptText ?? "";
            _sequenceStep = 0;
            _windowDuration = node.totalWindow > 0f ? node.totalWindow : node.windowDuration;
            _timer = node.windowPerStep > 0f ? node.windowPerStep : _windowDuration;
            RemainingTime = _timer;
            Progress = 0f;
            SequenceStepUpdated?.Invoke(0, node.inputSequence != null ? node.inputSequence.Count : 0);
            SetRequiredInput(null, GetSequenceExpectedInput(node));
            PromptUpdated?.Invoke(LastPrompt, 1f);
        }

        void BeginSequence(QTENodeData node)
        {
            _inSequence = true;
            _sequenceNode = node;
            _sequenceIndex = 0;
            EnterNextSequenceChild();
        }

        void EnterNextSequenceChild()
        {
            if (_sequenceNode == null || _sequenceNode.childNodeIds == null)
            {
                _inSequence = false;
                Enter(_graph.FindNext(_sequenceNode != null ? _sequenceNode.id : "", 0));
                return;
            }

            while (_sequenceIndex < _sequenceNode.childNodeIds.Count)
            {
                string childId = _sequenceNode.childNodeIds[_sequenceIndex];
                _sequenceIndex++;
                QTENodeData child = _graph.FindNode(childId);
                if (child == null)
                    continue;

                EnterSequenceChild(child);
                return;
            }

            _inSequence = false;
            LastNodeResult = QTEResult.Success;
            Enter(_graph.FindNext(_sequenceNode.id, 0));
        }

        void EnterSequenceChild(QTENodeData node)
        {
            _current = node;
            ResetNodeState();
            NodeEntered?.Invoke(node);

            switch (node.kind)
            {
                case QTENodeKind.Wait:
                case QTENodeKind.Delay:
                    _timer = Mathf.Max(0f, node.duration);
                    RemainingTime = _timer;
                    break;
                case QTENodeKind.Action:
                    QTEActionExecutor.Execute(node);
                    LastNodeResult = QTEResult.Success;
                    EnterNextSequenceChild();
                    break;
                case QTENodeKind.InputPrompt:
                case QTENodeKind.Hold:
                case QTENodeKind.Mash:
                    BeginInputNode(node);
                    break;
                case QTENodeKind.SequenceInput:
                    BeginSequenceInputNode(node);
                    break;
                default:
                    LastNodeResult = QTEResult.Success;
                    EnterNextSequenceChild();
                    break;
            }
        }

        void EnterBranch(QTENodeData node)
        {
            int port = 0;
            if (node.branchMode == QTEBranchMode.OverallOutcome)
            {
                port = CurrentOutcome switch
                {
                    QTEOutcome.Success => 0,
                    QTEOutcome.Failure => 1,
                    QTEOutcome.TimedOut => 2,
                    _ => 3
                };
            }
            else
            {
                port = LastNodeResult switch
                {
                    QTEResult.Success => 0,
                    QTEResult.Failure => 1,
                    QTEResult.TimedOut => 2,
                    _ => 3
                };
            }

            string nextId = _graph.FindNext(node.id, port);
            if (string.IsNullOrEmpty(nextId) && port != 3)
                nextId = _graph.FindNext(node.id, 3);

            Enter(nextId);
        }

        void ResetNodeState()
        {
            _timer = 0f;
            _windowDuration = 0f;
            _mashCount = 0;
            _sequenceStep = 0;
            _holdElapsed = 0f;
            _inputResolved = false;
            RemainingTime = 0f;
            Progress = 0f;
            SetRequiredInput(null, null);
        }

        void UpdateSequenceExpectedInput()
        {
            if (_current == null || _current.inputSequence == null)
            {
                SetRequiredInput(null, null);
                return;
            }

            SetRequiredInput(null, GetSequenceExpectedInput(_current));
        }

        QTEInputKind? GetSequenceExpectedInput(QTENodeData node)
        {
            if (node.inputSequence == null || _sequenceStep < 0 || _sequenceStep >= node.inputSequence.Count)
                return null;

            return node.inputSequence[_sequenceStep];
        }

        void SetRequiredInput(QTEInputKind? required, QTEInputKind? sequenceExpected)
        {
            CurrentRequiredInput = required;
            CurrentSequenceExpectedInput = sequenceExpected;
            RequiredInputUpdated?.Invoke(required, sequenceExpected);
        }

        void Finish(QTEOutcome outcome)
        {
            IsRunning = false;
            _current = null;
            _inSequence = false;
            _sequenceNode = null;
            CurrentOutcome = outcome;
            LastCompletedOutcome = outcome;
            Completed?.Invoke(outcome);
        }
    }
}
