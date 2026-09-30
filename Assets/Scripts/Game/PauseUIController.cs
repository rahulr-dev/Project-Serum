using Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    /// <summary>Keep this controller on an active object outside the pause panel's hierarchy.</summary>
    public sealed class PauseUIController : MonoBehaviour
    {
        [Tooltip("The pause panel. Keep this controller outside that object's hierarchy.")]
        [SerializeField] GameObject pauseUI;
        [SerializeField] UnityEvent onPause = new UnityEvent();
        [SerializeField] UnityEvent onResume = new UnityEvent();

        float _previousTimeScale = 1f;
        GameStateManager _pausedStateManager;

        public bool IsPaused { get; private set; }

        void OnEnable()
        {
            SetUIVisible(false);
            InteractionManager.OnPausePressed += TogglePause;
            GameStateManager.OnStateChanged += HandleStateChanged;
        }

        void OnDisable()
        {
            InteractionManager.OnPausePressed -= TogglePause;
            GameStateManager.OnStateChanged -= HandleStateChanged;
            // Restore time even if the scene or controller is disabled while paused.
            ClosePause(false);
            SetUIVisible(false);
        }

        public void TogglePause()
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }

        public void Pause()
        {
            if (!isActiveAndEnabled || IsPaused || pauseUI == null)
                return;

            _previousTimeScale = Time.timeScale;
            IsPaused = true;
            Time.timeScale = 0f;
            SetUIVisible(true);
            _pausedStateManager = GameStateManager.Instance;
            if (_pausedStateManager != null)
                _pausedStateManager.Pause();

            onPause.Invoke();
        }

        public void Resume()
        {
            ClosePause(true);
        }

        void ClosePause(bool invokeEvent)
        {
            if (!IsPaused)
                return;

            IsPaused = false;
            Time.timeScale = _previousTimeScale;
            SetUIVisible(false);
            GameStateManager stateManager = _pausedStateManager;
            _pausedStateManager = null;
            if (stateManager != null && stateManager.CurrentState == GameState.Paused)
                stateManager.Resume();

            if (invokeEvent)
                onResume.Invoke();
        }

        void HandleStateChanged(GameState previous, GameState current)
        {
            if (IsPaused && current != GameState.Paused)
                Resume();
        }

        void SetUIVisible(bool visible)
        {
            if (pauseUI != null)
                pauseUI.SetActive(visible);
        }
    }
}
