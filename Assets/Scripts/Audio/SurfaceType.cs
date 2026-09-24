using UnityEngine;

namespace Serum.Audio
{
    public class SurfaceType : MonoBehaviour
    {
        [SerializeField] SoundEvent footstepSound;
        public SoundEvent FootstepSound => footstepSound;
    }
}
