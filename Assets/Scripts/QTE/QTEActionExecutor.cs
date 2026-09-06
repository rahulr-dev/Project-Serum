using UnityEngine;

namespace QTE
{
    public static class QTEActionExecutor
    {
        public static void Execute(QTENodeData node)
        {
            if (node == null || string.IsNullOrEmpty(node.executeHandlerId))
                return;

            QTEActionHandler handler = QTEActionHandler.FindById(node.executeHandlerId);
            if (handler == null)
            {
                Debug.LogWarning($"QTE Action could not find handler '{node.executeHandlerId}' for node {node.id}.");
                return;
            }

            handler.Execute();
        }
    }
}
