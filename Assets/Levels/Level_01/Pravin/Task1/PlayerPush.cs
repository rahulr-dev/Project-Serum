using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPush : MonoBehaviour
{
    public float pushForce = 20f;
    public float pushRadius = 2.5f;

    void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryPush();
        }
    }

    void TryPush()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pushRadius);

        foreach (Collider col in hits)
        {
            InteractableObject obj = col.GetComponent<InteractableObject>();

            if (obj != null && obj.isInteractable)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.AddForce(transform.forward * pushForce, ForceMode.Impulse);
                    Debug.Log("Pushed: " + col.name);
                }

                return;
            }
        }

        Debug.Log("Nothing to push");
    }
}