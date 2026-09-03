using System;
using System.Collections.Generic;
using Interaction;
using UnityEngine;

namespace Dialogue
{
    public class DialogueRunner
    {
        public event Action<DialogueLineInfo> OnLineStarted;
        public event Action<string> OnLineTextUpdated;
        public event Action<IReadOnlyList<string>> OnChoicesPresented;
        public event Action<int> OnChoiceIndexChanged;
        public event Action OnFinished;

        public bool IsPlaying { get; private set; }
        public int SelectedIndex { get; private set; }
        public IReadOnlyList<string> CurrentChoices => _choiceLabels;

        DialogueGraph _graph;
        DialogueNodeData _current;
        float _visibleChars;
        float _autoTimer;
        bool _lineRevealed;
        bool _waitingAuto;
        bool _inChoice;
        readonly List<string> _choiceLabels = new List<string>();
        float _choiceRepeatTimer;
        int _choiceHoldDir;

        public void Start(DialogueGraph graph)
        {
            Stop(false);
            _graph = graph;
            IsPlaying = graph != null;
            if (!IsPlaying)
            {
                OnFinished?.Invoke();
                return;
            }

            DialogueNodeData start = graph.FindStart();
            if (start == null)
            {
                Finish();
                return;
            }

            Enter(graph.FindNext(start.id, 0));
        }

        public void Stop(bool notifyFinished)
        {
            bool wasPlaying = IsPlaying;
            IsPlaying = false;
            _graph = null;
            _current = null;
            _inChoice = false;
            _waitingAuto = false;
            _choiceLabels.Clear();
            if (notifyFinished && wasPlaying)
                OnFinished?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            if (!IsPlaying || _current == null)
                return;

            if (_current.kind == DialogueNodeKind.Line)
                TickLine(deltaTime);
            else if (_inChoice)
                TickChoiceNav(deltaTime);
        }

        public void HandleInteract()
        {
            if (!IsPlaying || _current == null)
                return;

            if (_inChoice)
            {
                SelectChoice(SelectedIndex);
                return;
            }

            if (_current.kind != DialogueNodeKind.Line)
                return;

            if (!_lineRevealed)
            {
                RevealAll();
                return;
            }

            if (_current.advanceMode == DialogueAdvanceMode.Interact)
                AdvanceLine();
        }

        public void SelectChoice(int index)
        {
            if (!IsPlaying || !_inChoice || _current == null)
                return;

            if (index < 0 || index >= _choiceLabels.Count)
                return;

            SelectedIndex = index;
            string nextId = _graph.FindNext(_current.id, index);
            _inChoice = false;
            _choiceLabels.Clear();
            Enter(nextId);
        }

        void TickLine(float deltaTime)
        {
            if (!_lineRevealed)
            {
                float cps = _graph.ResolveCharsPerSecond(_current);
                int before = Mathf.FloorToInt(_visibleChars);
                _visibleChars += cps * deltaTime;
                int after = Mathf.FloorToInt(_visibleChars);
                string body = _current.body ?? "";
                if (after >= body.Length)
                    RevealAll();
                else if (after != before)
                    OnLineTextUpdated?.Invoke(body.Substring(0, Mathf.Max(0, after)));
                return;
            }

            if (!_waitingAuto)
                return;

            _autoTimer -= deltaTime;
            if (_autoTimer <= 0f)
                AdvanceLine();
        }

        void TickChoiceNav(float deltaTime)
        {
            InteractionManager input = InteractionManager.Instance;
            if (input == null || _choiceLabels.Count == 0)
                return;

            int dir = 0;
            if (input.KeyW || input.GamepadDpadUp || input.GamepadLeftStick.y > 0.5f)
                dir = -1;
            else if (input.KeyS || input.GamepadDpadDown || input.GamepadLeftStick.y < -0.5f)
                dir = 1;

            float delay = _graph.choiceRepeatDelay > 0f ? _graph.choiceRepeatDelay : 0.2f;

            if (dir == 0)
            {
                _choiceHoldDir = 0;
                _choiceRepeatTimer = 0f;
                return;
            }

            if (dir != _choiceHoldDir)
            {
                _choiceHoldDir = dir;
                _choiceRepeatTimer = delay;
                MoveChoice(dir);
                return;
            }

            _choiceRepeatTimer -= deltaTime;
            if (_choiceRepeatTimer <= 0f)
            {
                _choiceRepeatTimer = delay;
                MoveChoice(dir);
            }
        }

        void MoveChoice(int dir)
        {
            if (_choiceLabels.Count == 0)
                return;

            int next = SelectedIndex + dir;
            if (next < 0)
                next = _choiceLabels.Count - 1;
            else if (next >= _choiceLabels.Count)
                next = 0;

            if (next == SelectedIndex)
                return;

            SelectedIndex = next;
            OnChoiceIndexChanged?.Invoke(SelectedIndex);
        }

        void RevealAll()
        {
            string body = _current.body ?? "";
            _visibleChars = body.Length;
            _lineRevealed = true;
            OnLineTextUpdated?.Invoke(body);

            if (_current.advanceMode == DialogueAdvanceMode.Auto)
            {
                _waitingAuto = true;
                _autoTimer = _current.autoDelay;
            }
        }

        void AdvanceLine()
        {
            _waitingAuto = false;
            Enter(_graph.FindNext(_current.id, 0));
        }

        void Enter(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                Finish();
                return;
            }

            DialogueNodeData node = _graph.FindNode(nodeId);
            if (node == null)
            {
                Finish();
                return;
            }

            _current = node;
            _inChoice = false;
            _waitingAuto = false;
            _lineRevealed = false;
            _choiceHoldDir = 0;
            _choiceRepeatTimer = 0f;

            switch (node.kind)
            {
                case DialogueNodeKind.Start:
                    Enter(_graph.FindNext(node.id, 0));
                    break;
                case DialogueNodeKind.Line:
                    node.onStart?.Invoke();
                    OnLineStarted?.Invoke(new DialogueLineInfo(node.speaker, node.body ?? "", node.advanceMode));
                    _visibleChars = 0f;
                    if (string.IsNullOrEmpty(node.body))
                        RevealAll();
                    else
                        OnLineTextUpdated?.Invoke("");
                    break;
                case DialogueNodeKind.Choice:
                    _choiceLabels.Clear();
                    if (node.choiceLabels != null)
                        _choiceLabels.AddRange(node.choiceLabels);
                    if (_choiceLabels.Count == 0)
                    {
                        Finish();
                        return;
                    }

                    _inChoice = true;
                    SelectedIndex = 0;
                    OnChoicesPresented?.Invoke(_choiceLabels);
                    OnChoiceIndexChanged?.Invoke(0);
                    break;
                case DialogueNodeKind.End:
                    Finish();
                    break;
            }
        }

        void Finish()
        {
            IsPlaying = false;
            _current = null;
            _inChoice = false;
            OnFinished?.Invoke();
        }
    }
}
