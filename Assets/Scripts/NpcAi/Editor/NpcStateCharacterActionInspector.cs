using NpcAi;
using UnityEditor;
using UnityEngine;

namespace NpcAi.Editor
{
    public static class NpcStateCharacterActionInspector
    {
        public static void Draw(SerializedProperty node)
        {
            if (node == null)
                return;

            EditorGUILayout.PropertyField(node.FindPropertyRelative("characterAction"), new GUIContent("Action"));

            NpcStateCharacterAction action = (NpcStateCharacterAction)node.FindPropertyRelative("characterAction").enumValueIndex;
            switch (action)
            {
                case NpcStateCharacterAction.SetAnimSpeed:
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("animSpeed"));
                    break;
                case NpcStateCharacterAction.RunLeft:
                case NpcStateCharacterAction.RunRight:
                case NpcStateCharacterAction.RunForward:
                case NpcStateCharacterAction.RunBackward:
                case NpcStateCharacterAction.RunRandomLeftRight:
                case NpcStateCharacterAction.SmoothMoveLeft:
                case NpcStateCharacterAction.SmoothMoveRight:
                case NpcStateCharacterAction.SmoothMoveForward:
                case NpcStateCharacterAction.SmoothMoveBackward:
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("distance"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("speed"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("waitUntilDone"));
                    break;
                case NpcStateCharacterAction.RunTo:
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("offsetX"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("offsetZ"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("speed"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("waitUntilDone"));
                    break;
                case NpcStateCharacterAction.RunToOverDuration:
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("offsetX"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("offsetZ"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("duration"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("waitUntilDone"));
                    break;
                case NpcStateCharacterAction.RunToGameObject:
                    DrawMoveTarget(node.FindPropertyRelative("moveTargetId"), "The NPC will run to that object's transform.");
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("speed"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("waitUntilDone"));
                    break;
                case NpcStateCharacterAction.Follow:
                    DrawMoveTarget(node.FindPropertyRelative("moveTargetId"), "The NPC will follow that object until it is within Stop Distance.");
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("speed"));
                    EditorGUILayout.PropertyField(
                        node.FindPropertyRelative("stopDistance"),
                        new GUIContent("Stop Distance", "How close the NPC gets before Follow ends."));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("waitUntilDone"));
                    break;
                case NpcStateCharacterAction.LookAt:
                    DrawMoveTarget(node.FindPropertyRelative("moveTargetId"), "The NPC instantly faces that object.");
                    break;
                case NpcStateCharacterAction.SmoothLookAt:
                    DrawMoveTarget(node.FindPropertyRelative("moveTargetId"), "The NPC turns to face that object.");
                    EditorGUILayout.PropertyField(
                        node.FindPropertyRelative("rotateSpeed"),
                        new GUIContent("Speed (deg/s)", "Turn speed in degrees per second."));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("waitUntilDone"));
                    break;
                case NpcStateCharacterAction.MaintainLookAt:
                    DrawMoveTarget(node.FindPropertyRelative("moveTargetId"), "The NPC keeps facing that object.");
                    EditorGUILayout.PropertyField(
                        node.FindPropertyRelative("duration"),
                        new GUIContent("Duration", "Seconds to keep looking. 0 = until the state is interrupted."));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("waitUntilDone"));
                    break;
                case NpcStateCharacterAction.Raise:
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("eventId"));
                    break;
                case NpcStateCharacterAction.RotateByX:
                case NpcStateCharacterAction.RotateByY:
                case NpcStateCharacterAction.RotateByZ:
                    EditorGUILayout.PropertyField(
                        node.FindPropertyRelative("degrees"),
                        new GUIContent("Degrees", "Relative rotation around this world axis."));
                    break;
                case NpcStateCharacterAction.SmoothRotateByX:
                case NpcStateCharacterAction.SmoothRotateByY:
                case NpcStateCharacterAction.SmoothRotateByZ:
                    EditorGUILayout.PropertyField(
                        node.FindPropertyRelative("degrees"),
                        new GUIContent("Degrees", "Relative rotation around this world axis."));
                    EditorGUILayout.PropertyField(
                        node.FindPropertyRelative("rotateSpeed"),
                        new GUIContent("Speed (deg/s)", "Rotation speed in degrees per second. 360 is a typical turn."));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("waitUntilDone"));
                    break;
                case NpcStateCharacterAction.SmoothRotateToX:
                case NpcStateCharacterAction.SmoothRotateToY:
                case NpcStateCharacterAction.SmoothRotateToZ:
                    EditorGUILayout.PropertyField(
                        node.FindPropertyRelative("degrees"),
                        new GUIContent("Degrees", "Target world euler angle on this axis."));
                    EditorGUILayout.PropertyField(
                        node.FindPropertyRelative("rotateSpeed"),
                        new GUIContent("Speed (deg/s)", "Rotation speed in degrees per second. 360 is a typical turn."));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("waitUntilDone"));
                    break;
            }
        }

        static void DrawMoveTarget(SerializedProperty moveTargetIdProperty, string usage)
        {
            if (moveTargetIdProperty == null)
                return;

            string targetId = moveTargetIdProperty.stringValue;
            NpcMoveTarget currentTarget = NpcMoveTarget.FindById(targetId);
            GameObject current = currentTarget != null ? currentTarget.gameObject : null;

            EditorGUI.BeginChangeCheck();
            GameObject picked = EditorGUILayout.ObjectField("Target GameObject", current, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                if (picked == null)
                {
                    moveTargetIdProperty.stringValue = "";
                }
                else
                {
                    NpcMoveTarget target = GetOrAddMoveTarget(picked);
                    moveTargetIdProperty.stringValue = target != null ? target.TargetId : "";
                }
            }

            if (picked == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a scene GameObject. NpcMoveTarget is added automatically if missing. " + usage,
                    MessageType.Info);
                return;
            }

            if (!picked.scene.IsValid())
            {
                moveTargetIdProperty.stringValue = "";
                EditorGUILayout.HelpBox("Pick a GameObject from the open scene, not a prefab asset.", MessageType.Warning);
                return;
            }

            NpcMoveTarget activeTarget = GetOrAddMoveTarget(picked);
            if (activeTarget == null)
                return;

            if (moveTargetIdProperty.stringValue != activeTarget.TargetId)
                moveTargetIdProperty.stringValue = activeTarget.TargetId;

            EditorGUILayout.LabelField("Target Id", activeTarget.TargetId);
        }

        static NpcMoveTarget GetOrAddMoveTarget(GameObject picked)
        {
            if (picked == null)
                return null;

            NpcMoveTarget target = picked.GetComponent<NpcMoveTarget>();
            if (target == null)
                target = Undo.AddComponent<NpcMoveTarget>(picked);

            target?.EnsureRegistered();
            if (picked.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(picked.scene);

            EditorUtility.SetDirty(picked);
            if (target != null)
                EditorUtility.SetDirty(target);

            return target;
        }
    }
}
