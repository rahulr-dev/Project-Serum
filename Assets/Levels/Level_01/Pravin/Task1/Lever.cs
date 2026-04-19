using UnityEngine;

public class Lever : InteractableObject
{
    private bool isOn = false;

    public override void Interact()
    {
        isOn = !isOn;

        Debug.Log("Lever: " + isOn);

        transform.localRotation = Quaternion.Euler(0, 0, isOn ? -45 : 45);
    }
}