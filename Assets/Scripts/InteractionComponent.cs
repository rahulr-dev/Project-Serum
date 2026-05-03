using UnityEngine;

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
        if (InputSystem.Instance.InteractPressed())
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

        if (hits.Length > 0)
        {
            var interactable = hits[0].GetComponent<BaseInteractable>();
            interactable?.Interact();
        }
    }

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