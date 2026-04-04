using UnityEngine;

public class Lever : BaseInteractable
{
    public bool isOn = false;

    public event System.Action<bool> OnLeverToggled;

    protected override void OnInteract()
    {
        Debug.Log("Lever Interacted: " + (isOn ? "Turning OFF" : "Turning ON"));
        isOn = !isOn;
        OnLeverToggled?.Invoke(isOn);
    }
}