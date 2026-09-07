using UnityEngine;
using UnityEngine.Events;

namespace InteractionSystem
{
    public class SwitchEvent : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onSwitch = new UnityEvent();

        public UnityEvent OnSwitch => onSwitch;

        public void Play()
        {
            if (onSwitch != null)
            {
                onSwitch.Invoke();
            }
        }
    }
}
