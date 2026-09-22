namespace Dialogue
{
    public enum DialogueNodeKind
    {
        Start,
        Line,
        Choice,
        End
    }

    public enum DialogueAdvanceMode
    {
        Interact,
        Auto
    }

    /// <summary>
    /// The colour role used by a dialogue line.  This is shown as a dropdown on
    /// each line node and resolved from the DialogueGraph asset.
    /// </summary>
    public enum DialogueColourPreset
    {
        Protagonist,
        NPC,
        Enemies,
        Interaction
    }
}
