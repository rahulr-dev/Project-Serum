using NpcAi;
using UnityEditor;
using UnityEngine;

namespace NpcAi.Editor
{
    [CustomEditor(typeof(NpcSpotTrigger))]
    public class NpcSpotTriggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "interruptNodeId", "m_Script");

            NpcStateMachineGraph graph = null;
            SerializedProperty machineProp = serializedObject.FindProperty("machine");
            NpcStateMachine machine = machineProp != null ? machineProp.objectReferenceValue as NpcStateMachine : null;
            if (machine != null)
                graph = machine.Graph;

            NpcBrainStateNodePicker.Draw(
                serializedObject.FindProperty("interruptNodeId"),
                graph,
                new GUIContent(
                    "Interrupt When",
                    "Only interrupt if this brain State node is currently playing. Destination is that node's Interrupt pin."));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
