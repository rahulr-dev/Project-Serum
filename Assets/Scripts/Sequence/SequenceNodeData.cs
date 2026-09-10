using System;
using UnityEngine;

namespace SequenceSystem
{
    [Serializable]
    public class SequenceNodeData
    {
        public string id;
        public SequenceNodeKind kind;
        public Vector2 position;
        public string actionId = "";
        public string conditionKey = "default";
        public string ignoreKeys = "";

        public bool IgnoresKey(string key)
        {
            if (string.IsNullOrEmpty(ignoreKeys) || string.IsNullOrEmpty(key))
                return false;

            string[] parts = ignoreKeys.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.Equals(parts[i].Trim(), key, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }

    [Serializable]
    public class SequenceEdgeData
    {
        public string fromId;
        public int fromPort;
        public string toId;
    }
}
