using UnityEngine;

namespace Serum.Audio
{
    [RequireComponent(typeof(UnityEngine.AudioSource))]
    public class AudioEmitter : MonoBehaviour
    {
        UnityEngine.AudioSource source;
        Transform follow;
        public bool IsPlaying => source != null && source.isPlaying;

        void Awake()
        {
            source = GetComponent<UnityEngine.AudioSource>();
            source.playOnAwake = false;
        }

        void LateUpdate()
        {
            if (follow != null && IsPlaying) transform.position = follow.position;
        }

        public void Play(SoundEvent soundEvent, Vector3 position, Transform followTarget = null)
        {
            if (!soundEvent.TryGetPlayback(out AudioClip clip, out float volume, out float pitch))
                return;

            transform.position = position;
            follow = followTarget;
            soundEvent.ApplyTo(source);
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.loop = false;
            source.Play();
        }

        public void Stop()
        {
            source.Stop();
            follow = null;
        }
    }
}
