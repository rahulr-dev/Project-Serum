using Game;
using Interaction;
using Interaction.Editor;
using QTE;
using UnityEditor;
using UnityEngine;

namespace Interaction.Editor
{
    internal static class InteractionInputOverlay
    {
        const string HostName = "Serum Input Overlay Host";
        static Rect _overlayRect = new Rect(12f, 12f, 250f, 430f);

        [InitializeOnLoadMethod]
        static void Register()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += EnsureHost;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
                DestroyHost();
        }

        static void EnsureHost()
        {
            if (!Application.isPlaying)
                return;

            if (!EditorPrefs.GetBool(InteractionManager.InputOverlayPrefsKey, false))
            {
                DestroyHost();
                return;
            }

            if (GameObject.Find(HostName) != null)
                return;

            GameObject host = new GameObject(HostName);
            Object.DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideAndDontSave;
            host.AddComponent<InteractionInputOverlayHost>();
        }

        static void DestroyHost()
        {
            GameObject existing = GameObject.Find(HostName);
            if (existing != null)
                Object.DestroyImmediate(existing);
        }

        internal static void Draw()
        {
            InteractionManager input = InteractionManager.Instance;
            if (input == null)
            {
                GUILayout.Label("InteractionManager not found.");
                return;
            }

            bool qteActive = (GameStateManager.Instance != null && GameStateManager.Instance.AllowsQTEInput) ||
                             (QTEManager.Instance != null && QTEManager.Instance.IsRunning);

            GUILayout.Label("Gameplay InputActions", EditorStyles.boldLabel);
            SerumOverlayDraw.DrawSource("Move / Interact / Jump via Interaction map");
            DrawGameplayMove(input);
            DrawGameplayButton("Interact", input.IsInteractHeld, "InputAction Interact (E / West)", 230f);
            DrawGameplayButton("Jump", input.IsJumpHeld, "InputAction Jump (Space / South)", 230f);

            GUILayout.Space(8f);
            GUILayout.Label("Raw Device (QTE reads here)", EditorStyles.boldLabel);
            SerumOverlayDraw.DrawSource(QTEInputBindings.SourceDescription);
            if (!qteActive)
                SerumOverlayDraw.DrawSource("QTE input gated — enter GameState.QTE to enable");

            DrawRawKey("E  Interact", input.KeyE, input.GetActiveSources(QTEInputKind.Interact), 230f);
            DrawRawKey("Space  Jump", input.KeySpace, input.GetActiveSources(QTEInputKind.Jump), 230f);
            DrawRawKey("A  Move Left", input.KeyA || input.GamepadLeftStick.x < -0.5f,
                input.GetActiveSources(QTEInputKind.MoveLeft), 230f);
            DrawRawKey("D  Move Right", input.KeyD || input.GamepadLeftStick.x > 0.5f,
                input.GetActiveSources(QTEInputKind.MoveRight), 230f);

            GUILayout.BeginHorizontal();
            DrawRawKey("D-Pad L", input.GamepadDpadLeft, "Gamepad dpad left", 72f);
            DrawRawKey("D-Pad R", input.GamepadDpadRight, "Gamepad dpad right", 72f);
            DrawRawKey("D-Pad U", input.GamepadDpadUp, "Gamepad dpad up", 72f);
            GUILayout.EndHorizontal();

            bool stickActive = input.GamepadLeftStick.sqrMagnitude > 0.04f;
            DrawRawKey($"Stick  {input.GamepadLeftStick.x:0.00}, {input.GamepadLeftStick.y:0.00}",
                stickActive, "Gamepad leftStick", 230f);

            DrawRawKey("Cross / A  Jump", input.GamepadSouth, "Gamepad buttonSouth", 230f);
            DrawRawKey("Square / X  Interact", input.GamepadWest, "Gamepad buttonWest", 230f);
            DrawRawKey("Triangle / Y", input.GamepadNorth, "Gamepad buttonNorth", 230f);
            DrawRawKey("Circle / B", input.GamepadEast, "Gamepad buttonEast", 230f);

            if (qteActive)
            {
                GUILayout.Space(6f);
                GUILayout.Label("QTE edges this frame", EditorStyles.boldLabel);
                SerumOverlayDraw.DrawRow($"Interact x{input.QTEInteractPressesThisFrame}", input.QTEInteractPressesThisFrame > 0, 230f, 22f);
                SerumOverlayDraw.DrawRow($"Jump x{input.QTEJumpPressesThisFrame}", input.QTEJumpPressesThisFrame > 0, 230f, 22f);
                SerumOverlayDraw.DrawRow($"Left x{input.QTEMoveLeftPressesThisFrame}", input.QTEMoveLeftPressesThisFrame > 0, 230f, 22f);
                SerumOverlayDraw.DrawRow($"Right x{input.QTEMoveRightPressesThisFrame}", input.QTEMoveRightPressesThisFrame > 0, 230f, 22f);
                SerumOverlayDraw.DrawRow($"Face x{input.QTEAnyFacePressesThisFrame}", input.QTEAnyFacePressesThisFrame > 0, 230f, 22f);
            }
        }

        static void DrawGameplayMove(InteractionManager input)
        {
            bool up = input.MoveInput.y > 0.5f;
            bool down = input.MoveInput.y < -0.5f;
            bool left = input.MoveInput.x < -0.5f;
            bool right = input.MoveInput.x > 0.5f;
            bool any = up || down || left || right;

            SerumOverlayDraw.DrawRow(
                any ? $"Move  {input.MoveInput.x:0.00}, {input.MoveInput.y:0.00}" : "Move  idle",
                any,
                230f);
            SerumOverlayDraw.DrawSource("InputAction Move (WASD / stick / dpad composite)");
        }

        static void DrawGameplayButton(string label, bool pressed, string source, float width)
        {
            SerumOverlayDraw.DrawRow(label, pressed, width);
            SerumOverlayDraw.DrawSource(source);
        }

        static void DrawRawKey(string label, bool pressed, string source, float width)
        {
            SerumOverlayDraw.DrawRow(label, pressed, width);
            SerumOverlayDraw.DrawSource(source);
        }

        sealed class InteractionInputOverlayHost : MonoBehaviour
        {
            void OnGUI()
            {
                if (!EditorPrefs.GetBool(InteractionManager.InputOverlayPrefsKey, false))
                    return;

                _overlayRect = GUI.Window(GetInstanceID(), _overlayRect, DrawWindow, "Input Overlay");
            }

            static void DrawWindow(int windowId)
            {
                InteractionInputOverlay.Draw();
                GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
            }
        }
    }
}
