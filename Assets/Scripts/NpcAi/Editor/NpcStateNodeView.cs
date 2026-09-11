using System.Collections.Generic;
using NpcAi;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NpcAi.Editor
{
    public class NpcStateNodeView : Node
    {
        public NpcStateNodeData Data { get; private set; }
        public Port InputPort { get; private set; }
        public readonly List<Port> OutputPorts = new List<Port>();

        public NpcStateNodeView(NpcStateNodeData data)
        {
            Data = data;
            viewDataKey = data.id;
            title = TitleFor(data);
            SetPosition(new Rect(data.position, new Vector2(240f, 90f)));

            if (data.kind == NpcStateNodeKind.Start)
                capabilities &= ~Capabilities.Deletable;

            if (data.kind != NpcStateNodeKind.Start)
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

        static string TitleFor(NpcStateNodeData data)
        {
            if (data == null)
                return "Node";

            switch (data.kind)
            {
                case NpcStateNodeKind.CharacterAction:
                    return TitleForAction(data.characterAction);
                case NpcStateNodeKind.WaitEvent:
                    return string.IsNullOrEmpty(data.waitEventId) ? "WaitEvent" : data.waitEventId;
                case NpcStateNodeKind.RandomBranch:
                    return "Random";
                default:
                    return data.kind.ToString();
            }
        }

        static string TitleForAction(NpcStateCharacterAction action)
        {
            switch (action)
            {
                case NpcStateCharacterAction.RunToGameObject:
                    return "Run To GameObject";
                case NpcStateCharacterAction.RunRandomLeftRight:
                    return "Run Random Direction";
                case NpcStateCharacterAction.SmoothLookAt:
                    return "Smooth Look At";
                case NpcStateCharacterAction.MaintainLookAt:
                    return "Maintain Look At";
                default:
                    return action.ToString();
            }
        }

        public void RebuildRandomOutputs()
        {
            if (Data == null || Data.kind != NpcStateNodeKind.RandomBranch)
                return;

            int count = Data.GetBranchCount();
            GraphView graphView = GetFirstAncestorOfType<GraphView>();

            while (OutputPorts.Count > count)
            {
                Port port = OutputPorts[OutputPorts.Count - 1];
                if (graphView != null && port.connected)
                {
                    var edges = new List<GraphElement>();
                    foreach (Edge edge in port.connections)
                        edges.Add(edge);
                    graphView.DeleteElements(edges);
                }

                outputContainer.Remove(port);
                OutputPorts.RemoveAt(OutputPorts.Count - 1);
            }

            while (OutputPorts.Count < count)
                AddOutput(NpcStateNodeData.BranchPortName(OutputPorts.Count));

            for (int i = 0; i < OutputPorts.Count; i++)
                OutputPorts[i].portName = NpcStateNodeData.BranchPortName(i);

            RefreshExpandedState();
            RefreshPorts();
        }

        void BuildOutputs()
        {
            OutputPorts.Clear();
            outputContainer.Clear();

            if (Data.kind == NpcStateNodeKind.End)
                return;

            if (Data.kind == NpcStateNodeKind.WaitEvent)
            {
                AddOutput("Done");
                AddOutput("Timeout");
                return;
            }

            if (Data.kind == NpcStateNodeKind.RandomBranch)
            {
                int count = Data.GetBranchCount();
                for (int i = 0; i < count; i++)
                    AddOutput(NpcStateNodeData.BranchPortName(i));
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
            switch (Data.kind)
            {
                case NpcStateNodeKind.Wait:
                    AddFloatField("Duration", Data.duration, v => Data.duration = v);
                    break;
                case NpcStateNodeKind.CharacterAction:
                    AddEnumField("Action", Data.characterAction, v =>
                    {
                        Data.characterAction = v;
                        RefreshTitle();
                    });
                    AddToggle("Wait Until Done", Data.waitUntilDone, v => Data.waitUntilDone = v);
                    break;
                case NpcStateNodeKind.SceneAction:
                    extensionContainer.Add(new Label("Target edited in graph inspector."));
                    break;
                case NpcStateNodeKind.RandomBranch:
                    extensionContainer.Add(new Label("Pin count edited in graph inspector."));
                    break;
                case NpcStateNodeKind.WaitEvent:
                    AddTextField("Event", Data.waitEventId, v =>
                    {
                        Data.waitEventId = v ?? "";
                        RefreshTitle();
                    });
                    AddFloatField("Timeout", Data.duration, v => Data.duration = v);
                    break;
                case NpcStateNodeKind.End:
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

        void AddToggle(string label, bool value, System.Action<bool> onChanged)
        {
            Toggle field = new Toggle(label) { value = value };
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
