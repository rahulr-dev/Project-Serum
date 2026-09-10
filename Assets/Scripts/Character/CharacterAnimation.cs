using Game;
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
        [SerializeField] string stealthParam = "stealth";
        [SerializeField] float dampTime = 0.1f;
        [SerializeField] float stealthDampTime = 0.25f;

        INormalizedMoveSpeed _speedSource;
        int _speedHash;
        int _jumpHash;
        int _stealthHash;
        bool _speedOverrideActive;
        float _speedOverride;
        float _stealthTarget = StealthOff;

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

            if (locomotion == null)
                locomotion = GetComponent<SideScrollerController>();

            _speedHash = Animator.StringToHash(speedParam);
            _jumpHash = Animator.StringToHash(jumpParam);
            _stealthHash = Animator.StringToHash(stealthParam);
            _speedSource = moveSpeedSource as INormalizedMoveSpeed;
            if (_speedSource == null)
            {
                Debug.LogError(
                    "CharacterAnimation needs a moveSpeedSource that implements INormalizedMoveSpeed.",
                    this);
            }
        }

        void OnEnable()
        {
            if (locomotion != null)
            {
                locomotion.OnJumped += HandleJumped;
                locomotion.OnLanded += HandleLanded;
            }

            GameStateManager.OnStateChanged += HandleStateChanged;
            ApplyStealth(GameStateManager.Instance != null
                ? GameStateManager.Instance.CurrentState
                : GameState.Gameplay);
        }

        void OnDisable()
        {
            if (locomotion != null)
            {
                locomotion.OnJumped -= HandleJumped;
                locomotion.OnLanded -= HandleLanded;
            }

            GameStateManager.OnStateChanged -= HandleStateChanged;
        }

        void Update()
        {
            if (animator == null)
                return;

            float speed = _speedOverrideActive
                ? _speedOverride
                : _speedSource != null
                    ? _speedSource.NormalizedSpeed
                    : 0f;

            if (dampTime > 0f)
                animator.SetFloat(_speedHash, speed, dampTime, Time.deltaTime);
            else
                animator.SetFloat(_speedHash, speed);

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

        void HandleJumped()
        {
            if (animator == null)
                return;

            animator.SetTrigger(_jumpHash);
        }

        void HandleLanded()
        {
            if (animator == null)
                return;

            animator.ResetTrigger(_jumpHash);
        }

        void HandleStateChanged(GameState previous, GameState current)
        {
            ApplyStealth(current);
        }

        void ApplyStealth(GameState state)
        {
            _stealthTarget = state == GameState.GameplayStealth ? StealthOn : StealthOff;
        }
    }
}
