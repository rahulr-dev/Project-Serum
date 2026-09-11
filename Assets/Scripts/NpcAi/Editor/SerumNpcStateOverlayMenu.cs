using NpcAi;
using UnityEditor;

namespace NpcAi.Editor
{
    public static class SerumNpcStateOverlayMenu
    {
        const string EnablePath = "Serum/Enable NPC State Overlay";
        const string DisablePath = "Serum/Disable NPC State Overlay";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(NpcStateActor.OverlayPrefsKey, false);
            private set => EditorPrefs.SetBool(NpcStateActor.OverlayPrefsKey, value);
        }

        [MenuItem(EnablePath, false, 12)]
        static void EnableOverlay()
        {
            IsEnabled = true;
        }

        [MenuItem(EnablePath, true)]
        static bool EnableOverlayValidate()
        {
            return !IsEnabled;
        }

        [MenuItem(DisablePath, false, 13)]
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
