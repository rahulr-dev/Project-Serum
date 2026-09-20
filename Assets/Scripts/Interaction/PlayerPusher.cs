using Character;
using Game;
using UnityEngine;

namespace InteractionSystem
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerPusher : MonoBehaviour
    {
        const int ReleaseGraceFrames = 12;

        [SerializeField] SideScrollerController locomotion;
        [SerializeField] Vector3 detectionOffset = new Vector3(0.8f, 0.8f, 0f);
        [SerializeField] float radius = 0.6f;
        [SerializeField] LayerMask layerMask = ~0;

        [Header("Gizmos")]
        [SerializeField] bool drawGizmos = true;
        [SerializeField] Color gizmoColor = new Color(0.2f, 0.75f, 1f, 1f);

        static readonly Collider[] Hits = new Collider[32];

        CharacterController _controller;
        Pushable _hitThisFrame;
        Pushable _active;
        float _lastX;
        int _missedFrames;
        bool _ownsPushState;
        GameState _stateBeforePush = GameState.Gameplay;

        Vector3 DetectionOrigin
        {
            get
            {
                Vector3 local = detectionOffset;
                if (locomotion != null)
                    local.x = Mathf.Abs(detectionOffset.x) * locomotion.FacingSign;
                return transform.TransformPoint(local);
            }
        }

        void Awake()
        {
            if (locomotion == null)
                locomotion = GetComponent<SideScrollerController>();
            _controller = GetComponent<CharacterController>();
        }

        void OnEnable()
        {
            _lastX = transform.position.x;
        }

        void LateUpdate()
        {
            RefreshHit();

            float x = transform.position.x;
            float movedX = x - _lastX;

            if (locomotion != null && locomotion.HorizontalSpeed * locomotion.FacingSign < 0f)
            {
                if (_active != null)
                    EndPush();
                _hitThisFrame = null;
                _lastX = x;
                return;
            }

            if (_hitThisFrame != null)
            {
                _missedFrames = 0;
                BeginPush(_hitThisFrame);

                float intendedX = locomotion != null
                    ? locomotion.HorizontalSpeed * Time.deltaTime
                    : movedX;
                _hitThisFrame.Push(intendedX);

                float leftoverX = intendedX - movedX;
                if (_controller != null && Mathf.Abs(leftoverX) > 0.0001f)
                    _controller.Move(new Vector3(leftoverX, 0f, 0f));

                _hitThisFrame = null;
                _lastX = transform.position.x;
                return;
            }

            _lastX = x;
            if (_active == null)
                return;

            _missedFrames++;
            if (_missedFrames >= ReleaseGraceFrames)
                EndPush();
        }

        void RefreshHit()
        {
            _hitThisFrame = null;
            if (locomotion == null || !locomotion.IsGrounded)
                return;

            float facing = locomotion.FacingSign;
            if (locomotion.HorizontalSpeed * facing <= 0f)
                return;

            Physics.SyncTransforms();

            Vector3 origin = DetectionOrigin;
            int count = Physics.OverlapSphereNonAlloc(origin, radius, Hits, layerMask, QueryTriggerInteraction.Ignore);

            Pushable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider col = Hits[i];
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                    continue;
                if (col.gameObject == gameObject)
                    continue;

                Pushable pushable = col.GetComponentInParent<Pushable>();
                if (pushable == null || !pushable.isActiveAndEnabled)
                    continue;
                if (pushable.gameObject == gameObject)
                    continue;

                float dist = (col.ClosestPoint(origin) - origin).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = pushable;
                }
            }

            _hitThisFrame = best;
        }

        void BeginPush(Pushable pushable)
        {
            _active = pushable;
            if (GameStateManager.Instance == null)
                return;

            if (GameStateManager.Instance.CurrentState == GameState.GameplayPushing)
                return;

            _stateBeforePush = GameStateManager.Instance.CurrentState;
            _ownsPushState = true;
            GameStateManager.Instance.EnterGameplayPushing();
        }

        void EndPush()
        {
            if (_active != null)
                _active.ClearPush();
            _active = null;
            _missedFrames = 0;

            bool restore = _ownsPushState;
            _ownsPushState = false;
            if (!restore || GameStateManager.Instance == null)
                return;

            if (GameStateManager.Instance.CurrentState != GameState.GameplayPushing)
                return;

            GameStateManager.Instance.SetState(_stateBeforePush);
        }

        void OnDrawGizmos()
        {
            if (!drawGizmos)
                return;

            DrawPushSphere(0.25f);
        }

        void OnDrawGizmosSelected()
        {
            DrawPushSphere(1f);
        }

        void DrawPushSphere(float alphaScale)
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
