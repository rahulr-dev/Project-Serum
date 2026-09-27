using UnityEngine;

namespace Character
{
    /// <summary>Follows a player's world X and Y positions with independent offsets, preserving Z.</summary>
    public class PlayerFollower : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] float xOffset;
        [SerializeField] float yOffset;

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
            position.y = player.position.y + yOffset;
            transform.position = position;
        }

        public void SetPlayer(Transform playerTransform)
        {
            player = playerTransform;
        }
    }
}
