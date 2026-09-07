using System.Collections.Generic;
using UnityEngine;

namespace InteractionSystem
{
    [CreateAssetMenu(fileName = "InteractionSequenceSO", menuName = "Interaction/Interaction Sequence")]
    public class InteractionSequenceSO : ScriptableObject
    {
        [SerializeField]
        private string interactionName = "Door Interaction Sequence";

        [SerializeField]
        private List<InteractionNode> nodes = new List<InteractionNode>();

        [SerializeField]
        private List<InteractionEdge> edges = new List<InteractionEdge>();

        public string InteractionName
        {
            get => interactionName;
            set => interactionName = value;
        }

        public List<InteractionNode> Nodes => nodes;
        public List<InteractionEdge> Edges => edges;

        public InteractionNode GetNodeByID(string id)
        {
            if (string.IsNullOrEmpty(id) || nodes == null) return null;
            return nodes.Find(n => n.ID == id);
        }

        public List<InteractionEdge> GetOutgoingEdges(string nodeID)
        {
            List<InteractionEdge> result = new List<InteractionEdge>();
            if (edges == null) return result;
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].FromNodeID == nodeID)
                {
                    result.Add(edges[i]);
                }
            }
            return result;
        }
    }
}
