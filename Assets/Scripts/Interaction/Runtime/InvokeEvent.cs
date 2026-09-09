using UnityEngine;
using UnityEngine.Events;

namespace InteractionSystem
{
    public class InvokeEvent : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onInvoke = new UnityEvent();

        public UnityEvent OnInvoke => onInvoke;

        public void Play()
        {
            if (onInvoke != null)
            {
                onInvoke.Invoke();
            }
        }
    }
}
