using UnityEditor;

namespace QTE.Editor
{
    public static class SerumQTEInputOverlayMenu
    {
        const string EnablePath = "Serum/Enable QTE Input Overlay";
        const string DisablePath = "Serum/Disable QTE Input Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(QTEInputOverlay.PrefsKey, false);
            private set => EditorPrefs.SetBool(QTEInputOverlay.PrefsKey, value);
        }

        [MenuItem(EnablePath, false, 8)]
        static void EnableOverlay()
        {
            IsEnabled = true;
        }

        [MenuItem(EnablePath, true)]
        static bool EnableOverlayValidate()
        {
            return !IsEnabled;
        }

        [MenuItem(DisablePath, false, 9)]
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
