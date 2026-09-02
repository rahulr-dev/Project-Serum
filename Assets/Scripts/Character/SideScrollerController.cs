using System;
using Interaction;
using UnityEngine;

namespace Character
{
    [RequireComponent(typeof(CharacterController))]
    public class SideScrollerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float moveSmoothTime = 0.12f;
        [SerializeField] float turnSmoothTime = 0.1f;
        [SerializeField] float inputDeadzone = 0.15f;
        [SerializeField] float rightYaw = 90f;
        [SerializeField] float leftYaw = -90f;

        [Header("Jump")]
        [SerializeField] float gravity = 40f;
        [SerializeField] float fallGravityMultiplier = 1.7f;
        [SerializeField] float maxFallSpeed = 25f;
        [SerializeField] float jumpHeight = 2.2f;
        [SerializeField] float jumpCutMultiplier = 0.4f;
        [SerializeField] float coyoteTime = 0.1f;
        [SerializeField] float jumpBufferTime = 0.1f;
        [SerializeField] float apexHangThreshold = 2f;
        [SerializeField] float apexHangMultiplier = 0.5f;
        [SerializeField] float groundProbeExtra = 0.08f;
        [SerializeField] float groundedStickVelocity = -2f;

        public event Action OnJumped;
        public event Action OnLanded;
        public event Action<bool> OnMovingChanged;

        public bool IsGrounded { get; private set; }
        public bool IsMoving { get; private set; }

        CharacterController _controller;
        float _currentSpeed;
        float _speedVelocity;
        float _targetYaw;
        float _yawVelocity;
        float _verticalVelocity;
        float _coyoteTimer;
        float _jumpBufferTimer;
        bool _jumpCutApplied;
        bool _wasJumpHeld;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _targetYaw = transform.eulerAngles.y;
        }

        void Update()
        {
            InteractionManager input = InteractionManager.Instance;
            float inputX = input != null ? input.MoveInput.x : 0f;
            bool jumpHeld = input != null && input.IsJumpHeld;
            bool jumpPressed = input != null && (input.JumpPressedThisFrame || (jumpHeld && !_wasJumpHeld));
            _wasJumpHeld = jumpHeld;

            if (Mathf.Abs(inputX) < inputDeadzone)
                inputX = 0f;
            else
                inputX = Mathf.Clamp(inputX, -1f, 1f);

            float targetSpeed = moveSpeed * inputX;
            _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedVelocity, moveSmoothTime);

            bool moving = Mathf.Abs(_currentSpeed) > inputDeadzone;
            if (moving != IsMoving)
            {
                IsMoving = moving;
                OnMovingChanged?.Invoke(IsMoving);
            }

            bool wasGrounded = IsGrounded;
            IsGrounded = CheckGrounded();
            if (IsGrounded && !wasGrounded)
                OnLanded?.Invoke();

            if (IsGrounded)
                _coyoteTimer = coyoteTime;
            else
                _coyoteTimer -= Time.deltaTime;

            if (jumpPressed)
                _jumpBufferTimer = jumpBufferTime;
            else
                _jumpBufferTimer -= Time.deltaTime;

            bool jumpedThisFrame = false;
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                _verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                _jumpCutApplied = false;
                IsGrounded = false;
                jumpedThisFrame = true;
                OnJumped?.Invoke();
            }

            if (!jumpHeld && _verticalVelocity > 0f && !_jumpCutApplied)
            {
                _verticalVelocity *= jumpCutMultiplier;
                _jumpCutApplied = true;
            }

            if (!jumpedThisFrame)
            {
                if (IsGrounded && _verticalVelocity <= 0f)
                {
                    _verticalVelocity = groundedStickVelocity;
                }
                else
                {
                    float g = gravity;
                    if (_verticalVelocity < 0f)
                        g *= fallGravityMultiplier;
                    else if (Mathf.Abs(_verticalVelocity) < apexHangThreshold)
                        g *= apexHangMultiplier;

                    _verticalVelocity -= g * Time.deltaTime;
                    if (_verticalVelocity < -maxFallSpeed)
                        _verticalVelocity = -maxFallSpeed;
                }
            }

            _controller.Move(new Vector3(_currentSpeed, _verticalVelocity, 0f) * Time.deltaTime);

            if (inputX > 0f)
                _targetYaw = rightYaw;
            else if (inputX < 0f)
                _targetYaw = leftYaw;

            float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetYaw, ref _yawVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        bool CheckGrounded()
        {
            if (_controller.isGrounded)
                return true;

            Vector3 origin = transform.position + _controller.center;
            float castDistance = (_controller.height * 0.5f) - _controller.radius + _controller.skinWidth + groundProbeExtra;
            if (castDistance < 0f)
                castDistance = _controller.skinWidth + groundProbeExtra;

            return Physics.SphereCast(
                origin,
                _controller.radius * 0.9f,
                Vector3.down,
                out _,
                castDistance,
                ~0,
                QueryTriggerInteraction.Ignore);
        }
    }
}
