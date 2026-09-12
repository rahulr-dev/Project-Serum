using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public class SerumSceneManager : MonoBehaviour
    {
        public static SerumSceneManager Instance { get; private set; }

        /// <summary>Raised when a scene load is requested, before the load runs.</summary>
        public static event Action OnSceneChangeRequested;

        [SerializeField] float sceneLoadDelay = 0f;

        Coroutine _loadRoutine;
        bool _waitingForTransition;

        public float SceneLoadDelay
        {
            get => sceneLoadDelay;
            set => sceneLoadDelay = Mathf.Max(0f, value);
        }

        public bool IsLoading => _loadRoutine != null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ReloadActiveScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || !active.isLoaded)
            {
                Debug.LogWarning("SerumSceneManager.ReloadActiveScene: no valid active scene.", this);
                return;
            }

            if (active.buildIndex >= 0)
                BeginLoad(active.buildIndex, null);
            else
                BeginLoad(-1, active.name);
        }

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("SerumSceneManager.LoadScene: scene name is empty.", this);
                return;
            }

            BeginLoad(-1, sceneName);
        }

        /// <summary>
        /// Called by TransitionManager when the In transition has finished.
        /// </summary>
        public void NotifyTransitionComplete()
        {
            _waitingForTransition = false;
        }

        void BeginLoad(int buildIndex, string sceneName)
        {
            if (_loadRoutine != null)
            {
                Debug.LogWarning("SerumSceneManager: scene load already in progress.", this);
                return;
            }

            _loadRoutine = StartCoroutine(LoadRoutine(buildIndex, sceneName));
        }

        IEnumerator LoadRoutine(int buildIndex, string sceneName)
        {
            _waitingForTransition = TransitionManager.Instance != null;

            OnSceneChangeRequested?.Invoke();

            while (_waitingForTransition)
                yield return null;

            float delay = Mathf.Max(0f, sceneLoadDelay);
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            ResetToGameplay();

            if (buildIndex >= 0)
                SceneManager.LoadScene(buildIndex);
            else
                SceneManager.LoadScene(sceneName);

            _loadRoutine = null;
        }

        static void ResetToGameplay()
        {
            if (GameStateManager.Instance == null)
                return;

            GameStateManager.Instance.EnterGameplay();
        }
    }
}
