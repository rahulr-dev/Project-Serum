using Game;
using UnityEditor;

namespace Game.Editor
{
    public static class SerumGameStateOverlayMenu
    {
        const string TogglePath = "Serum/Game State Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(GameStateManager.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(GameStateManager.OverlayPrefsKey, value);
        }

        [MenuItem(TogglePath, false, 2)]
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
