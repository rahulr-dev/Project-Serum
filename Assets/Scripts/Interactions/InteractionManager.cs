using System;
using System.Collections.Generic;
using System.Text;
using Game;
using QTE;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Interaction
{
    [DefaultExecutionOrder(-100)]
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        public static event Action<Vector2> OnMove;
        public static event Action OnInteractStarted;
        public static event Action OnInteractCanceled;
        public static event Action OnJumpStarted;
        public static event Action OnJumpCanceled;
        public static event Action<QTEInputKind, string> OnQTEInputRegistered;

        public Vector2 MoveInput { get; private set; }
        public bool IsInteractHeld { get; private set; }
        public bool IsJumpHeld { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }

        public bool KeyW { get; private set; }
        public bool KeyA { get; private set; }
        public bool KeyS { get; private set; }
        public bool KeyD { get; private set; }
        public bool KeyE { get; private set; }
        public bool KeySpace { get; private set; }

        public bool GamepadSouth { get; private set; }
        public bool GamepadWest { get; private set; }
        public bool GamepadNorth { get; private set; }
        public bool GamepadEast { get; private set; }
        public bool GamepadDpadUp { get; private set; }
        public bool GamepadDpadDown { get; private set; }
        public bool GamepadDpadLeft { get; private set; }
        public bool GamepadDpadRight { get; private set; }
        public Vector2 GamepadLeftStick { get; private set; }

        public const string InputOverlayPrefsKey = "Serum.InputOverlay.Enabled";

        public int QTEInteractPressesThisFrame => _qteInteractPresses;
        public int QTEJumpPressesThisFrame => _qteJumpPresses;
        public int QTEMoveLeftPressesThisFrame => _qteLeftPresses;
        public int QTEMoveRightPressesThisFrame => _qteRightPresses;
        public int QTEAnyFacePressesThisFrame => _qteAnyFacePresses;

        InputActionMap _map;
        InputAction _move;
        InputAction _interact;
        InputAction _jump;

        bool _qteJumpWas;
        bool _qteInteractWas;
        bool _qteLeftWas;
        bool _qteRightWas;
        bool _qteAnyFaceWas;
        int _qteJumpPresses;
        int _qteInteractPresses;
        int _qteLeftPresses;
        int _qteRightPresses;
        int _qteAnyFacePresses;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
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

            if (_jump != null)
            {
                _jump.started -= HandleJumpStarted;
                _jump.canceled -= HandleJumpCanceled;
            }

            _map?.Dispose();
            _map = null;
        }

        static bool AllowsMove => GameStateManager.Instance == null || GameStateManager.Instance.AllowsMove;
        static bool AllowsJump => GameStateManager.Instance == null || GameStateManager.Instance.AllowsJump;
        static bool AllowsInteract => GameStateManager.Instance == null || GameStateManager.Instance.AllowsInteract;
        static bool AllowsQTEInput =>
            (GameStateManager.Instance != null && GameStateManager.Instance.AllowsQTEInput) ||
            (QTEManager.Instance != null && QTEManager.Instance.IsRunning);

        void Update()
        {
            ClearQTEPressCounters();

            Vector2 move = AllowsMove && _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            if (move != MoveInput)
            {
                MoveInput = move;
                OnMove?.Invoke(move);
            }
            else
            {
                MoveInput = move;
            }

            bool interactHeld = AllowsInteract && _interact != null && _interact.IsPressed();
            if (IsInteractHeld && !interactHeld)
                OnInteractCanceled?.Invoke();
            IsInteractHeld = interactHeld;

            bool jumpHeld = AllowsJump && _jump != null && _jump.IsPressed();
            if (IsJumpHeld && !jumpHeld)
                OnJumpCanceled?.Invoke();
            IsJumpHeld = jumpHeld;

            RefreshDeviceFlags();
            RefreshQTEInputEdges();
        }

        void LateUpdate()
        {
            JumpPressedThisFrame = false;
        }

        void ClearQTEPressCounters()
        {
            _qteJumpPresses = 0;
            _qteInteractPresses = 0;
            _qteLeftPresses = 0;
            _qteRightPresses = 0;
            _qteAnyFacePresses = 0;
        }

        public bool WasInputPressedThisFrame(QTEInputKind kind)
        {
            return kind switch
            {
                QTEInputKind.Interact => _qteInteractPresses > 0,
                QTEInputKind.Jump => _qteJumpPresses > 0,
                QTEInputKind.MoveLeft => _qteLeftPresses > 0,
                QTEInputKind.MoveRight => _qteRightPresses > 0,
                QTEInputKind.AnyFaceButton => _qteAnyFacePresses > 0,
                _ => false
            };
        }

        public bool IsInputHeld(QTEInputKind kind)
        {
            return kind switch
            {
                QTEInputKind.Interact => IsInteractPressed(),
                QTEInputKind.Jump => IsJumpPressed(),
                QTEInputKind.MoveLeft => IsMoveLeftPressed(),
                QTEInputKind.MoveRight => IsMoveRightPressed(),
                QTEInputKind.AnyFaceButton => IsAnyFacePressed(),
                _ => false
            };
        }

        public int CountInputPressesThisFrame(QTEInputKind kind)
        {
            return kind switch
            {
                QTEInputKind.Interact => _qteInteractPresses,
                QTEInputKind.Jump => _qteJumpPresses,
                QTEInputKind.MoveLeft => _qteLeftPresses,
                QTEInputKind.MoveRight => _qteRightPresses,
                QTEInputKind.AnyFaceButton => _qteAnyFacePresses,
                _ => 0
            };
        }

        public bool WasAnyWrongQTEInputPressed(IReadOnlyList<QTEInputKind> sequence, int expectedStep)
        {
            if (sequence == null || expectedStep < 0 || expectedStep >= sequence.Count)
                return false;

            QTEInputKind expected = sequence[expectedStep];
            foreach (QTEInputKind kind in AllQTEInputKinds)
            {
                if (kind == expected)
                    continue;

                if (WasInputPressedThisFrame(kind))
                    return true;
            }

            return false;
        }

        static readonly QTEInputKind[] AllQTEInputKinds =
        {
            QTEInputKind.Interact,
            QTEInputKind.Jump,
            QTEInputKind.MoveLeft,
            QTEInputKind.MoveRight,
            QTEInputKind.AnyFaceButton
        };

        void RefreshQTEInputEdges()
        {
            bool jump = IsJumpPressed();
            bool interact = IsInteractPressed();
            bool left = IsMoveLeftPressed();
            bool right = IsMoveRightPressed();
            bool anyFace = IsAnyFacePressed();

            if (AllowsQTEInput)
            {
                if (_jump != null && _jump.WasPressedThisFrame())
                    RegisterQTEPress(QTEInputKind.Jump);
                else if (jump && !_qteJumpWas)
                    RegisterQTEPress(QTEInputKind.Jump);

                if (_interact != null && _interact.WasPressedThisFrame())
                    RegisterQTEPress(QTEInputKind.Interact);
                else if (interact && !_qteInteractWas)
                    RegisterQTEPress(QTEInputKind.Interact);

                if (left && !_qteLeftWas)
                    RegisterQTEPress(QTEInputKind.MoveLeft);
                if (right && !_qteRightWas)
                    RegisterQTEPress(QTEInputKind.MoveRight);

                Gamepad pad = Gamepad.current;
                if (pad != null &&
                    (pad.buttonSouth.wasPressedThisFrame ||
                     pad.buttonWest.wasPressedThisFrame ||
                     pad.buttonNorth.wasPressedThisFrame ||
                     pad.buttonEast.wasPressedThisFrame))
                {
                    RegisterQTEPress(QTEInputKind.AnyFaceButton);
                }
                else if (anyFace && !_qteAnyFaceWas)
                    RegisterQTEPress(QTEInputKind.AnyFaceButton);
            }

            _qteJumpWas = jump;
            _qteInteractWas = interact;
            _qteLeftWas = left;
            _qteRightWas = right;
            _qteAnyFaceWas = anyFace;
        }

        bool IsJumpPressed() => KeySpace || GamepadSouth || (_jump != null && _jump.IsPressed());
        bool IsInteractPressed() => KeyE || GamepadWest || (_interact != null && _interact.IsPressed());

        bool IsMoveLeftPressed()
        {
            float moveX = _move != null ? _move.ReadValue<Vector2>().x : 0f;
            return KeyA || GamepadDpadLeft || moveX < -0.5f;
        }

        bool IsMoveRightPressed()
        {
            float moveX = _move != null ? _move.ReadValue<Vector2>().x : 0f;
            return KeyD || GamepadDpadRight || moveX > 0.5f;
        }

        bool IsAnyFacePressed() =>
            GamepadSouth || GamepadWest || GamepadNorth || GamepadEast;

        void RegisterQTEPress(QTEInputKind kind)
        {
            switch (kind)
            {
                case QTEInputKind.Interact:
                    _qteInteractPresses++;
                    break;
                case QTEInputKind.Jump:
                    _qteJumpPresses++;
                    break;
                case QTEInputKind.MoveLeft:
                    _qteLeftPresses++;
                    break;
                case QTEInputKind.MoveRight:
                    _qteRightPresses++;
                    break;
                case QTEInputKind.AnyFaceButton:
                    _qteAnyFacePresses++;
                    break;
            }

            OnQTEInputRegistered?.Invoke(kind, GetActiveSources(kind));
        }

        public string GetActiveSources(QTEInputKind kind)
        {
            var sources = new List<string>(4);

            switch (kind)
            {
                case QTEInputKind.Interact:
                    if (KeyE) sources.Add("Keyboard E");
                    if (GamepadWest) sources.Add("Gamepad West");
                    break;
                case QTEInputKind.Jump:
                    if (KeySpace) sources.Add("Keyboard Space");
                    if (GamepadSouth) sources.Add("Gamepad South");
                    break;
                case QTEInputKind.MoveLeft:
                    if (KeyA) sources.Add("Keyboard A");
                    if (GamepadDpadLeft) sources.Add("Gamepad D-Pad Left");
                    if (GamepadLeftStick.x < -0.5f) sources.Add("Gamepad Stick Left");
                    break;
                case QTEInputKind.MoveRight:
                    if (KeyD) sources.Add("Keyboard D");
                    if (GamepadDpadRight) sources.Add("Gamepad D-Pad Right");
                    if (GamepadLeftStick.x > 0.5f) sources.Add("Gamepad Stick Right");
                    break;
                case QTEInputKind.AnyFaceButton:
                    if (GamepadSouth) sources.Add("Gamepad South");
                    if (GamepadWest) sources.Add("Gamepad West");
                    if (GamepadNorth) sources.Add("Gamepad North");
                    if (GamepadEast) sources.Add("Gamepad East");
                    break;
            }

            if (sources.Count == 0)
                return "—";

            if (sources.Count == 1)
                return sources[0];

            var builder = new StringBuilder(sources[0]);
            for (int i = 1; i < sources.Count; i++)
            {
                builder.Append(", ");
                builder.Append(sources[i]);
            }

            return builder.ToString();
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
            _interact.AddBinding("<Gamepad>/buttonWest");
            _interact.started += HandleInteractStarted;
            _interact.canceled += HandleInteractCanceled;

            _jump = _map.AddAction("Jump", InputActionType.Button);
            _jump.AddBinding("<Keyboard>/space");
            _jump.AddBinding("<Gamepad>/buttonSouth");
            _jump.started += HandleJumpStarted;
            _jump.canceled += HandleJumpCanceled;
        }

        void HandleInteractStarted(InputAction.CallbackContext context)
        {
            if (!AllowsInteract)
                return;

            IsInteractHeld = true;
            OnInteractStarted?.Invoke();
        }

        void HandleInteractCanceled(InputAction.CallbackContext context)
        {
            if (!IsInteractHeld)
                return;

            IsInteractHeld = false;
            OnInteractCanceled?.Invoke();
        }

        void HandleJumpStarted(InputAction.CallbackContext context)
        {
            if (!AllowsJump)
                return;

            IsJumpHeld = true;
            JumpPressedThisFrame = true;
            OnJumpStarted?.Invoke();
        }

        void HandleJumpCanceled(InputAction.CallbackContext context)
        {
            if (!IsJumpHeld)
                return;

            IsJumpHeld = false;
            OnJumpCanceled?.Invoke();
        }

        void RefreshDeviceFlags()
        {
            Keyboard keyboard = Keyboard.current;
            KeyW = keyboard != null && keyboard.wKey.isPressed;
            KeyA = keyboard != null && keyboard.aKey.isPressed;
            KeyS = keyboard != null && keyboard.sKey.isPressed;
            KeyD = keyboard != null && keyboard.dKey.isPressed;
            KeyE = keyboard != null && keyboard.eKey.isPressed;
            KeySpace = keyboard != null && keyboard.spaceKey.isPressed;

            Gamepad pad = Gamepad.current;
            if (pad == null)
            {
                GamepadSouth = false;
                GamepadWest = false;
                GamepadNorth = false;
                GamepadEast = false;
                GamepadDpadUp = false;
                GamepadDpadDown = false;
                GamepadDpadLeft = false;
                GamepadDpadRight = false;
                GamepadLeftStick = Vector2.zero;
            }
            else
            {
                GamepadSouth = pad.buttonSouth.isPressed;
                GamepadWest = pad.buttonWest.isPressed;
                GamepadNorth = pad.buttonNorth.isPressed;
                GamepadEast = pad.buttonEast.isPressed;
                GamepadDpadUp = pad.dpad.up.isPressed;
                GamepadDpadDown = pad.dpad.down.isPressed;
                GamepadDpadLeft = pad.dpad.left.isPressed;
                GamepadDpadRight = pad.dpad.right.isPressed;
                GamepadLeftStick = pad.leftStick.ReadValue();
            }

            if (_jump != null)
                KeySpace = KeySpace || _jump.IsPressed();
            if (_interact != null)
                KeyE = KeyE || _interact.IsPressed();
            if (_move != null)
            {
                Vector2 move = _move.ReadValue<Vector2>();
                KeyW = KeyW || move.y > 0.5f;
                KeyA = KeyA || move.x < -0.5f;
                KeyS = KeyS || move.y < -0.5f;
                KeyD = KeyD || move.x > 0.5f;
            }
        }
    }
}
