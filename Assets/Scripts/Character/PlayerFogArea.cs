using UnityEngine;
using UnityEngine.Rendering;

namespace Character
{
    [ExecuteAlways, DisallowMultipleComponent]
    [AddComponentMenu("Serum/Player Fog Area")]
    public sealed class PlayerFogArea : MonoBehaviour
    {
        [Header("Follow the active player")]
        [Tooltip("Uses this camera follow's current player, including possessed clones.")]
        [SerializeField] SideScrollerCameraFollow cameraFollow;
        [Tooltip("Used when no camera follow target is available.")]
        [SerializeField] Transform player;
        [Tooltip("Follow the player's Y position. Turn off to keep the fog at Fixed Y plus Center Offset Y.")]
        [SerializeField] bool followY = true;
        [Tooltip("World-space base Y position when Follow Y is disabled. Center Offset Y is added to this value.")]
        [SerializeField] float fixedY = 0f;

        [Header("Fog bounds (world units, world axes)")]
        [Tooltip("Turn off to restore the original unlimited-area fog.")]
        [SerializeField] bool limitToPlayerArea = true;
        [SerializeField] Vector3 centerOffset = new Vector3(0f, 2f, 10f);
        [SerializeField, Min(0.1f)] float widthX = 40f;
        [SerializeField, Min(0.1f)] float heightY = 20f;
        [Tooltip("Total Z thickness around the offset center. Solid scene depth and the material's Max Distance also limit visible fog.")]
        [SerializeField, Min(0.1f)] float depthZ = 60f;
        [SerializeField, Min(0f)] float edgeFade = 3f;

        [Header("Sampling cost")]
        [Tooltip("Desired world units between samples. Larger values are faster but may show banding.")]
        [SerializeField, Min(0.05f)] float sampleSpacing = 1f;
        [Tooltip("Maximum samples per visible ray, also capped by the fog material's Steps value.")]
        [SerializeField, Range(1, 128)] int maxSteps = 32;

        static readonly int CenterId = Shader.PropertyToID("_SerumFogAreaCenter");
        static readonly int ExtentsId = Shader.PropertyToID("_SerumFogAreaExtents");
        static readonly int SamplingId = Shader.PropertyToID("_SerumFogAreaSampling");
        static PlayerFogArea owner;

        Transform Target => cameraFollow != null && cameraFollow.Target != null
            ? cameraFollow.Target : player;
        Vector3 Size => new Vector3(Mathf.Max(0.1f, widthX), Mathf.Max(0.1f, heightY), Mathf.Max(0.1f, depthZ));

        Vector3 GetCenter(Transform target)
        {
            Vector3 position = target.position;
            if (!followY)
                position.y = fixedY;
            return position + centerOffset;
        }

        void OnEnable()
        {
            if (cameraFollow == null)
                cameraFollow = GetComponent<SideScrollerCameraFollow>();
            RenderPipelineManager.beginCameraRendering += BeforeCameraRendering;
            Apply();
        }

        void LateUpdate() => Apply();

        // Update after player/camera movement, including editor previews.
        void BeforeCameraRendering(ScriptableRenderContext context, Camera camera) => Apply();

        void Apply()
        {
            // One area controls AERO fog in the scene. Missing targets fail closed.
            owner = this;
            Transform target = Target;
            Vector3 center = target != null ? GetCenter(target) : Vector3.zero;
            Vector3 extents = Size * 0.5f;
            Shader.SetGlobalVector(CenterId, new Vector4(center.x, center.y, center.z, limitToPlayerArea ? 1f : 0f));
            Shader.SetGlobalVector(ExtentsId, new Vector4(extents.x, extents.y, extents.z, Mathf.Max(0f, edgeFade)));
            Shader.SetGlobalVector(SamplingId, new Vector4(Mathf.Max(0.05f, sampleSpacing), Mathf.Clamp(maxSteps, 1, 128), target != null ? 1f : 0f, 0f));
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCameraRendering;
            if (owner != this)
                return;
            Shader.SetGlobalVector(CenterId, Vector4.zero);
            owner = null;
        }

        void OnDrawGizmosSelected()
        {
            if (!limitToPlayerArea || Target == null)
                return;
            Vector3 size = Size;
            Vector3 center = GetCenter(Target);
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 1f);
            Gizmos.DrawWireCube(center, size);
            float fade = Mathf.Clamp(edgeFade, 0f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.5f);
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.35f);
            Gizmos.DrawWireCube(center, size - Vector3.one * (2f * fade));
        }
    }
}
