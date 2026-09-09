using InteractionSystem;
using UnityEditor;

namespace InteractionSystem.Editor
{
    public static class SerumInteractablesOverlayMenu
    {
        const string EnablePath = "Serum/Enable Interactables Overlay";
        const string DisablePath = "Serum/Disable Interactables Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(PlayerInteractor.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(PlayerInteractor.OverlayPrefsKey, value);
        }

        [MenuItem(EnablePath, false, 24)]
        static void EnableOverlay()
        {
            IsEnabled = true;
        }

        [MenuItem(EnablePath, true)]
        static bool EnableOverlayValidate()
        {
            return !IsEnabled;
        }

        [MenuItem(DisablePath, false, 25)]
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
