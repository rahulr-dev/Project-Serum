using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dialogue.Editor
{
    public class DialogueNodeView : Node
    {
        public DialogueNodeData Data { get; private set; }
        public Port InputPort { get; private set; }
        public readonly List<Port> OutputPorts = new List<Port>();

        public DialogueNodeView(DialogueNodeData data)
        {
            Data = data;
            viewDataKey = data.id;
            title = data.kind.ToString();
            SetPosition(new Rect(data.position, new Vector2(220f, 80f)));

            if (data.kind != DialogueNodeKind.Start)
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

        public void RebuildChoiceOutputs()
        {
            if (Data.kind != DialogueNodeKind.Choice)
                return;

            for (int i = OutputPorts.Count - 1; i >= 0; i--)
                outputContainer.Remove(OutputPorts[i]);

            OutputPorts.Clear();
            BuildOutputs();
            RefreshPorts();
        }

        void BuildOutputs()
        {
            if (Data.kind == DialogueNodeKind.End)
                return;

            if (Data.kind == DialogueNodeKind.Choice)
            {
                if (Data.choiceLabels == null)
                    Data.choiceLabels = new List<string>();
                if (Data.choiceLabels.Count == 0)
                    Data.choiceLabels.Add("Option");

                for (int i = 0; i < Data.choiceLabels.Count; i++)
                {
                    Port port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    port.portName = string.IsNullOrEmpty(Data.choiceLabels[i]) ? $"Out {i}" : Data.choiceLabels[i];
                    outputContainer.Add(port);
                    OutputPorts.Add(port);
                }

                return;
            }

            Port single = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            single.portName = "Out";
            outputContainer.Add(single);
            OutputPorts.Add(single);
        }

        void BuildFields()
        {
            if (Data.kind == DialogueNodeKind.Line)
            {
                TextField speaker = new TextField("Speaker") { value = Data.speaker };
                speaker.RegisterValueChangedCallback(evt => Data.speaker = evt.newValue);
                extensionContainer.Add(speaker);

                TextField body = new TextField("Text") { value = Data.body, multiline = true };
                body.style.minHeight = 60;
                body.RegisterValueChangedCallback(evt => Data.body = evt.newValue);
                extensionContainer.Add(body);

                EnumField mode = new EnumField("Advance", Data.advanceMode);
                mode.RegisterValueChangedCallback(evt => Data.advanceMode = (DialogueAdvanceMode)evt.newValue);
                extensionContainer.Add(mode);

                FloatField delay = new FloatField("Auto delay") { value = Data.autoDelay };
                delay.RegisterValueChangedCallback(evt => Data.autoDelay = evt.newValue);
                extensionContainer.Add(delay);

                FloatField cps = new FloatField("Chars/sec (0=default)") { value = Data.charsPerSecond };
                cps.RegisterValueChangedCallback(evt => Data.charsPerSecond = evt.newValue);
                extensionContainer.Add(cps);
            }

            if (Data.kind == DialogueNodeKind.Choice)
            {
                Button add = new Button(() =>
                {
                    Data.choiceLabels.Add("Option");
                    RebuildChoiceOutputs();
                    RefreshChoiceLabelFields();
                }) { text = "Add option" };
                extensionContainer.Add(add);
                RefreshChoiceLabelFields();
            }
        }

        void RefreshChoiceLabelFields()
        {
            List<VisualElement> remove = new List<VisualElement>();
            foreach (VisualElement child in extensionContainer.Children())
            {
                if (child is TextField)
                    remove.Add(child);
            }

            for (int i = 0; i < remove.Count; i++)
                extensionContainer.Remove(remove[i]);

            for (int i = 0; i < Data.choiceLabels.Count; i++)
            {
                int index = i;
                TextField field = new TextField($"Option {index}") { value = Data.choiceLabels[index] };
                field.RegisterValueChangedCallback(evt =>
                {
                    Data.choiceLabels[index] = evt.newValue;
                    if (index < OutputPorts.Count)
                        OutputPorts[index].portName = string.IsNullOrEmpty(evt.newValue) ? $"Out {index}" : evt.newValue;
                });
                extensionContainer.Add(field);
            }
        }
    }
}
