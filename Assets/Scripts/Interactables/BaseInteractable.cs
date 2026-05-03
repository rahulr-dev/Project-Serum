using UnityEngine;

public abstract class BaseInteractable : MonoBehaviour
{
    [Header("Common Settings")]
    public float cooldown = 0.2f;

    private bool canInteract = true;

    // Called by player
    public void Interact()
    {
        if (!canInteract) return;

        canInteract = false;

        // Call child-specific logic
        OnInteract();

        // Cooldown
        Invoke(nameof(ResetInteract), cooldown);
    }

    // Each interactable must implement this
    protected abstract void OnInteract();

    void ResetInteract()
    {
        canInteract = true;
    }
}
