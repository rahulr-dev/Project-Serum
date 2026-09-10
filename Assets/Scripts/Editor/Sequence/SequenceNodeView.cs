using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using SequenceSystem;

namespace SequenceSystem.Editor
{
    public class SequenceNodeView : Node
    {
        public SequenceNodeData Data { get; private set; }
        public Port InputPort { get; private set; }
        public readonly List<Port> OutputPorts = new List<Port>();

        public SequenceNodeView(SequenceNodeData data)
        {
            Data = data;
            viewDataKey = data.id;
            title = data.kind.ToString();
            SetPosition(new Rect(data.position, new Vector2(240f, 90f)));

            if (data.kind == SequenceNodeKind.Start)
                capabilities &= ~Capabilities.Deletable;

            if (data.kind != SequenceNodeKind.Start)
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
            if (Data.kind == SequenceNodeKind.End)
                return;

            if (Data.kind == SequenceNodeKind.Condition)
            {
                Port continuePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                continuePort.portName = "Continue";
                outputContainer.Add(continuePort);
                OutputPorts.Add(continuePort);

                Port cancelPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                cancelPort.portName = "Cancel";
                outputContainer.Add(cancelPort);
                OutputPorts.Add(cancelPort);
                return;
            }

            Port single = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            single.portName = "Out";
            outputContainer.Add(single);
            OutputPorts.Add(single);
        }

        void BuildFields()
        {
            if (Data.kind == SequenceNodeKind.Action)
            {
                TextField action = new TextField("Action Id") { value = Data.actionId };
                action.RegisterValueChangedCallback(evt => Data.actionId = evt.newValue ?? "");
                extensionContainer.Add(action);
                return;
            }

            if (Data.kind == SequenceNodeKind.Condition)
            {
                TextField key = new TextField("Key") { value = string.IsNullOrEmpty(Data.conditionKey) ? "default" : Data.conditionKey };
                key.RegisterValueChangedCallback(evt => Data.conditionKey = string.IsNullOrEmpty(evt.newValue) ? "default" : evt.newValue);
                extensionContainer.Add(key);

                TextField ignore = new TextField("Ignore") { value = Data.ignoreKeys ?? "" };
                ignore.RegisterValueChangedCallback(evt => Data.ignoreKeys = evt.newValue ?? "");
                extensionContainer.Add(ignore);
            }
        }
    }
}
