using System.Collections.Generic;
using NpcAi;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NpcAi.Editor
{
    public class NpcStateMachineNodeView : Node
    {
        public NpcStateMachineNodeData Data { get; private set; }
        public Port InputPort { get; private set; }
        public readonly List<Port> OutputPorts = new List<Port>();

        public NpcStateMachineNodeView(NpcStateMachineNodeData data)
        {
            Data = data;
            viewDataKey = data.id;
            title = TitleFor(data);
            SetPosition(new Rect(data.position, new Vector2(240f, 90f)));

            if (data.kind == NpcStateMachineNodeKind.Start)
                capabilities &= ~Capabilities.Deletable;

            if (data.kind != NpcStateMachineNodeKind.Start)
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

        public void RefreshTitle()
        {
            title = TitleFor(Data);
        }

        static string TitleFor(NpcStateMachineNodeData data)
        {
            if (data == null)
                return "Node";

            switch (data.kind)
            {
                case NpcStateMachineNodeKind.State:
                    return data.DisplayName;
                case NpcStateMachineNodeKind.Action:
                    return string.IsNullOrEmpty(data.actionId) ? "Action" : data.actionId;
                default:
                    return data.kind.ToString();
            }
        }

        void BuildOutputs()
        {
            OutputPorts.Clear();
            outputContainer.Clear();

            if (Data.kind == NpcStateMachineNodeKind.End)
                return;

            if (Data.kind == NpcStateMachineNodeKind.State)
            {
                AddOutput("Completed");
                AddOutput("Failed");
                AddOutput("Interrupt");
                return;
            }

            AddOutput("Out");
        }

        void AddOutput(string name)
        {
            Port port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = name;
            outputContainer.Add(port);
            OutputPorts.Add(port);
        }

        void BuildFields()
        {
            if (Data.kind == NpcStateMachineNodeKind.Action)
            {
                TextField action = new TextField("Action Id") { value = Data.actionId };
                action.RegisterValueChangedCallback(evt =>
                {
                    Data.actionId = evt.newValue ?? "";
                    RefreshTitle();
                });
                extensionContainer.Add(action);
                return;
            }

            if (Data.kind == NpcStateMachineNodeKind.State)
            {
                TextField nameField = new TextField("Node Name") { value = Data.nodeName ?? "" };
                nameField.tooltip = "Optional name for UnityEvents and interrupt pickers, e.g. Patrol.";
                nameField.RegisterValueChangedCallback(evt =>
                {
                    Data.nodeName = evt.newValue ?? "";
                    RefreshTitle();
                });
                extensionContainer.Add(nameField);
            }
        }
    }
}
