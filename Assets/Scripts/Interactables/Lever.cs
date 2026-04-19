using UnityEngine;

public class Lever : BaseInteractable
{
    public bool isOn = false;
    public Door door;

    protected override void OnInteract()
    {
        isOn = !isOn;

        if (door != null)
        {
            if (isOn) door.Activate();
            else door.Deactivate();
        }
    }
}