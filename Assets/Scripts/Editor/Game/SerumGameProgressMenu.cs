using Game;
using UnityEditor;

namespace Game.Editor
{
    static class SerumGameProgressMenu
    {
        const string ClearProgressPath = "Serum/Progress/Clear Saved Location";

        [MenuItem(ClearProgressPath, false, 20)]
        static void ClearSavedLocation()
        {
            if (!EditorUtility.DisplayDialog(
                    "Clear Saved Location",
                    "This removes the saved player location from PlayerPrefs.",
                    "Clear",
                    "Cancel"))
                return;

            GameProgressManager.ClearSavedProgress();
        }
    }
}
