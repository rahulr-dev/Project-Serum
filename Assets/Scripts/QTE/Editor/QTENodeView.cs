using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace QTE.Editor
{
    public class QTENodeView : Node
    {
        public QTENodeData Data { get; private set; }
        public Port InputPort { get; private set; }
        public readonly List<Port> OutputPorts = new List<Port>();

        public QTENodeView(QTENodeData data)
        {
            Data = data;
            viewDataKey = data.id;
            title = data.kind.ToString();
            SetPosition(new Rect(data.position, new Vector2(240f, 90f)));

            if (data.kind != QTENodeKind.Start)
            {
                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                InputPort.portName = "In";
                inputContainer.Add(InputPort);
            }

            BuildOutputs();
            BuildFields();
            RefreshExpandedState();
            RefreshPorts();
        }

        public void SyncPosition()
        {
            Data.position = GetPosition().position;
        }

        void BuildOutputs()
        {
            OutputPorts.Clear();
            outputContainer.Clear();

            if (Data.kind == QTENodeKind.End)
                return;

            switch (Data.kind)
            {
                case QTENodeKind.InputPrompt:
                case QTENodeKind.Hold:
                case QTENodeKind.Mash:
                case QTENodeKind.SequenceInput:
                    AddOutput("Success");
                    AddOutput("Failure");
                    break;
                case QTENodeKind.Branch:
                    EnsureBranchLabels();
                    for (int i = 0; i < Data.branchLabels.Count; i++)
                        AddOutput(Data.branchLabels[i]);
                    break;
                default:
                    AddOutput("Out");
                    break;
            }
        }

        void AddOutput(string name)
        {
            Port port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = name;
            outputContainer.Add(port);
            OutputPorts.Add(port);
        }

        void EnsureBranchLabels()
        {
            if (Data.branchLabels == null)
                Data.branchLabels = new List<string>();
            if (Data.branchLabels.Count == 0)
            {
                Data.branchLabels.Add("Success");
                Data.branchLabels.Add("Failure");
                Data.branchLabels.Add("Timeout");
                Data.branchLabels.Add("Default");
            }
        }

        void BuildFields()
        {
            switch (Data.kind)
            {
                case QTENodeKind.Wait:
                case QTENodeKind.Delay:
                    AddFloatField("Duration", Data.duration, v => Data.duration = v);
                    break;
                case QTENodeKind.InputPrompt:
                case QTENodeKind.Hold:
                case QTENodeKind.Mash:
                    AddTextField("Prompt", Data.promptText, v => Data.promptText = v);
                    AddEnumField("Input", Data.requiredInput, v => Data.requiredInput = v);
                    AddFloatField("Window", Data.windowDuration, v => Data.windowDuration = v);
                    if (Data.kind == QTENodeKind.Hold)
                        AddFloatField("Hold", Data.holdDuration, v => Data.holdDuration = v);
                    if (Data.kind == QTENodeKind.Mash)
                        AddIntField("Target", Data.targetCount, v => Data.targetCount = v);
                    break;
                case QTENodeKind.SequenceInput:
                    AddTextField("Prompt", Data.promptText, v => Data.promptText = v);
                    AddFloatField("Step Window", Data.windowPerStep, v => Data.windowPerStep = v);
                    AddFloatField("Total Window", Data.totalWindow, v => Data.totalWindow = v);
                    break;
                case QTENodeKind.Sequence:
                    extensionContainer.Add(new Label("Child IDs edited in graph inspector."));
                    break;
                case QTENodeKind.Branch:
                    AddEnumField("Mode", Data.branchMode, v => Data.branchMode = v);
                    break;
                case QTENodeKind.End:
                    AddEnumField("Outcome", Data.endOutcome, v => Data.endOutcome = v);
                    break;
            }
        }

        void AddTextField(string label, string value, System.Action<string> onChanged)
        {
            TextField field = new TextField(label) { value = value };
            field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            extensionContainer.Add(field);
        }

        void AddFloatField(string label, float value, System.Action<float> onChanged)
        {
            FloatField field = new FloatField(label) { value = value };
            field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            extensionContainer.Add(field);
        }

        void AddIntField(string label, int value, System.Action<int> onChanged)
        {
            IntegerField field = new IntegerField(label) { value = value };
            field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            extensionContainer.Add(field);
        }

        void AddEnumField<T>(string label, T value, System.Action<T> onChanged) where T : System.Enum
        {
            EnumField field = new EnumField(label, value);
            field.RegisterValueChangedCallback(evt => onChanged((T)evt.newValue));
            extensionContainer.Add(field);
        }
    }
}
