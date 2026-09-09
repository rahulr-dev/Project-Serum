using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace InteractionSystem
{
    public class InteractionActionHandler : MonoBehaviour
    {
        [SerializeField] string handlerId;
        [SerializeField] UnityEvent onExecute = new UnityEvent();

        static readonly Dictionary<string, InteractionActionHandler> Registry = new Dictionary<string, InteractionActionHandler>();

        public string HandlerId => handlerId;

        void Reset()
        {
            EnsureHandlerId();
        }

        void Awake()
        {
            EnsureHandlerId();
            Register();
        }

        void OnEnable()
        {
            EnsureHandlerId();
            Register();
        }

        void OnDisable()
        {
            Unregister();
        }

        public void Execute()
        {
            onExecute?.Invoke();
        }

        public static InteractionActionHandler FindById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (Registry.TryGetValue(id, out InteractionActionHandler handler) && handler != null)
                return handler;

            InteractionActionHandler[] handlers = Object.FindObjectsByType<InteractionActionHandler>(FindObjectsSortMode.None);
            for (int i = 0; i < handlers.Length; i++)
            {
                InteractionActionHandler candidate = handlers[i];
                if (candidate == null || string.IsNullOrEmpty(candidate.handlerId))
                    continue;

                if (candidate.handlerId != id)
                    continue;

                candidate.Register();
                return candidate;
            }

            return null;
        }

        void EnsureHandlerId()
        {
            if (!string.IsNullOrEmpty(handlerId))
                return;

            handlerId = System.Guid.NewGuid().ToString("N");
        }

        void Register()
        {
            if (string.IsNullOrEmpty(handlerId))
                return;

            Registry[handlerId] = this;
        }

        void Unregister()
        {
            if (string.IsNullOrEmpty(handlerId))
                return;

            if (Registry.TryGetValue(handlerId, out InteractionActionHandler current) && current == this)
                Registry.Remove(handlerId);
        }
    }
}
