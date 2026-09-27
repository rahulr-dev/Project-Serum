using InteractionSystem;
using UnityEditor;

namespace InteractionSystem.Editor
{
    public static class SerumInteractablesOverlayMenu
    {
        const string TogglePath = "Serum/Interactables Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(PlayerInteractor.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(PlayerInteractor.OverlayPrefsKey, value);
        }

        [MenuItem(TogglePath, false, 24)]
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
