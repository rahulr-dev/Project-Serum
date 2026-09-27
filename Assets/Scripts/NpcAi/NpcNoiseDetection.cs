using Character;
using Game;
using System.Collections.Generic;
using UnityEngine;

namespace NpcAi
{
    /// <summary>
    /// Detects player noise in one of two trigger spheres. Normal gameplay uses the
    /// larger sphere; stealth gameplay uses the smaller one.
    /// </summary>
    [ExecuteAlways]
    public class NpcNoiseDetection : MonoBehaviour
    {
        [SerializeField] NpcStateMachine machine;
        [SerializeField] List<string> interruptNodeIds = new List<string>();
        [SerializeField, Min(0.05f)] float normalNoiseRadius = 10f;
        [SerializeField, Min(0.05f)] float stealthNoiseRadius = 4f;
        [SerializeField] Vector3 sphereCenter = Vector3.zero;
        [SerializeField, HideInInspector] SphereCollider normalNoiseSphere;
        [SerializeField, HideInInspector] SphereCollider stealthNoiseSphere;

        SideScrollerController _player;
        bool _isStealthActive;

        void Reset()
        {
            machine = GetComponentInParent<NpcStateMachine>();
            EnsureSpheres();
        }

        void Awake()
        {
            Resolve();
            EnsureSpheres();
            RefreshState();
        }

        void OnEnable()
        {
            Resolve();
            EnsureSpheres();
            GameStateManager.OnStateChanged += HandleStateChanged;
            RefreshState();
        }

        void OnDisable()
        {
            GameStateManager.OnStateChanged -= HandleStateChanged;
        }

        void Start()
        {
            // All Awake calls have completed here, so the manager's initial state is available.
            RefreshState();
            TryHear(ResolvePlayer());
        }

        void OnValidate()
        {
            EnsureSpheres();
            ApplySphereSettings();
        }

        void OnTriggerEnter(Collider other)
        {
            TryHear(other.GetComponentInParent<SideScrollerController>());
        }

        // Also covers a player already inside the other sphere when stealth changes.
        void OnTriggerStay(Collider other)
        {
            TryHear(other.GetComponentInParent<SideScrollerController>());
        }

        void OnDrawGizmos()
        {
            DrawSphereGizmo(normalNoiseRadius, new Color(1f, 0.66f, 0.1f, 0.8f));
            DrawSphereGizmo(stealthNoiseRadius, new Color(0.2f, 0.8f, 1f, 0.9f));
        }

        void HandleStateChanged(GameState previous, GameState current)
        {
            SetStealthActive(IsStealthState(current));
            TryHear(ResolvePlayer());
        }

        void RefreshState()
        {
            GameStateManager stateManager = GameStateManager.Instance;
            SetStealthActive(stateManager != null && stateManager.IsStealthActive);
        }

        void SetStealthActive(bool isStealthActive)
        {
            _isStealthActive = isStealthActive;
            ApplySphereSettings();
        }

        void TryHear(SideScrollerController player)
        {
            if (!Application.isPlaying || player == null)
                return;

            if (machine == null)
                machine = GetComponentInParent<NpcStateMachine>();

            if (machine == null || !machine.IsPlaying)
                return;

            float activeRadius = _isStealthActive ? stealthNoiseRadius : normalNoiseRadius;
            Vector3 origin = transform.TransformPoint(sphereCenter);
            if ((player.transform.position - origin).sqrMagnitude > activeRadius * activeRadius)
                return;

            for (int i = 0; i < interruptNodeIds.Count; i++)
            {
                if (machine.TryInterrupt(interruptNodeIds[i]))
                    return;
            }
        }

        SideScrollerController ResolvePlayer()
        {
            if (_player != null)
                return _player;

            SideScrollerController[] candidates = FindObjectsByType<SideScrollerController>(FindObjectsSortMode.None);
            for (int i = 0; i < candidates.Length; i++)
            {
                SideScrollerController candidate = candidates[i];
                if (candidate == null || IsOwnedByNpc(candidate.transform))
                    continue;

                _player = candidate;
                break;
            }

            return _player;
        }

        bool IsOwnedByNpc(Transform target)
        {
            Transform npcRoot = machine != null ? machine.transform : transform.root;
            return target == transform || target.IsChildOf(transform) ||
                   (npcRoot != null && (target == npcRoot || target.IsChildOf(npcRoot)));
        }

        void Resolve()
        {
            if (machine == null)
                machine = GetComponentInParent<NpcStateMachine>();
        }

        void EnsureSpheres()
        {
            if (normalNoiseSphere == null || stealthNoiseSphere == null || normalNoiseSphere == stealthNoiseSphere)
            {
                SphereCollider[] spheres = GetComponents<SphereCollider>();
                if (normalNoiseSphere == null && spheres.Length > 0)
                    normalNoiseSphere = spheres[0];

                if (stealthNoiseSphere == null || stealthNoiseSphere == normalNoiseSphere)
                {
                    for (int i = 0; i < spheres.Length; i++)
                    {
                        if (spheres[i] != normalNoiseSphere)
                        {
                            stealthNoiseSphere = spheres[i];
                            break;
                        }
                    }
                }
            }

            if (normalNoiseSphere == null)
                normalNoiseSphere = gameObject.AddComponent<SphereCollider>();
            if (stealthNoiseSphere == null || stealthNoiseSphere == normalNoiseSphere)
                stealthNoiseSphere = gameObject.AddComponent<SphereCollider>();

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();

            body.isKinematic = true;
            body.useGravity = false;
            ApplySphereSettings();
        }

        void ApplySphereSettings()
        {
            if (normalNoiseSphere != null)
            {
                normalNoiseSphere.center = sphereCenter;
                normalNoiseSphere.radius = Mathf.Max(0.05f, normalNoiseRadius);
                normalNoiseSphere.isTrigger = true;
                normalNoiseSphere.enabled = !_isStealthActive;
            }

            if (stealthNoiseSphere != null)
            {
                stealthNoiseSphere.center = sphereCenter;
                stealthNoiseSphere.radius = Mathf.Max(0.05f, stealthNoiseRadius);
                stealthNoiseSphere.isTrigger = true;
                stealthNoiseSphere.enabled = _isStealthActive;
            }
        }

        void DrawSphereGizmo(float radius, Color color)
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(sphereCenter, Mathf.Max(0.05f, radius));
            Gizmos.matrix = previousMatrix;
        }

        static bool IsStealthState(GameState state)
        {
            return state == GameState.GameplayStealth || state == GameState.GameplayStealthForced;
        }
    }
}
