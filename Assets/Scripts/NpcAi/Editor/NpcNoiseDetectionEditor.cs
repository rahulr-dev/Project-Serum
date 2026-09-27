using NpcAi;
using UnityEditor;
using UnityEngine;

namespace NpcAi.Editor
{
    [CustomEditor(typeof(NpcNoiseDetection))]
    public class NpcNoiseDetectionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "interruptNodeIds", "m_Script");

            NpcStateMachineGraph graph = null;
            SerializedProperty machineProp = serializedObject.FindProperty("machine");
            NpcStateMachine machine = machineProp != null ? machineProp.objectReferenceValue as NpcStateMachine : null;
            if (machine != null)
                graph = machine.Graph;

            SerializedProperty interruptNodes = serializedObject.FindProperty("interruptNodeIds");
            if (interruptNodes.arraySize == 0)
                interruptNodes.arraySize = 1;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Interrupt When", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The NPC will interrupt when any selected State node is currently playing. Each selected state's Interrupt pin should lead to Chase.",
                MessageType.None);

            for (int i = 0; i < interruptNodes.arraySize; i++)
            {
                SerializedProperty node = interruptNodes.GetArrayElementAtIndex(i);
                NpcBrainStateNodePicker.Draw(
                    node,
                    graph,
                    new GUIContent("State " + (i + 1)));

                if (interruptNodes.arraySize > 1 && GUILayout.Button("Remove State " + (i + 1)))
                {
                    interruptNodes.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            if (GUILayout.Button("Add Interrupt State"))
            {
                int index = interruptNodes.arraySize;
                interruptNodes.arraySize++;
                interruptNodes.GetArrayElementAtIndex(index).stringValue = "";
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
