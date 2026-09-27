using UnityEditor;

namespace QTE.Editor
{
    public static class SerumQTEInputOverlayMenu
    {
        const string TogglePath = "Serum/QTE Input Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(QTEInputOverlay.PrefsKey, false);
            private set => EditorPrefs.SetBool(QTEInputOverlay.PrefsKey, value);
        }

        [MenuItem(TogglePath, false, 8)]
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
