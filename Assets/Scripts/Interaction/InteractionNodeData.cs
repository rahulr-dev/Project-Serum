using System;
using UnityEngine;

namespace InteractionSystem
{
    public enum InteractionNodeKind
    {
        Start,
        Wait,
        Action,
        End
    }

    [Serializable]
    public class InteractionNodeData
    {
        public string id;
        public InteractionNodeKind kind;
        public Vector2 position;
        public float duration = 1f;
        public string executeHandlerId = "";
    }

    [Serializable]
    public class InteractionEdgeData
    {
        public string fromId;
        public int fromPort;
        public string toId;
    }
}
