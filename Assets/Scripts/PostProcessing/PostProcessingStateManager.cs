using System.Collections.Generic;
using Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Serum.PostProcessing
{
    /// <summary>
    /// Owns the current post-processing state. Controllers are scene-local, while
    /// this manager persists with the other game managers. Gameplay systems should
    /// set this state directly. Optionally, it can mirror GameState changes.
    /// </summary>
    public class PostProcessingStateManager : MonoBehaviour
    {
        [System.Serializable]
        struct GameStateBinding
        {
            public GameState gameState;
            public PostProcessingState postProcessingState;
        }

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

        [Tooltip("When enabled, matching GameState changes automatically update the post-processing state.")]
        [SerializeField] bool followGameState;

        [Tooltip("Automatically switch to the game-over look when GameState enters GameOver, then restore the previous look when it exits.")]
        [SerializeField] bool followGameOverState = true;

        [Tooltip("GameState-to-post-processing mappings used when Follow Game State is enabled. Remove an entry to ignore that GameState.")]
        [SerializeField] GameStateBinding[] gameStateBindings =
        {
            new GameStateBinding { gameState = GameState.Loading, postProcessingState = PostProcessingState.Loading },
            new GameStateBinding { gameState = GameState.MainMenu, postProcessingState = PostProcessingState.MainMenu },
            new GameStateBinding { gameState = GameState.Gameplay, postProcessingState = PostProcessingState.Gameplay },
            new GameStateBinding { gameState = GameState.GameplayNoJump, postProcessingState = PostProcessingState.GameplayNoJump },
            new GameStateBinding { gameState = GameState.GameplayStealth, postProcessingState = PostProcessingState.GameplayStealth },
            new GameStateBinding { gameState = GameState.GameplayDialogue, postProcessingState = PostProcessingState.GameplayDialogue },
            new GameStateBinding { gameState = GameState.Dialogue, postProcessingState = PostProcessingState.Dialogue },
            new GameStateBinding { gameState = GameState.Cutscene, postProcessingState = PostProcessingState.Cutscene },
            new GameStateBinding { gameState = GameState.QTE, postProcessingState = PostProcessingState.QTE },
            new GameStateBinding { gameState = GameState.Paused, postProcessingState = PostProcessingState.Paused },
            new GameStateBinding { gameState = GameState.GameOver, postProcessingState = PostProcessingState.GameOver },
            new GameStateBinding { gameState = GameState.GameplayPushing, postProcessingState = PostProcessingState.GameplayPushing },
            new GameStateBinding { gameState = GameState.GameplayStealthForced, postProcessingState = PostProcessingState.GameplayStealthForced }
        };

        [Tooltip("State used by SwitchState. This parameterless method is available in UnityEvent/Actions pickers.")]
        [SerializeField] PostProcessingState actionState = PostProcessingState.Gameplay;

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

            if (followGameState || followGameOverState)
                GameStateManager.OnStateChanged += HandleGameStateChanged;
        }

        void Start()
        {
            RegisterSceneControllers();

            if (GameStateManager.Instance != null)
            {
                GameState gameState = GameStateManager.Instance.CurrentState;
                if (followGameOverState && gameState == GameState.GameOver)
                    SetState(PostProcessingState.GameOver, true);
                else if (followGameState)
                    ApplyGameStateBinding(gameState, true);
            }

            ApplyCurrentState(true);
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            if (followGameState || followGameOverState)
                GameStateManager.OnStateChanged -= HandleGameStateChanged;
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

        /// <summary>
        /// Switches to the state selected in the inspector. Use this from a
        /// UnityEvent or Actions picker, which only exposes parameterless methods.
        /// </summary>
        public void SwitchState()
        {
            SetState(actionState);
        }

        // Parameterless state actions for UnityEvent/Actions pickers. Those
        // pickers cannot reliably supply enum arguments for each invocation.
        public void SwitchToLoading() => SetState(PostProcessingState.Loading);
        public void SwitchToMainMenu() => SetState(PostProcessingState.MainMenu);
        public void SwitchToGameplay() => SetState(PostProcessingState.Gameplay);
        public void SwitchToGameplayNoJump() => SetState(PostProcessingState.GameplayNoJump);
        public void SwitchToGameplayStealth() => SetState(PostProcessingState.GameplayStealth);
        public void SwitchToGameplayDialogue() => SetState(PostProcessingState.GameplayDialogue);
        public void SwitchToDialogue() => SetState(PostProcessingState.Dialogue);
        public void SwitchToCutscene() => SetState(PostProcessingState.Cutscene);
        public void SwitchToQTE() => SetState(PostProcessingState.QTE);
        public void SwitchToPaused() => SetState(PostProcessingState.Paused);
        public void SwitchToGameOver() => SetState(PostProcessingState.GameOver);
        public void SwitchToGameplayPushing() => SetState(PostProcessingState.GameplayPushing);
        public void SwitchToGameplayStealthForced() => SetState(PostProcessingState.GameplayStealthForced);
        public void SwitchToIndoor() => SetState(PostProcessingState.Indoor);
        public void SwitchToChase() => SetState(PostProcessingState.Chase);
        public void SwitchToStealthArea() => SetState(PostProcessingState.StealthArea);

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

        void HandleGameStateChanged(GameState previousState, GameState currentState)
        {
            if (followGameOverState)
            {
                if (currentState == GameState.GameOver)
                {
                    SetState(PostProcessingState.GameOver);
                    return;
                }

                if (previousState == GameState.GameOver && CurrentState == PostProcessingState.GameOver)
                {
                    SetState(PreviousState);
                    return;
                }
            }

            if (followGameState)
                ApplyGameStateBinding(currentState, false);
        }

        void ApplyGameStateBinding(GameState gameState, bool immediate)
        {
            if (gameStateBindings == null)
                return;

            for (int i = 0; i < gameStateBindings.Length; i++)
            {
                if (gameStateBindings[i].gameState != gameState)
                    continue;

                SetState(gameStateBindings[i].postProcessingState, immediate);
                return;
            }
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
