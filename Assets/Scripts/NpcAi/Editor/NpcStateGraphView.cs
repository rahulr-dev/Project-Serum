using System.Collections.Generic;
using System.Linq;
using NpcAi;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NpcAi.Editor
{
    public class NpcStateGraphView : GraphView
    {
        public NpcStateGraph Graph { get; private set; }
        public Vector2 LastContentMouse { get; private set; }

        public NpcStateGraphView()
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
            evt.menu.AppendAction("Create Wait", _ => CreateNode(NpcStateNodeKind.Wait, LastContentMouse));
            evt.menu.AppendAction("Create Character Action", _ => CreateNode(NpcStateNodeKind.CharacterAction, LastContentMouse));
            evt.menu.AppendAction("Create Scene Action", _ => CreateNode(NpcStateNodeKind.SceneAction, LastContentMouse));
            evt.menu.AppendAction("Create Wait Event", _ => CreateNode(NpcStateNodeKind.WaitEvent, LastContentMouse));
            evt.menu.AppendAction("Create Random Branch", _ => CreateNode(NpcStateNodeKind.RandomBranch, LastContentMouse));
            evt.menu.AppendAction("Create End", _ => CreateNode(NpcStateNodeKind.End, LastContentMouse));
        }

        public void Populate(NpcStateGraph graph)
        {
            Graph = graph;
            graphViewChanged = null;
            DeleteElements(new List<GraphElement>(graphElements.ToList()));
            graphViewChanged = OnGraphChanged;

            if (graph == null)
                return;

            EnsureStartNode();

            Dictionary<string, NpcStateNodeView> views = new Dictionary<string, NpcStateNodeView>();
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                NpcStateNodeData data = graph.nodes[i];
                if (data == null || string.IsNullOrEmpty(data.id))
                    continue;

                NpcStateNodeView view = new NpcStateNodeView(data);
                AddElement(view);
                views[data.id] = view;
            }

            if (graph.edges == null)
                return;

            for (int i = 0; i < graph.edges.Count; i++)
            {
                NpcStateEdgeData edgeData = graph.edges[i];
                if (edgeData == null)
                    continue;
                if (!views.TryGetValue(edgeData.fromId, out NpcStateNodeView from))
                    continue;
                if (!views.TryGetValue(edgeData.toId, out NpcStateNodeView to) || to.InputPort == null)
                    continue;
                if (edgeData.fromPort < 0 || edgeData.fromPort >= from.OutputPorts.Count)
                    continue;

                Edge edge = from.OutputPorts[edgeData.fromPort].ConnectTo(to.InputPort);
                AddElement(edge);
            }
        }

        public NpcStateNodeView CreateNode(NpcStateNodeKind kind, Vector2 position)
        {
            if (Graph == null)
                return null;

            if (kind == NpcStateNodeKind.Start && Graph.FindStart() != null)
                return null;

            NpcStateNodeData data = new NpcStateNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = kind,
                position = position
            };

            Graph.nodes.Add(data);
            NpcStateNodeView view = new NpcStateNodeView(data);
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
                    element is NpcStateNodeView startView &&
                    startView.Data != null &&
                    startView.Data.kind == NpcStateNodeKind.Start);

                for (int i = 0; i < change.elementsToRemove.Count; i++)
                {
                    if (change.elementsToRemove[i] is NpcStateNodeView nodeView)
                        Graph.nodes.Remove(nodeView.Data);
                }
            }

            if (change.movedElements != null)
            {
                for (int i = 0; i < change.movedElements.Count; i++)
                {
                    if (change.movedElements[i] is NpcStateNodeView nodeView)
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
                if (edge.output?.node is NpcStateNodeView from && edge.input?.node is NpcStateNodeView to)
                {
                    int port = from.OutputPorts.IndexOf(edge.output);
                    if (port < 0)
                        return;

                    Graph.edges.Add(new NpcStateEdgeData
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

            Graph.nodes.Add(new NpcStateNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = NpcStateNodeKind.Start,
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
