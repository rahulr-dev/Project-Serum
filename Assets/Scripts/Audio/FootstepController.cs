using UnityEngine;

namespace Serum.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class FootstepController : MonoBehaviour
    {
        [Header("Animation Event Methods: FootstepLeft / FootstepRight")]
        [SerializeField] Transform leftFoot;
        [SerializeField] Transform rightFoot;
        [SerializeField] SoundEvent defaultFootstep;
        [SerializeField] SoundEvent landingSound;
        [SerializeField] LayerMask groundLayers = ~0;
        [SerializeField, Min(0.05f)] float raycastDistance = 0.6f;
        [SerializeField, Min(0f)] float raycastStartHeight = 0.15f;
        [Header("Debug")]
        [SerializeField] bool showDebugGizmos = true;
        [SerializeField] bool logFootsteps;

        Vector3 leftOrigin;
        Vector3 rightOrigin;
        RaycastHit leftHit;
        RaycastHit rightHit;
        bool leftHitGround;
        bool rightHitGround;
        AudioSource footstepSource;

        void Awake()
        {
            footstepSource = GetComponent<AudioSource>();
            if (footstepSource == null)
                footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.enabled = true;
            footstepSource.mute = false;
            footstepSource.playOnAwake = false;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (GetComponent<AudioSource>() == null)
                gameObject.AddComponent<AudioSource>();
        }
#endif

        public void FootstepLeft() => PlayFootstep(leftFoot, true);
        public void FootstepRight() => PlayFootstep(rightFoot, false);
        public void Landing() => PlaySoundAtFeet(landingSound);

        void PlayFootstep(Transform foot, bool isLeft)
        {
            if (foot == null)
            {
                Debug.LogWarning("FootstepController is missing a foot transform.", this);
                return;
            }

            Vector3 origin = foot.position + Vector3.up * raycastStartHeight;
            bool hitGround = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayers, QueryTriggerInteraction.Ignore);
            if (isLeft)
            {
                leftOrigin = origin;
                leftHit = hit;
                leftHitGround = hitGround;
            }
            else
            {
                rightOrigin = origin;
                rightHit = hit;
                rightHitGround = hitGround;
            }

            if (hitGround)
            {
                SurfaceType surface = hit.collider.GetComponentInParent<SurfaceType>();
                SoundEvent sound = surface != null && surface.FootstepSound != null ? surface.FootstepSound : defaultFootstep;
                if (logFootsteps)
                    Debug.Log($"{name}: {(isLeft ? "Left" : "Right")} foot hit {hit.collider.name}; sound = {(sound != null ? sound.name : "NONE")}", this);
                PlayOnPlayerSource(sound);
                return;
            }

            if (logFootsteps)
                Debug.LogWarning($"{name}: {(isLeft ? "Left" : "Right")} foot did not hit ground. Check raycast distance/layers.", this);
            PlayOnPlayerSource(defaultFootstep);
        }

        void PlaySoundAtFeet(SoundEvent soundEvent)
        {
            if (soundEvent == null)
                return;

            Transform foot = leftFoot != null ? leftFoot : transform;
            PlayOnPlayerSource(soundEvent);
        }

        void PlayOnPlayerSource(SoundEvent soundEvent)
        {
            if (soundEvent == null || footstepSource == null)
                return;

            if (!footstepSource.enabled)
                footstepSource.enabled = true;

            if (footstepSource.mute)
                footstepSource.mute = false;

            if (!soundEvent.TryGetPlayback(out AudioClip clip, out float volume, out float pitch))
                return;

            soundEvent.ApplyTo(footstepSource);
            footstepSource.volume = 1f;
            footstepSource.pitch = pitch;
            footstepSource.PlayOneShot(clip, volume);

            if (logFootsteps)
                Debug.Log($"{name}: playing {clip.name} on {footstepSource.name}; " +
                          $"listener paused = {AudioListener.pause}, listener volume = {AudioListener.volume:0.00}.", this);
        }

        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos)
                return;

            DrawFootRay(leftFoot, leftOrigin, leftHit, leftHitGround, Color.cyan, "Left");
            DrawFootRay(rightFoot, rightOrigin, rightHit, rightHitGround, Color.magenta, "Right");
        }

        void DrawFootRay(Transform foot, Vector3 lastOrigin, RaycastHit lastHit, bool didHit, Color color, string label)
        {
            if (foot == null)
                return;

            Vector3 origin = Application.isPlaying ? lastOrigin : foot.position + Vector3.up * raycastStartHeight;
            if (origin == Vector3.zero && foot.position != Vector3.zero)
                origin = foot.position + Vector3.up * raycastStartHeight;

            Gizmos.color = color;
            Gizmos.DrawWireSphere(origin, 0.06f);
            Gizmos.DrawLine(origin, origin + Vector3.down * raycastDistance);

            if (Application.isPlaying && didHit)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(lastHit.point, 0.06f);
                Gizmos.DrawLine(lastHit.point, lastHit.point + lastHit.normal * 0.3f);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(lastHit.point + Vector3.up * 0.12f, $"{label}: {lastHit.collider.name}");
#endif
            }
        }
    }
}
