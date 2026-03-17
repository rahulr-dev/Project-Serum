using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CinemachineCameraBlender : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private int activePriority = 99;
    [SerializeField] private int inactivePriority = 0;

    [Header("Gizmo")]
    [SerializeField] private Color gizmoColor = new Color(0f, 0.8f, 1f, 0.2f);

    private BoxCollider trigger;

    void Start()
    {
        trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        transform.position = new Vector3(0f, transform.position.y, transform.position.z);
        cam.Priority = inactivePriority;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag)) cam.Priority = activePriority;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag)) cam.Priority = inactivePriority;
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
