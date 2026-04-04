using UnityEngine;

public class Switch : BaseInteractable
{
    public event System.Action OnActivated;

    protected override void OnInteract()
    {
        Debug.Log("Switch Activated");
        OnActivated?.Invoke();
    }
}