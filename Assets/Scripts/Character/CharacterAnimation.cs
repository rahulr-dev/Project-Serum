using System;
using Game;
using Interaction;
using UnityEngine;

namespace Character
{
    public class CharacterAnimation : MonoBehaviour
    {
        const float StealthOff = 0.01f;
        const float StealthOn = 1f;

        [SerializeField] Animator animator;
        [SerializeField] MonoBehaviour moveSpeedSource;
        [SerializeField] SideScrollerController locomotion;
        [SerializeField] string speedParam = "speed";
        [SerializeField] string jumpParam = "jump";
        [SerializeField] string landParam = "land";
        [SerializeField] string landRunParam = "landRun";
        [SerializeField] string impactfulLandParam = "impactfulLand";
        [SerializeField] string groundedParam = "grounded";
        [SerializeField] string verticalSpeedParam = "verticalSpeed";
        [SerializeField] string stealthParam = "stealth";
        [SerializeField] string pushingParam = "pushing";
        [SerializeField] float dampTime = 0.1f;
        [SerializeField] float stealthDampTime = 0.25f;
        [Tooltip("How long root motion remains disabled after pushing stops.")]
        [SerializeField, Min(0f)] float pushingReleaseDelay = 1f;

        [Header("Idle Variants")]
        [Tooltip("Animator Trigger parameters that play the one-shot idle clips.")]
        [SerializeField] string idleVariant2Trigger = "idleVariant2";
        [SerializeField] string idleVariant3Trigger = "idleVariant3";
        [Tooltip("How long the player must stand still before an additional idle can play.")]
        [SerializeField, Min(0f)] float idleVariantDelay = 8f;
        [Tooltip("Pause after a variant plays before starting the next idle delay.")]
        [SerializeField, Min(0f)] float idleVariantInterval = 2f;
        [SerializeField, Min(0f)] float idleSpeedThreshold = 0.02f;
        [Header("Landing")]
        [Tooltip("Vertical impact speed required to select the impactful landing animation.")]
        [SerializeField, Min(0f)] float impactfulLandingSpeed = 10f;
        [Tooltip("Horizontal speed required to select the running landing animation.")]
        [SerializeField, Min(0f)] float landRunSpeedThreshold = 0.5f;
        [Tooltip("How long the controller keeps the player moving after a non-impactful running landing.")]
        [SerializeField, Min(0f)] float landingRunMoveDuration = 1f;
        [Header("Animation-only landing prediction")]
        [SerializeField] LayerMask landingGroundLayers = ~0;
        [Tooltip("How far ahead, in seconds, to start the landing animation.")]
        [SerializeField, Min(0f)] float landingAnticipationTime = 0.1f;
        [Tooltip("Maximum distance for the animation-only floor prediction.")]
        [SerializeField, Min(0.01f)] float maxLandingAnticipationDistance = 0.65f;
        [Tooltip("Floor hits closer than this mean the player is already at the landing point.")]
        [SerializeField, Min(0f)] float landingContactMargin = 0.04f;

        INormalizedMoveSpeed _speedSource;
        Action _unbindJump;
        int _speedHash;
        int _jumpHash;
        int _landHash;
        int _landRunHash;
        int _impactfulLandHash;
        int _groundedHash;
        int _verticalSpeedHash;
        int _stealthHash;
        int _pushingHash;
        int _idleVariant2Hash;
        int _idleVariant3Hash;
        bool _speedOverrideActive;
        float _speedOverride;
        float _stealthTarget = StealthOff;
        bool _pushing;
        bool _rootMotionDisabledForPushing;
        float _rootMotionRestoreTime;
        bool _landingAnimationTriggered;
        bool _landRunLandingPending;
        float _landRunLandingSpeed;
        CharacterController _characterController;
        float _idleTimer;
        float _idleVariantCooldown;
        const float PushAnimSpeed = 0.05f;
        bool _landingAnimationArmed;

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

            if (locomotion == null)
                locomotion = GetComponent<SideScrollerController>();
            _characterController = GetComponent<CharacterController>();

            _speedHash = Animator.StringToHash(speedParam);
            _jumpHash = Animator.StringToHash(jumpParam);
            _landHash = Animator.StringToHash(landParam);
            _landRunHash = Animator.StringToHash(landRunParam);
            _impactfulLandHash = Animator.StringToHash(impactfulLandParam);
            _groundedHash = Animator.StringToHash(groundedParam);
            _verticalSpeedHash = Animator.StringToHash(verticalSpeedParam);
            _stealthHash = Animator.StringToHash(stealthParam);
            _pushingHash = Animator.StringToHash(pushingParam);
            _idleVariant2Hash = Animator.StringToHash(idleVariant2Trigger);
            _idleVariant3Hash = Animator.StringToHash(idleVariant3Trigger);
            _speedSource = FindSpeedSource();
            if (_speedSource == null)
            {
                Debug.LogError(
                    "CharacterAnimation needs a moveSpeedSource that implements INormalizedMoveSpeed.",
                    this);
            }
        }

        void OnEnable()
        {
            if (locomotion == null)
                locomotion = GetComponent<SideScrollerController>();

            BindJumpEvent();
            InteractionManager.OnInteractStarted += HandleInteractStarted;
            if (locomotion != null)
                locomotion.OnLanded += HandleLanded;

            GameStateManager.OnStateChanged += HandleStateChanged;
            GameState state = GameStateManager.Instance != null
                ? GameStateManager.Instance.CurrentState
                : GameState.Gameplay;
            ApplyStealth(state);

            SetRootMotionEnabled(true);
        }

        void OnDisable()
        {
            _unbindJump?.Invoke();
            _unbindJump = null;
            InteractionManager.OnInteractStarted -= HandleInteractStarted;
            if (locomotion != null)
                locomotion.OnLanded -= HandleLanded;

            GameStateManager.OnStateChanged -= HandleStateChanged;

            if (locomotion != null)
            {
                locomotion.UseAnimationRootMotion = false;
                locomotion.UseAnimationRootMotionInAir = false;
            }
            if (animator != null)
                animator.applyRootMotion = false;
        }

        void OnAnimatorMove()
        {
            if (animator == null || locomotion == null || !animator.applyRootMotion)
                return;

            locomotion.ApplyAnimationRootMotion(animator.deltaPosition);
        }

        void Update()
        {
            if (animator == null)
                return;

            GameState state = GameStateManager.Instance != null
                ? GameStateManager.Instance.CurrentState
                : GameState.Gameplay;

            float speed = _speedOverrideActive
                ? _speedOverride
                : _speedSource != null
                    ? _speedSource.NormalizedSpeed
                    : 0f;

            bool wantPush = state == GameState.GameplayPushing &&
                            (_pushing || speed > PushAnimSpeed);

            if (wantPush || dampTime <= 0f)
                animator.SetFloat(_speedHash, speed);
            else
                animator.SetFloat(_speedHash, speed, dampTime, Time.deltaTime);

            if (locomotion != null)
            {
                animator.SetBool(_groundedHash, locomotion.IsGrounded);
                animator.SetFloat(_verticalSpeedHash, locomotion.VerticalSpeed);
            }

            PredictLandingAnimation();

            UpdateIdleVariant(speed);

            ApplyPushing(wantPush);

            if (stealthDampTime > 0f)
                animator.SetFloat(_stealthHash, _stealthTarget, stealthDampTime, Time.deltaTime);
            else
                animator.SetFloat(_stealthHash, _stealthTarget);
        }

        public void SetSpeed(float value)
        {
            _speedOverride = value;
            _speedOverrideActive = true;

            if (animator == null)
                return;

            animator.SetFloat(_speedHash, value);
        }

        public void ClearSpeedOverride()
        {
            _speedOverrideActive = false;
        }

        void UpdateIdleVariant(float speed)
        {
            if (speed > idleSpeedThreshold || _pushing)
            {
                _idleTimer = 0f;
                _idleVariantCooldown = 0f;
                return;
            }

            if (_idleVariantCooldown > 0f)
            {
                _idleVariantCooldown -= Time.deltaTime;
                return;
            }

            _idleTimer += Time.deltaTime;
            if (_idleTimer < idleVariantDelay)
                return;

            // Idle 1 remains the default state. Fire one trigger so a variant plays once.
            if (UnityEngine.Random.value < 0.5f)
                animator.SetTrigger(_idleVariant2Hash);
            else
                animator.SetTrigger(_idleVariant3Hash);
            _idleTimer = 0f;
            _idleVariantCooldown = idleVariantInterval;
        }

        void HandleJumped()
        {
            ResetIdleTimer();
            _landingAnimationTriggered = false;
            _landingAnimationArmed = true;
            _landRunLandingPending = false;
            _landRunLandingSpeed = 0f;

            if (animator == null || _pushing)
                return;

            animator.SetTrigger(_jumpHash);
        }

        void HandleInteractStarted()
        {
            ResetIdleTimer();
        }

        void HandleLanded()
        {
            if (!_landRunLandingPending || locomotion == null)
                return;

            _landRunLandingPending = false;
            if (locomotion.LastLandingImpactSpeed >= impactfulLandingSpeed)
                return;

            locomotion.StartLandingRunMovement(landingRunMoveDuration, _landRunLandingSpeed);
        }

        void ResetIdleTimer()
        {
            _idleTimer = 0f;
            _idleVariantCooldown = 0f;
        }

        void PredictLandingAnimation()
        {
            if (animator == null || locomotion == null || _characterController == null ||
                !_landingAnimationArmed || locomotion.VerticalSpeed >= 0f)
                return;

            float lookAhead = GetLandingLookAhead();
            if (!TryFindLandingFloor(_characterController, lookAhead, out RaycastHit hit))
            {
                _landingAnimationTriggered = false;
                _landRunLandingPending = false;
                _landRunLandingSpeed = 0f;
                return;
            }

            float remainingGap = Mathf.Clamp(hit.distance - _characterController.bounds.extents.y, 0f, lookAhead);
            if (remainingGap <= landingContactMargin)
            {
                if (!_landingAnimationTriggered)
                    TriggerLandingAnimation(Mathf.Abs(locomotion.VerticalSpeed));

                _landingAnimationArmed = false;
                _landingAnimationTriggered = false;
                return;
            }

            if (_landingAnimationTriggered)
                return;

            float verticalSpeed = locomotion.VerticalSpeed;
            float fallGravity = locomotion.Gravity * locomotion.FallGravityMultiplier;
            float impactEstimate = Mathf.Sqrt(
                verticalSpeed * verticalSpeed + 2f * fallGravity * remainingGap);
            TriggerLandingAnimation(impactEstimate);
        }

        float GetLandingLookAhead()
        {
            if (locomotion == null)
                return maxLandingAnticipationDistance;

            return Mathf.Clamp(
                Mathf.Abs(locomotion.VerticalSpeed) * landingAnticipationTime,
                0.08f,
                maxLandingAnticipationDistance);
        }

        bool TryFindLandingFloor(CharacterController characterController, float lookAhead, out RaycastHit hit)
        {
            Bounds bounds = characterController.bounds;
            float castDistance = bounds.extents.y + lookAhead;
            if (!Physics.Raycast(
                    bounds.center,
                    Vector3.down,
                    out hit,
                    castDistance,
                    landingGroundLayers,
                    QueryTriggerInteraction.Ignore))
                return false;

            float minimumGroundNormalY = Mathf.Cos(characterController.slopeLimit * Mathf.Deg2Rad);
            return hit.normal.y >= minimumGroundNormalY;
        }

        void TriggerLandingAnimation(float impactSpeed)
        {
            animator.ResetTrigger(_landHash);
            animator.ResetTrigger(_landRunHash);
            animator.ResetTrigger(_impactfulLandHash);

            float horizontalSpeed = locomotion != null ? Mathf.Abs(locomotion.HorizontalSpeed) : 0f;
            bool impactful = impactSpeed >= impactfulLandingSpeed;
            bool landRun = !impactful && horizontalSpeed >= landRunSpeedThreshold;
            _landRunLandingPending = landRun;
            _landRunLandingSpeed = landRun ? locomotion.HorizontalSpeed : 0f;

            if (impactful)
                animator.SetTrigger(_impactfulLandHash);
            else if (landRun)
                animator.SetTrigger(_landRunHash);
            else
                animator.SetTrigger(_landHash);

            _landingAnimationTriggered = true;
        }

        void OnDrawGizmosSelected()
        {
            CharacterController characterController = _characterController != null
                ? _characterController
                : GetComponent<CharacterController>();
            if (characterController == null)
                return;

            Bounds bounds = characterController.bounds;
            float lookAhead = Application.isPlaying ? GetLandingLookAhead() : maxLandingAnticipationDistance;
            float castDistance = bounds.extents.y + lookAhead;
            Vector3 end = bounds.center + Vector3.down * castDistance;

            bool foundFloor = TryFindLandingFloor(characterController, lookAhead, out RaycastHit hit);
            Gizmos.color = foundFloor ? new Color(0.2f, 0.9f, 0.3f, 1f) : new Color(1f, 0.55f, 0.1f, 1f);
            Gizmos.DrawLine(bounds.center, end);
            Gizmos.DrawWireSphere(end, 0.06f);
            if (foundFloor)
            {
                Gizmos.DrawSphere(hit.point, 0.08f);
                Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.3f);
            }
        }

        void HandleStateChanged(GameState previous, GameState current)
        {
            ApplyStealth(current);
        }

        void ApplyStealth(GameState state)
        {
            _stealthTarget = state == GameState.GameplayStealth ? StealthOn : StealthOff;
        }

        void ApplyPushing(bool pushing)
        {
            if (pushing)
            {
                SetPushing(true);
                DisableRootMotionForPushing();
                return;
            }

            SetPushing(false);
            if (!_rootMotionDisabledForPushing)
                return;

            if (_rootMotionRestoreTime <= 0f)
                _rootMotionRestoreTime = Time.time + pushingReleaseDelay;

            if (Time.time < _rootMotionRestoreTime)
                return;

            _rootMotionRestoreTime = 0f;
            _rootMotionDisabledForPushing = false;
            SetRootMotionEnabled(true);
        }

        void SetPushing(bool pushing)
        {
            if (_pushing == pushing)
                return;

            _pushing = pushing;
            if (animator != null)
                animator.SetBool(_pushingHash, pushing);
        }

        void DisableRootMotionForPushing()
        {
            _rootMotionRestoreTime = 0f;
            _rootMotionDisabledForPushing = true;
            SetRootMotionEnabled(false);
        }

        void SetRootMotionEnabled(bool enabled)
        {
            if (animator != null)
                animator.applyRootMotion = enabled;
            if (locomotion != null)
                locomotion.UseAnimationRootMotion = enabled;
        }

        INormalizedMoveSpeed FindSpeedSource()
        {
            if (moveSpeedSource is INormalizedMoveSpeed fromAssigned)
                return fromAssigned;

            if (locomotion is INormalizedMoveSpeed fromLocomotion)
                return fromLocomotion;

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is INormalizedMoveSpeed speed)
                    return speed;
            }

            return null;
        }

        void BindJumpEvent()
        {
            _unbindJump?.Invoke();
            _unbindJump = null;

            IJumpEvents jump = GetComponent<IJumpEvents>();
            if (jump == null)
                jump = locomotion as IJumpEvents;

            if (jump == null)
                return;

            jump.OnJumped += HandleJumped;
            _unbindJump = () =>
            {
                jump.OnJumped -= HandleJumped;
            };
        }
    }
}
