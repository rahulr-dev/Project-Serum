using System;
using UnityEngine;

namespace InteractionSystem
{
    public enum InteractionNodeType
    {
        Start,
        SwitchEvent,
        End
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
        private SwitchEvent switchEvent;

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

        public SwitchEvent SwitchEvent
        {
            get => switchEvent;
            set => switchEvent = value;
        }

        public InteractionNode()
        {
            id = Guid.NewGuid().ToString();
            nodeType = InteractionNodeType.SwitchEvent;
        }

        public InteractionNode(InteractionNodeType type, Vector2 position)
        {
            id = Guid.NewGuid().ToString();
            nodeType = type;
            editorX = position.x;
            editorY = position.y;
        }
    }
}
