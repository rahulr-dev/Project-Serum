using System.Collections.Generic;
using Game;
using UnityEngine;

namespace QTE
{
    [CreateAssetMenu(fileName = "QTEGraph", menuName = "Serum/QTE Graph", order = 1)]
    public class QTEGraph : ScriptableObject
    {
        public GameState playState = GameState.QTE;
        public GameState endState = GameState.Gameplay;
        public QTEOutcome defaultOutcome = QTEOutcome.Cancelled;

        public List<QTENodeData> nodes = new List<QTENodeData>();
        public List<QTEEdgeData> edges = new List<QTEEdgeData>();

        public QTENodeData FindNode(string id)
        {
            if (string.IsNullOrEmpty(id) || nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].id == id)
                    return nodes[i];
            }

            return null;
        }

        public QTENodeData FindStart()
        {
            if (nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].kind == QTENodeKind.Start)
                    return nodes[i];
            }

            return null;
        }

        public QTENodeData FindEnd()
        {
            if (nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].kind == QTENodeKind.End)
                    return nodes[i];
            }

            return null;
        }

        public string FindNext(string fromId, int fromPort)
        {
            if (edges == null)
                return null;

            for (int i = 0; i < edges.Count; i++)
            {
                QTEEdgeData edge = edges[i];
                if (edge != null && edge.fromId == fromId && edge.fromPort == fromPort)
                    return edge.toId;
            }

            return null;
        }
    }
}
