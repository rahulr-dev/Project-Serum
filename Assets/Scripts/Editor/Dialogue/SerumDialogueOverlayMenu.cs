using Dialogue;
using UnityEditor;

namespace Dialogue.Editor
{
    public static class SerumDialogueOverlayMenu
    {
        const string EnablePath = "Serum/Enable Dialogue Overlay";
        const string DisablePath = "Serum/Disable Dialogue Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(DialogueManager.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(DialogueManager.OverlayPrefsKey, value);
        }

        [MenuItem(EnablePath, false, 4)]
        static void EnableOverlay()
        {
            IsEnabled = true;
        }

        [MenuItem(EnablePath, true)]
        static bool EnableOverlayValidate()
        {
            return !IsEnabled;
        }

        [MenuItem(DisablePath, false, 5)]
        static void DisableOverlay()
        {
            IsEnabled = false;
        }

        [MenuItem(DisablePath, true)]
        static bool DisableOverlayValidate()
        {
            return IsEnabled;
        }
    }
}
