using UnityEngine;
using UnityEngine.InputSystem;

namespace InteractionSystem
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private float moveSpeed = 4f;

        [SerializeField]
        private float gravity = -9.81f;

        private CharacterController characterController;
        private float verticalVelocity = 0f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            float horizontal = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed ||
                    Keyboard.current.leftArrowKey.isPressed)
                {
                    horizontal -= 1f;
                }

                if (Keyboard.current.dKey.isPressed ||
                    Keyboard.current.rightArrowKey.isPressed)
                {
                    horizontal += 1f;
                }
            }

            if (characterController != null)
            {
                if (characterController.isGrounded && verticalVelocity < 0f)
                {
                    verticalVelocity = -2f;
                }
                else
                {
                    verticalVelocity += gravity * Time.deltaTime;
                }

                Vector3 movement = new Vector3(horizontal * moveSpeed, verticalVelocity, 0f);
                characterController.Move(movement * Time.deltaTime);
            }

            // Keep player strictly at Z = 0
            Vector3 currentPos = transform.position;
            if (currentPos.z != 0f)
            {
                currentPos.z = 0f;
                transform.position = currentPos;
            }
        }
    }
}