using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Drag your Player GameObject here")]
    public Transform playerTarget;

    [Header("Camera Position")]
    [Tooltip("Distance from player: X=Left/Right offset, Y=Height, Z=Depth")]
    public Vector3 offset = new Vector3(0f, 3f, -10f);

    [Header("Movement Settings")]
    [Tooltip("How fast the camera moves. 0 = instant, higher = slower/smoother")]
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        // If no target is set, do nothing
        if (playerTarget == null) return;

        // Calculate the position we WANT the camera to be in
        Vector3 desiredPosition = playerTarget.position + offset;

        // Smoothly move from current position to desired position
        // (If you want instant movement with no delay, replace the line below with: transform.position = desiredPosition;)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Apply the new position
        transform.position = smoothedPosition;
    }
}