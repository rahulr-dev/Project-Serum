using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Jump Settings")]
    public float jumpForce = 7f;

    [Header("Animation")]
    public Animator animator; // Drag your Animator component here in Inspector

    [Header("Control State")]
    public bool canControl = true;

    private Rigidbody rb;
    private bool isGrounded;
    private float horizontalInput; // To store input for animation

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // If you forget to assign animator in inspector, try to find it
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (canControl)
        {
            HandleInput();
            HandleMovement();
            HandleJump();
        }
        else
        {
            // If not controlling (AI or inactive clone), stop movement input
            horizontalInput = 0f;
        }

        UpdateAnimations();
    }

    void HandleInput()
    {
        horizontalInput = 0f;

        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
        {
            horizontalInput = -1f;
        }
        else if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
        {
            horizontalInput = 1f;
        }
    }

    void HandleMovement()
    {
        // Calculate velocity
        Vector3 movement = new Vector3(horizontalInput, 0f, 0f) * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, 0f);

        // Flip the character model to face movement direction
        if (horizontalInput != 0)
        {
            // Assuming your model faces Right by default.
            // If it faces Left, swap the 0 and 180.
            Vector3 rotation = transform.eulerAngles;
            rotation.y = horizontalInput > 0 ? 0 : 180;
            transform.eulerAngles = rotation;
        }
    }

    void HandleJump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
            // Jump animation is handled by IsGrounded parameter in UpdateAnimations
        }
    }

    void UpdateAnimations()
    {
        if (animator != null)
        {
            // Pass Speed (absolute value so we don't have negative speed)
            animator.SetFloat("Speed", Mathf.Abs(horizontalInput));

            // Pass Grounded State
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}