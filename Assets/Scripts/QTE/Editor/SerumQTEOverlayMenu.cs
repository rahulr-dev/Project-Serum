using QTE;
using UnityEditor;

namespace QTE.Editor
{
    public static class SerumQTEOverlayMenu
    {
        const string EnablePath = "Serum/Enable QTE Overlay";
        const string DisablePath = "Serum/Disable QTE Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(QTEManager.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(QTEManager.OverlayPrefsKey, value);
        }

        [MenuItem(EnablePath, false, 6)]
        static void EnableOverlay()
        {
            IsEnabled = true;
        }

        [MenuItem(EnablePath, true)]
        static bool EnableOverlayValidate()
        {
            return !IsEnabled;
        }

        [MenuItem(DisablePath, false, 7)]
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
