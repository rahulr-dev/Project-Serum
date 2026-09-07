using UnityEditor;
using UnityEngine;

namespace QTE.Editor
{
    public static class QTEActionInspector
    {
        public static void DrawExecuteTarget(SerializedProperty executeHandlerIdProperty)
        {
            if (executeHandlerIdProperty == null)
                return;

            string handlerId = executeHandlerIdProperty.stringValue;
            QTEActionHandler currentHandler = QTEActionHandler.FindById(handlerId);
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
                    QTEActionHandler handler = GetOrAddHandler(picked);
                    executeHandlerIdProperty.stringValue = handler != null ? handler.HandlerId : "";
                }
            }

            if (picked == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a scene GameObject. QTEActionHandler will be added automatically if missing.",
                    MessageType.Info);
                return;
            }

            QTEActionHandler activeHandler = GetOrAddHandler(picked);
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

        static QTEActionHandler GetOrAddHandler(GameObject picked)
        {
            QTEActionHandler handler = picked.GetComponent<QTEActionHandler>();
            if (handler != null)
                return handler;

            return Undo.AddComponent<QTEActionHandler>(picked);
        }
    }
}
