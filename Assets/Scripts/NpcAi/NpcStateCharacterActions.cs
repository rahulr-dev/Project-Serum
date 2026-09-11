using Events;
using UnityEngine;

namespace NpcAi
{
    public static class NpcStateCharacterActions
    {
        public const string ScriptedRunEnded = "ScriptedRunEnded";
        public const string SmoothMoveEnded = "SmoothMoveEnded";
        public const string SmoothRotateEnded = "SmoothRotateEnded";
        public const string FollowEnded = "FollowEnded";
        public const string MaintainLookAtEnded = "MaintainLookAtEnded";
        const float DefaultRotateSpeed = 360f;

        public static bool TryGetCompletionEvent(NpcStateCharacterAction action, out string eventId)
        {
            switch (action)
            {
                case NpcStateCharacterAction.RunLeft:
                case NpcStateCharacterAction.RunRight:
                case NpcStateCharacterAction.RunForward:
                case NpcStateCharacterAction.RunBackward:
                case NpcStateCharacterAction.RunTo:
                case NpcStateCharacterAction.RunToOverDuration:
                case NpcStateCharacterAction.RunRandomLeftRight:
                case NpcStateCharacterAction.RunToGameObject:
                    eventId = ScriptedRunEnded;
                    return true;
                case NpcStateCharacterAction.Follow:
                    eventId = FollowEnded;
                    return true;
                case NpcStateCharacterAction.SmoothMoveLeft:
                case NpcStateCharacterAction.SmoothMoveRight:
                case NpcStateCharacterAction.SmoothMoveForward:
                case NpcStateCharacterAction.SmoothMoveBackward:
                    eventId = SmoothMoveEnded;
                    return true;
                case NpcStateCharacterAction.SmoothRotateByX:
                case NpcStateCharacterAction.SmoothRotateByY:
                case NpcStateCharacterAction.SmoothRotateByZ:
                case NpcStateCharacterAction.SmoothRotateToX:
                case NpcStateCharacterAction.SmoothRotateToY:
                case NpcStateCharacterAction.SmoothRotateToZ:
                    eventId = SmoothRotateEnded;
                    return true;
                case NpcStateCharacterAction.SmoothLookAt:
                    eventId = SmoothRotateEnded;
                    return true;
                case NpcStateCharacterAction.MaintainLookAt:
                    eventId = MaintainLookAtEnded;
                    return true;
                default:
                    eventId = null;
                    return false;
            }
        }

        public static bool IsWaitingAfterExecute(SerumActionBridge bridge, NpcStateCharacterAction action)
        {
            if (bridge == null)
                return false;

            if (!TryGetCompletionEvent(action, out string eventId))
                return false;

            if (eventId == ScriptedRunEnded)
                return bridge.IsScriptedRunning;

            if (eventId == FollowEnded)
                return bridge.IsFollowing;

            if (eventId == SmoothRotateEnded)
                return bridge.IsSmoothRotating;

            if (eventId == MaintainLookAtEnded)
                return bridge.IsLooking;

            return bridge.IsSmoothMoving;
        }

        public static void Execute(SerumActionBridge bridge, NpcStateNodeData node)
        {
            if (node == null)
                return;

            if (bridge == null)
            {
                Debug.LogWarning("[NpcState] CharacterAction requires a SerumActionBridge on the actor.");
                return;
            }

            switch (node.characterAction)
            {
                case NpcStateCharacterAction.PlayIdle:
                    bridge.PlayIdle();
                    break;
                case NpcStateCharacterAction.PlayRun:
                    bridge.PlayRun();
                    break;
                case NpcStateCharacterAction.SetAnimSpeed:
                    bridge.SetAnimSpeed(node.animSpeed);
                    break;
                case NpcStateCharacterAction.ClearAnimOverride:
                    bridge.ClearAnimOverride();
                    break;
                case NpcStateCharacterAction.EnableLocomotion:
                    bridge.EnableLocomotion();
                    break;
                case NpcStateCharacterAction.DisableLocomotion:
                    bridge.DisableLocomotion();
                    break;
                case NpcStateCharacterAction.FaceLeft:
                    bridge.FaceLeft();
                    break;
                case NpcStateCharacterAction.FaceRight:
                    bridge.FaceRight();
                    break;
                case NpcStateCharacterAction.FacePlayerLeft:
                    bridge.FacePlayerLeft();
                    break;
                case NpcStateCharacterAction.FacePlayerRight:
                    bridge.FacePlayerRight();
                    break;
                case NpcStateCharacterAction.ForceJump:
                    bridge.ForceJump();
                    break;
                case NpcStateCharacterAction.RunLeft:
                    bridge.RunLeft(node.distance, node.speed);
                    break;
                case NpcStateCharacterAction.RunRight:
                    bridge.RunRight(node.distance, node.speed);
                    break;
                case NpcStateCharacterAction.RunForward:
                    bridge.RunForward(node.distance, node.speed);
                    break;
                case NpcStateCharacterAction.RunBackward:
                    bridge.RunBackward(node.distance, node.speed);
                    break;
                case NpcStateCharacterAction.RunTo:
                    bridge.RunTo(node.offsetX, node.offsetZ, node.speed);
                    break;
                case NpcStateCharacterAction.RunToOverDuration:
                    bridge.RunToOverDuration(node.offsetX, node.offsetZ, node.duration);
                    break;
                case NpcStateCharacterAction.RunToGameObject:
                    ExecuteOnTarget(node, "RunToGameObject", target =>
                        bridge.RunToWorld(target.position, node.speed));
                    break;
                case NpcStateCharacterAction.Follow:
                    ExecuteOnTarget(node, "Follow", target =>
                        bridge.Follow(target, node.speed, node.stopDistance, 0f));
                    break;
                case NpcStateCharacterAction.LookAt:
                    ExecuteOnTarget(node, "LookAt", target => bridge.LookAt(target));
                    break;
                case NpcStateCharacterAction.SmoothLookAt:
                    ExecuteOnTarget(node, "SmoothLookAt", target =>
                        bridge.SmoothLookAt(target, RotateSpeed(node)));
                    break;
                case NpcStateCharacterAction.MaintainLookAt:
                    ExecuteOnTarget(node, "MaintainLookAt", target =>
                        bridge.MaintainLookAt(target, node.duration));
                    break;
                case NpcStateCharacterAction.RunRandomLeftRight:
                    switch (Random.Range(0, 4))
                    {
                        case 0:
                            bridge.RunLeft(node.distance, node.speed);
                            break;
                        case 1:
                            bridge.RunRight(node.distance, node.speed);
                            break;
                        case 2:
                            bridge.RunForward(node.distance, node.speed);
                            break;
                        default:
                            bridge.RunBackward(node.distance, node.speed);
                            break;
                    }
                    break;
                case NpcStateCharacterAction.StopScriptedRun:
                    bridge.StopScriptedRun();
                    break;
                case NpcStateCharacterAction.SmoothMoveLeft:
                    bridge.SmoothMoveLeft(node.distance, node.speed);
                    break;
                case NpcStateCharacterAction.SmoothMoveRight:
                    bridge.SmoothMoveRight(node.distance, node.speed);
                    break;
                case NpcStateCharacterAction.SmoothMoveForward:
                    bridge.SmoothMoveForward(node.distance, node.speed);
                    break;
                case NpcStateCharacterAction.SmoothMoveBackward:
                    bridge.SmoothMoveBackward(node.distance, node.speed);
                    break;
                case NpcStateCharacterAction.StopSmoothMotion:
                    bridge.StopSmoothMotion();
                    break;
                case NpcStateCharacterAction.Raise:
                    bridge.Raise(node.eventId);
                    break;
                case NpcStateCharacterAction.RotateByX:
                    bridge.RotateByX(node.degrees);
                    break;
                case NpcStateCharacterAction.RotateByY:
                    bridge.RotateByY(node.degrees);
                    break;
                case NpcStateCharacterAction.RotateByZ:
                    bridge.RotateByZ(node.degrees);
                    break;
                case NpcStateCharacterAction.SmoothRotateByX:
                    bridge.SmoothRotateByX(node.degrees, RotateSpeed(node));
                    break;
                case NpcStateCharacterAction.SmoothRotateByY:
                    bridge.SmoothRotateByY(node.degrees, RotateSpeed(node));
                    break;
                case NpcStateCharacterAction.SmoothRotateByZ:
                    bridge.SmoothRotateByZ(node.degrees, RotateSpeed(node));
                    break;
                case NpcStateCharacterAction.SmoothRotateToX:
                    bridge.SmoothRotateToX(node.degrees, RotateSpeed(node));
                    break;
                case NpcStateCharacterAction.SmoothRotateToY:
                    bridge.SmoothRotateToY(node.degrees, RotateSpeed(node));
                    break;
                case NpcStateCharacterAction.SmoothRotateToZ:
                    bridge.SmoothRotateToZ(node.degrees, RotateSpeed(node));
                    break;
            }
        }

        static float RotateSpeed(NpcStateNodeData node)
        {
            return node != null && node.rotateSpeed > 0.01f ? node.rotateSpeed : DefaultRotateSpeed;
        }

        static void ExecuteOnTarget(NpcStateNodeData node, string actionName, System.Action<Transform> execute)
        {
            NpcMoveTarget target = NpcMoveTarget.FindById(node.moveTargetId);
            if (target == null)
            {
                Debug.LogWarning($"[NpcState] {actionName} could not find move target '{node.moveTargetId}'.");
                return;
            }

            execute(target.transform);
        }
    }
}
