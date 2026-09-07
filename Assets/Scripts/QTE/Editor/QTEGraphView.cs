using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace QTE.Editor
{
    public class QTEGraphView : GraphView
    {
        public QTEGraph Graph { get; private set; }
        public Vector2 LastContentMouse { get; private set; }

        public QTEGraphView()
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
            evt.menu.AppendAction("Create Wait", _ => CreateNode(QTENodeKind.Wait, LastContentMouse));
            evt.menu.AppendAction("Create Delay", _ => CreateNode(QTENodeKind.Delay, LastContentMouse));
            evt.menu.AppendAction("Create Action", _ => CreateNode(QTENodeKind.Action, LastContentMouse));
            evt.menu.AppendAction("Create InputPrompt", _ => CreateNode(QTENodeKind.InputPrompt, LastContentMouse));
            evt.menu.AppendAction("Create Hold", _ => CreateNode(QTENodeKind.Hold, LastContentMouse));
            evt.menu.AppendAction("Create Mash", _ => CreateNode(QTENodeKind.Mash, LastContentMouse));
            evt.menu.AppendAction("Create SequenceInput", _ => CreateNode(QTENodeKind.SequenceInput, LastContentMouse));
            evt.menu.AppendAction("Create Sequence", _ => CreateNode(QTENodeKind.Sequence, LastContentMouse));
            evt.menu.AppendAction("Create Branch", _ => CreateNode(QTENodeKind.Branch, LastContentMouse));
            evt.menu.AppendAction("Create End", _ => CreateNode(QTENodeKind.End, LastContentMouse));
        }

        public void Populate(QTEGraph graph)
        {
            Graph = graph;
            graphViewChanged = null;
            DeleteElements(new List<GraphElement>(graphElements.ToList()));
            graphViewChanged = OnGraphChanged;

            if (graph == null)
                return;

            EnsureStartNode();

            Dictionary<string, QTENodeView> views = new Dictionary<string, QTENodeView>();
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                QTENodeData data = graph.nodes[i];
                if (data == null || string.IsNullOrEmpty(data.id))
                    continue;

                QTENodeView view = new QTENodeView(data);
                AddElement(view);
                views[data.id] = view;
            }

            if (graph.edges == null)
                return;

            for (int i = 0; i < graph.edges.Count; i++)
            {
                QTEEdgeData edgeData = graph.edges[i];
                if (edgeData == null)
                    continue;
                if (!views.TryGetValue(edgeData.fromId, out QTENodeView from))
                    continue;
                if (!views.TryGetValue(edgeData.toId, out QTENodeView to) || to.InputPort == null)
                    continue;
                if (edgeData.fromPort < 0 || edgeData.fromPort >= from.OutputPorts.Count)
                    continue;

                Edge edge = from.OutputPorts[edgeData.fromPort].ConnectTo(to.InputPort);
                AddElement(edge);
            }
        }

        public QTENodeView CreateNode(QTENodeKind kind, Vector2 position)
        {
            if (Graph == null)
                return null;

            if (kind == QTENodeKind.Start && Graph.FindStart() != null)
                return null;

            QTENodeData data = new QTENodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = kind,
                position = position
            };

            if (kind == QTENodeKind.SequenceInput)
            {
                data.inputSequence.Add(QTEInputKind.Jump);
                data.inputSequence.Add(QTEInputKind.Interact);
            }

            Graph.nodes.Add(data);
            QTENodeView view = new QTENodeView(data);
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
                for (int i = 0; i < change.elementsToRemove.Count; i++)
                {
                    if (change.elementsToRemove[i] is QTENodeView nodeView)
                        Graph.nodes.Remove(nodeView.Data);
                }
            }

            if (change.movedElements != null)
            {
                for (int i = 0; i < change.movedElements.Count; i++)
                {
                    if (change.movedElements[i] is QTENodeView nodeView)
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
                if (edge.output?.node is QTENodeView from && edge.input?.node is QTENodeView to)
                {
                    int port = from.OutputPorts.IndexOf(edge.output);
                    if (port < 0)
                        return;

                    Graph.edges.Add(new QTEEdgeData
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

            Graph.nodes.Add(new QTENodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = QTENodeKind.Start,
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
