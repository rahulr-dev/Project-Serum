using UnityEngine;

namespace Character
{
    /// <summary>Follows a player's world X position while preserving this object's Y and Z coordinates.</summary>
    public class PlayerFollower : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] float xOffset;

        void Awake()
        {
            if (player == null)
            {
                SideScrollerController playerController = FindFirstObjectByType<SideScrollerController>();
                if (playerController != null)
                    player = playerController.transform;
            }
        }

        void LateUpdate()
        {
            if (player == null)
                return;

            Vector3 position = transform.position;
            position.x = player.position.x + xOffset;
            transform.position = position;
        }

        public void SetPlayer(Transform playerTransform)
        {
            player = playerTransform;
        }
    }
}
