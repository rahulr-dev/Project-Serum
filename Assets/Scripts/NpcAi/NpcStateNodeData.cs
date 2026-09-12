using System;
using UnityEngine;

namespace NpcAi
{
    [Serializable]
    public class NpcStateNodeData
    {
        public string id;
        public NpcStateNodeKind kind;
        public Vector2 position;

        public float duration = 1f;

        public NpcStateCharacterAction characterAction = NpcStateCharacterAction.PlayIdle;
        public float distance = 4f;
        public float speed = 4f;
        public float offsetX;
        public float offsetZ;
        public float animSpeed = 1f;
        public float degrees = 45f;
        public float rotateSpeed = 360f;
        public string eventId = "";
        public bool waitUntilDone = true;

        public string executeHandlerId = "";
        public string moveTargetId = "";
        public int branchCount = 2;
        public float stopDistance = 0.4f;

        public string waitEventId = "ScriptedRunEnded";

        public NpcStateOutcome endOutcome = NpcStateOutcome.Completed;

        public const int MinBranchCount = 2;
        public const int MaxBranchCount = 8;

        public int GetBranchCount()
        {
            if (branchCount < MinBranchCount)
                return MinBranchCount;

            return branchCount > MaxBranchCount ? MaxBranchCount : branchCount;
        }

        public static string BranchPortName(int index)
        {
            if (index < 0 || index > 25)
                return (index + 1).ToString();

            return ((char)('A' + index)).ToString();
        }
    }

    [Serializable]
    public class NpcStateEdgeData
    {
        public string fromId;
        public int fromPort;
        public string toId;
    }
}
