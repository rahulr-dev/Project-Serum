using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dialogue.Editor
{
    public class DialogueGraphView : GraphView
    {
        public DialogueGraph Graph { get; private set; }
        public Vector2 LastContentMouse { get; private set; }

        public DialogueGraphView()
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
            evt.menu.AppendAction("Create Line", _ => CreateNode(DialogueNodeKind.Line, LastContentMouse));
            evt.menu.AppendAction("Create Choice", _ => CreateNode(DialogueNodeKind.Choice, LastContentMouse));
            evt.menu.AppendAction("Create End", _ => CreateNode(DialogueNodeKind.End, LastContentMouse));
        }

        public void Populate(DialogueGraph graph)
        {
            Graph = graph;
            graphViewChanged = null;
            DeleteElements(new List<GraphElement>(graphElements.ToList()));
            graphViewChanged = OnGraphChanged;

            if (graph == null)
                return;

            EnsureStartNode();

            Dictionary<string, DialogueNodeView> views = new Dictionary<string, DialogueNodeView>();
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DialogueNodeData data = graph.nodes[i];
                if (data == null || string.IsNullOrEmpty(data.id))
                    continue;

                DialogueNodeView view = new DialogueNodeView(data);
                AddElement(view);
                views[data.id] = view;
            }

            if (graph.edges == null)
                return;

            for (int i = 0; i < graph.edges.Count; i++)
            {
                DialogueEdgeData edgeData = graph.edges[i];
                if (edgeData == null)
                    continue;
                if (!views.TryGetValue(edgeData.fromId, out DialogueNodeView from))
                    continue;
                if (!views.TryGetValue(edgeData.toId, out DialogueNodeView to) || to.InputPort == null)
                    continue;
                if (edgeData.fromPort < 0 || edgeData.fromPort >= from.OutputPorts.Count)
                    continue;

                Edge edge = from.OutputPorts[edgeData.fromPort].ConnectTo(to.InputPort);
                AddElement(edge);
            }
        }

        public DialogueNodeView CreateNode(DialogueNodeKind kind, Vector2 position)
        {
            if (Graph == null)
                return null;

            if (kind == DialogueNodeKind.Start && Graph.FindStart() != null)
                return null;

            DialogueNodeData data = new DialogueNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = kind,
                position = position
            };
            if (kind == DialogueNodeKind.Choice)
                data.choiceLabels.Add("Option A");

            Graph.nodes.Add(data);
            DialogueNodeView view = new DialogueNodeView(data);
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
                    if (change.elementsToRemove[i] is DialogueNodeView nodeView)
                        Graph.nodes.Remove(nodeView.Data);
                }
            }

            if (change.movedElements != null)
            {
                for (int i = 0; i < change.movedElements.Count; i++)
                {
                    if (change.movedElements[i] is DialogueNodeView nodeView)
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
                if (edge.output?.node is DialogueNodeView from && edge.input?.node is DialogueNodeView to)
                {
                    int port = from.OutputPorts.IndexOf(edge.output);
                    if (port < 0)
                        return;

                    Graph.edges.Add(new DialogueEdgeData
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

            Graph.nodes.Add(new DialogueNodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                kind = DialogueNodeKind.Start,
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
