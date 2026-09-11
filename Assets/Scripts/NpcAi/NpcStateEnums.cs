using UnityEngine;

namespace NpcAi
{
    public enum NpcStateNodeKind
    {
        Start,
        End,
        Wait,
        CharacterAction,
        SceneAction,
        WaitEvent,
        RandomBranch
    }

    public enum NpcStateOutcome
    {
        None,
        Completed,
        Failed,
        Interrupted
    }

    public enum NpcStateCharacterAction
    {
        PlayIdle,
        PlayRun,
        SetAnimSpeed,
        ClearAnimOverride,
        EnableLocomotion,
        DisableLocomotion,
        FaceLeft,
        FaceRight,
        FacePlayerLeft,
        FacePlayerRight,
        ForceJump,
        RunLeft,
        RunRight,
        RunForward,
        RunBackward,
        RunTo,
        RunToOverDuration,
        StopScriptedRun,
        SmoothMoveLeft,
        SmoothMoveRight,
        SmoothMoveForward,
        SmoothMoveBackward,
        StopSmoothMotion,
        Raise,
        [InspectorName("Run Random Direction")]
        RunRandomLeftRight,
        RotateByX,
        RotateByY,
        RotateByZ,
        SmoothRotateByX,
        SmoothRotateByY,
        SmoothRotateByZ,
        SmoothRotateToX,
        SmoothRotateToY,
        SmoothRotateToZ,
        [InspectorName("Run To GameObject")]
        RunToGameObject,
        Follow,
        LookAt,
        SmoothLookAt,
        MaintainLookAt
    }
}
