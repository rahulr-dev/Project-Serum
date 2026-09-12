using System.Collections.Generic;
using UnityEngine;

namespace NpcAi
{
    [CreateAssetMenu(fileName = "NpcStateMachineGraph", menuName = "Serum/NPC State Machine Graph", order = 5)]
    public class NpcStateMachineGraph : ScriptableObject
    {
        public List<NpcStateMachineNodeData> nodes = new List<NpcStateMachineNodeData>();
        public List<NpcStateMachineEdgeData> edges = new List<NpcStateMachineEdgeData>();

        public NpcStateMachineNodeData FindNode(string id)
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

        public NpcStateMachineNodeData FindStart()
        {
            if (nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].kind == NpcStateMachineNodeKind.Start)
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
                NpcStateMachineEdgeData edge = edges[i];
                if (edge != null && edge.fromId == fromId && edge.fromPort == fromPort)
                    return edge.toId;
            }

            return null;
        }
    }
}
