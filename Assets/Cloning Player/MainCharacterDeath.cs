using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainCharacterDeath : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Time in seconds before the scene restarts")]
    public float restartDelay = 2f;

    private Rigidbody rb;
    private PlayerMovement movement;
    private bool isDead = false; // Prevent multiple death triggers

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if we hit a trap and aren't already dead
        if (!isDead && collision.gameObject.CompareTag("Trap"))
        {
            Die();
        }
    }

    // Support for Trigger Traps (e.g., spikes that are triggers)
    void OnTriggerEnter(Collider other)
    {
        if (!isDead && other.gameObject.CompareTag("Trap"))
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        if (movement != null) movement.canControl = false;

        CloneManager manager = GetComponent<CloneManager>();
        if (manager != null) manager.enabled = false;

        // --- NEW: Disable Animator ---
        Animator mainAnim = GetComponentInChildren<Animator>();
        if (mainAnim != null) mainAnim.enabled = false;

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None;
            rb.AddTorque(Random.onUnitSphere * 2f, ForceMode.Impulse);
        }

        StartCoroutine(RestartLevel());
    }

    IEnumerator RestartLevel()
    {
        // Wait for the specified time
        yield return new WaitForSeconds(restartDelay);

        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}