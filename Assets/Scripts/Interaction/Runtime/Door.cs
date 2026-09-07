using UnityEngine;

namespace InteractionSystem
{
    public class Door : MonoBehaviour
    {
        [SerializeField]
        private bool isOpen = false;

        [SerializeField]
        private Vector3 openOffset = new Vector3(0f, 3f, 0f);

        [SerializeField]
        private float openSpeed = 5f;

        private Vector3 closedPosition;
        private Vector3 targetPosition;
        private bool isOpening = false;

        private void Awake()
        {
            closedPosition = transform.position;
            targetPosition = closedPosition;
        }

        private void Update()
        {
            if (isOpening)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, openSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
                {
                    isOpening = false;
                }
            }
        }

        public void Open()
        {
            if (isOpen) return;
            isOpen = true;
            targetPosition = closedPosition + openOffset;
            isOpening = true;
            Debug.Log("Door opened.");
        }
    }
}
