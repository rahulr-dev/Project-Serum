using Game;
using UnityEditor;

namespace Game.Editor
{
    public static class SerumGameStateOverlayMenu
    {
        const string EnablePath = "Serum/Enable Game State Overlay";
        const string DisablePath = "Serum/Disable Game State Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(GameStateManager.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(GameStateManager.OverlayPrefsKey, value);
        }

        [MenuItem(EnablePath, false, 2)]
        static void EnableOverlay()
        {
            IsEnabled = true;
        }

        [MenuItem(EnablePath, true)]
        static bool EnableOverlayValidate()
        {
            return !IsEnabled;
        }

        [MenuItem(DisablePath, false, 3)]
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
