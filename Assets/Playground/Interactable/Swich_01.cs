using UnityEngine;

public class Switch_01 : MonoBehaviour
{
    public Switch targetSwitch;
    public Door_01 door;

    void Start()
    {
        targetSwitch.OnActivated += HandleSwitch;
    }

    void HandleSwitch()
    {
        if (door != null)
        {
            door.Activate();
        }
    }

    void OnDestroy()
    {
        targetSwitch.OnActivated -= HandleSwitch;
    }
}