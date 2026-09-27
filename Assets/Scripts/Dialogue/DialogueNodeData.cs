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
        public string body = "";
        [TextArea] public string tamilBody = "";
        public DialogueColourPreset colourPreset = DialogueColourPreset.NPC;
        public DialogueAdvanceMode advanceMode = DialogueAdvanceMode.Interact;
        public float autoDelay = 1f;
        public float charsPerSecond;
        public UnityEvent onStart = new UnityEvent();
        public List<string> choiceLabels = new List<string>();
        public List<string> tamilChoiceLabels = new List<string>();

        public string GetBody(DialogueManager.Language language)
        {
            return language == DialogueManager.Language.Tamil && !string.IsNullOrEmpty(tamilBody)
                ? tamilBody
                : body ?? "";
        }

        public IReadOnlyList<string> GetChoiceLabels(DialogueManager.Language language)
        {
            if (choiceLabels == null)
                return null;

            if (language != DialogueManager.Language.Tamil || tamilChoiceLabels == null)
                return choiceLabels;

            List<string> labels = new List<string>(choiceLabels.Count);
            for (int i = 0; i < choiceLabels.Count; i++)
            {
                string tamilLabel = i < tamilChoiceLabels.Count ? tamilChoiceLabels[i] : null;
                labels.Add(!string.IsNullOrEmpty(tamilLabel) ? tamilLabel : choiceLabels[i]);
            }

            return labels;
        }
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
        public readonly string FullText;
        public readonly DialogueAdvanceMode AdvanceMode;
        public readonly Color Colour;

        public DialogueLineInfo(string fullText, DialogueAdvanceMode advanceMode, Color colour)
        {
            FullText = fullText;
            AdvanceMode = advanceMode;
            Colour = colour;
        }
    }
}
