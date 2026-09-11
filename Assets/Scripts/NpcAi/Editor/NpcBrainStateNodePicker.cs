using System.Collections.Generic;
using NpcAi;
using UnityEditor;
using UnityEngine;

namespace NpcAi.Editor
{
    public static class NpcBrainStateNodePicker
    {
        public static void Draw(SerializedProperty nodeIdProperty, NpcStateMachineGraph graph, GUIContent label)
        {
            if (nodeIdProperty == null)
                return;

            if (graph == null || graph.nodes == null)
            {
                EditorGUILayout.PropertyField(
                    nodeIdProperty,
                    new GUIContent(label.text + " (Name / Id)", label.tooltip));
                return;
            }

            List<NpcStateMachineNodeData> states = CollectStates(graph);
            if (states.Count == 0)
            {
                EditorGUILayout.PropertyField(
                    nodeIdProperty,
                    new GUIContent(label.text + " (Name / Id)", label.tooltip));
                EditorGUILayout.HelpBox("This brain has no State nodes.", MessageType.Info);
                return;
            }

            string[] options = new string[states.Count + 1];
            options[0] = "None";
            int selected = 0;
            string current = nodeIdProperty.stringValue ?? "";

            for (int i = 0; i < states.Count; i++)
            {
                NpcStateMachineNodeData node = states[i];
                options[i + 1] = LabelFor(node, states);
                if (!string.IsNullOrEmpty(current) && node.MatchesKey(current))
                    selected = i + 1;
            }

            if (!string.IsNullOrEmpty(current) && selected == 0)
                options[0] = "None (unknown: " + current + ")";

            int next = EditorGUILayout.Popup(label, selected, options);
            if (next != selected)
                nodeIdProperty.stringValue = next <= 0 ? "" : states[next - 1].id;
        }

        static List<NpcStateMachineNodeData> CollectStates(NpcStateMachineGraph graph)
        {
            var states = new List<NpcStateMachineNodeData>();
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                NpcStateMachineNodeData node = graph.nodes[i];
                if (node != null && node.kind == NpcStateMachineNodeKind.State)
                    states.Add(node);
            }

            return states;
        }

        static string LabelFor(NpcStateMachineNodeData node, List<NpcStateMachineNodeData> states)
        {
            string name = node.DisplayName;
            int copies = 0;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i] != null && states[i].DisplayName == name)
                    copies++;
            }

            if (copies > 1 && !string.IsNullOrEmpty(node.id) && node.id.Length >= 8)
                return name + " (" + node.id.Substring(0, 8) + ")";

            return name;
        }
    }
}
