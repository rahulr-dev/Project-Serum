using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionComponent : MonoBehaviour
{
    [Header("Detection")]
    public float interactDistance = 2f;
    public Vector3 boxSize = new Vector3(1.5f, 1.5f, 1.5f);
    public LayerMask interactLayer;

    [Header("Offset")]
    public Vector3 offset;

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        Vector3 direction = transform.forward;

        Vector3 center = transform.position
            + direction * interactDistance
            + offset;

        Collider[] hits = Physics.OverlapBox(center, boxSize / 2, Quaternion.identity, interactLayer);

        BaseInteractable closest = null;
        float minDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            BaseInteractable interactable = hit.GetComponent<BaseInteractable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                closest = interactable;
            }
        }

        closest?.Interact();
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Vector3 direction = transform.forward;
        Vector3 center = transform.position
            + direction * interactDistance
            + offset;

        Gizmos.DrawWireCube(center, boxSize);
    }
}