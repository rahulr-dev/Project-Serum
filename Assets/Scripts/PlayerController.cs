using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ─── State ───────────────────────────────────────────────────────────────
    public enum LocomotionState { Idle, Walk, Crouch, Jump, Fall, Slide }
    public LocomotionState CurrentState { get; private set; } = LocomotionState.Idle;

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 6f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 25f;
    [SerializeField] private float crouchSpeedMultiplier = 0.4f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 9f;
    [SerializeField] private float fallMultiplier = 3.2f;
    [SerializeField] private float jumpCutMultiplier = 2.5f;
    [SerializeField] private float coyoteTime = 0.12f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;

    [Header("Crouch")]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 12f;

    [Header("Slide")]
    [SerializeField] private float slideSpeed = 10f;
    [SerializeField] private float slideDuration = 0.6f;
    [SerializeField] private float slideDeceleration = 14f;
    [SerializeField] private float slideSpeedThreshold = 0.75f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 12f;

    // ─── Runtime ──────────────────────────────────────────────────────────────
    private CharacterController cc;

    private float currentSpeedZ;
    private float velocityY;
    private float currentYAngle;
    private float targetYAngle;

    private float coyoteTimer;
    private bool jumpConsumed;

    private float targetCapsuleHeight;
    private float slideTimer;
    private float currentSlideSpeed;

    // ─── Input cache (refreshed each frame) ───────────────────────────────────
    private float inputAxis;
    private bool crouchHeld;
    private bool crouchPressed;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool prevCrouchHeld;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        cc = GetComponent<CharacterController>();
        cc.height = standHeight;
        cc.center = new Vector3(0f, standHeight / 2f, 0f);
        targetCapsuleHeight = standHeight;
    }

    void Update()
    {
        CacheInput();
        UpdateCoyote();
        HandleStateTransition();
        ExecuteState();
        HandleRotation();
        HandleCapsuleResize();

        float finalSpeedZ = CurrentState == LocomotionState.Slide ? currentSlideSpeed : currentSpeedZ;
        cc.Move(new Vector3(0f, velocityY, finalSpeedZ) * Time.deltaTime);
    }

    // ─── Input ────────────────────────────────────────────────────────────────
    private void CacheInput()
    {
        inputAxis = InputSystem.Instance.Move();
        crouchHeld = InputSystem.Instance.Crouch();
        jumpPressed = InputSystem.Instance.JumpPressed();
        jumpHeld = InputSystem.Instance.JumpHeld();
        crouchPressed = crouchHeld && !prevCrouchHeld;
        prevCrouchHeld = crouchHeld;
    }

    // ─── Coyote ───────────────────────────────────────────────────────────────
    private void UpdateCoyote()
    {
        if (cc.isGrounded) { coyoteTimer = coyoteTime; jumpConsumed = false; }
        else { coyoteTimer -= Time.deltaTime; }
    }

    // ─── State Transitions ────────────────────────────────────────────────────
    //
    //  GROUNDED                         AIRBORNE
    //  ┌──────┐  input    ┌──────┐      ┌──────┐  vy<0   ┌──────┐
    //  │ Idle │ ────────► │ Walk │      │ Jump │ ──────► │ Fall │
    //  └──────┘           └──┬───┘      └──────┘         └──┬───┘
    //     ▲                  │ crouch+speed                  │ land
    //     │ land             ▼                               ▼
    //     │              ┌───────┐                       (Idle/Walk)
    //     │              │ Slide │
    //     │              └───────┘
    //     │  crouch
    //     └──────────── ┌────────┐
    //                   │ Crouch │
    //                   └────────┘
    //
    private void HandleStateTransition()
    {
        LocomotionState next = CurrentState;

        switch (CurrentState)
        {
            case LocomotionState.Idle:
                if (TryJump()) next = LocomotionState.Jump;
                else if (inputAxis != 0f) next = LocomotionState.Walk;
                else if (crouchHeld) next = LocomotionState.Crouch;
                else if (!cc.isGrounded) next = LocomotionState.Fall;
                break;

            case LocomotionState.Walk:
                if (TryJump()) next = LocomotionState.Jump;
                else if (TrySlide()) next = LocomotionState.Slide;
                else if (inputAxis == 0f) next = LocomotionState.Idle;
                else if (crouchHeld) next = LocomotionState.Crouch;
                else if (!cc.isGrounded) next = LocomotionState.Fall;
                break;

            case LocomotionState.Crouch:
                if (TryJump()) next = LocomotionState.Jump;
                else if (!crouchHeld && !CeilingBlocked()) next = inputAxis != 0f
                                                               ? LocomotionState.Walk
                                                               : LocomotionState.Idle;
                else if (!cc.isGrounded) next = LocomotionState.Fall;
                break;

            case LocomotionState.Jump:
                if (velocityY < 0f) next = LocomotionState.Fall;
                break;

            case LocomotionState.Fall:
                if (cc.isGrounded) next = inputAxis != 0f
                                                               ? LocomotionState.Walk
                                                               : LocomotionState.Idle;
                break;

            case LocomotionState.Slide:
                if (!cc.isGrounded) next = LocomotionState.Fall;
                else if (SlideExpired()) next = crouchHeld
                                                               ? LocomotionState.Crouch
                                                               : LocomotionState.Idle;
                break;
        }

        if (next != CurrentState) EnterState(next);
    }

    private void EnterState(LocomotionState next)
    {
        // Exit logic
        switch (CurrentState)
        {
            case LocomotionState.Slide:
                currentSpeedZ = currentSlideSpeed;
                currentSlideSpeed = 0f;
                break;
        }

        // Entry logic
        switch (next)
        {
            case LocomotionState.Jump:
                velocityY = jumpForce;
                coyoteTimer = 0f;
                jumpConsumed = true;
                break;

            case LocomotionState.Slide:
                slideTimer = slideDuration;
                currentSlideSpeed = slideSpeed * Mathf.Sign(currentSpeedZ);
                break;

            case LocomotionState.Crouch:
                targetCapsuleHeight = crouchHeight;
                break;

            case LocomotionState.Idle:
            case LocomotionState.Walk:
                if (!CeilingBlocked()) targetCapsuleHeight = standHeight;
                break;
        }

        CurrentState = next;
    }

    // ─── State Execution ──────────────────────────────────────────────────────
    private void ExecuteState()
    {
        switch (CurrentState)
        {
            case LocomotionState.Idle:
                ApplyDeceleration();
                ApplyGroundedGravity();
                break;

            case LocomotionState.Walk:
                ApplyMovement(maxSpeed);
                ApplyGroundedGravity();
                break;

            case LocomotionState.Crouch:
                ApplyMovement(maxSpeed * crouchSpeedMultiplier);
                ApplyGroundedGravity();
                targetCapsuleHeight = crouchHeight;
                break;

            case LocomotionState.Jump:
                ApplyMovement(maxSpeed);
                ApplyAirGravity();
                break;

            case LocomotionState.Fall:
                ApplyMovement(maxSpeed);
                ApplyAirGravity();
                break;

            case LocomotionState.Slide:
                ApplySlideMovement();
                ApplyGroundedGravity();
                targetCapsuleHeight = crouchHeight;
                break;
        }
    }

    // ─── Movement Primitives ──────────────────────────────────────────────────
    private void ApplyMovement(float speedCap)
    {
        currentSpeedZ = Mathf.MoveTowards(currentSpeedZ, speedCap * inputAxis, acceleration * Time.deltaTime);
    }

    private void ApplyDeceleration()
    {
        currentSpeedZ = Mathf.MoveTowards(currentSpeedZ, 0f, deceleration * Time.deltaTime);
    }

    private void ApplyGroundedGravity()
    {
        velocityY = groundedGravity;
    }

    private void ApplyAirGravity()
    {
        float scale = velocityY < 0f ? fallMultiplier :
                      !jumpHeld ? jumpCutMultiplier : 1f;
        velocityY += gravity * scale * Time.deltaTime;
    }

    private void ApplySlideMovement()
    {
        slideTimer -= Time.deltaTime;
        currentSlideSpeed = Mathf.MoveTowards(currentSlideSpeed, 0f, slideDeceleration * Time.deltaTime);
    }

    // ─── Rotation ─────────────────────────────────────────────────────────────
    private void HandleRotation()
    {
        if (inputAxis > 0f) targetYAngle = 0f;
        else if (inputAxis < 0f) targetYAngle = 180f;

        currentYAngle = Mathf.LerpAngle(currentYAngle, targetYAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, currentYAngle, 0f);
    }

    // ─── Capsule ──────────────────────────────────────────────────────────────
    private void HandleCapsuleResize()
    {
        float newHeight = Mathf.Lerp(cc.height, targetCapsuleHeight, crouchTransitionSpeed * Time.deltaTime);
        cc.height = newHeight;
        cc.center = new Vector3(0f, newHeight / 2f, 0f);
    }

    // ─── Transition Guards ────────────────────────────────────────────────────
    private bool TryJump() => jumpPressed && coyoteTimer > 0f && !jumpConsumed;
    private bool TrySlide() => crouchPressed && Mathf.Abs(currentSpeedZ) >= maxSpeed * slideSpeedThreshold;
    private bool SlideExpired() => slideTimer <= 0f || Mathf.Abs(currentSlideSpeed) < 0.1f;
    private bool CeilingBlocked()
    {
        Vector3 origin = transform.position + Vector3.up * (crouchHeight + cc.skinWidth);
        float dist = standHeight - crouchHeight - cc.skinWidth * 2f;
        return Physics.SphereCast(origin, cc.radius * 0.9f, Vector3.up, out _, dist);
    }
}
