using UnityEditor;
using UnityEngine;

namespace InteractionSystem.Editor
{
    public class InteractablesOverlayWindow : EditorWindow
    {
        Vector2 _scroll;

        [MenuItem("Serum/Interactables Overlay Window", false, 26)]
        public static void Open()
        {
            InteractablesOverlayWindow window = GetWindow<InteractablesOverlayWindow>();
            window.titleContent = new GUIContent("Interactables Overlay");
            window.minSize = new Vector2(320f, 220f);
        }

        void OnEnable()
        {
            autoRepaintOnSceneChange = true;
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see live overlap from PlayerInteractor.", MessageType.Info);
                DrawSceneInteractables();
                return;
            }

            PlayerInteractor interactor = PlayerInteractor.Instance;
            if (interactor == null)
                interactor = FindFirstObjectByType<PlayerInteractor>();

            if (interactor == null)
            {
                EditorGUILayout.HelpBox("No PlayerInteractor in the scene.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Player Interactor", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Component", interactor, typeof(PlayerInteractor), true);

            Interactable current = interactor.Current;
            EditorGUILayout.ObjectField("Touching object", current != null ? current.gameObject : null, typeof(GameObject), true);
            EditorGUILayout.ObjectField("Touching collider", interactor.CurrentCollider, typeof(Collider), true);

            InteractionGraph graph = current != null ? current.Graph : null;
            EditorGUILayout.ObjectField("Interaction graph", graph, typeof(InteractionGraph), false);
            EditorGUILayout.LabelField("Graph asset", graph != null ? graph.name : "—");
            EditorGUILayout.LabelField("Running", current != null && current.IsRunning ? "Yes" : "No");

            if (graph != null && GUILayout.Button("Ping graph"))
                EditorGUIUtility.PingObject(graph);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Overlaps ({interactor.Overlaps.Count})", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < interactor.Overlaps.Count; i++)
            {
                PlayerInteractor.OverlapHit hit = interactor.Overlaps[i];
                Interactable interactable = hit.Interactable;
                GameObject go = interactable != null ? interactable.gameObject : (hit.Collider != null ? hit.Collider.gameObject : null);
                InteractionGraph hitGraph = interactable != null ? interactable.Graph : null;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.ObjectField("Object", go, typeof(GameObject), true);
                EditorGUILayout.ObjectField("Collider", hit.Collider, typeof(Collider), true);
                EditorGUILayout.Toggle("Is trigger", hit.Collider != null && hit.Collider.isTrigger);
                EditorGUILayout.ObjectField("Graph", hitGraph, typeof(InteractionGraph), false);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawSceneInteractables()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Interactables", EditorStyles.boldLabel);

            Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < interactables.Length; i++)
            {
                Interactable interactable = interactables[i];
                if (interactable == null)
                    continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.ObjectField("Object", interactable.gameObject, typeof(GameObject), true);
                EditorGUILayout.ObjectField("Graph", interactable.Graph, typeof(InteractionGraph), false);
                EditorGUILayout.LabelField("Active", interactable.isActiveAndEnabled ? "Yes" : "No");
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
