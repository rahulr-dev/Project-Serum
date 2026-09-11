using UnityEngine;

namespace InteractionSystem
{
    [RequireComponent(typeof(Rigidbody))]
    public class Pushable : MonoBehaviour
    {
        [SerializeField] bool clampX;
        [SerializeField] float minX;
        [SerializeField] float maxX;

        Rigidbody _body;

        public bool IsBeingPushed { get; private set; }

        void Reset()
        {
            SetupBody(GetComponent<Rigidbody>());
        }

        void Awake()
        {
            _body = GetComponent<Rigidbody>();
            SetupBody(_body);
        }

        static void SetupBody(Rigidbody body)
        {
            if (body == null)
                return;

            body.isKinematic = false;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezePositionZ |
                               RigidbodyConstraints.FreezeRotation;
        }

        public void Push(float worldDeltaX)
        {
            if (_body == null)
                return;

            float dt = Time.deltaTime;
            if (dt <= 0.0001f)
                return;

            Vector3 velocity = _body.linearVelocity;
            velocity.x = worldDeltaX / dt;
            velocity.z = 0f;
            _body.linearVelocity = velocity;
            IsBeingPushed = true;
        }

        public void ClearPush()
        {
            IsBeingPushed = false;
            if (_body == null)
                return;

            Vector3 velocity = _body.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            _body.linearVelocity = velocity;
        }

        void FixedUpdate()
        {
            if (_body == null || !clampX)
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
    }
}
