using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Serum.PostProcessing
{
    /// <summary>
    /// Owns the current post-processing state. Controllers are scene-local, while
    /// this manager persists with the other game managers. Gameplay systems should
    /// set this state directly; it deliberately does not subscribe to GameState.
    /// </summary>
    public class PostProcessingStateManager : MonoBehaviour
    {
        // Keep the legacy values aligned with GameState so existing controller
        // assignments in scenes remain valid after this system is decoupled.
        public enum PostProcessingState
        {
            Loading = 0,
            MainMenu = 1,
            Gameplay = 2,
            GameplayNoJump = 3,
            GameplayStealth = 4,
            GameplayDialogue = 5,
            Dialogue = 6,
            Cutscene = 7,
            QTE = 8,
            Paused = 9,
            GameOver = 10,
            GameplayPushing = 11,
            GameplayStealthForced = 12,

            Indoor = 13,
            Chase = 14,
            StealthArea = 15
        }

        public static PostProcessingStateManager Instance { get; private set; }

        [SerializeField] PostProcessingState initialState = PostProcessingState.Gameplay;

        readonly List<PostProcessingStateController> controllers = new();

        public PostProcessingState CurrentState { get; private set; }
        public PostProcessingState PreviousState { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentState = initialState;
            PreviousState = initialState;
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void Start()
        {
            RegisterSceneControllers();
            ApplyCurrentState(true);
        }

        void OnDisable()
        {
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

        /// <summary>
        /// Changes the active post-processing state. Call this from a trigger,
        /// encounter controller, or other gameplay object that owns the context.
        /// </summary>
        public void SetState(PostProcessingState state, bool immediate = false)
        {
            if (CurrentState == state)
                return;

            PreviousState = CurrentState;
            CurrentState = state;
            ApplyState(CurrentState, immediate);
        }

        /// <summary>Activates the game-over post-processing look.</summary>
        public void EnterGameOver(bool immediate = false)
        {
            SetState(PostProcessingState.GameOver, immediate);
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

            controller.SetActive(controller.MatchesState(CurrentState), false);
        }

        void ApplyCurrentState(bool immediate)
        {
            ApplyState(CurrentState, immediate);
        }

        void ApplyState(PostProcessingState state, bool immediate)
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
