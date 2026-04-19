using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public bool isInteractable = true;
    public bool isPickable = false;

    public virtual void Interact()
    {
        Debug.Log(gameObject.name + " interacted");
    }
}