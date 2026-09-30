using System.Collections;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Serum.MenuUI
{
    public sealed class SerumMainMenu : MonoBehaviour
    {
        [Tooltip("-1 loads the scene immediately after this menu in the enabled build scene list.")]
        public int newGameBuildIndex = -1;
        public Button newGameButton;
        public Button continueButton;
        public RectTransform selection;
        public RectTransform underline;
        public GameObject menuGroup;
        public GameObject optionsPanel;
        public GameObject messagePanel;
        public TMP_Text messageText;
        public Button optionsBack;
        public Button messageBack;
        public Slider volume;
        public Toggle fullscreen;
        public CanvasGroup fade;
        bool loading;
        SerumMenuItem activeItem;
        bool CanContinue => GameProgressManager.Instance != null && GameProgressManager.Instance.HasCheckpoint;

        void Start()
        {
            if (GameProgressManager.Instance == null)
                new GameObject("GameProgressManager").AddComponent<GameProgressManager>();
            optionsPanel.SetActive(false);
            messagePanel.SetActive(false);
            fade.alpha = 0;
            fade.blocksRaycasts = false;
            volume.SetValueWithoutNotify(AudioListener.volume);
            fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            RefreshContinueButton();
            newGameButton.Select();
        }

        void Update()
        {
            if (!loading && continueButton != null && continueButton.interactable != CanContinue)
                RefreshContinueButton();
        }

        void RefreshContinueButton()
        {
            if (continueButton == null) return;
            bool canContinue = CanContinue;
            continueButton.interactable = canContinue;
            if (!canContinue && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == continueButton.gameObject)
                newGameButton.Select();

            // Explicit navigation must skip Continue when it is disabled.
            var exitButton = continueButton.navigation.selectOnUp;
            var newGameNavigation = newGameButton.navigation;
            newGameNavigation.selectOnUp = canContinue ? continueButton : exitButton;
            newGameButton.navigation = newGameNavigation;
            if (exitButton != null)
            {
                var exitNavigation = exitButton.navigation;
                exitNavigation.selectOnDown = canContinue ? continueButton : newGameButton;
                exitButton.navigation = exitNavigation;
            }
        }

        public void Highlight(SerumMenuItem item)
        {
            if (loading) return;
            if (activeItem != null) activeItem.SetHighlighted(false);
            activeItem = item;
            item.SetHighlighted(true);
            selection.anchoredPosition = new Vector2(-48, item.GetComponent<RectTransform>().anchoredPosition.y);
            underline.anchoredPosition = new Vector2(0, item.GetComponent<RectTransform>().anchoredPosition.y - 38);
        }

        public void NewGame()
        {
            StartGame(true);
        }

        public void Continue()
        {
            if (loading) return;
            RefreshContinueButton();
            if (!CanContinue) return;
            int levelOne = SceneUtility.GetBuildIndexByScenePath("Assets/Scene/Level_01/Level_1.unity");
            if (levelOne < 0 || !Application.CanStreamedLevelBeLoaded(levelOne))
            {
                ShowMessage("THE JOURNEY AWAITS", "Enable Level_1 in the Build Profiles scene list, then try again.");
                return;
            }
            StartCoroutine(LoadScene(levelOne));
        }

        void StartGame(bool clearPlayerPrefs)
        {
            if (loading) return;
            int current = SceneManager.GetActiveScene().buildIndex;
            int next = newGameBuildIndex >= 0 ? newGameBuildIndex : (current >= 0 ? current + 1 : 1);
            if (next < 0 || next >= SceneManager.sceneCountInBuildSettings || !Application.CanStreamedLevelBeLoaded(next))
            {
                ShowMessage("THE JOURNEY AWAITS", "Add your gameplay scene after Main menu in the enabled Build Profiles scene list, then try again.");
                return;
            }
            if (next == current)
            {
                ShowMessage("CHOOSE A DESTINATION", "New Game points to the menu itself. Choose a different scene index on the Main Menu UI component.");
                return;
            }
            if (clearPlayerPrefs)
            {
                PlayerPrefs.DeleteAll();
                GameProgressManager.ClearSavedProgress();
            }
            StartCoroutine(LoadScene(next));
        }

        IEnumerator LoadScene(int index)
        {
            loading = true;
            fade.blocksRaycasts = true;
            float elapsed = 0;
            while (elapsed < .45f)
            {
                elapsed += Time.unscaledDeltaTime;
                fade.alpha = Mathf.Clamp01(elapsed / .45f);
                yield return null;
            }
            Time.timeScale = 1;
            var operation = SceneManager.LoadSceneAsync(index);
            if (operation != null) yield return operation;
        }

        public void OpenOptions()
        {
            menuGroup.SetActive(false);
            optionsPanel.SetActive(true);
            optionsBack.Select();
        }

        public void ClosePanel()
        {
            optionsPanel.SetActive(false);
            messagePanel.SetActive(false);
            menuGroup.SetActive(true);
            RefreshContinueButton();
            newGameButton.Select();
        }

        void ShowMessage(string title, string message)
        {
            menuGroup.SetActive(false);
            messageText.text = title + "\n\n<size=23>" + message + "</size>";
            messagePanel.SetActive(true);
            messageBack.Select();
        }

        public void SetVolume(float value) => AudioListener.volume = value;
        public void SetFullscreen(bool value) => Screen.fullScreen = value;
        public void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
