using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

public static class CameraZoneCreator
{
    [MenuItem("GameObject/Camera/Setup Trigger Zone", false, 0)]
    static void SetupTriggerZone()
    {
        var camGO = Selection.activeGameObject;
        ShotNameDialog.Open(camGO);
    }

    [MenuItem("GameObject/Camera/Setup Trigger Zone", true)]
    static bool ValidateSetupTriggerZone()
    {
        return Selection.activeGameObject != null
            && Selection.activeGameObject.GetComponent<CinemachineCamera>() != null;
    }

    public static void Build(GameObject camGO, string contextName)
    {
        int shotNumber = GameObject.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None).Length - 1;
        string index = shotNumber.ToString("D2");

        var shot = new GameObject($"SHOT_{index}_{contextName}");
        var zone = new GameObject($"TRIGGER_{contextName}");
        var blender = zone.AddComponent<CinemachineCameraBlender>();
        var col = zone.GetComponent<BoxCollider>();

        col.size = new Vector3(10f, 6f, 20f);
        zone.transform.position = new Vector3(0f,
            camGO.transform.position.y,
            camGO.transform.position.z);

        Undo.RegisterCreatedObjectUndo(shot, "Setup Trigger Zone");
        Undo.SetTransformParent(camGO.transform, shot.transform, "Setup Trigger Zone");
        Undo.SetTransformParent(zone.transform, shot.transform, "Setup Trigger Zone");

        camGO.name = $"CAM_{contextName}";

        var so = new SerializedObject(blender);
        so.FindProperty("cam").objectReferenceValue = camGO.GetComponent<CinemachineCamera>();
        so.ApplyModifiedProperties();

        Selection.activeGameObject = shot;
        SceneView.FrameLastActiveSceneView();
    }
}

public class ShotNameDialog : EditorWindow
{
    private string contextName = "Location";
    private GameObject camGO;

    public static void Open(GameObject camGO)
    {
        var window = GetWindow<ShotNameDialog>(true, "Name This Shot", true);
        window.camGO = camGO;
        window.minSize = new Vector2(300f, 90f);
        window.maxSize = new Vector2(300f, 90f);
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Shot Context Name", EditorStyles.boldLabel);
        contextName = EditorGUILayout.TextField(contextName);
        EditorGUILayout.Space(6f);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(contextName)))
        {
            if (GUILayout.Button("Create"))
            {
                CameraZoneCreator.Build(camGO, contextName.Trim());
                Close();
            }
        }
    }
}
