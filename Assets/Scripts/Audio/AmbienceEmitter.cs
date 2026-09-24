using UnityEngine;

namespace Serum.Audio
{
    [RequireComponent(typeof(UnityEngine.AudioSource))]
    public class AmbienceEmitter : MonoBehaviour
    {
        [SerializeField] AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] float volume = 0.6f;
        [SerializeField] Vector2 delayRange = new(5f, 15f);
        [SerializeField] bool playOnStart = true;

        UnityEngine.AudioSource source;
        float nextPlayTime;

        void Awake()
        {
            source = GetComponent<UnityEngine.AudioSource>();
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.maxDistance = Mathf.Max(source.minDistance + 0.01f, source.maxDistance);
            source.playOnAwake = false;
        }

        void Start()
        {
            if (playOnStart)
                ScheduleNext(0f);
        }

        void Update()
        {
            if (Time.time >= nextPlayTime && clips != null && clips.Length > 0)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                if (clip != null)
                    source.PlayOneShot(clip, volume);
                ScheduleNext(clip == null ? 0f : clip.length);
            }
        }

        void ScheduleNext(float clipLength) => nextPlayTime = Time.time + clipLength + Random.Range(delayRange.x, delayRange.y);
    }
}
