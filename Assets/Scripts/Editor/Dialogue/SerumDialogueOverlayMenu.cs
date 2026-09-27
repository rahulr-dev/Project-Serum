using Dialogue;
using UnityEditor;

namespace Dialogue.Editor
{
    public static class SerumDialogueOverlayMenu
    {
        const string TogglePath = "Serum/Dialogue Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(DialogueManager.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(DialogueManager.OverlayPrefsKey, value);
        }

        [MenuItem(TogglePath, false, 4)]
        static void ToggleOverlay()
        {
            IsEnabled = !IsEnabled;
        }

        [MenuItem(TogglePath, true)]
        static bool ToggleOverlayValidate()
        {
            Menu.SetChecked(TogglePath, IsEnabled);
            return true;
        }
    }
}
