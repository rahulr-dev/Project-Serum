using System.Collections;
using Game;
using InteractionSystem;
using UnityEngine;

namespace Character
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterClimber : MonoBehaviour
    {
        [SerializeField] SideScrollerController locomotion;
        [SerializeField] LayerMask climbMask = ~0;
        [SerializeField] float grabHeight = 1.1f;
        [SerializeField] float grabRange = 0.55f;
        [SerializeField] float hangDistance = 0.35f;
        [SerializeField] float hangHeight = 1.2f;
        [SerializeField] float standInset = 0.4f;
        [SerializeField] float topOvershoot = 0.2f;
        [SerializeField] float topProbeHeight = 0.8f;
        [SerializeField] float mantleDuration = 0.25f;
        [SerializeField] float cooldown = 0.3f;
        [SerializeField] float standSkin = 0.02f;
        [SerializeField] bool drawGizmos = true;

        CharacterController _controller;
        Animator _animator;
        bool _climbing;
        float _cooldownUntil;
        Vector3 _gizmoForwardOrigin;
        Vector3 _gizmoForwardEnd;
        Vector3 _gizmoDownOrigin;
        Vector3 _gizmoDownEnd;
        Vector3 _gizmoHang;
        Vector3 _gizmoStand;
        bool _gizmoHasLedge;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (locomotion == null)
                locomotion = GetComponent<SideScrollerController>();
            _animator = GetComponentInChildren<Animator>();
        }

        void Update()
        {
            if (_climbing || Time.time < _cooldownUntil)
                return;

            if (locomotion != null && locomotion.IsGrounded)
                return;

            if (GameStateManager.Instance != null &&
                GameStateManager.Instance.CurrentState == GameState.GameplayClimbing)
                return;

            if (!TryFindLedge(out Vector3 hang, out Vector3 stand, out float facingSign))
                return;

            StartCoroutine(Mantle(hang, stand, facingSign));
        }

        bool TryFindLedge(out Vector3 hangWorld, out Vector3 standWorld, out float facingSign)
        {
            hangWorld = default;
            standWorld = default;
            facingSign = locomotion != null ? locomotion.FacingSign : 1f;

            Vector3 origin = transform.position + Vector3.up * grabHeight;
            Vector3 forward = Vector3.right * facingSign;
            _gizmoForwardOrigin = origin;
            _gizmoForwardEnd = origin + forward * grabRange;
            _gizmoHasLedge = false;

            if (!Physics.Raycast(origin, forward, out RaycastHit wallHit, grabRange, climbMask, QueryTriggerInteraction.Ignore))
                return false;

            if (wallHit.normal.y > 0.5f)
                return false;

            Climbable climbable = wallHit.collider.GetComponentInParent<Climbable>();
            if (climbable == null || !climbable.AllowsFacing(facingSign))
                return false;

            Vector3 downOrigin = wallHit.point + forward * topOvershoot + Vector3.up * topProbeHeight;
            float downDist = topProbeHeight + 0.5f;
            _gizmoDownOrigin = downOrigin;
            _gizmoDownEnd = downOrigin + Vector3.down * downDist;

            if (!Physics.Raycast(downOrigin, Vector3.down, out RaycastHit topHit, downDist, climbMask, QueryTriggerInteraction.Ignore))
                return false;

            Climbable topClimb = topHit.collider.GetComponentInParent<Climbable>();
            if (topClimb != climbable && topClimb == null)
                return false;

            float topY = climbable.TopOverride != null ? climbable.TopOverride.position.y : topHit.point.y;
            Vector3 edge = new Vector3(topHit.point.x, topY, transform.position.z);

            hangWorld = new Vector3(edge.x - facingSign * hangDistance, 0f, transform.position.z);
            hangWorld.y = PivotYForFeet(edge.y - hangHeight);

            standWorld = new Vector3(edge.x + facingSign * standInset, 0f, transform.position.z);
            standWorld.y = PivotYForFeet(topY + _controller.skinWidth + standSkin);

            Vector3 head = standWorld + _controller.center + Vector3.up * (_controller.height * 0.45f);
            if (Physics.Raycast(head, Vector3.up, 0.4f, climbMask, QueryTriggerInteraction.Ignore))
                return false;

            _gizmoHang = hangWorld;
            _gizmoStand = standWorld;
            _gizmoHasLedge = true;
            return true;
        }

        float FeetY()
        {
            return transform.position.y + _controller.center.y - _controller.height * 0.5f;
        }

        float PivotYForFeet(float targetFeetY)
        {
            return transform.position.y + (targetFeetY - FeetY());
        }

        IEnumerator Mantle(Vector3 hang, Vector3 stand, float facingSign)
        {
            _climbing = true;
            GameState restore = GameState.Gameplay;
            bool ownsState = false;

            if (GameStateManager.Instance != null &&
                GameStateManager.Instance.CurrentState != GameState.GameplayClimbing)
            {
                restore = GameStateManager.Instance.CurrentState;
                ownsState = true;
                GameStateManager.Instance.EnterGameplayClimbing();
            }

            if (locomotion != null)
            {
                locomotion.SetLocomotionEnabled(false);
                if (facingSign >= 0f)
                    locomotion.FaceRight();
                else
                    locomotion.FaceLeft();
            }

            SetClimbingAnim(true);
            SnapTo(hang);

            float duration = Mathf.Max(0.01f, mantleDuration);
            float elapsed = 0f;
            Vector3 start = hang;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                SnapTo(Vector3.Lerp(start, stand, t));
                yield return null;
            }

            SnapTo(stand);
            _controller.enabled = true;
            SetClimbingAnim(false);

            if (locomotion != null)
                locomotion.SetLocomotionEnabled(true);

            if (ownsState && GameStateManager.Instance != null &&
                GameStateManager.Instance.CurrentState == GameState.GameplayClimbing)
                GameStateManager.Instance.SetState(restore);

            _cooldownUntil = Time.time + cooldown;
            _climbing = false;
        }

        void SnapTo(Vector3 worldPivot)
        {
            _controller.enabled = false;
            transform.position = worldPivot;
        }

        void SetClimbingAnim(bool climbing)
        {
            if (_animator == null)
                return;

            for (int i = 0; i < _animator.parameterCount; i++)
            {
                AnimatorControllerParameter p = _animator.GetParameter(i);
                if (p.name == "climbing" && p.type == AnimatorControllerParameterType.Bool)
                {
                    _animator.SetBool("climbing", climbing);
                    return;
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
                return;

            if (_controller == null)
                _controller = GetComponent<CharacterController>();
            if (locomotion == null)
                locomotion = GetComponent<SideScrollerController>();
            if (_controller != null)
                TryFindLedge(out _, out _, out _);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_gizmoForwardOrigin, _gizmoForwardEnd);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_gizmoDownOrigin, _gizmoDownEnd);
            if (!_gizmoHasLedge)
                return;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_gizmoHang, 0.08f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_gizmoStand, 0.08f);
        }
    }
}
