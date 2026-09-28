using UnityEngine;
using UnityEngine.Rendering;

namespace Serum.PostProcessing
{
    /// <summary>
    /// Controls one existing post-processing Volume for selected post-processing states.
    /// Configure the Volume's profile normally; this component only controls its weight.
    /// </summary>
    public class PostProcessingStateController : MonoBehaviour
    {
        [SerializeField] PostProcessingStateManager.PostProcessingState[] states =
        {
            PostProcessingStateManager.PostProcessingState.Gameplay
        };
        [SerializeField] Volume volume;
        [SerializeField, Min(0f)] float transitionDuration = 1.5f;

        bool isActive;
        bool initialized;
        float currentWeight;

        public Volume Volume => volume;
        public bool IsActive => isActive;
        public float TransitionDuration => transitionDuration;

        public bool MatchesState(PostProcessingStateManager.PostProcessingState state)
        {
            if (states == null)
                return false;

            for (int i = 0; i < states.Length; i++)
            {
                if (states[i] == state)
                    return true;
            }

            return false;
        }

        void Awake()
        {
            Initialize();
        }

        void OnEnable()
        {
            PostProcessingStateManager.Register(this);
        }

        void OnDisable()
        {
            PostProcessingStateManager.Unregister(this);
        }

        void Update()
        {
            Initialize();

            float duration = Mathf.Max(transitionDuration, 0.0001f);
            float step = Time.unscaledDeltaTime / duration;

            currentWeight = Mathf.MoveTowards(currentWeight, isActive ? 1f : 0f, step);
            if (volume != null)
                volume.weight = currentWeight;
        }

        /// <summary>Called by PostProcessingStateManager when its state changes.</summary>
        public void SetActive(bool active, bool immediate = false)
        {
            Initialize();
            isActive = active;

            if (!immediate)
                return;

            currentWeight = active ? 1f : 0f;
            if (volume != null)
                volume.weight = currentWeight;
        }

        void Initialize()
        {
            if (initialized)
                return;

            // Start neutral so a newly loaded scene cannot briefly show an
            // unselected profile before its state has been applied.
            currentWeight = 0f;
            if (volume != null)
                volume.weight = 0f;

            initialized = true;
        }
    }
}
