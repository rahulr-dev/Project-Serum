using System;
using UnityEngine;

namespace NpcAi
{
    [Serializable]
    public class NpcStateMachineNodeData
    {
        public string id;
        public NpcStateMachineNodeKind kind;
        public Vector2 position;
        public NpcStateGraph stateGraph;
        public string actionId = "";
        public string nodeName = "";

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(nodeName))
                    return nodeName;

                if (kind == NpcStateMachineNodeKind.State && stateGraph != null && !string.IsNullOrEmpty(stateGraph.name))
                    return stateGraph.name;

                if (kind == NpcStateMachineNodeKind.Action && !string.IsNullOrEmpty(actionId))
                    return actionId;

                return kind.ToString();
            }
        }

        public bool MatchesKey(string nodeIdOrName)
        {
            if (string.IsNullOrEmpty(nodeIdOrName))
                return false;

            if (id == nodeIdOrName)
                return true;

            return !string.IsNullOrEmpty(nodeName) &&
                   string.Equals(nodeName, nodeIdOrName, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Serializable]
    public class NpcStateMachineEdgeData
    {
        public string fromId;
        public int fromPort;
        public string toId;
    }
}
