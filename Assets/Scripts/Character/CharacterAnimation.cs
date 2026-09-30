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
        [SerializeField] string idleVariantPlayingParam = "isPlayingIdleVariation";

        [Tooltip("How long the player must stand still before an additional idle can play.")]
        [SerializeField, Min(0f)] float idleVariantDelay = 8f;
        [Tooltip("Pause after a variant plays before starting the next idle delay.")]
        [SerializeField, Min(0f)] float idleVariantInterval = 2f;
        [SerializeField, Min(0f)] float idleSpeedThreshold = 0.02f;
        [Header("Landing")]
        [Tooltip("Vertical distance fallen before the impactful landing animation is used.")]
        [SerializeField, Min(0f)] float impactfulLandingDistance = 1.5f;
        [Tooltip("Horizontal speed required to select the running landing animation.")]
        [SerializeField, Min(0f)] float landRunSpeedThreshold = 0.5f;
        [Tooltip("How long the controller keeps the player moving after a non-impactful running landing.")]
        [SerializeField, Min(0f)] float landingRunMoveDuration = 1f;

        public bool IsPlayingIdleVariation { get; private set; }

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
        int _idleVariantPlayingHash;
        bool _speedOverrideActive;
        float _speedOverride;
        float _stealthTarget = StealthOff;
        bool _pushing;
        bool _rootMotionDisabledForPushing;
        float _rootMotionRestoreTime;
        bool _landRunLandingPending;
        float _landRunLandingSpeed;
        CharacterController _characterController;
        bool _wasGrounded;
        bool _airStartHeightRecorded;
        float _airStartHeight;
        float _idleTimer;
        float _idleVariantCooldown;
        int _idleVariantStartFrame;
        const float PushAnimSpeed = 0.05f;
        const string IdleVariantTag = "IdleVariation";

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
            _idleVariantPlayingHash = Animator.StringToHash(idleVariantPlayingParam);
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
            InteractionManager.OnMove += HandleMoveInput;
            InteractionManager.OnJumpStarted += CancelIdleVariation;
            InteractionManager.OnButtonPressed += HandleButtonPressed;
            if (locomotion != null)
            {
                locomotion.OnLanded += HandleLanded;
                _wasGrounded = locomotion.IsGrounded;
            }

            GameStateManager.OnStateChanged += HandleStateChanged;
            GameState state = GameStateManager.Instance != null
                ? GameStateManager.Instance.CurrentState
                : GameState.Gameplay;
            ApplyStealth(state);

            SetRootMotionEnabled(true);
        }

        void OnDisable()
        {
            CancelIdleVariation();
            _unbindJump?.Invoke();
            _unbindJump = null;
            InteractionManager.OnInteractStarted -= HandleInteractStarted;
            InteractionManager.OnMove -= HandleMoveInput;
            InteractionManager.OnJumpStarted -= CancelIdleVariation;
            InteractionManager.OnButtonPressed -= HandleButtonPressed;
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
                TrackAirStartHeight();
            }

            UpdateIdleVariant(speed, state);

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

        void UpdateIdleVariant(float speed, GameState state)
        {
            InteractionManager input = InteractionManager.Instance;
            bool hasInput = input != null &&
                            (input.MoveInput.sqrMagnitude > 0f || input.IsJumpHeld || input.JumpPressedThisFrame);
            bool canIdle = state == GameState.Gameplay || state == GameState.GameplayNoJump;
            if (!canIdle || hasInput || speed > idleSpeedThreshold || _pushing ||
                (locomotion != null && (!locomotion.IsGrounded || locomotion.IsClimbing ||
                                        locomotion.IsScriptedRunning || !locomotion.LocomotionEnabled)))
            {
                CancelIdleVariation();
                return;
            }

            if (IsPlayingIdleVariation)
            {
                // Allow the trigger to be evaluated before checking for a natural exit.
                bool inVariant = animator.GetCurrentAnimatorStateInfo(0).IsTag(IdleVariantTag) ||
                                 (animator.IsInTransition(0) &&
                                  animator.GetNextAnimatorStateInfo(0).IsTag(IdleVariantTag));
                if (!inVariant && Time.frameCount > _idleVariantStartFrame + 1)
                {
                    CancelIdleVariation();
                    _idleVariantCooldown = idleVariantInterval;
                }
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

            SetIdleVariationPlaying(true);
            _idleVariantStartFrame = Time.frameCount;

            // Idle 1 remains the default state. Fire one trigger so a variant plays once.
            if (UnityEngine.Random.value < 0.5f)
                animator.SetTrigger(_idleVariant2Hash);
            else
                animator.SetTrigger(_idleVariant3Hash);
            _idleTimer = 0f;
        }

        void SetIdleVariationPlaying(bool playing)
        {
            IsPlayingIdleVariation = playing;
            if (animator != null)
                animator.SetBool(_idleVariantPlayingHash, playing);
        }

        void CancelIdleVariation()
        {
            ResetIdleTimer();
            if (!IsPlayingIdleVariation)
                return;

            SetIdleVariationPlaying(false);
            if (animator != null)
            {
                animator.ResetTrigger(_idleVariant2Hash);
                animator.ResetTrigger(_idleVariant3Hash);
            }
        }

        void HandleMoveInput(Vector2 move)
        {
            if (move.sqrMagnitude > 0f)
                CancelIdleVariation();
        }

        void HandleButtonPressed(bool isJump)
        {
            CancelIdleVariation();
        }

        void HandleJumped()
        {
            CancelIdleVariation();
            _landRunLandingPending = false;
            _landRunLandingSpeed = 0f;
            RecordAirStartHeight();

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
            if (locomotion == null)
                return;

            float landedHeight = GetGroundHeight();
            float fallDistance = _airStartHeightRecorded
                ? Mathf.Max(0f, _airStartHeight - landedHeight)
                : 0f;
            _airStartHeightRecorded = false;
            TriggerLandingAnimation(fallDistance);

            if (_landRunLandingPending)
            {
                _landRunLandingPending = false;
                locomotion.StartLandingRunMovement(landingRunMoveDuration, _landRunLandingSpeed);
            }
        }

        void ResetIdleTimer()
        {
            _idleTimer = 0f;
            _idleVariantCooldown = 0f;
        }

        void TrackAirStartHeight()
        {
            bool grounded = locomotion.IsGrounded;
            if (!grounded && _wasGrounded)
                RecordAirStartHeight();

            _wasGrounded = grounded;
        }

        void RecordAirStartHeight()
        {
            if (_airStartHeightRecorded)
                return;

            _airStartHeight = GetGroundHeight();
            _airStartHeightRecorded = true;
        }

        float GetGroundHeight()
        {
            return _characterController != null
                ? _characterController.bounds.min.y
                : transform.position.y;
        }

        void TriggerLandingAnimation(float fallDistance)
        {
            animator.ResetTrigger(_landHash);
            animator.ResetTrigger(_landRunHash);
            animator.ResetTrigger(_impactfulLandHash);

            float horizontalSpeed = locomotion != null ? Mathf.Abs(locomotion.HorizontalSpeed) : 0f;
            bool impactful = fallDistance > impactfulLandingDistance;
            bool landRun = !impactful && horizontalSpeed >= landRunSpeedThreshold;
            _landRunLandingPending = landRun;
            _landRunLandingSpeed = landRun ? locomotion.HorizontalSpeed : 0f;

            if (impactful)
                animator.SetTrigger(_impactfulLandHash);
            else if (landRun)
                animator.SetTrigger(_landRunHash);
            else
                animator.SetTrigger(_landHash);

        }

        void HandleStateChanged(GameState previous, GameState current)
        {
            ApplyStealth(current);
        }

        void ApplyStealth(GameState state)
        {
            _stealthTarget = state == GameState.GameplayStealth || state == GameState.GameplayStealthForced
                ? StealthOn
                : StealthOff;
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
