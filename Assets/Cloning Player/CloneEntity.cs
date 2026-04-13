using UnityEngine;

public class CloneEntity : MonoBehaviour
{
    private CloneManager manager;

    void Start()
    {
        // Find the manager in the scene (assuming only one Main Character)
        manager = FindObjectOfType<CloneManager>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // If we hit a trap or enemy
        if (collision.gameObject.CompareTag("Trap"))
        {
            Die();
        }
    }

    // Example: If you have spikes that use Triggers instead of Colliders
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Trap"))
        {
            Die();
        }
    }

    void Die()
    {
        if (manager != null)
        {
            // Notify manager that we died environmentally (leave body = true)
            manager.KillClone(true);
        }
    }
}