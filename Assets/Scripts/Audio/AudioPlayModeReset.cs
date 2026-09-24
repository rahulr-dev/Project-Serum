using UnityEngine;

namespace Serum.Audio
{
    /// <summary>Keeps Unity editor play sessions from inheriting a paused global audio state.</summary>
    public static class AudioPlayModeReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ResetGlobalAudioState()
        {
            AudioListener.pause = false;
        }
    }
}
