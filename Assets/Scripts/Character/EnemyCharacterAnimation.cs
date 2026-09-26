using UnityEngine;

namespace Character
{
    /// <summary>
    /// Fires the enemy Animator's search, walk, and run triggers.
    /// Configure the parameter names to match the enemy Animator Controller.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EnemyCharacterAnimation : MonoBehaviour
    {
        [SerializeField] Animator animator;

        [Header("Animator Triggers")]
        [SerializeField] string searchTrigger = "search";
        [SerializeField] string walkTrigger = "walk";
        [SerializeField] string runTrigger = "run";

        [Header("Movement")]
        [Tooltip("Root transform whose world movement drives the walk animation. Defaults to the top-level enemy object.")]
        [SerializeField] Transform movementRoot;
        [Tooltip("Scales the normalized root movement before it is applied to the walk animation.")]
        [SerializeField, Min(0f)] float movementSpeedMultiplier = 1f;
        [Tooltip("Used as the normalization maximum when the movement root has no locomotion controller.")]
        [SerializeField, Min(0.01f)] float fallbackMaxMovementSpeed = 4f;
        [SerializeField] string movementSpeedParam = "Speed";

        int _searchTriggerHash;
        int _walkTriggerHash;
        int _runTriggerHash;
        int _movementSpeedHash;
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
                movementRoot = transform.root;

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

        /// <summary>Fires the search trigger.</summary>
        public void PlaySearch()
        {
            if (animator == null)
                return;

            animator.SetTrigger(_searchTriggerHash);
        }

        /// <summary>Fires the walk trigger.</summary>
        public void PlayWalk()
        {
            if (animator == null)
                return;

            animator.SetFloat(_movementSpeedHash, _movementSpeed);
            animator.SetTrigger(_walkTriggerHash);
        }

        /// <summary>Overrides the transform used to measure movement for the walk animation.</summary>
        public void SetMovementRoot(Transform root)
        {
            movementRoot = root;
            _hasMovementRootPosition = false;
            CacheRootLocomotion();
        }

        /// <summary>Fires the run trigger.</summary>
        public void PlayRun()
        {
            if (animator == null)
                return;

            animator.SetTrigger(_runTriggerHash);
        }

        /// <summary>Returns the enemy to its search state.</summary>
        public void StopMovement()
        {
            PlaySearch();
        }

        void CacheParameterHashes()
        {
            _searchTriggerHash = Animator.StringToHash(searchTrigger);
            _walkTriggerHash = Animator.StringToHash(walkTrigger);
            _runTriggerHash = Animator.StringToHash(runTrigger);
            _movementSpeedHash = Animator.StringToHash(movementSpeedParam);
        }

        void UpdateMovementSpeed()
        {
            if (animator == null || movementRoot == null)
                return;

            Vector3 currentPosition = movementRoot.position;
            if (!_hasMovementRootPosition)
            {
                _lastMovementRootPosition = currentPosition;
                _hasMovementRootPosition = true;
                _movementSpeed = 0f;
            }
            else
            {
                Vector3 delta = currentPosition - _lastMovementRootPosition;
                delta.y = 0f;
                float worldSpeed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
                float maxSpeed = _rootLocomotion != null
                    ? _rootLocomotion.MoveSpeed
                    : fallbackMaxMovementSpeed;
                _movementSpeed = Mathf.Clamp01(
                    worldSpeed / Mathf.Max(0.01f, maxSpeed) * movementSpeedMultiplier);
                _lastMovementRootPosition = currentPosition;
            }

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
    }
}
