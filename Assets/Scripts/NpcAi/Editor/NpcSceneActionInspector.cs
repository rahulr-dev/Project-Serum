using Events;
using NpcAi;
using UnityEditor;
using UnityEngine;

namespace NpcAi.Editor
{
    public static class NpcSceneActionInspector
    {
        public static void DrawExecuteTarget(SerializedProperty executeHandlerIdProperty)
        {
            if (executeHandlerIdProperty == null)
                return;

            string handlerId = executeHandlerIdProperty.stringValue;
            NpcSceneActionHandler currentHandler = NpcSceneActionHandler.FindById(handlerId);
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
                    NpcSceneActionHandler handler = GetOrAddHandler(picked);
                    executeHandlerIdProperty.stringValue = handler != null ? handler.HandlerId : "";
                }
            }

            if (picked == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a scene GameObject. NpcSceneActionHandler will be added automatically if missing.",
                    MessageType.Info);
                return;
            }

            NpcSceneActionHandler activeHandler = GetOrAddHandler(picked);
            if (activeHandler == null)
                return;

            if (executeHandlerIdProperty.stringValue != activeHandler.HandlerId)
                executeHandlerIdProperty.stringValue = activeHandler.HandlerId;

            EditorGUILayout.LabelField("Handler Id", activeHandler.HandlerId);

            SerumActionBridge bridge = picked.GetComponent<SerumActionBridge>();
            if (bridge != null)
            {
                EditorGUILayout.HelpBox(
                    "SerumActionBridge methods:\n" +
                    "Transform: SmoothMoveLeft/Right/Forward/Backward (distance, speed), StopSmoothMotion\n" +
                    "Events: Raise (string id)\n" +
                    "Character: PlayIdle, PlayRun, RunLeft/Right/Forward/Backward (distance, speed), " +
                    "RunTo (offsetX, offsetZ, speed), ForceJump, EnableLocomotion, DisableLocomotion, " +
                    "FacePlayerLeft, FacePlayerRight",
                    MessageType.Info);
            }

            SerializedObject handlerSo = new SerializedObject(activeHandler);
            handlerSo.Update();
            EditorGUILayout.LabelField("On Execute", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(handlerSo.FindProperty("onExecute"));
            handlerSo.ApplyModifiedProperties();
        }

        static NpcSceneActionHandler GetOrAddHandler(GameObject picked)
        {
            NpcSceneActionHandler handler = picked.GetComponent<NpcSceneActionHandler>();
            if (handler != null)
                return handler;

            return Undo.AddComponent<NpcSceneActionHandler>(picked);
        }
    }
}
