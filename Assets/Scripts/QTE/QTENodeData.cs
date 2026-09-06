using System;
using System.Collections.Generic;
using UnityEngine;

namespace QTE
{
    [Serializable]
    public class QTENodeData
    {
        public string id;
        public QTENodeKind kind;
        public Vector2 position;

        public float duration = 1f;

        public string executeHandlerId = "";

        public string promptText = "";
        public QTEInputKind requiredInput = QTEInputKind.Jump;
        public float windowDuration = 1f;
        public bool failOnTimeout = true;

        public float holdDuration = 0.5f;

        public int targetCount = 5;

        public List<QTEInputKind> inputSequence = new List<QTEInputKind>();
        public float windowPerStep = 1f;
        public float totalWindow = 3f;

        public List<string> childNodeIds = new List<string>();

        public QTEBranchMode branchMode = QTEBranchMode.LastNodeResult;
        public List<string> branchLabels = new List<string>();

        public QTEOutcome endOutcome = QTEOutcome.Success;
    }

    [Serializable]
    public class QTEEdgeData
    {
        public string fromId;
        public int fromPort;
        public string toId;
    }
}
