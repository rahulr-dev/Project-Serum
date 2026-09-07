using UnityEngine;

namespace Character
{
    public class CharacterAnimation : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] MonoBehaviour moveSpeedSource;
        [SerializeField] string speedParam = "speed";
        [SerializeField] float dampTime = 0.1f;

        INormalizedMoveSpeed _speedSource;
        int _speedHash;
        bool _speedOverrideActive;
        float _speedOverride;

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

            _speedHash = Animator.StringToHash(speedParam);
            _speedSource = moveSpeedSource as INormalizedMoveSpeed;
            if (_speedSource == null)
            {
                Debug.LogError(
                    "CharacterAnimation needs a moveSpeedSource that implements INormalizedMoveSpeed.",
                    this);
            }
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
    }
}
