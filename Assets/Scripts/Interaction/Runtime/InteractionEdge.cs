using System;
using UnityEngine;

namespace InteractionSystem
{
    [Serializable]
    public class InteractionEdge
    {
        [SerializeField]
        private string fromNodeID;

        [SerializeField]
        private string toNodeID;

        public string FromNodeID
        {
            get => fromNodeID;
            set => fromNodeID = value;
        }

        public string ToNodeID
        {
            get => toNodeID;
            set => toNodeID = value;
        }

        public InteractionEdge()
        {
        }

        public InteractionEdge(string from, string to)
        {
            fromNodeID = from;
            toNodeID = to;
        }
    }
}
