using System.Collections.Generic;
using Game;
using UnityEngine;

namespace Dialogue
{
    [CreateAssetMenu(fileName = "DialogueGraph", menuName = "Serum/Dialogue Graph", order = 0)]
    public class DialogueGraph : ScriptableObject
    {
        public GameState playState = GameState.Dialogue;
        public GameState endState = GameState.Gameplay;
        public float charsPerSecond = 40f;
        public float choiceRepeatDelay = 0.2f;

        public List<DialogueNodeData> nodes = new List<DialogueNodeData>();
        public List<DialogueEdgeData> edges = new List<DialogueEdgeData>();

        public DialogueNodeData FindNode(string id)
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

        public DialogueNodeData FindStart()
        {
            if (nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].kind == DialogueNodeKind.Start)
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
                DialogueEdgeData edge = edges[i];
                if (edge != null && edge.fromId == fromId && edge.fromPort == fromPort)
                    return edge.toId;
            }

            return null;
        }

        public float ResolveCharsPerSecond(DialogueNodeData node)
        {
            if (node != null && node.charsPerSecond > 0f)
                return node.charsPerSecond;
            return charsPerSecond > 0f ? charsPerSecond : 40f;
        }
    }
}
