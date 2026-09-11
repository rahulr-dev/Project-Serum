using System.Collections.Generic;
using System.Linq;
using NpcAi;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NpcAi.Editor
{
    public class NpcStateMachineGraphView : GraphView
    {
        public NpcStateMachineGraph Graph { get; private set; }
        public Vector2 LastContentMouse { get; private set; }

        public NpcStateMachineGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            RegisterCallback<MouseMoveEvent>(evt =>
            {
                LastContentMouse = contentViewContainer.WorldToLocal(evt.mousePosition);
            });

            graphViewChanged = OnGraphChanged;
        }

        void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Create State", _ => CreateNode(NpcStateMachineNodeKind.State, LastContentMouse));
            evt.menu.AppendAction("Create Action", _ => CreateNode(NpcStateMachineNodeKind.Action, LastContentMouse));
            evt.menu.AppendAction("Create End", _ => CreateNode(NpcStateMachineNodeKind.End, LastContentMouse));
        }

        public void Populate(NpcStateMachineGraph graph)
        {
            Graph = graph;
            graphViewChanged = null;
            DeleteElements(new List<GraphElement>(graphElements.ToList()));
            graphViewChanged = OnGraphChanged;

            if (graph == null)
                return;

            EnsureStartNode();

            Dictionary<string, NpcStateMachineNodeView> views = new Dictionary<string, NpcStateMachineNodeView>();
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                NpcStateMachineNodeData data = graph.nodes[i];
                if (data == null || string.IsNullOrEmpty(data.id))
                    continue;

                NpcStateMachineNodeView view = new NpcStateMachineNodeView(data);
                AddElement(view);
                views[data.id] = view;
            }

            if (graph.edges == null)
                return;

            for (int i = 0; i < graph.edges.Count; i++)
            {
                NpcStateMachineEdgeData edgeData = graph.edges[i];
                if (edgeData == null)
                    continue;
                if (!views.TryGetValue(edgeData.fromId, out NpcStateMachineNodeView from))
                    continue;
                if (!views.TryGetValue(edgeData.toId, out NpcStateMachineNodeView to) || to.InputPort == null)
                    continue;
                if (edgeData.fromPort < 0 || edgeData.fromPort >= from.OutputPorts.Count)
                    continue;

                Edge edge = from.OutputPorts[edgeData.fromPort].ConnectTo(to.InputPort);
                AddElement(edge);
            }
        }

        public NpcStateMachineNodeView CreateNode(NpcStateMachineNodeKind kind, Vector2 position)
        {
            if (Graph == null)
                return null;

            if (kind == NpcStateMachineNodeKind.Start && Graph.FindStart() != null)
                return null;

            NpcStateMachineNodeData data = new NpcStateMachineNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = kind,
                position = position
            };

            Graph.nodes.Add(data);
            NpcStateMachineNodeView view = new NpcStateMachineNodeView(data);
            AddElement(view);
            MarkDirty();
            return view;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatible = new List<Port>();
            ports.ForEach(port =>
            {
                if (startPort == port || startPort.node == port.node)
                    return;
                if (startPort.direction == port.direction)
                    return;
                compatible.Add(port);
            });
            return compatible;
        }

        GraphViewChange OnGraphChanged(GraphViewChange change)
        {
            if (Graph == null)
                return change;

            if (change.elementsToRemove != null)
            {
                change.elementsToRemove.RemoveAll(element =>
                    element is NpcStateMachineNodeView startView &&
                    startView.Data != null &&
                    startView.Data.kind == NpcStateMachineNodeKind.Start);

                for (int i = 0; i < change.elementsToRemove.Count; i++)
                {
                    if (change.elementsToRemove[i] is NpcStateMachineNodeView nodeView)
                        Graph.nodes.Remove(nodeView.Data);
                }
            }

            if (change.movedElements != null)
            {
                for (int i = 0; i < change.movedElements.Count; i++)
                {
                    if (change.movedElements[i] is NpcStateMachineNodeView nodeView)
                        nodeView.SyncPosition();
                }
            }

            SerializeEdges();
            MarkDirty();
            return change;
        }

        public void SerializeEdges()
        {
            if (Graph == null)
                return;

            Graph.edges.Clear();
            edges.ForEach(edge =>
            {
                if (edge.output?.node is NpcStateMachineNodeView from && edge.input?.node is NpcStateMachineNodeView to)
                {
                    int port = from.OutputPorts.IndexOf(edge.output);
                    if (port < 0)
                        return;

                    Graph.edges.Add(new NpcStateMachineEdgeData
                    {
                        fromId = from.Data.id,
                        fromPort = port,
                        toId = to.Data.id
                    });
                }
            });
        }

        void EnsureStartNode()
        {
            if (Graph.FindStart() != null)
                return;

            Graph.nodes.Add(new NpcStateMachineNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = NpcStateMachineNodeKind.Start,
                position = new Vector2(80f, 160f)
            });
        }

        void MarkDirty()
        {
            if (Graph != null)
                UnityEditor.EditorUtility.SetDirty(Graph);
        }
    }
}
