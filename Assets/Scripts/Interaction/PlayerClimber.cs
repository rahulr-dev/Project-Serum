using Character;
using Game;
using Interaction;
using UnityEngine;

namespace InteractionSystem
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerClimber : MonoBehaviour
    {
        [SerializeField] SideScrollerController locomotion;
        [SerializeField] Animator animator;
        [SerializeField] string climbingParameter = "climbing";
        [SerializeField] Vector3 detectionOffset = new Vector3(0f, 0.8f, 0f);
        [SerializeField, Min(0.05f)] float radius = 0.8f;
        [SerializeField] LayerMask layerMask = ~0;
        [SerializeField, Range(0.01f, 1f)] float verticalInputThreshold = 0.2f;
        [SerializeField] bool logDiagnostics = true;

        [Header("Gizmos")]
        [SerializeField] bool drawGizmos = true;
        [SerializeField] Color gizmoColor = new Color(0.2f, 0.75f, 1f, 1f);

        static readonly Collider[] Hits = new Collider[32];

        CharacterController _controller;
        Climbable _nearby;
        Climbable _active;
        int _climbingHash;
        bool _locomotionWasEnabled;
        string _lastAttemptWarning;
        float _nextInputDiagnosticTime;
        float _nextOverlapDiagnosticTime;

        Vector3 DetectionOrigin => transform.TransformPoint(detectionOffset);

        void Awake()
        {
            if (locomotion == null)
                locomotion = GetComponent<SideScrollerController>();
            if (animator == null)
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

            _controller = GetComponent<CharacterController>();
            _climbingHash = Animator.StringToHash(climbingParameter);

        }

        void OnEnable()
        {
            InteractionManager.OnJumpStarted += HandleJumpStarted;
            Debug.LogWarning(
                $"[PlayerClimber] Ready on '{name}'. Diagnostics={logDiagnostics}, " +
                $"Locomotion={(locomotion != null)}, CharacterController={(_controller != null)}, " +
                $"radius={radius:0.##}, layerMask={layerMask.value} (0x{layerMask.value:X}).",
                this);
        }

        void OnDisable()
        {
            InteractionManager.OnJumpStarted -= HandleJumpStarted;
            EndClimb();
        }

        void Update()
        {
            if (locomotion == null || _controller == null)
                return;

            InteractionManager input = InteractionManager.Instance;
            float verticalInput = input != null ? input.MoveInput.y : 0f;
            if (logDiagnostics && Mathf.Abs(verticalInput) >= verticalInputThreshold &&
                Time.unscaledTime >= _nextInputDiagnosticTime)
            {
                _nextInputDiagnosticTime = Time.unscaledTime + 1f;
                Debug.Log(
                    $"[PlayerClimber] Climb input={verticalInput:0.##}; grounded={locomotion.IsGrounded}; " +
                    $"locomotionEnabled={locomotion.LocomotionEnabled}; climbing={_active != null}; " +
                    $"sphereCenter={DetectionOrigin}; radius={radius:0.##}; mask={layerMask.value} (0x{layerMask.value:X}).",
                    this);
            }
            if (_active != null)
            {
                if (!_active.isActiveAndEnabled)
                {
                    EndClimb("ladder component became unavailable");
                    return;
                }

                MoveOnLadder(verticalInput);
                return;
            }

            if (Mathf.Abs(verticalInput) < verticalInputThreshold)
            {
                _lastAttemptWarning = null;
                return;
            }

            if (!locomotion.LocomotionEnabled || !locomotion.IsGrounded || input == null)
            {
                string reason = !locomotion.LocomotionEnabled
                    ? "normal locomotion is disabled"
                    : !locomotion.IsGrounded
                        ? "the Player is not grounded"
                        : "InteractionManager is missing";
                LogAttemptWarning($"Cannot start climbing because {reason}.");
                return;
            }

            if (GameStateManager.Instance != null &&
                GameStateManager.Instance.CurrentState == GameState.GameplayPushing)
            {
                LogAttemptWarning("Cannot start climbing while the Player is pushing.");
                return;
            }

            RefreshNearbyLadder();
            if (_nearby != null)
            {
                _lastAttemptWarning = null;
                BeginClimb(_nearby);
            }
            else
                LogFailedDetection();
        }

        void RefreshNearbyLadder()
        {
            _nearby = null;
            Physics.SyncTransforms();
            int count = Physics.OverlapSphereNonAlloc(
                DetectionOrigin,
                radius,
                Hits,
                layerMask,
                QueryTriggerInteraction.Collide);

            if (logDiagnostics && Time.unscaledTime >= _nextOverlapDiagnosticTime)
            {
                _nextOverlapDiagnosticTime = Time.unscaledTime + 1f;
                string firstHit = count > 0 && Hits[0] != null
                    ? $"{Hits[0].name} (layer {LayerMask.LayerToName(Hits[0].gameObject.layer)})"
                    : "none";
                Debug.Log(
                    $"[PlayerClimber] OverlapSphereNonAlloc found {count} collider(s); first hit: {firstHit}. " +
                    $"Center={DetectionOrigin}, radius={radius:0.##}, mask={layerMask.value} (0x{layerMask.value:X}).",
                    this);
            }

            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider hit = Hits[i];
                if (hit == null || !hit.enabled || !hit.gameObject.activeInHierarchy ||
                    hit.transform.IsChildOf(transform))
                    continue;

                Climbable ladder = hit.GetComponentInParent<Climbable>();
                if (ladder == null || !ladder.isActiveAndEnabled)
                    continue;

                float distance = (hit.ClosestPoint(DetectionOrigin) - DetectionOrigin).sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                _nearby = ladder;
            }
        }

        void BeginClimb(Climbable ladder)
        {
            if (ladder == null || locomotion == null)
                return;

            _active = ladder;
            _locomotionWasEnabled = locomotion.LocomotionEnabled;
            _controller.Move(new Vector3(ladder.ClimbX - transform.position.x, 0f, 0f));
            locomotion.SetClimbing(true);
            locomotion.SetLocomotionEnabled(false);
            SetClimbingAnimation(true);

            if (logDiagnostics)
                Debug.Log(
                    $"[PlayerClimber] Started climbing '{ladder.name}' (layer {LayerMask.LayerToName(ladder.gameObject.layer)}, " +
                    $"speed {ladder.ClimbSpeed:0.##}, vertical range {ladder.BottomY:0.##}–{ladder.TopY:0.##}).",
                    this);
        }

        void MoveOnLadder(float verticalInput)
        {
            if (_active == null)
                return;

            if (Mathf.Abs(verticalInput) < verticalInputThreshold)
                verticalInput = 0f;

            float currentY = transform.position.y;
            float desiredY = Mathf.Clamp(
                currentY + verticalInput * _active.ClimbSpeed * Time.deltaTime,
                _active.BottomY,
                _active.TopY);
            _controller.Move(Vector3.up * (desiredY - currentY));

            bool reachedTop = verticalInput > 0f && desiredY >= _active.TopY;
            bool reachedBottom = verticalInput < 0f && desiredY <= _active.BottomY;
            if (reachedTop)
                EndClimb("reached ladder top");
            else if (reachedBottom)
                EndClimb("reached ladder bottom");
        }

        void HandleJumpStarted()
        {
            if (_active != null)
                EndClimb("jump input");
        }

        void EndClimb(string reason = "component disabled")
        {
            if (_active == null)
                return;

            string ladderName = _active.name;
            _active = null;
            SetClimbingAnimation(false);
            if (locomotion != null)
            {
                locomotion.SetClimbing(false);
                locomotion.SetLocomotionEnabled(_locomotionWasEnabled);
            }

            if (logDiagnostics)
                Debug.Log($"[PlayerClimber] Stopped climbing '{ladderName}': {reason}.", this);
        }

        void LogFailedDetection()
        {
            if (!logDiagnostics)
                return;

            string maskDescription = $"{layerMask.value} (0x{layerMask.value:X})";
            LogAttemptWarning(
                $"Up/down input detected, but no Climbable collider was found within {radius:0.##}m " +
                $"of {DetectionOrigin}. Layer mask is {maskDescription}. Check that the ladder has an enabled collider " +
                "on an included layer and an enabled Climbable component.");
        }

        void LogAttemptWarning(string message)
        {
            if (!logDiagnostics || _lastAttemptWarning == message)
                return;

            _lastAttemptWarning = message;
            Debug.LogWarning($"[PlayerClimber] {message}", this);
        }

        void SetClimbingAnimation(bool climbing)
        {
            if (animator != null)
                animator.SetBool(_climbingHash, climbing);
        }

        void OnDrawGizmos()
        {
            if (drawGizmos)
                DrawDetectionGizmo(0.25f);
        }

        void OnDrawGizmosSelected()
        {
            DrawDetectionGizmo(1f);
        }

        void DrawDetectionGizmo(float alphaScale)
        {
            Vector3 origin = DetectionOrigin;
            Color color = gizmoColor;
            color.a *= alphaScale;
            Gizmos.color = color;
            Gizmos.DrawLine(transform.position, origin);
            Gizmos.DrawWireSphere(origin, radius);

            Color fill = color;
            fill.a *= 0.12f;
            Gizmos.color = fill;
            Gizmos.DrawSphere(origin, radius);
        }
    }
}
