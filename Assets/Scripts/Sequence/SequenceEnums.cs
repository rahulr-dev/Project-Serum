namespace SequenceSystem
{
    public enum SequenceNodeKind
    {
        Start,
        Action,
        Condition,
        ClearSequence,
        End
    }

    public enum SequenceOutcome
    {
        None,
        Completed,
        Cancelled,
        Stopped
    }
}
