using Character;
using Game;
using Interaction;
using System.Collections.Generic;
using UnityEngine;

namespace NpcAi
{
    /// <summary>
    /// Detects player noise with one trigger sphere. Its radius shrinks while the
    /// player is in stealth.
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
        // Retained only to remove the second collider created by older versions of this component.
        [SerializeField, HideInInspector] SphereCollider stealthNoiseSphere;

        SideScrollerController _player;
        readonly HashSet<Collider> _touchingPlayerColliders = new HashSet<Collider>();
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
            InteractionManager.OnMove += HandlePlayerMove;
            RefreshState();
        }

        void OnDisable()
        {
            GameStateManager.OnStateChanged -= HandleStateChanged;
            InteractionManager.OnMove -= HandlePlayerMove;
            _touchingPlayerColliders.Clear();
        }

        void Start()
        {
            // All Awake calls have completed here, so the manager's initial state is available.
            RefreshState();
        }

        void OnValidate()
        {
            EnsureSpheres();
            ApplySphereSettings();
        }

        void OnTriggerEnter(Collider other)
        {
            TrackPlayerCollider(other);
        }

        // Also covers a player already inside the other sphere when stealth changes.
        void OnTriggerStay(Collider other)
        {
            TrackPlayerCollider(other);
        }

        void OnTriggerExit(Collider other)
        {
            _touchingPlayerColliders.Remove(other);
        }

        void OnDrawGizmos()
        {
            float radius = _isStealthActive ? stealthNoiseRadius : normalNoiseRadius;
            Color color = _isStealthActive
                ? new Color(0.2f, 0.8f, 1f, 0.9f)
                : new Color(1f, 0.66f, 0.1f, 0.8f);
            DrawSphereGizmo(radius, color);
        }

        void HandleStateChanged(GameState previous, GameState current)
        {
            SetStealthActive(IsStealthState(current));
        }

        void HandlePlayerMove(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude > 0f)
                TryHear(_player);
        }

        void RefreshState()
        {
            GameStateManager stateManager = GameStateManager.Instance;
            SetStealthActive(stateManager != null && stateManager.IsStealthActive);
        }

        void SetStealthActive(bool isStealthActive)
        {
            _isStealthActive = isStealthActive;
            _touchingPlayerColliders.Clear();
            ApplySphereSettings();
        }

        void TrackPlayerCollider(Collider other)
        {
            if (other == null || other.GetComponent<InteractionSystem.PlayerInteractionSensor>() != null)
                return;

            SideScrollerController player = other.GetComponentInParent<SideScrollerController>();
            if (player == null || IsOwnedByNpc(player.transform))
                return;

            _player = player;
            _touchingPlayerColliders.Add(other);
            TryHear(player);
        }

        void TryHear(SideScrollerController player)
        {
            if (!Application.isPlaying || player == null || !player.IsMoving)
                return;

            _touchingPlayerColliders.RemoveWhere(collider => collider == null);
            if (_touchingPlayerColliders.Count == 0)
                return;

            if (machine == null)
                machine = GetComponentInParent<NpcStateMachine>();

            if (machine == null || !machine.IsPlaying)
                return;

            for (int i = 0; i < interruptNodeIds.Count; i++)
            {
                if (machine.TryInterrupt(interruptNodeIds[i]))
                    return;
            }
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
            if (normalNoiseSphere == null)
                normalNoiseSphere = GetComponent<SphereCollider>();
            if (normalNoiseSphere == null)
                normalNoiseSphere = gameObject.AddComponent<SphereCollider>();

            RemoveLegacyStealthSphere();

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();

            body.isKinematic = true;
            body.useGravity = false;
            ApplySphereSettings();
        }

        void RemoveLegacyStealthSphere()
        {
            if (stealthNoiseSphere == null || stealthNoiseSphere == normalNoiseSphere)
            {
                stealthNoiseSphere = null;
                return;
            }

            if (Application.isPlaying)
                Destroy(stealthNoiseSphere);
            else
                DestroyImmediate(stealthNoiseSphere);

            stealthNoiseSphere = null;
        }

        void ApplySphereSettings()
        {
            if (normalNoiseSphere != null)
            {
                normalNoiseSphere.center = sphereCenter;
                normalNoiseSphere.radius = Mathf.Max(
                    0.05f,
                    _isStealthActive ? stealthNoiseRadius : normalNoiseRadius);
                normalNoiseSphere.isTrigger = true;
                normalNoiseSphere.enabled = true;
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
