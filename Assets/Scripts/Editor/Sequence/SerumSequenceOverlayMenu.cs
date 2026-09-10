using SequenceSystem;
using UnityEditor;

namespace SequenceSystem.Editor
{
    public static class SerumSequenceOverlayMenu
    {
        const string EnablePath = "Serum/Enable Sequence Overlay";
        const string DisablePath = "Serum/Disable Sequence Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(Sequencer.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(Sequencer.OverlayPrefsKey, value);
        }

        [MenuItem(EnablePath, false, 10)]
        static void EnableOverlay()
        {
            IsEnabled = true;
        }

        [MenuItem(EnablePath, true)]
        static bool EnableOverlayValidate()
        {
            return !IsEnabled;
        }

        [MenuItem(DisablePath, false, 11)]
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
