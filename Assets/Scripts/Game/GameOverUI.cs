using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] GameObject rootPanel;
        [SerializeField] Button restartButton;

        void Awake()
        {
            ResolveReferences();
            SetRootVisible(false);
        }

        void OnEnable()
        {
            ResolveReferences();

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartFromCheckpoint);
                restartButton.onClick.AddListener(RestartFromCheckpoint);
            }

            GameStateManager.OnStateChanged += HandleStateChanged;
            SyncToCurrentState();
        }

        void OnDisable()
        {
            GameStateManager.OnStateChanged -= HandleStateChanged;

            if (restartButton != null)
                restartButton.onClick.RemoveListener(RestartFromCheckpoint);
        }

        void ResolveReferences()
        {
            if (rootPanel == null)
            {
                Transform panel = transform.Find("GameOver_Panel");
                if (panel != null)
                    rootPanel = panel.gameObject;
            }

            if (restartButton == null && rootPanel != null)
            {
                Transform button = rootPanel.transform.Find("RestartFromCheckpoint_Button");
                if (button != null)
                    restartButton = button.GetComponent<Button>();
            }
        }

        void HandleStateChanged(GameState previous, GameState current)
        {
            SetRootVisible(current == GameState.GameOver);
        }

        void SyncToCurrentState()
        {
            bool show = GameStateManager.Instance != null &&
                        GameStateManager.Instance.CurrentState == GameState.GameOver;
            SetRootVisible(show);
        }

        public void Show()
        {
            SetRootVisible(true);
        }

        public void Hide()
        {
            SetRootVisible(false);
        }

        public void RestartFromCheckpoint()
        {
            if (SerumSceneManager.Instance == null)
            {
                Debug.LogWarning("GameOverUI.RestartFromCheckpoint requires SerumSceneManager.", this);
                return;
            }

            SerumSceneManager.Instance.ReloadActiveScene();
        }

        void SetRootVisible(bool visible)
        {
            if (rootPanel == null)
                return;

            if (rootPanel.activeSelf != visible)
                rootPanel.SetActive(visible);
        }
    }
}
