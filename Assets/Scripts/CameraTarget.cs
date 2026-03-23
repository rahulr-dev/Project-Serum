using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Follow")]
    [SerializeField] private float horizontalSmoothing = 12f;
    [SerializeField] private float verticalSmoothing = 3f;   // low = ignores jump bounce
    [SerializeField] private float verticalDeadzone = 0.05f; // stops micro-drift on flat ground

    private float smoothedY;

    void Start()
    {
        // Snap to player immediately — no lerp on first frame
        transform.position = player.position;
        smoothedY = player.position.y;
    }

    void LateUpdate()
    {
        // X/Z follow instantly-smoothed (tracks left/right, depth)
        // Y follows sluggishly — absorbs jump/fall without bouncing
        float targetY = player.position.y;

        if (Mathf.Abs(targetY - smoothedY) > verticalDeadzone)
            smoothedY = Mathf.Lerp(smoothedY, targetY, verticalSmoothing * Time.deltaTime);

        transform.position = new Vector3(
            Mathf.Lerp(transform.position.x, player.position.x, horizontalSmoothing * Time.deltaTime),
            smoothedY,
            Mathf.Lerp(transform.position.z, player.position.z, horizontalSmoothing * Time.deltaTime)
        );

        // Rotation is never inherited — this transform stays axis-aligned
    }
}
