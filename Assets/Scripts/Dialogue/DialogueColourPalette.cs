using UnityEngine;

namespace Dialogue
{
    [CreateAssetMenu(fileName = "DialogueColourPalette", menuName = "Serum/Dialogue Colour Palette", order = 1)]
    public class DialogueColourPalette : ScriptableObject
    {
        public Color protagonistColour = new Color(0.35f, 0.8f, 1f, 1f);
        public Color npcColour = Color.white;
        public Color enemiesColour = new Color(1f, 0.38f, 0.38f, 1f);
        public Color interactionColour = new Color(1f, 0.85f, 0.35f, 1f);

        public Color Resolve(DialogueColourPreset preset)
        {
            switch (preset)
            {
                case DialogueColourPreset.Protagonist:
                    return protagonistColour;
                case DialogueColourPreset.Enemies:
                    return enemiesColour;
                case DialogueColourPreset.Interaction:
                    return interactionColour;
                case DialogueColourPreset.NPC:
                default:
                    return npcColour;
            }
        }
    }
}
