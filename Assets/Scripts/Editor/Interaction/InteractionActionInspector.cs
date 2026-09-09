using UnityEditor;
using UnityEngine;

namespace InteractionSystem.Editor
{
    public static class InteractionActionInspector
    {
        public static void DrawExecuteTarget(SerializedProperty executeHandlerIdProperty)
        {
            if (executeHandlerIdProperty == null)
                return;

            string handlerId = executeHandlerIdProperty.stringValue;
            InteractionActionHandler currentHandler = InteractionActionHandler.FindById(handlerId);
            GameObject current = currentHandler != null ? currentHandler.gameObject : null;

            EditorGUI.BeginChangeCheck();
            GameObject picked = EditorGUILayout.ObjectField("Target GameObject", current, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                if (picked == null)
                {
                    executeHandlerIdProperty.stringValue = "";
                }
                else
                {
                    InteractionActionHandler handler = GetOrAddHandler(picked);
                    executeHandlerIdProperty.stringValue = handler != null ? handler.HandlerId : "";
                }
            }

            if (picked == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a scene GameObject. InteractionActionHandler will be added automatically if missing.",
                    MessageType.Info);
                return;
            }

            InteractionActionHandler activeHandler = GetOrAddHandler(picked);
            if (activeHandler == null)
                return;

            if (executeHandlerIdProperty.stringValue != activeHandler.HandlerId)
                executeHandlerIdProperty.stringValue = activeHandler.HandlerId;

            EditorGUILayout.LabelField("Handler Id", activeHandler.HandlerId);

            SerializedObject handlerSo = new SerializedObject(activeHandler);
            handlerSo.Update();
            EditorGUILayout.LabelField("On Execute", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(handlerSo.FindProperty("onExecute"));
            handlerSo.ApplyModifiedProperties();
        }

        static InteractionActionHandler GetOrAddHandler(GameObject picked)
        {
            InteractionActionHandler handler = picked.GetComponent<InteractionActionHandler>();
            if (handler != null)
                return handler;

            return Undo.AddComponent<InteractionActionHandler>(picked);
        }
    }
}
