using Character;
using UnityEngine;

namespace NpcAi
{
    [ExecuteAlways]
    public class NpcSpotTrigger : MonoBehaviour
    {
        const int HitBufferSize = 12;

        [SerializeField] NpcStateMachine machine;
        [SerializeField] Light spotLight;
        [SerializeField] string interruptNodeId = "";
        [SerializeField] float range = 10f;
        [SerializeField] float angle = 50f;
        [SerializeField] int segments = 16;
        [SerializeField] LayerMask occluderMask = ~0;

        static readonly RaycastHit[] Hits = new RaycastHit[HitBufferSize];

        MeshCollider _cone;
        Mesh _mesh;
        float _builtRange = -1f;
        float _builtAngle = -1f;
        int _builtSegments = -1;
        SideScrollerController _player;

        void Reset()
        {
            machine = GetComponentInParent<NpcStateMachine>();
            spotLight = GetComponentInParent<Light>();
        }

        void Awake()
        {
            Resolve();
            AlignToLight();
            SyncCone();
        }

        void OnEnable()
        {
            Resolve();
            AlignToLight();
            SyncCone();
        }

        void LateUpdate()
        {
            SyncCone();
            if (Application.isPlaying)
                TryDetectPlayer();
        }

        void OnDestroy()
        {
            if (_mesh != null)
                DestroyMesh(_mesh);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other == null)
                return;

            TrySpot(other.GetComponentInParent<SideScrollerController>());
        }

        void OnDrawGizmos()
        {
            float coneRange;
            float coneAngle;
            ReadCone(out coneRange, out coneAngle);
            DrawConeGizmo(coneRange, coneAngle, new Color(1f, 0.82f, 0.28f, 0.22f));

            if (!Application.isPlaying)
                return;

            SideScrollerController player = ResolvePlayer();
            if (player == null)
                return;

            Vector3 origin = transform.position;
            Vector3 target = PlayerAimPoint(player);
            bool inCone = IsInsideCone(origin, target, coneRange, coneAngle);
            bool visible = inCone && HasLineOfSight(origin, target, player);
            Gizmos.color = visible ? Color.green : (inCone ? Color.red : new Color(1f, 1f, 1f, 0.12f));
            Gizmos.DrawLine(origin, target);
        }

        public void Bind(NpcStateMachine stateMachine, Light light)
        {
            machine = stateMachine;
            spotLight = light;
            AlignToLight();
            SyncCone();
        }

        void Resolve()
        {
            if (machine == null)
                machine = GetComponentInParent<NpcStateMachine>();

            if (spotLight == null)
                spotLight = GetComponentInParent<Light>();

            if (spotLight == null && machine != null)
                spotLight = machine.GetComponentInChildren<Light>();
        }

        void TryDetectPlayer()
        {
            TrySpot(ResolvePlayer());
        }

        void TrySpot(SideScrollerController player)
        {
            if (player == null)
                return;

            if (machine == null)
                machine = GetComponentInParent<NpcStateMachine>();

            if (machine == null || !machine.IsPlaying)
                return;

            Vector3 origin = transform.position;
            Vector3 target = PlayerAimPoint(player);
            float coneRange;
            float coneAngle;
            ReadCone(out coneRange, out coneAngle);

            if (!IsInsideCone(origin, target, coneRange, coneAngle))
                return;

            if (!HasLineOfSight(origin, target, player))
                return;

            machine.TryInterrupt(interruptNodeId);
        }

        SideScrollerController ResolvePlayer()
        {
            if (_player != null)
                return _player;

            SideScrollerController[] candidates = FindObjectsByType<SideScrollerController>(FindObjectsSortMode.None);
            for (int i = 0; i < candidates.Length; i++)
            {
                SideScrollerController candidate = candidates[i];
                if (candidate == null)
                    continue;

                if (IsOwnedByNpc(candidate.transform))
                    continue;

                _player = candidate;
                break;
            }

            return _player;
        }

        bool IsOwnedByNpc(Transform target)
        {
            if (target == null)
                return false;

            Transform npcRoot = machine != null ? machine.transform : transform.root;
            return target == transform ||
                   target.IsChildOf(transform) ||
                   (npcRoot != null && (target == npcRoot || target.IsChildOf(npcRoot)));
        }

        static Vector3 PlayerAimPoint(SideScrollerController player)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
                return controller.bounds.center;

            Collider body = player.GetComponent<Collider>();
            if (body != null)
                return body.bounds.center;

            return player.transform.position + Vector3.up;
        }

        bool IsInsideCone(Vector3 origin, Vector3 target, float coneRange, float coneAngle)
        {
            Vector3 toTarget = target - origin;
            float distance = toTarget.magnitude;
            if (distance < 0.01f || distance > coneRange)
                return false;

            return Vector3.Angle(transform.forward, toTarget) <= coneAngle * 0.5f;
        }

        bool HasLineOfSight(Vector3 origin, Vector3 target, SideScrollerController player)
        {
            Vector3 toTarget = target - origin;
            float distance = toTarget.magnitude;
            if (distance < 0.01f)
                return false;

            int count = Physics.RaycastNonAlloc(
                origin,
                toTarget / distance,
                Hits,
                distance,
                occluderMask,
                QueryTriggerInteraction.Ignore);

            Collider closest = null;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (hit.collider == null || IsOwnedByNpc(hit.collider.transform))
                    continue;

                if (hit.distance >= closestDistance)
                    continue;

                closestDistance = hit.distance;
                closest = hit.collider;
            }

            if (closest == null)
                return true;

            return closest.GetComponentInParent<SideScrollerController>() == player;
        }

        void AlignToLight()
        {
            if (spotLight == null || transform == spotLight.transform)
                return;

            if (transform.parent != spotLight.transform)
                transform.SetParent(spotLight.transform, false);

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        void SyncCone()
        {
            if (spotLight != null && spotLight.type != LightType.Spot)
                spotLight.type = LightType.Spot;

            float coneRange;
            float coneAngle;
            ReadCone(out coneRange, out coneAngle);
            int coneSegments = Mathf.Clamp(segments, 6, 32);

            if (Mathf.Approximately(coneRange, _builtRange) &&
                Mathf.Approximately(coneAngle, _builtAngle) &&
                coneSegments == _builtSegments &&
                _cone != null &&
                _cone.sharedMesh != null)
                return;

            EnsureCollider();
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "NpcSpotCone" };
                _mesh.hideFlags = HideFlags.HideAndDontSave;
            }

            BuildConeMesh(_mesh, coneRange, coneAngle, coneSegments);
            _cone.convex = true;
            _cone.isTrigger = true;
            _cone.sharedMesh = null;
            _cone.sharedMesh = _mesh;

            _builtRange = coneRange;
            _builtAngle = coneAngle;
            _builtSegments = coneSegments;
        }

        void EnsureCollider()
        {
            Collider[] colliders = GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider is MeshCollider)
                    continue;

                if (Application.isPlaying)
                    Destroy(collider);
                else
                    DestroyImmediate(collider);
            }

            _cone = GetComponent<MeshCollider>();
            if (_cone == null)
                _cone = gameObject.AddComponent<MeshCollider>();

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();

            body.isKinematic = true;
            body.useGravity = false;
        }

        void ReadCone(out float coneRange, out float coneAngle)
        {
            coneRange = spotLight != null ? spotLight.range : range;
            coneAngle = spotLight != null ? spotLight.spotAngle : angle;
            coneRange = Mathf.Max(0.05f, coneRange);
            coneAngle = Mathf.Clamp(coneAngle, 1f, 179f);
        }

        void DrawConeGizmo(float coneRange, float coneAngle, Color color)
        {
            Gizmos.color = color;
            int sides = 16;
            float radius = Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad) * coneRange;
            Vector3 apex = transform.position;
            Vector3 previous = Vector3.zero;

            for (int i = 0; i <= sides; i++)
            {
                float t = (i % sides) / (float)sides * Mathf.PI * 2f;
                Vector3 rim = transform.TransformPoint(new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, coneRange));
                Gizmos.DrawLine(apex, rim);
                if (i > 0)
                    Gizmos.DrawLine(previous, rim);
                previous = rim;
            }
        }

        static void BuildConeMesh(Mesh mesh, float length, float angleDegrees, int sides)
        {
            float radius = Mathf.Tan(angleDegrees * 0.5f * Mathf.Deg2Rad) * length;
            int vertexCount = sides + 2;
            var vertices = new Vector3[vertexCount];
            vertices[0] = Vector3.zero;
            vertices[1] = new Vector3(0f, 0f, length);

            for (int i = 0; i < sides; i++)
            {
                float t = i / (float)sides * Mathf.PI * 2f;
                vertices[i + 2] = new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, length);
            }

            var triangles = new int[sides * 6];
            int tri = 0;
            for (int i = 0; i < sides; i++)
            {
                int current = i + 2;
                int next = (i + 1) % sides + 2;

                triangles[tri++] = 0;
                triangles[tri++] = current;
                triangles[tri++] = next;

                triangles[tri++] = 1;
                triangles[tri++] = next;
                triangles[tri++] = current;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        static void DestroyMesh(Mesh mesh)
        {
            if (mesh == null)
                return;

            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }
    }
}
