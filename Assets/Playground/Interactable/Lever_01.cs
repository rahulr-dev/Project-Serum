using UnityEngine;

public class Lever_01 : MonoBehaviour
{
    public Lever lever;
    public Door_01 door;

    void Start()
    {
        lever.OnLeverToggled += HandleLever;
    }

    void HandleLever(bool state)
    {
        if (state) door.Activate();
        else door.Deactivate();
    }

    void OnDestroy()
    {
        lever.OnLeverToggled -= HandleLever;
    }
}