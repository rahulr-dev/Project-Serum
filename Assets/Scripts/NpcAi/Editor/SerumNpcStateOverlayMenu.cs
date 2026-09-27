using NpcAi;
using UnityEditor;

namespace NpcAi.Editor
{
    public static class SerumNpcStateOverlayMenu
    {
        const string TogglePath = "Serum/NPC State Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(NpcStateActor.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(NpcStateActor.OverlayPrefsKey, value);
        }

        [MenuItem(TogglePath, false, 12)]
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
