using UnityEngine;

internal static class MobileFrameRate
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // VSync takes precedence over targetFrameRate on desktop platforms.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Application.isMobilePlatform ? 30 : 60;
    }
}
