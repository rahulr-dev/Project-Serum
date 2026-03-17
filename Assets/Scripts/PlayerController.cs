using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 6f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 25f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 12f;

    private CharacterController cc;
    private float currentSpeedZ;
    private float velocityY;
    private float currentYAngle = 0f;
    private float targetYAngle = 0f;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        float input = InputSystem.Instance.Move();

        HandleMovement(input);
        HandleGravity();
        HandleRotation(input);

        cc.Move(new Vector3(0f, velocityY, currentSpeedZ) * Time.deltaTime);
    }

    private void HandleMovement(float input)
    {
        float rate = input != 0f ? acceleration : deceleration;
        currentSpeedZ = Mathf.MoveTowards(currentSpeedZ, maxSpeed * input, rate * Time.deltaTime);
    }

    private void HandleGravity()
    {
        // Small constant when grounded prevents isGrounded flickering
        velocityY = cc.isGrounded ? groundedGravity : velocityY + gravity * Time.deltaTime;
    }

    private void HandleRotation(float input)
    {
        if (input > 0f) targetYAngle = 0f;
        else if (input < 0f) targetYAngle = 180f;

        currentYAngle = Mathf.LerpAngle(currentYAngle, targetYAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, currentYAngle, 0f);
    }
}
