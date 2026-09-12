namespace NpcAi
{
    public enum NpcStateMachineNodeKind
    {
        Start,
        End,
        State,
        Action
    }

    public enum NpcStateMachineOutcome
    {
        None,
        Completed,
        Failed,
        Interrupted
    }
}
