using System.Collections.Generic;
using Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Serum.PostProcessing
{
    /// <summary>
    /// Connects post-processing looks to GameState. Controllers are scene-local,
    /// while this manager persists with the other game managers.
    /// </summary>
    public class PostProcessingStateManager : MonoBehaviour
    {
        public static PostProcessingStateManager Instance { get; private set; }

        readonly List<PostProcessingStateController> controllers = new();

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

        void OnEnable()
        {
            GameStateManager.OnStateChanged += HandleGameStateChanged;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void Start()
        {
            RegisterSceneControllers();
            ApplyCurrentState(true);
        }

        void OnDisable()
        {
            GameStateManager.OnStateChanged -= HandleGameStateChanged;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void Register(PostProcessingStateController controller)
        {
            if (Instance != null)
                Instance.RegisterController(controller);
        }

        public static void Unregister(PostProcessingStateController controller)
        {
            if (Instance != null)
                Instance.controllers.Remove(controller);
        }

        void HandleGameStateChanged(GameState previousState, GameState currentState)
        {
            ApplyState(currentState, false);
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterSceneControllers();
            ApplyCurrentState(false);
        }

        void RegisterSceneControllers()
        {
            PostProcessingStateController[] sceneControllers =
                FindObjectsByType<PostProcessingStateController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (PostProcessingStateController controller in sceneControllers)
                RegisterController(controller);
        }

        void RegisterController(PostProcessingStateController controller)
        {
            if (controller == null || controllers.Contains(controller))
                return;

            controllers.Add(controller);

            if (GameStateManager.Instance != null)
                controller.SetActive(controller.MatchesState(GameStateManager.Instance.CurrentState), false);
        }

        void ApplyCurrentState(bool immediate)
        {
            if (GameStateManager.Instance != null)
                ApplyState(GameStateManager.Instance.CurrentState, immediate);
        }

        void ApplyState(GameState state, bool immediate)
        {
            for (int i = controllers.Count - 1; i >= 0; i--)
            {
                PostProcessingStateController controller = controllers[i];
                if (controller == null)
                {
                    controllers.RemoveAt(i);
                    continue;
                }

                controller.SetActive(controller.MatchesState(state), immediate);
            }
        }
    }
}
