using Character;
using UnityEditor;

namespace Character.Editor
{
    public static class SerumCloneOverlayMenu
    {
        const string TogglePath = "Serum/Clone Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(PlayerCloneSystem.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(PlayerCloneSystem.OverlayPrefsKey, value);
        }

        [MenuItem(TogglePath, false, 14)]
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
