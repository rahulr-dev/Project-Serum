using UnityEngine;
using UnityEngine.Events;

namespace InteractionSystem
{
    public class SwitchEvent : InvokeEvent
    {
        public UnityEvent OnSwitch => OnInvoke;
    }
}

