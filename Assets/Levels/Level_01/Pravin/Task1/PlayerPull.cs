using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPull : MonoBehaviour
{
    public float pullForce = 20f;
    public float pullRadius = 2.5f;

    void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            TryPull();
        }
    }

    void TryPull()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pullRadius);

        foreach (Collider col in hits)
        {
            InteractableObject obj = col.GetComponent<InteractableObject>();

            if (obj != null && obj.isInteractable)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    Vector3 dir = (transform.position - col.transform.position).normalized;
                    rb.AddForce(dir * pullForce, ForceMode.Impulse);

                    Debug.Log("Pulled: " + col.name);
                }

                return;
            }
        }

        Debug.Log("Nothing to pull");
    }
}