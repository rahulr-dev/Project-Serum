using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace InteractionSystem.Editor
{
    public class InteractionGraphView : GraphView
    {
        public InteractionGraph Graph { get; private set; }
        public Vector2 LastContentMouse { get; private set; }

        public InteractionGraphView()
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
            evt.menu.AppendAction("Create Wait", _ => CreateNode(InteractionNodeKind.Wait, LastContentMouse));
            evt.menu.AppendAction("Create Action", _ => CreateNode(InteractionNodeKind.Action, LastContentMouse));
            evt.menu.AppendAction("Create End", _ => CreateNode(InteractionNodeKind.End, LastContentMouse));
        }

        public void Populate(InteractionGraph graph)
        {
            Graph = graph;
            graphViewChanged = null;
            DeleteElements(new List<GraphElement>(graphElements.ToList()));
            graphViewChanged = OnGraphChanged;

            if (graph == null)
                return;

            EnsureStartNode();

            Dictionary<string, InteractionNodeView> views = new Dictionary<string, InteractionNodeView>();
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                InteractionNodeData data = graph.nodes[i];
                if (data == null || string.IsNullOrEmpty(data.id))
                    continue;

                InteractionNodeView view = new InteractionNodeView(data);
                AddElement(view);
                views[data.id] = view;
            }

            if (graph.edges == null)
                return;

            for (int i = 0; i < graph.edges.Count; i++)
            {
                InteractionEdgeData edgeData = graph.edges[i];
                if (edgeData == null)
                    continue;
                if (!views.TryGetValue(edgeData.fromId, out InteractionNodeView from))
                    continue;
                if (!views.TryGetValue(edgeData.toId, out InteractionNodeView to) || to.InputPort == null)
                    continue;
                if (edgeData.fromPort < 0 || edgeData.fromPort >= from.OutputPorts.Count)
                    continue;

                Edge edge = from.OutputPorts[edgeData.fromPort].ConnectTo(to.InputPort);
                AddElement(edge);
            }
        }

        public InteractionNodeView CreateNode(InteractionNodeKind kind, Vector2 position)
        {
            if (Graph == null)
                return null;

            if (kind == InteractionNodeKind.Start && Graph.FindStart() != null)
                return null;

            InteractionNodeData data = new InteractionNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = kind,
                position = position,
                duration = kind == InteractionNodeKind.Wait ? 1f : 0f
            };

            Graph.nodes.Add(data);
            InteractionNodeView view = new InteractionNodeView(data);
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
                for (int i = change.elementsToRemove.Count - 1; i >= 0; i--)
                {
                    if (change.elementsToRemove[i] is not InteractionNodeView nodeView)
                        continue;

                    if (nodeView.Data.kind == InteractionNodeKind.Start)
                    {
                        change.elementsToRemove.RemoveAt(i);
                        continue;
                    }

                    Graph.nodes.Remove(nodeView.Data);
                }
            }

            if (change.movedElements != null)
            {
                for (int i = 0; i < change.movedElements.Count; i++)
                {
                    if (change.movedElements[i] is InteractionNodeView nodeView)
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
                if (edge.output?.node is InteractionNodeView from && edge.input?.node is InteractionNodeView to)
                {
                    int port = from.OutputPorts.IndexOf(edge.output);
                    if (port < 0)
                        return;

                    Graph.edges.Add(new InteractionEdgeData
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

            Graph.nodes.Add(new InteractionNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = InteractionNodeKind.Start,
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
