using System;
using System.Collections.Generic;
using UnityEngine;

namespace InteractionSystem
{
    public enum InteractionNodeType
    {
        Start = 0,
        InvokeEvent = 1,
        End = 2,
        Wait = 3
    }

    [Serializable]
    public class InteractionNode
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private InteractionNodeType nodeType;

        [SerializeField]
        private float editorX;

        [SerializeField]
        private float editorY;

        [SerializeField]
        private InvokeEvent invokeEvent;

        [SerializeField]
        private float waitDuration = 1.0f;

        [SerializeField]
        private List<InteractionAction> actions = new List<InteractionAction>();

        public string ID
        {
            get => id;
            set => id = value;
        }

        public InteractionNodeType NodeType
        {
            get => nodeType;
            set => nodeType = value;
        }

        public float EditorX
        {
            get => editorX;
            set => editorX = value;
        }

        public float EditorY
        {
            get => editorY;
            set => editorY = value;
        }

        public Vector2 Position
        {
            get => new Vector2(editorX, editorY);
            set
            {
                editorX = value.x;
                editorY = value.y;
            }
        }

        public InvokeEvent InvokeEvent
        {
            get => invokeEvent;
            set => invokeEvent = value;
        }

        // Backward compatibility property
        public SwitchEvent SwitchEvent
        {
            get => invokeEvent as SwitchEvent;
            set => invokeEvent = value;
        }

        public float WaitDuration
        {
            get => waitDuration;
            set => waitDuration = Mathf.Max(0f, value);
        }

        public List<InteractionAction> Actions
        {
            get
            {
                if (actions == null) actions = new List<InteractionAction>();
                return actions;
            }
            set => actions = value;
        }

        public InteractionNode()
        {
            id = Guid.NewGuid().ToString();
            nodeType = InteractionNodeType.InvokeEvent;
            actions = new List<InteractionAction>();
        }

        public InteractionNode(InteractionNodeType type, Vector2 position)
        {
            id = Guid.NewGuid().ToString();
            nodeType = type;
            editorX = position.x;
            editorY = position.y;
            actions = new List<InteractionAction>();
        }
    }
}
