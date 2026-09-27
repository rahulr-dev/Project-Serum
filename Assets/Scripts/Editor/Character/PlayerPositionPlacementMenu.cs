using Character;
using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    /// <summary>
    /// Main-toolbar action for placing the side-scroller player on clicked scene geometry.
    /// </summary>
    static class PlayerPositionPlacementMenu
    {
        const string UndoName = "Swap Player Position";
        static bool isPlacing;

        [InitializeOnLoadMethod]
        static void RegisterSceneGui()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.duringSceneGui += OnSceneGui;
        }

        [MenuItem("Player/Swap Player Position", false, 0)]
        static void Activate()
        {
            isPlacing = true;
            SceneView.lastActiveSceneView?.ShowNotification(
                new GUIContent("Click scene geometry to place the player."));
        }

        static void OnSceneGui(SceneView sceneView)
        {
            if (!isPlacing)
                return;

            Event currentEvent = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
            {
                isPlacing = false;
                sceneView.ShowNotification(new GUIContent("Player placement cancelled."));
                currentEvent.Use();
                return;
            }

            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0 || currentEvent.alt)
                return;

            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                sceneView.ShowNotification(new GUIContent("No collider under the cursor."));
                currentEvent.Use();
                return;
            }

            SideScrollerController player = Object.FindFirstObjectByType<SideScrollerController>();
            if (player == null)
            {
                sceneView.ShowNotification(new GUIContent("No SideScrollerController found in the open scene."));
                currentEvent.Use();
                return;
            }

            Undo.RecordObject(player.transform, UndoName);
            Vector3 playerPosition = player.transform.position;
            player.transform.position = new Vector3(hit.point.x, hit.point.y, playerPosition.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);

            isPlacing = false;
            sceneView.ShowNotification(new GUIContent("Player position updated."));
            currentEvent.Use();
        }
    }
}
