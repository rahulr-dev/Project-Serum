using System;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Interaction
{
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        public static event Action<Vector2> OnMove;
        public static event Action OnInteractStarted;
        public static event Action OnInteractCanceled;

        public Vector2 MoveInput { get; private set; }
        public bool IsInteractHeld { get; private set; }

        public bool KeyW { get; private set; }
        public bool KeyA { get; private set; }
        public bool KeyS { get; private set; }
        public bool KeyD { get; private set; }
        public bool KeyE { get; private set; }

        public bool GamepadSouth { get; private set; }
        public bool GamepadDpadUp { get; private set; }
        public bool GamepadDpadDown { get; private set; }
        public bool GamepadDpadLeft { get; private set; }
        public bool GamepadDpadRight { get; private set; }
        public Vector2 GamepadLeftStick { get; private set; }

        public const string InputOverlayPrefsKey = "Serum.InputOverlay.Enabled";

        InputActionMap _map;
        InputAction _move;
        InputAction _interact;

#if UNITY_EDITOR
        static readonly Color OverlayIdleBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        static readonly Color OverlayActiveBg = new Color(0.15f, 0.85f, 0.28f, 1f);
        static readonly Color OverlayIdleText = new Color(0.75f, 0.75f, 0.75f, 1f);
        Rect _overlayRect = new Rect(12f, 12f, 210f, 250f);
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
            BuildActions();
        }

        void OnEnable()
        {
            _map?.Enable();
        }

        void OnDisable()
        {
            _map?.Disable();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (_interact != null)
            {
                _interact.started -= HandleInteractStarted;
                _interact.canceled -= HandleInteractCanceled;
            }

            _map?.Dispose();
            _map = null;
        }

        void Update()
        {
            Vector2 move = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            if (move != MoveInput)
            {
                MoveInput = move;
                OnMove?.Invoke(move);
            }
            else
            {
                MoveInput = move;
            }

            IsInteractHeld = _interact != null && _interact.IsPressed();
            RefreshDeviceFlags();
        }

        void BuildActions()
        {
            _map = new InputActionMap("Interaction");

            _move = _map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/leftStick");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Gamepad>/dpad/up")
                .With("Down", "<Gamepad>/dpad/down")
                .With("Left", "<Gamepad>/dpad/left")
                .With("Right", "<Gamepad>/dpad/right");

            _interact = _map.AddAction("Interact", InputActionType.Button);
            _interact.AddBinding("<Keyboard>/e");
            _interact.AddBinding("<Gamepad>/buttonSouth");
            _interact.started += HandleInteractStarted;
            _interact.canceled += HandleInteractCanceled;
        }

        void HandleInteractStarted(InputAction.CallbackContext context)
        {
            IsInteractHeld = true;
            OnInteractStarted?.Invoke();
        }

        void HandleInteractCanceled(InputAction.CallbackContext context)
        {
            IsInteractHeld = false;
            OnInteractCanceled?.Invoke();
        }

        void RefreshDeviceFlags()
        {
            Keyboard keyboard = Keyboard.current;
            KeyW = keyboard != null && keyboard.wKey.isPressed;
            KeyA = keyboard != null && keyboard.aKey.isPressed;
            KeyS = keyboard != null && keyboard.sKey.isPressed;
            KeyD = keyboard != null && keyboard.dKey.isPressed;
            KeyE = keyboard != null && keyboard.eKey.isPressed;

            Gamepad pad = Gamepad.current;
            if (pad == null)
            {
                GamepadSouth = false;
                GamepadDpadUp = false;
                GamepadDpadDown = false;
                GamepadDpadLeft = false;
                GamepadDpadRight = false;
                GamepadLeftStick = Vector2.zero;
                return;
            }

            GamepadSouth = pad.buttonSouth.isPressed;
            GamepadDpadUp = pad.dpad.up.isPressed;
            GamepadDpadDown = pad.dpad.down.isPressed;
            GamepadDpadLeft = pad.dpad.left.isPressed;
            GamepadDpadRight = pad.dpad.right.isPressed;
            GamepadLeftStick = pad.leftStick.ReadValue();
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (!EditorPrefs.GetBool(InputOverlayPrefsKey, false))
                return;

            _overlayRect = GUI.Window(GetInstanceID(), _overlayRect, DrawInputOverlay, "Input Overlay");
        }

        void DrawInputOverlay(int windowId)
        {
            GUILayout.Label("Keyboard");
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            DrawOverlayKey("W", KeyW || MoveInput.y > 0.5f, 32f);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawOverlayKey("A", KeyA || MoveInput.x < -0.5f, 32f);
            DrawOverlayKey("S", KeyS || MoveInput.y < -0.5f, 32f);
            DrawOverlayKey("D", KeyD || MoveInput.x > 0.5f, 32f);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            DrawOverlayKey("E  Interact", KeyE, 180f);

            GUILayout.Space(8);
            GUILayout.Label("Gamepad");
            bool stickActive = GamepadLeftStick.sqrMagnitude > 0.04f;
            DrawOverlayKey($"Stick  {GamepadLeftStick.x:0.00}, {GamepadLeftStick.y:0.00}", stickActive, 180f);

            GUILayout.BeginHorizontal();
            DrawOverlayKey("Up", GamepadDpadUp, 40f);
            DrawOverlayKey("Dn", GamepadDpadDown, 40f);
            DrawOverlayKey("Lt", GamepadDpadLeft, 40f);
            DrawOverlayKey("Rt", GamepadDpadRight, 40f);
            GUILayout.EndHorizontal();

            DrawOverlayKey("A / Cross  Interact", GamepadSouth, 180f);
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

        void DrawOverlayKey(string label, bool pressed, float width)
        {
            Color previousBg = GUI.backgroundColor;
            GUI.backgroundColor = pressed ? OverlayActiveBg : OverlayIdleBg;
            OverlayKeyStyle.normal.textColor = pressed ? Color.black : OverlayIdleText;
            OverlayKeyStyle.hover.textColor = OverlayKeyStyle.normal.textColor;
            OverlayKeyStyle.active.textColor = OverlayKeyStyle.normal.textColor;
            GUILayout.Box(label, OverlayKeyStyle, GUILayout.Width(width), GUILayout.Height(24f));
            GUI.backgroundColor = previousBg;
        }
#endif
    }
}
