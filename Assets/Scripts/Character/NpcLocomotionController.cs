using System;
using UnityEngine;

namespace Character
{
    [RequireComponent(typeof(CharacterController))]
    public class NpcLocomotionController : MonoBehaviour, ICharacterLocomotion
    {
        [SerializeField] float moveSpeed = 4f;
        [SerializeField] float rightYaw = 90f;
        [SerializeField] float leftYaw = -90f;
        [Tooltip("When enabled, locomotion does not turn the NPC. They keep the current facing.")]
        public bool lockRotation;

        [Header("Jump")]
        [SerializeField] float gravity = 40f;
        [SerializeField] float fallGravityMultiplier = 1.7f;
        [SerializeField] float maxFallSpeed = 25f;
        [SerializeField] float jumpHeight = 2.2f;
        [SerializeField] float jumpCooldown = 0.15f;
        [SerializeField] float apexHangThreshold = 2f;
        [SerializeField] float apexHangMultiplier = 0.5f;
        [SerializeField] float groundProbeExtra = 0.08f;
        [SerializeField] float groundedStickVelocity = -2f;

        public event Action OnJumped;
        public event Action OnLanded;
        public event Action<bool> OnMovingChanged;
        public event Action OnScriptedRunStarted;
        public event Action OnScriptedRunCompleted;

        public bool IsGrounded { get; private set; }
        public bool IsMoving { get; private set; }
        public bool LocomotionEnabled { get; private set; }
        public bool IsScriptedRunning { get; private set; }
        public float MoveSpeed => moveSpeed;
        public float NormalizedSpeed
        {
            get
            {
                if (IsScriptedRunning)
                    return moveSpeed > 0f ? Mathf.Clamp01(_scriptedRunSpeed / moveSpeed) : 1f;

                return 0f;
            }
        }

        CharacterController _controller;
        Vector3 _scriptedStart;
        Vector3 _scriptedOffset;
        float _scriptedDuration;
        float _scriptedElapsed;
        float _scriptedRunSpeed;
        Vector3 _scriptedHorizDelta;
        float _targetYaw;
        float _verticalVelocity;
        float _coyoteTimer;
        float _jumpCooldownTimer;
        const float CoyoteTime = 0.1f;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _targetYaw = transform.eulerAngles.y;
            LocomotionEnabled = false;
        }

        public void SetLocomotionEnabled(bool enabled)
        {
            LocomotionEnabled = enabled;
            if (enabled)
                return;

            if (IsMoving)
            {
                IsMoving = false;
                OnMovingChanged?.Invoke(false);
            }
        }

        public void ForceJump()
        {
            if (_jumpCooldownTimer > 0f)
                return;

            if (_coyoteTimer <= 0f && !CheckGrounded())
                return;

            PerformJump();
        }

        public void SetFacing(float yaw)
        {
            _targetYaw = yaw;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        public void FaceLeft()
        {
            SetFacing(leftYaw);
        }

        public void FaceRight()
        {
            SetFacing(rightYaw);
        }

        public void StartScriptedRunAtSpeed(Vector3 worldOffset, float speed)
        {
            if (worldOffset.sqrMagnitude <= 0.0001f)
                return;

            _scriptedRunSpeed = Mathf.Max(0.01f, speed);
            float duration = worldOffset.magnitude / _scriptedRunSpeed;
            StartScriptedRun(worldOffset, duration);
        }

        public void StartScriptedRun(Vector3 worldOffset, float duration)
        {
            if (worldOffset.sqrMagnitude <= 0.0001f)
                return;

            _scriptedStart = transform.position;
            _scriptedOffset = worldOffset;
            _scriptedDuration = Mathf.Max(0.01f, duration);
            if (_scriptedRunSpeed <= 0f)
                _scriptedRunSpeed = worldOffset.magnitude / _scriptedDuration;
            _scriptedElapsed = 0f;
            _scriptedHorizDelta = Vector3.zero;
            IsScriptedRunning = true;

            if (!lockRotation)
            {
                float yaw = Mathf.Atan2(worldOffset.x, worldOffset.z) * Mathf.Rad2Deg;
                SetFacing(yaw);
            }

            if (IsMoving)
            {
                IsMoving = false;
                OnMovingChanged?.Invoke(false);
            }

            OnScriptedRunStarted?.Invoke();
        }

        public void StopScriptedRun()
        {
            if (!IsScriptedRunning)
                return;

            CompleteScriptedRun();
        }

        void Update()
        {
            UpdateScriptedRun();

            bool wasGrounded = IsGrounded;
            IsGrounded = CheckGrounded();
            if (IsGrounded && !wasGrounded)
                OnLanded?.Invoke();

            if (IsGrounded)
                _coyoteTimer = CoyoteTime;
            else
                _coyoteTimer -= Time.deltaTime;

            if (_jumpCooldownTimer > 0f)
                _jumpCooldownTimer -= Time.deltaTime;

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

            if (IsScriptedRunning)
            {
                _controller.Move(_scriptedHorizDelta + new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
            }
            else
            {
                _controller.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
            }
        }

        void UpdateScriptedRun()
        {
            if (!IsScriptedRunning)
                return;

            _scriptedElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_scriptedElapsed / _scriptedDuration);
            float eased = t * t * (3f - 2f * t);
            Vector3 desired = _scriptedStart + _scriptedOffset * eased;
            _scriptedHorizDelta = new Vector3(
                desired.x - transform.position.x,
                0f,
                desired.z - transform.position.z);

            if (t >= 1f)
                CompleteScriptedRun();
        }

        void CompleteScriptedRun()
        {
            IsScriptedRunning = false;
            _scriptedHorizDelta = Vector3.zero;
            _scriptedRunSpeed = 0f;
            OnScriptedRunCompleted?.Invoke();
        }

        void PerformJump()
        {
            _verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            _coyoteTimer = 0f;
            _jumpCooldownTimer = jumpCooldown;
            IsGrounded = false;
            OnJumped?.Invoke();
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
