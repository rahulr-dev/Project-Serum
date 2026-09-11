using System;
using UnityEngine;

namespace Character
{
    public interface ICharacterLocomotion : INormalizedMoveSpeed, IJumpEvents
    {
        event Action<bool> OnMovingChanged;
        event Action OnScriptedRunStarted;
        event Action OnScriptedRunCompleted;

        bool IsScriptedRunning { get; }
        float MoveSpeed { get; }

        void SetLocomotionEnabled(bool enabled);
        void ForceJump();
        void FaceLeft();
        void FaceRight();
        void SetFacing(float yaw);
        void StartScriptedRunAtSpeed(Vector3 worldOffset, float speed);
        void StopScriptedRun();
    }
}
