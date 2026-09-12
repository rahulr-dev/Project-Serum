using NpcAi;
using UnityEditor;
using UnityEngine;

namespace NpcAi.Editor
{
    [CustomEditor(typeof(NpcStateMachineBridge))]
    public class NpcStateMachineBridgeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "interruptNodeId", "m_Script");

            NpcStateMachineGraph graph = null;
            SerializedProperty machineProp = serializedObject.FindProperty("machine");
            NpcStateMachine machine = machineProp != null ? machineProp.objectReferenceValue as NpcStateMachine : null;
            if (machine == null)
                machine = ((NpcStateMachineBridge)target).GetComponent<NpcStateMachine>();
            if (machine != null)
                graph = machine.Graph;

            NpcBrainStateNodePicker.Draw(
                serializedObject.FindProperty("interruptNodeId"),
                graph,
                new GUIContent(
                    "Interrupt When",
                    "InterruptIfCurrent() uses this node. InterruptIfCurrent(string) can pass another id or node name."));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
