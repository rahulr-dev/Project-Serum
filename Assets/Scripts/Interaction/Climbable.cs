using UnityEngine;

namespace InteractionSystem
{
    public class Climbable : MonoBehaviour
    {
        [SerializeField] Transform topOverride;
        [SerializeField] bool allowRight = true;
        [SerializeField] bool allowLeft = true;

        public Transform TopOverride => topOverride;

        public bool AllowsFacing(float facingSign)
        {
            if (facingSign >= 0f)
                return allowRight;
            return allowLeft;
        }
    }
}
