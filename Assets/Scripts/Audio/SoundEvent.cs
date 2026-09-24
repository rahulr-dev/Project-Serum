using UnityEngine;
using UnityEngine.Audio;

namespace Serum.Audio
{
    [CreateAssetMenu(menuName = "Serum/Audio/Sound Event", fileName = "SoundEvent_")]
    public class SoundEvent : ScriptableObject
    {
        [SerializeField] AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] float volume = 1f;
        [SerializeField] Vector2 pitchRange = new Vector2(0.95f, 1.05f);
        [SerializeField] AudioMixerGroup mixerGroup;
        [SerializeField] bool spatial = true;
        [SerializeField, Min(0f)] float minDistance = 1f;
        [SerializeField, Min(0f)] float maxDistance = 20f;
        [SerializeField, Min(0)] int priority = 128;
        [SerializeField, Min(0f)] float cooldown;

        float lastPlayTime = float.NegativeInfinity;

        // ScriptableObjects can stay alive between Unity Play sessions while game time resets to zero.
        // A zero cooldown must always permit playback, independent of a previous session's lastPlayTime.
        public bool CanPlay => clips != null && clips.Length > 0 &&
                               (cooldown <= 0f || Time.unscaledTime >= lastPlayTime + cooldown);

        public bool TryGetPlayback(out AudioClip clip, out float playbackVolume, out float pitch)
        {
            clip = null;
            playbackVolume = volume;
            pitch = Random.Range(Mathf.Min(pitchRange.x, pitchRange.y), Mathf.Max(pitchRange.x, pitchRange.y));

            if (!CanPlay)
                return false;

            for (int i = 0; i < clips.Length; i++)
            {
                AudioClip candidate = clips[Random.Range(0, clips.Length)];
                if (candidate == null)
                    continue;

                clip = candidate;
                if (cooldown > 0f)
                    lastPlayTime = Time.unscaledTime;
                return true;
            }

            return false;
        }

        public void ApplyTo(UnityEngine.AudioSource source)
        {
            source.outputAudioMixerGroup = mixerGroup;
            source.spatialBlend = spatial ? 1f : 0f;
            // Linear attenuation reaches silence at Max Distance.
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = minDistance;
            source.maxDistance = Mathf.Max(minDistance + 0.01f, maxDistance);
            source.priority = Mathf.Clamp(priority, 0, 256);
        }
    }
}
