using System.Collections.Generic;
using UnityEngine;

namespace NpcAi
{
    [CreateAssetMenu(fileName = "NpcStateGraph", menuName = "Serum/NPC State Graph", order = 4)]
    public class NpcStateGraph : ScriptableObject
    {
        public List<NpcStateNodeData> nodes = new List<NpcStateNodeData>();
        public List<NpcStateEdgeData> edges = new List<NpcStateEdgeData>();

        public NpcStateNodeData FindNode(string id)
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

        public NpcStateNodeData FindStart()
        {
            if (nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].kind == NpcStateNodeKind.Start)
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
                NpcStateEdgeData edge = edges[i];
                if (edge != null && edge.fromId == fromId && edge.fromPort == fromPort)
                    return edge.toId;
            }

            return null;
        }
    }
}
