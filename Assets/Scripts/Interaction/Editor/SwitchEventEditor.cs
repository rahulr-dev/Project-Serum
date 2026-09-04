using UnityEditor;
using UnityEngine;

namespace InteractionSystem.Editor
{
    [CustomEditor(typeof(SwitchEvent))]
    public class SwitchEventEditor : UnityEditor.Editor
    {
        private SerializedProperty onSwitchProp;

        private void OnEnable()
        {
            onSwitchProp = serializedObject.FindProperty("onSwitch");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header Banner
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.3f, 0.7f, 1f) }
            };
            EditorGUILayout.LabelField("Switch Event (Universal Action Hub)", headerStyle);
            EditorGUILayout.LabelField("Click '+' below, drag a target component (Door, AudioSource, ParticleSystem, Animator, QTE, etc.), and select the method to execute.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // UnityEvent property drawer
            if (onSwitchProp != null)
            {
                EditorGUILayout.PropertyField(onSwitchProp, new GUIContent("On Switch"));
            }

            serializedObject.ApplyModifiedProperties();

            // Quick Testing Button in Play Mode
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(6);
                if (GUILayout.Button("▶ Test Play()", GUILayout.Height(24)))
                {
                    ((SwitchEvent)target).Play();
                }
            }
        }
    }
}
