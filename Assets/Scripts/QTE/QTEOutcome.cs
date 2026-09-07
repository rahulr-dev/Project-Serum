namespace QTE
{
    public enum QTEOutcome
    {
        Success,
        Failure,
        TimedOut,
        Cancelled
    }

    public enum QTEInputKind
    {
        Interact,
        Jump,
        MoveLeft,
        MoveRight,
        AnyFaceButton
    }

    public enum QTENodeKind
    {
        Start,
        End,
        Wait,
        Delay,
        Action,
        InputPrompt,
        Hold,
        Mash,
        SequenceInput,
        Sequence,
        Branch
    }

    public enum QTEBranchMode
    {
        LastNodeResult,
        OverallOutcome
    }

    public enum QTEResult
    {
        None,
        Success,
        Failure,
        TimedOut
    }
}
