using UnityEngine;

public class Door_01 : MonoBehaviour
{
    [Header("Door Parts")]
    public Transform doorVisual;

    [Header("Movement")]
    public Vector3 openOffset = new Vector3(0, 3f, 0);
    public float speed = 3f;

    private Vector3 closedPos;
    private Vector3 openPos;

    private int activeInputs = 0;

    void Start()
    {
        if (doorVisual == null)
            doorVisual = transform;

        closedPos = doorVisual.position;
        openPos = closedPos + openOffset;
    }

    void Update()
    {
        Vector3 target = activeInputs > 0 ? openPos : closedPos;

        doorVisual.position = Vector3.Lerp(
            doorVisual.position,
            target,
            Time.deltaTime * speed
        );
    }

    public void Activate()
    {
        activeInputs++;
    }

    public void Deactivate()
    {
        activeInputs = Mathf.Max(0, activeInputs - 1);
    }
}