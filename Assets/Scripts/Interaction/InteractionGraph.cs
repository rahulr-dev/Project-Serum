using System.Collections.Generic;
using Game;
using UnityEngine;

namespace InteractionSystem
{
    [CreateAssetMenu(fileName = "InteractionGraph", menuName = "Serum/Interaction Graph", order = 2)]
    public class InteractionGraph : ScriptableObject
    {
        public GameState playState = GameState.Gameplay;
        public GameState endState = GameState.Gameplay;

        public List<InteractionNodeData> nodes = new List<InteractionNodeData>();
        public List<InteractionEdgeData> edges = new List<InteractionEdgeData>();

        public InteractionNodeData FindNode(string id)
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

        public InteractionNodeData FindStart()
        {
            if (nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].kind == InteractionNodeKind.Start)
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
                InteractionEdgeData edge = edges[i];
                if (edge != null && edge.fromId == fromId && edge.fromPort == fromPort)
                    return edge.toId;
            }

            return null;
        }
    }
}
