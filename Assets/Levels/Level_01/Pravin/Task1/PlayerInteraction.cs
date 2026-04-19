using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public float grabRadius = 3f;
    public Transform holdPoint;

    private GameObject currentObject;
    private bool isGrabbing = false;

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (!isGrabbing)
                TryInteract();
            else
                DropObject();
        }
    }

    void TryInteract()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, grabRadius);

        foreach (Collider col in hits)
        {
            InteractableObject obj = col.GetComponent<InteractableObject>();

            if (obj != null && obj.isInteractable)
            {
                Debug.Log("Found interactable: " + col.name);

                // GRAB
                if (obj.isPickable)
                {
                    currentObject = col.gameObject;
                    GrabObject();
                }
                else
                {
                    // LEVER / SWITCH
                    obj.Interact();
                }

                return;
            }
        }

        Debug.Log("Nothing interactable nearby");
    }

    void GrabObject()
    {
        Rigidbody rb = currentObject.GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;

        currentObject.transform.SetParent(holdPoint);
        currentObject.transform.localPosition = Vector3.zero;

        isGrabbing = true;

        Debug.Log("Grabbed");
    }

    void DropObject()
    {
        Rigidbody rb = currentObject.GetComponent<Rigidbody>();

        rb.isKinematic = false;

        currentObject.transform.SetParent(null);

        currentObject = null;
        isGrabbing = false;

        Debug.Log("Dropped");
    }
}