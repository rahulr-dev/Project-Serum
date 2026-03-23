using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Follow")]
    [SerializeField] private float horizontalSmoothing = 12f;
    [SerializeField] private float verticalSmoothing = 3f;
    [SerializeField] private float verticalDeadzone = 0.05f;

    private float smoothedY;

    void Start()
    {
        transform.position = player.position;
        smoothedY = player.position.y;
    }

    void LateUpdate()
    {
        float targetY = player.position.y;

        if (Mathf.Abs(targetY - smoothedY) > verticalDeadzone)
            smoothedY = Mathf.Lerp(smoothedY, targetY, verticalSmoothing * Time.deltaTime);

        transform.position = new Vector3(
            Mathf.Lerp(transform.position.x, player.position.x, horizontalSmoothing * Time.deltaTime),
            smoothedY,
            Mathf.Lerp(transform.position.z, player.position.z, horizontalSmoothing * Time.deltaTime)
        );
    }
}
