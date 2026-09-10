using System.Collections.Generic;
using Game;
using UnityEngine;

namespace SequenceSystem
{
    [CreateAssetMenu(fileName = "SequenceGraph", menuName = "Serum/Sequence Graph", order = 3)]
    public class SequenceGraph : ScriptableObject
    {
        public GameState playState = GameState.Gameplay;
        public GameState endState = GameState.Gameplay;

        public List<SequenceNodeData> nodes = new List<SequenceNodeData>();
        public List<SequenceEdgeData> edges = new List<SequenceEdgeData>();

        public SequenceNodeData FindNode(string id)
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

        public SequenceNodeData FindStart()
        {
            if (nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].kind == SequenceNodeKind.Start)
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
                SequenceEdgeData edge = edges[i];
                if (edge != null && edge.fromId == fromId && edge.fromPort == fromPort)
                    return edge.toId;
            }

            return null;
        }
    }
}
