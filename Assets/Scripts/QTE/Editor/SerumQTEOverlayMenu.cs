using QTE;
using UnityEditor;

namespace QTE.Editor
{
    public static class SerumQTEOverlayMenu
    {
        const string TogglePath = "Serum/QTE Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(QTEManager.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(QTEManager.OverlayPrefsKey, value);
        }

        [MenuItem(TogglePath, false, 6)]
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
