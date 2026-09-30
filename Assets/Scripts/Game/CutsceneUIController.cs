using Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    /// <summary>Enable for a cutscene and disable when it ends. Keep this on an active parent of the skip UI.</summary>
    public sealed class CutsceneUIController : MonoBehaviour
    {
        [Tooltip("The object to show. Keep this controller outside that object's hierarchy.")]
        [SerializeField] GameObject skipButton;
        [SerializeField, Min(0.1f)] float displayDuration = 3f;
        [SerializeField] UnityEvent onSkip = new UnityEvent();

        float _hideAt;
        bool _visible;
        bool _skipInvoked;

        void OnEnable()
        {
            _skipInvoked = false;
            Hide();
            InteractionManager.OnButtonPressed += HandleButtonPressed;
        }

        void OnDisable()
        {
            InteractionManager.OnButtonPressed -= HandleButtonPressed;
            Hide();
        }

        void Update()
        {
            if (_visible && Time.unscaledTime >= _hideAt)
                Hide();
        }

        void HandleButtonPressed(bool _)
        {
            if (_skipInvoked || skipButton == null)
                return;

            // Check expiry here as input is published before this component's Update.
            if (_visible && Time.unscaledTime >= _hideAt)
                Hide();

            if (_visible && skipButton.activeInHierarchy)
            {
                _skipInvoked = true;
                Hide();
                onSkip.Invoke();
                return;
            }

            _hideAt = Time.unscaledTime + Mathf.Max(0.1f, displayDuration);
            _visible = true;
            skipButton.SetActive(true);
        }

        void Hide()
        {
            _visible = false;
            if (skipButton != null)
                skipButton.SetActive(false);
        }
    }
}
