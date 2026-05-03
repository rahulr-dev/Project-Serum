using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Movement")]
    public Transform doorVisual;
    public Vector3 openOffset = new Vector3(0, 3f, 0);
    public float speed = 3f;

    private Vector3 closedPos;
    private Vector3 openPos;

    private int activeInputs = 0;

    private bool isOpen = false;

    void Start()
    {
        if (doorVisual == null)
            doorVisual = transform;

        closedPos = doorVisual.position;
        openPos = closedPos + openOffset;
    }

    void Update()
    {
        bool shouldBeOpen = activeInputs > 0;

        Vector3 target = shouldBeOpen ? openPos : closedPos;

        if (Vector3.Distance(doorVisual.position, target) > 0.001f)
        {
            doorVisual.position = Vector3.MoveTowards(
                doorVisual.position,
                target,
                speed * Time.deltaTime
            );
        }
        else
        {
            doorVisual.position = target;
        }

        isOpen = shouldBeOpen;
    }

    public void Activate()
    {
        activeInputs++;
    }

    public void Deactivate()
    {
        activeInputs = Mathf.Max(0, activeInputs - 1);
    }

    public bool IsOpen() => isOpen;

    public void ForceOpen()
    {
        activeInputs = 1;
        doorVisual.position = openPos;
        isOpen = true;
    }

    public void ForceClose()
    {
        activeInputs = 0;
        doorVisual.position = closedPos;
        isOpen = false;
    }
}