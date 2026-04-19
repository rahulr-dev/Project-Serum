using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CinemachineCameraBlender : MonoBehaviour
{
    public enum BlendMode { Priority, PositionBased }

    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private BlendMode blendMode = BlendMode.PositionBased;

    [Header("Position Based")]
    [SerializeField] private AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Gizmo")]
    [SerializeField] private Color gizmoColor = new Color(0f, 0.8f, 1f, 0.2f);

    private BoxCollider trigger;
    private CinemachineBrain brain;
    private CinemachineCamera previousCam;
    private Transform player;
    private int overrideId = -1;

    private const int ACTIVE_PRIORITY = 99;
    private const int INACTIVE_PRIORITY = 0;

    void Start()
    {
        trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        transform.position = new Vector3(0f, transform.position.y, transform.position.z);
        brain = CinemachineBrain.GetActiveBrain(0);
        previousCam = FindPlayerCam();
        cam.Priority = INACTIVE_PRIORITY;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;
        player = other.transform;

        if (blendMode == BlendMode.Priority)
            cam.Priority = ACTIVE_PRIORITY;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;
        player = null;

        if (blendMode == BlendMode.Priority)
        {
            cam.Priority = INACTIVE_PRIORITY;
        }
        else
        {
            if (overrideId >= 0)
            {
                brain.ReleaseCameraOverride(overrideId);
                overrideId = -1;
            }
        }
    }

    void Update()
    {
        if (blendMode != BlendMode.PositionBased || player == null || brain == null) return;

        float weight = blendCurve.Evaluate(GetNormalizedDepth());

        overrideId = brain.SetCameraOverride(
            overrideId,
            priority: 0,
            camA: previousCam,
            camB: cam,
            weightB: weight,
            deltaTime: Time.deltaTime
        );
    }

    float GetNormalizedDepth()
    {
        Vector3 local = transform.InverseTransformPoint(player.position) - trigger.center;
        Vector3 half = trigger.size * 0.5f;

        float dx = 1f - Mathf.Clamp01(Mathf.Abs(local.x) / half.x);
        float dz = 1f - Mathf.Clamp01(Mathf.Abs(local.z) / half.z);

        return Mathf.Min(dx, dz);
    }

    CinemachineCamera FindPlayerCam()
    {
        var go = GameObject.FindWithTag("PlayerCam");
        return go != null ? go.GetComponent<CinemachineCamera>() : null;
    }

    void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(col.center, col.size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(col.center, col.size);
    }
}
