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

        int _searchTriggerHash;
        int _walkTriggerHash;
        int _runTriggerHash;

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            CacheParameterHashes();
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

            animator.SetTrigger(_walkTriggerHash);
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
        }
    }
}
