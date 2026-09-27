using SequenceSystem;
using UnityEditor;

namespace SequenceSystem.Editor
{
    public static class SerumSequenceOverlayMenu
    {
        const string TogglePath = "Serum/Sequence Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(Sequencer.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(Sequencer.OverlayPrefsKey, value);
        }

        [MenuItem(TogglePath, false, 10)]
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
