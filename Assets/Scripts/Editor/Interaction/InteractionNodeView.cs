using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace InteractionSystem.Editor
{
    public class InteractionNodeView : Node
    {
        public InteractionNodeData Data { get; private set; }
        public Port InputPort { get; private set; }
        public readonly List<Port> OutputPorts = new List<Port>();

        public InteractionNodeView(InteractionNodeData data)
        {
            Data = data;
            viewDataKey = data.id;
            title = data.kind.ToString();
            SetPosition(new Rect(data.position, new Vector2(220f, 80f)));

            if (data.kind == InteractionNodeKind.Start)
                capabilities &= ~Capabilities.Deletable;

            if (data.kind != InteractionNodeKind.Start)
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
            if (Data.kind == InteractionNodeKind.End)
                return;

            Port single = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            single.portName = "Out";
            outputContainer.Add(single);
            OutputPorts.Add(single);
        }

        void BuildFields()
        {
            if (Data.kind != InteractionNodeKind.Wait)
                return;

            FloatField delay = new FloatField("Duration") { value = Data.duration };
            delay.RegisterValueChangedCallback(evt => Data.duration = Mathf.Max(0f, evt.newValue));
            extensionContainer.Add(delay);
        }
    }
}
