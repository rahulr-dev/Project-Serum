using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public static event Action<GameState, GameState> OnStateChanged;

        public const string OverlayPrefsKey = "Serum.GameStateOverlay.Enabled";

        [SerializeField] GameState initialState = GameState.Gameplay;

        public GameState CurrentState { get; private set; }
        public GameState PreviousState { get; private set; }

        public bool AllowsMove => CurrentState == GameState.Gameplay || CurrentState == GameState.GameplayDialogue;
        public bool AllowsJump => AllowsMove;
        public bool AllowsInteract =>
            CurrentState == GameState.Gameplay ||
            CurrentState == GameState.GameplayDialogue ||
            CurrentState == GameState.Dialogue;

#if UNITY_EDITOR
        static readonly Color OverlayIdleBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        static readonly Color OverlayActiveBg = new Color(0.15f, 0.85f, 0.28f, 1f);
        static readonly Color OverlayIdleText = new Color(0.75f, 0.75f, 0.75f, 1f);
        Rect _overlayRect = new Rect(230f, 12f, 220f, 340f);
        GUIStyle _overlayKeyStyle;
#endif

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentState = initialState;
            PreviousState = initialState;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetState(GameState state)
        {
            TrySetState(state);
        }

        public bool TrySetState(GameState state)
        {
            if (CurrentState == state)
                return false;

            PreviousState = CurrentState;
            CurrentState = state;
            OnStateChanged?.Invoke(PreviousState, CurrentState);
            return true;
        }

        public bool IsState(GameState state)
        {
            return CurrentState == state;
        }

        public bool IsAny(params GameState[] states)
        {
            if (states == null)
                return false;

            for (int i = 0; i < states.Length; i++)
            {
                if (CurrentState == states[i])
                    return true;
            }

            return false;
        }

        public void EnterLoading() => SetState(GameState.Loading);
        public void EnterMainMenu() => SetState(GameState.MainMenu);
        public void EnterGameplay() => SetState(GameState.Gameplay);
        public void EnterGameplayDialogue() => SetState(GameState.GameplayDialogue);
        public void EnterDialogue() => SetState(GameState.Dialogue);
        public void EnterCutscene() => SetState(GameState.Cutscene);
        public void EnterGameOver() => SetState(GameState.GameOver);

        public void Pause()
        {
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (CurrentState != GameState.Paused)
                return;

            SetState(PreviousState);
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (!EditorPrefs.GetBool(OverlayPrefsKey, false))
                return;

            _overlayRect = GUI.Window(GetInstanceID(), _overlayRect, DrawOverlay, "Game State Overlay");
        }

        void DrawOverlay(int windowId)
        {
            DrawRow($"Current  {CurrentState}", true, 200f);
            DrawRow($"Previous  {PreviousState}", false, 200f);
            GUILayout.Space(6);
            DrawRow("Move", AllowsMove, 200f);
            DrawRow("Jump", AllowsJump, 200f);
            DrawRow("Interact", AllowsInteract, 200f);
            GUILayout.Space(6);

            if (GUILayout.Button("Gameplay"))
                EnterGameplay();
            if (GUILayout.Button("Gameplay Dialogue"))
                EnterGameplayDialogue();
            if (GUILayout.Button("Dialogue"))
                EnterDialogue();
            if (GUILayout.Button("Cutscene"))
                EnterCutscene();
            if (GUILayout.Button("Main Menu"))
                EnterMainMenu();
            if (GUILayout.Button("Loading"))
                EnterLoading();
            if (GUILayout.Button("Game Over"))
                EnterGameOver();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause"))
                Pause();
            if (GUILayout.Button("Resume"))
                Resume();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        GUIStyle OverlayKeyStyle
        {
            get
            {
                if (_overlayKeyStyle == null)
                {
                    _overlayKeyStyle = new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        fontSize = 12
                    };
                    _overlayKeyStyle.normal.background = Texture2D.whiteTexture;
                    _overlayKeyStyle.hover.background = Texture2D.whiteTexture;
                    _overlayKeyStyle.active.background = Texture2D.whiteTexture;
                }

                return _overlayKeyStyle;
            }
        }

        void DrawRow(string label, bool active, float width)
        {
            Color previousBg = GUI.backgroundColor;
            GUI.backgroundColor = active ? OverlayActiveBg : OverlayIdleBg;
            OverlayKeyStyle.normal.textColor = active ? Color.black : OverlayIdleText;
            OverlayKeyStyle.hover.textColor = OverlayKeyStyle.normal.textColor;
            OverlayKeyStyle.active.textColor = OverlayKeyStyle.normal.textColor;
            GUILayout.Box(label, OverlayKeyStyle, GUILayout.Width(width), GUILayout.Height(22f));
            GUI.backgroundColor = previousBg;
        }
#endif
    }
}
