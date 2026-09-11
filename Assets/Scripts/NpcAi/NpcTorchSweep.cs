using Events;
using UnityEngine;

namespace NpcAi
{
    public class NpcTorchSweep : MonoBehaviour
    {
        [SerializeField] SerumActionBridge bridge;
        [SerializeField] float yawDegrees = 45f;

        public void Sweep()
        {
            if (bridge == null)
                bridge = GetComponent<SerumActionBridge>();

            bridge?.SmoothRotateByY(yawDegrees);
        }
    }
}
