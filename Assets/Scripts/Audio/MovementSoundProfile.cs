using UnityEngine;

namespace Serum.Audio
{
    [CreateAssetMenu(menuName = "Serum/Audio/Movement Sound Profile", fileName = "MovementSound_")]
    public class MovementSoundProfile : ScriptableObject
    {
        [Header("Clips")]
        [SerializeField] AudioClip[] movementLoops;
        [SerializeField] AudioClip[] startClips;
        [SerializeField] AudioClip[] stopClips;

        [Header("Motion")]
        [SerializeField, Min(0f)] float startSpeed = 0.05f;
        [SerializeField, Min(0f)] float maxSpeed = 3f;
        [SerializeField, Range(0f, 1f)] float maxVolume = 0.4f;
        [SerializeField] Vector2 pitchRange = new(0.85f, 1.1f);
        [SerializeField, Min(0.01f)] float intensityResponse = 8f;
        [SerializeField, Range(0f, 0.25f)] float scrapePulseAmount = 0.06f;
        [SerializeField, Min(0f)] float scrapePulseFrequency = 4f;

        [Header("3D Audio")]
        [SerializeField, Min(0f)] float minDistance = 1f;
        [SerializeField, Min(0f)] float maxDistance = 14f;

        public float StartSpeed => startSpeed;
        public float MaxSpeed => Mathf.Max(startSpeed + 0.01f, maxSpeed);
        public float MinDistance => minDistance;
        public float MaxDistance => Mathf.Max(minDistance, maxDistance);
        public float IntensityResponse => intensityResponse;
        public float ScrapePulseAmount => scrapePulseAmount;
        public float ScrapePulseFrequency => scrapePulseFrequency;

        public AudioClip GetLoop() => GetRandom(movementLoops);
        public AudioClip GetStart() => GetRandom(startClips);
        public AudioClip GetStop() => GetRandom(stopClips);
        public float GetIntensity(float speed) => Mathf.InverseLerp(startSpeed, MaxSpeed, speed);
        public float GetVolume(float intensity) => maxVolume * Mathf.Clamp01(intensity);
        public float GetPitch(float intensity) => Mathf.Lerp(pitchRange.x, pitchRange.y, intensity);

        static AudioClip GetRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;
            return clips[Random.Range(0, clips.Length)];
        }
    }
}
