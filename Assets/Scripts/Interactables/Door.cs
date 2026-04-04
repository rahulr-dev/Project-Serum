using UnityEngine;

public class Door : BaseInteractable
{
    public bool isOpen = false;

    protected override void OnInteract()
    {
        Debug.Log("Door Interacted: " + (isOpen ? "Closing" : "Opening"));
        isOpen = !isOpen;
    }
}