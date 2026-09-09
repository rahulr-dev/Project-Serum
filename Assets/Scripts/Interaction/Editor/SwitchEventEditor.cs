using UnityEditor;
using UnityEngine;

namespace InteractionSystem.Editor
{
    [CustomEditor(typeof(InvokeEvent), true)]
    public class InvokeEventEditor : UnityEditor.Editor
    {
        private SerializedProperty onInvokeProp;

        private void OnEnable()
        {
            onInvokeProp = serializedObject.FindProperty("onInvoke");
            if (onInvokeProp == null)
            {
                onInvokeProp = serializedObject.FindProperty("onSwitch");
            }
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
            EditorGUILayout.LabelField("Invoke Event (Universal Action Hub)", headerStyle);
            EditorGUILayout.LabelField("Configured actions will be executed when Play() is called.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // UnityEvent property drawer
            if (onInvokeProp != null)
            {
                EditorGUILayout.PropertyField(onInvokeProp, new GUIContent("On Invoke"));
            }

            serializedObject.ApplyModifiedProperties();

            // Quick Testing Button in Play Mode
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(6);
                if (GUILayout.Button("▶ Test Play()", GUILayout.Height(24)))
                {
                    ((InvokeEvent)target).Play();
                }
            }
        }
    }
}
