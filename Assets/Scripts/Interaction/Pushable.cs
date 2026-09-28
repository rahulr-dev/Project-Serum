using System;
using UnityEngine;
using UnityEngine.Events;

namespace InteractionSystem
{
    [RequireComponent(typeof(Rigidbody))]
    public class Pushable : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] bool clampX;
        [SerializeField] float minX;
        [SerializeField] float maxX;
        [Tooltip("Push the object around a base hinge instead of moving it linearly. Gravity can then tip it over.")]
        [SerializeField] bool rotateOnly;
        [Tooltip("Optional existing hinge. When empty, one is added at this GameObject's origin (the base pivot).")]
        [SerializeField] HingeJoint hinge;
        [Tooltip("Angular acceleration applied for each unit of player push.")]
        [SerializeField] float rotationPushTorque = 150f;

        [Header("Fall Detection")]
        [SerializeField] bool detectFall;
        [Tooltip("Local position from which the downward SphereCast starts. Place this near the wall's leading/top edge.")]
        [SerializeField] Vector3 fallCheckOffset;
        [Min(0.001f)] [SerializeField] float fallCheckRadius = 0.2f;
        [Min(0f)] [SerializeField] float fallCheckDistance = 0.25f;
        [Range(0f, 90f)] [SerializeField] float minimumFallAngle = 60f;
        [SerializeField] LayerMask fallCheckLayers = ~0;
        [SerializeField] UnityEvent onFellDown = new UnityEvent();

        Rigidbody _body;
        bool _fellDown;

        public bool IsBeingPushed { get; private set; }
        public event Action PushStarted;
        public event Action PushStopped;

        void Reset()
        {
            SetupBody(GetComponent<Rigidbody>());
        }

        void Awake()
        {
            _body = GetComponent<Rigidbody>();
            if (rotateOnly)
                EnsureBaseHinge();
            SetupBody(_body);
        }

        void SetupBody(Rigidbody body)
        {
            if (body == null)
                return;

            body.isKinematic = false;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = rotateOnly
                ? RigidbodyConstraints.FreezePositionZ |
                  RigidbodyConstraints.FreezeRotationX |
                  RigidbodyConstraints.FreezeRotationY
                : RigidbodyConstraints.FreezePositionZ |
                  RigidbodyConstraints.FreezeRotation;
        }

        public void Push(float worldDeltaX)
        {
            if (_body == null)
                return;

            float dt = Time.deltaTime;
            if (dt <= 0.0001f)
                return;

            if (rotateOnly)
            {
                _body.AddTorque(transform.forward * (-worldDeltaX / dt * rotationPushTorque),
                    ForceMode.Acceleration);
                NotifyPushStarted();
                return;
            }

            Vector3 velocity = _body.linearVelocity;
            velocity.x = worldDeltaX / dt;
            velocity.z = 0f;
            _body.linearVelocity = velocity;
            NotifyPushStarted();
        }

        public void ClearPush()
        {
            if (IsBeingPushed)
            {
                IsBeingPushed = false;
                PushStopped?.Invoke();
            }
            if (_body == null)
                return;

            if (rotateOnly)
                return;

            Vector3 velocity = _body.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            _body.linearVelocity = velocity;
        }

        void FixedUpdate()
        {
            if (_body == null)
                return;

            CheckFellDown();

            if (!clampX || rotateOnly)
                return;

            Vector3 pos = _body.position;
            float x = Mathf.Clamp(pos.x, minX, maxX);
            if (Mathf.Abs(x - pos.x) < 0.0001f)
                return;

            pos.x = x;
            _body.position = pos;
            Vector3 velocity = _body.linearVelocity;
            velocity.x = 0f;
            _body.linearVelocity = velocity;
        }

        void NotifyPushStarted()
        {
            if (IsBeingPushed)
                return;

            IsBeingPushed = true;
            PushStarted?.Invoke();
        }

        void EnsureBaseHinge()
        {
            if (hinge == null)
                hinge = GetComponent<HingeJoint>();
            if (hinge == null)
                hinge = gameObject.AddComponent<HingeJoint>();

            // The parent/object origin is the hinge point. A world-connected joint pins it there.
            hinge.connectedBody = null;
            hinge.anchor = Vector3.zero;
            hinge.axis = Vector3.forward;
            hinge.autoConfigureConnectedAnchor = true;
        }

        void CheckFellDown()
        {
            if (!detectFall || _fellDown)
                return;

            float tilt = Vector3.Angle(transform.up, Vector3.up);
            if (tilt < minimumFallAngle)
                return;

            Vector3 origin = transform.TransformPoint(fallCheckOffset);
            if (!Physics.SphereCast(origin, fallCheckRadius, Vector3.down, out _, fallCheckDistance,
                    fallCheckLayers, QueryTriggerInteraction.Ignore))
                return;

            _fellDown = true;
            ClearPush();
            onFellDown?.Invoke();
            enabled = false;
        }

        void OnDrawGizmosSelected()
        {
            if (!detectFall)
                return;

            Vector3 origin = transform.TransformPoint(fallCheckOffset);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, fallCheckRadius);
            Gizmos.DrawLine(origin, origin + Vector3.down * fallCheckDistance);
            Gizmos.DrawWireSphere(origin + Vector3.down * fallCheckDistance, fallCheckRadius);
        }
    }
}
