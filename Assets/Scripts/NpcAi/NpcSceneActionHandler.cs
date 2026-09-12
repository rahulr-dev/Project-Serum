using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NpcAi
{
    public class NpcSceneActionHandler : MonoBehaviour
    {
        [SerializeField] string handlerId;
        [SerializeField] UnityEvent onExecute = new UnityEvent();

        static readonly Dictionary<string, NpcSceneActionHandler> Registry = new Dictionary<string, NpcSceneActionHandler>();

        public string HandlerId => handlerId;
        public UnityEvent OnExecute => onExecute;

        public void AssignHandlerId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            Unregister();
            handlerId = id;
            Register();
        }

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

        public static NpcSceneActionHandler FindById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (Registry.TryGetValue(id, out NpcSceneActionHandler handler) && handler != null)
                return handler;

            NpcSceneActionHandler[] handlers = Object.FindObjectsByType<NpcSceneActionHandler>(FindObjectsSortMode.None);
            for (int i = 0; i < handlers.Length; i++)
            {
                NpcSceneActionHandler candidate = handlers[i];
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

            if (Registry.TryGetValue(handlerId, out NpcSceneActionHandler current) && current == this)
                Registry.Remove(handlerId);
        }
    }
}
