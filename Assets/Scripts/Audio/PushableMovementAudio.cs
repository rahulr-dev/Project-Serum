using InteractionSystem;
using UnityEngine;

namespace Serum.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class PushableMovementAudio : MonoBehaviour
    {
        [SerializeField] MovementSoundProfile profile;
        [SerializeField] Rigidbody body;
        [SerializeField] bool debugLogs;

        AudioSource source;
        Pushable pushable;
        bool loopPlaying;
        float intensity;

        /// <summary>Smoothed 0..1 movement intensity for animation, VFX, or other audio systems.</summary>
        public float Intensity01 => intensity;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            if (source == null)
                source = gameObject.AddComponent<AudioSource>();
            body ??= GetComponent<Rigidbody>();
            pushable = GetComponent<Pushable>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            if (profile != null)
            {
                source.minDistance = profile.MinDistance;
                source.maxDistance = Mathf.Max(profile.MinDistance + 0.01f, profile.MaxDistance);
            }
            source.dopplerLevel = 0f;
        }

        void OnEnable()
        {
            if (pushable == null)
                pushable = GetComponent<Pushable>();
            if (pushable != null)
            {
                pushable.PushStarted += HandlePushStarted;
                pushable.PushStopped += HandlePushStopped;
            }
        }

        void OnDisable()
        {
            if (pushable != null)
            {
                pushable.PushStarted -= HandlePushStarted;
                pushable.PushStopped -= HandlePushStopped;
            }
            StopLoop(false);
        }

        void Update()
        {
            if (profile == null || body == null)
                return;

            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;
            float targetIntensity = profile.GetIntensity(speed);
            intensity = Mathf.MoveTowards(intensity, targetIntensity, profile.IntensityResponse * Time.deltaTime);
            bool shouldPlay = intensity > 0.01f;

            if (shouldPlay && !loopPlaying)
                StartLoop();
            else if (!shouldPlay && loopPlaying)
                StopLoop(true);

            if (loopPlaying)
            {
                // PingPong gives a gentle 0 -> 1 -> 0 scrape variation without changing movement.
                float pulse01 = Mathf.PingPong(Time.time * profile.ScrapePulseFrequency, 1f);
                float pulse = Mathf.Lerp(1f - profile.ScrapePulseAmount, 1f, pulse01);
                source.volume = profile.GetVolume(intensity) * pulse;
                source.pitch = profile.GetPitch(intensity) * pulse;
            }
        }

        void HandlePushStarted()
        {
            PlayOneShot(profile != null ? profile.GetStart() : null);
        }

        void HandlePushStopped()
        {
            // Rigidbody velocity has been cleared. Update now eases intensity down to zero
            // before the loop stops, avoiding an abrupt scrape cut-off.
        }

        void StartLoop()
        {
            AudioClip loop = profile.GetLoop();
            if (loop == null)
                return;

            source.clip = loop;
            source.loop = true;
            source.minDistance = profile.MinDistance;
            source.maxDistance = Mathf.Max(profile.MinDistance + 0.01f, profile.MaxDistance);
            source.volume = 0f;
            source.Play();
            loopPlaying = true;
            if (debugLogs) Debug.Log($"{name}: started movement loop {loop.name}", this);
        }

        void StopLoop(bool playStopSound)
        {
            if (!loopPlaying)
                return;

            source.Stop();
            source.clip = null;
            source.loop = false;
            loopPlaying = false;
            intensity = 0f;
            if (playStopSound)
                PlayOneShot(profile != null ? profile.GetStop() : null);
            if (debugLogs) Debug.Log($"{name}: stopped movement loop", this);
        }

        void PlayOneShot(AudioClip clip)
        {
            if (clip != null)
                source.PlayOneShot(clip);
        }
    }
}
