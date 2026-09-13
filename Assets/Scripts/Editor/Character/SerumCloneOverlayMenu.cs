using Character;
using UnityEditor;

namespace Character.Editor
{
    public static class SerumCloneOverlayMenu
    {
        const string EnablePath = "Serum/Enable Clone Overlay";
        const string DisablePath = "Serum/Disable Clone Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(PlayerCloneSystem.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(PlayerCloneSystem.OverlayPrefsKey, value);
        }

        [MenuItem(EnablePath, false, 14)]
        static void EnableOverlay()
        {
            IsEnabled = true;
        }

        [MenuItem(EnablePath, true)]
        static bool EnableOverlayValidate()
        {
            return !IsEnabled;
        }

        [MenuItem(DisablePath, false, 15)]
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
