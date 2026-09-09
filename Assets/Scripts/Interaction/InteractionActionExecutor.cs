using UnityEngine;

namespace InteractionSystem
{
    public static class InteractionActionExecutor
    {
        public static void Execute(InteractionNodeData node)
        {
            if (node == null || string.IsNullOrEmpty(node.executeHandlerId))
                return;

            InteractionActionHandler handler = InteractionActionHandler.FindById(node.executeHandlerId);
            if (handler == null)
            {
                Debug.LogWarning($"Interaction Action could not find handler '{node.executeHandlerId}' for node {node.id}.");
                return;
            }

            handler.Execute();
        }
    }
}
