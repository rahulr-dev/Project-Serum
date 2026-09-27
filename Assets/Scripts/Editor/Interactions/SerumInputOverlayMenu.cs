using Interaction;
using UnityEditor;

namespace Interaction.Editor
{
    public static class SerumInputOverlayMenu
    {
        const string TogglePath = "Serum/Input Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(InteractionManager.InputOverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(InteractionManager.InputOverlayPrefsKey, value);
        }

        [MenuItem(TogglePath, false, 0)]
        static void ToggleInputOverlay()
        {
            IsEnabled = !IsEnabled;
        }

        [MenuItem(TogglePath, true)]
        static bool ToggleInputOverlayValidate()
        {
            Menu.SetChecked(TogglePath, IsEnabled);
            return true;
        }
    }
}
