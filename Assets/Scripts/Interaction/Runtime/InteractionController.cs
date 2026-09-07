using UnityEngine;
using UnityEngine.InputSystem;

namespace InteractionSystem
{
    public class InteractionController : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField]
        private float interactionDistance = 2f;

        [SerializeField]
        private Key interactionKey = Key.E;

        [Header("Detection Point")]
        [SerializeField]
        private Transform interactionPoint;

        private void Update()
        {
            bool isEPressed = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    isEPressed = true;
                }
                else if (interactionKey != Key.None && Keyboard.current[interactionKey].wasPressedThisFrame)
                {
                    isEPressed = true;
                }
            }

            if (isEPressed)
            {
                TryInteract();
            }
        }

        private void TryInteract()
        {
            Vector3 origin = (interactionPoint != null) ? interactionPoint.position : transform.position;

            // Reliable 3D overlap sphere detection
            Collider[] hitColliders = Physics.OverlapSphere(origin, interactionDistance);
            InteractionInteractable bestInteractable = null;
            float closestDistance = float.MaxValue;

            if (hitColliders != null)
            {
                for (int i = 0; i < hitColliders.Length; i++)
                {
                    Collider col = hitColliders[i];
                    if (col == null || col.gameObject == gameObject) continue;

                    InteractionInteractable interactable = col.GetComponent<InteractionInteractable>();
                    if (interactable == null)
                    {
                        interactable = col.GetComponentInParent<InteractionInteractable>();
                    }

                    if (interactable != null)
                    {
                        float dist = Vector3.Distance(origin, col.transform.position);
                        if (dist < closestDistance)
                        {
                            closestDistance = dist;
                            bestInteractable = interactable;
                        }
                    }
                }
            }

            if (bestInteractable != null)
            {
                Debug.Log("E pressed - Door interacted");
                bestInteractable.Interact();
            }
            else
            {
                Debug.Log("No interactable in range.");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = (interactionPoint != null) ? interactionPoint.position : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, interactionDistance);
        }
    }
}