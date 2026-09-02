using Interaction;
using UnityEditor;

namespace Interaction.Editor
{
    public static class SerumInputOverlayMenu
    {
        const string EnablePath = "Serum/Enable Input Overlay";
        const string DisablePath = "Serum/Disable Input Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(InteractionManager.InputOverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(InteractionManager.InputOverlayPrefsKey, value);
        }

        [MenuItem(EnablePath, false, 0)]
        static void EnableInputOverlay()
        {
            IsEnabled = true;
        }

        [MenuItem(EnablePath, true)]
        static bool EnableInputOverlayValidate()
        {
            return !IsEnabled;
        }

        [MenuItem(DisablePath, false, 1)]
        static void DisableInputOverlay()
        {
            IsEnabled = false;
        }

        [MenuItem(DisablePath, true)]
        static bool DisableInputOverlayValidate()
        {
            return IsEnabled;
        }
    }
}
