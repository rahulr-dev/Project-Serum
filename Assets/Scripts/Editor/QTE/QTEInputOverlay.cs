using Game;
using Interaction;
using Interaction.Editor;
using QTE;
using UnityEditor;
using UnityEngine;

namespace QTE.Editor
{
    internal static class QTEInputOverlay
    {
        public const string PrefsKey = "Serum.QTEInputOverlay.Enabled";
        const string HostName = "Serum QTE Input Overlay Host";
        static Rect _overlayRect = new Rect(460f, 12f, 280f, 420f);

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

            if (!EditorPrefs.GetBool(PrefsKey, false))
            {
                DestroyHost();
                return;
            }

            if (GameObject.Find(HostName) != null)
                return;

            GameObject host = new GameObject(HostName);
            Object.DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideAndDontSave;
            host.AddComponent<QTEInputOverlayHost>();
        }

        static void DestroyHost()
        {
            GameObject existing = GameObject.Find(HostName);
            if (existing != null)
                Object.DestroyImmediate(existing);
        }

        internal static void Draw()
        {
            QTEManager manager = QTEManager.Instance;
            InteractionManager input = InteractionManager.Instance;

            bool qteAllowed = GameStateManager.Instance != null && GameStateManager.Instance.AllowsQTEInput;
            bool qteRunning = manager != null && manager.IsRunning;
            bool qteInputEnabled = qteAllowed || qteRunning;
            SerumOverlayDraw.DrawRow($"QTE input  {(qteInputEnabled ? "enabled" : "disabled")}", qteInputEnabled, 250f, 22f);
            SerumOverlayDraw.DrawSource(QTEInputBindings.SourceDescription);

            if (manager != null && manager.IsRunning)
            {
                QTEInputKind? required = manager.CurrentRequiredInput;
                string requiredLabel = required.HasValue
                    ? $"{required.Value}  ({QTEInputBindings.GetBindingLabel(required.Value)})"
                    : "none";
                SerumOverlayDraw.DrawRow($"Required  {requiredLabel}", required.HasValue, 250f, 22f);

                if (manager.CurrentSequenceExpectedInput.HasValue)
                {
                    QTEInputKind step = manager.CurrentSequenceExpectedInput.Value;
                    SerumOverlayDraw.DrawRow(
                        $"Sequence step  {step} ({QTEInputBindings.GetBindingLabel(step)})",
                        true,
                        250f,
                        22f);
                }

                SerumOverlayDraw.DrawRow($"Node  {manager.CurrentNodeKind}", true, 250f, 22f);
            }
            else
            {
                SerumOverlayDraw.DrawRow("QTE idle", false, 250f, 22f);
            }

            GUILayout.Space(8f);
            GUILayout.Label("QTE bindings", EditorStyles.boldLabel);

            if (input == null)
            {
                GUILayout.Label("InteractionManager not found.");
                return;
            }

            DrawKind(QTEInputKind.Interact, input);
            DrawKind(QTEInputKind.Jump, input);
            DrawKind(QTEInputKind.MoveLeft, input);
            DrawKind(QTEInputKind.MoveRight, input);
            DrawKind(QTEInputKind.AnyFaceButton, input);
        }

        static void DrawKind(QTEInputKind kind, InteractionManager input)
        {
            bool held = input.IsInputHeld(kind);
            bool edge = input.WasInputPressedThisFrame(kind);
            bool required = QTEManager.Instance != null &&
                            (QTEManager.Instance.CurrentRequiredInput == kind ||
                             QTEManager.Instance.CurrentSequenceExpectedInput == kind);

            string label = $"{kind}  {QTEInputBindings.GetBindingLabel(kind)}";
            SerumOverlayDraw.DrawRow(label, held || edge || required, 250f);
            SerumOverlayDraw.DrawSource(input.GetActiveSources(kind));
            if (edge)
                SerumOverlayDraw.DrawSource($"Edge this frame x{input.CountInputPressesThisFrame(kind)}");
        }

        sealed class QTEInputOverlayHost : MonoBehaviour
        {
            void OnGUI()
            {
                if (!EditorPrefs.GetBool(PrefsKey, false))
                    return;

                _overlayRect = GUI.Window(GetInstanceID(), _overlayRect, DrawWindow, "QTE Input Overlay");
            }

            static void DrawWindow(int windowId)
            {
                QTEInputOverlay.Draw();
                GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
            }
        }
    }
}
