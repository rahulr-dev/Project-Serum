using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Dialogue
{
    [Serializable]
    public class DialogueNodeData
    {
        public string id;
        public DialogueNodeKind kind;
        public Vector2 position;
        public string speaker = "";
        public string body = "";
        public DialogueAdvanceMode advanceMode = DialogueAdvanceMode.Interact;
        public float autoDelay = 1f;
        public float charsPerSecond;
        public UnityEvent onStart = new UnityEvent();
        public List<string> choiceLabels = new List<string>();
    }

    [Serializable]
    public class DialogueEdgeData
    {
        public string fromId;
        public int fromPort;
        public string toId;
    }

    public readonly struct DialogueLineInfo
    {
        public readonly string Speaker;
        public readonly string FullText;
        public readonly DialogueAdvanceMode AdvanceMode;

        public DialogueLineInfo(string speaker, string fullText, DialogueAdvanceMode advanceMode)
        {
            Speaker = speaker;
            FullText = fullText;
            AdvanceMode = advanceMode;
        }
    }
}
