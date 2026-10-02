using UnityEngine;

namespace Character
{
    /// <summary>
    /// Drives the enemy's Speed blend tree from movement, with optional gait selection.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EnemyCharacterAnimation : MonoBehaviour
    {
        [SerializeField] Animator animator;

        [Header("Blend Tree")]
        [SerializeField] string movementSpeedParam = "Speed";
        [SerializeField, Min(0f)] float walkBlendSpeed = 0.5f;
        [SerializeField, Min(0f)] float runBlendSpeed = 1f;
        [Tooltip("Speed blend smoothing time in seconds. Smaller values respond faster; 0 changes instantly.")]
        [SerializeField, Min(0f)] float speedSmoothTime = 0.15f;

        [Header("Movement")]
        [Tooltip("Transform whose world movement drives animation speed. Defaults to the nearest parent with a locomotion controller or SerumActionBridge, then this object.")]
        [SerializeField] Transform movementRoot;
        [Tooltip("Scales normalized movement when no walk/run action has selected a gait.")]
        [SerializeField, Min(0f)] float movementSpeedMultiplier = 1f;
        [Tooltip("Used as the normalization maximum when the movement root has no locomotion controller.")]
        [SerializeField, Min(0.01f)] float fallbackMaxMovementSpeed = 4f;
        int _movementSpeedHash;
        float? _selectedBlendSpeed;
        float _movementSpeed;
        Vector3 _lastMovementRootPosition;
        bool _hasMovementRootPosition;
        ICharacterLocomotion _rootLocomotion;

        public float MovementSpeed => _movementSpeed;

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (movementRoot == null)
                movementRoot = FindMovementRoot();

            CacheRootLocomotion();
            CacheParameterHashes();
        }

        void LateUpdate()
        {
            UpdateMovementSpeed();
        }

        void OnEnable()
        {
            _hasMovementRootPosition = false;
        }

        /// <summary>Legacy UnityEvent entry point: selects idle (Speed = 0).</summary>
        public void PlaySearch()
        {
            SetSpeed(0f);
        }

        /// <summary>Selects the walk blend value. Actual movement keeps it at zero while stopped.</summary>
        public void PlayWalk()
        {
            SetSpeed(walkBlendSpeed);
        }

        /// <summary>Overrides the transform used to measure movement for the walk animation.</summary>
        public void SetMovementRoot(Transform root)
        {
            movementRoot = root;
            _hasMovementRootPosition = false;
            CacheRootLocomotion();
        }

        /// <summary>Selects the run blend value. Actual movement keeps it at zero while stopped.</summary>
        public void PlayRun()
        {
            SetSpeed(runBlendSpeed);
        }

        /// <summary>Selects idle.</summary>
        public void StopMovement()
        {
            PlaySearch();
        }

        void CacheParameterHashes()
        {
            _movementSpeedHash = Animator.StringToHash(movementSpeedParam);
        }

        public void SetSpeed(float value)
        {
            _selectedBlendSpeed = Mathf.Max(0f, value);
        }

        public void ClearSpeedOverride()
        {
            _selectedBlendSpeed = null;
        }

        void UpdateMovementSpeed()
        {
            if (animator == null || movementRoot == null)
                return;

            Vector3 currentPosition = movementRoot.position;
            float targetSpeed = 0f;
            if (!_hasMovementRootPosition)
            {
                _lastMovementRootPosition = currentPosition;
                _hasMovementRootPosition = true;
            }
            else
            {
                Vector3 delta = currentPosition - _lastMovementRootPosition;
                delta.y = 0f;
                float worldSpeed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
                float maxSpeed = _rootLocomotion != null
                    ? _rootLocomotion.MoveSpeed
                    : fallbackMaxMovementSpeed;
                targetSpeed = _selectedBlendSpeed.HasValue
                    ? (worldSpeed > 0.01f ? _selectedBlendSpeed.Value : 0f)
                    : Mathf.Clamp01(worldSpeed / Mathf.Max(0.01f, maxSpeed) * movementSpeedMultiplier) * runBlendSpeed;
                _lastMovementRootPosition = currentPosition;
            }

            // Exponential lerp gives the same response across frame rates.
            float blend = speedSmoothTime > 0f
                ? 1f - Mathf.Exp(-Time.deltaTime / speedSmoothTime)
                : 1f;
            _movementSpeed = Mathf.Lerp(_movementSpeed, targetSpeed, blend);
            if (Mathf.Abs(_movementSpeed - targetSpeed) < 0.0001f)
                _movementSpeed = targetSpeed;
            animator.SetFloat(_movementSpeedHash, _movementSpeed);
        }

        void CacheRootLocomotion()
        {
            _rootLocomotion = null;
            if (movementRoot == null)
                return;

            MonoBehaviour[] components = movementRoot.GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is ICharacterLocomotion locomotion)
                {
                    _rootLocomotion = locomotion;
                    return;
                }
            }
        }

        Transform FindMovementRoot()
        {
            for (Transform candidate = transform; candidate != null; candidate = candidate.parent)
            {
                foreach (MonoBehaviour component in candidate.GetComponents<MonoBehaviour>())
                {
                    if (component is ICharacterLocomotion || component is Events.SerumActionBridge)
                        return candidate;
                }
            }

            return transform;
        }
    }
}
