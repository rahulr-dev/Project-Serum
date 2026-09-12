using UnityEngine;

namespace Game
{
    public class TransitionAnimatorDriver : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] string inTrigger = "In";

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        void OnEnable()
        {
            TransitionManager.OnTransitionInStarted += HandleInStarted;
        }

        void OnDisable()
        {
            TransitionManager.OnTransitionInStarted -= HandleInStarted;
        }

        void HandleInStarted()
        {
            if (animator == null || string.IsNullOrEmpty(inTrigger))
                return;

            animator.ResetTrigger(inTrigger);
            animator.SetTrigger(inTrigger);
        }
    }
}
