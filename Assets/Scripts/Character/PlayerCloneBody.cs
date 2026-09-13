using UnityEngine;

namespace Character
{
    public class PlayerCloneBody : MonoBehaviour
    {
        public bool IsDead { get; private set; }

        public void Kill()
        {
            if (IsDead)
                return;

            IsDead = true;

            SideScrollerController motor = GetComponent<SideScrollerController>();
            if (motor != null)
            {
                motor.SetLocomotionEnabled(false);
                motor.enabled = false;
            }

            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                if (GetComponent<CapsuleCollider>() == null)
                {
                    CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
                    capsule.height = controller.height;
                    capsule.radius = controller.radius;
                    capsule.center = controller.center;
                }

                controller.enabled = false;
            }

            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
                animator.enabled = false;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();

            body.useGravity = true;
            body.isKinematic = false;
            body.AddForce(new Vector3(0f, 1.5f, 0.8f), ForceMode.Impulse);
            body.AddTorque(new Vector3(1.2f, 0f, 0.4f), ForceMode.Impulse);
        }
    }
}
