using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace QTE
{
    public class QTEActionHandler : MonoBehaviour
    {
        [SerializeField] string handlerId;
        [SerializeField] UnityEvent onExecute = new UnityEvent();

        static readonly Dictionary<string, QTEActionHandler> Registry = new Dictionary<string, QTEActionHandler>();

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

        public static QTEActionHandler FindById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (Registry.TryGetValue(id, out QTEActionHandler handler) && handler != null)
                return handler;

            QTEActionHandler[] handlers = Object.FindObjectsByType<QTEActionHandler>(FindObjectsSortMode.None);
            for (int i = 0; i < handlers.Length; i++)
            {
                QTEActionHandler candidate = handlers[i];
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

            if (Registry.TryGetValue(handlerId, out QTEActionHandler current) && current == this)
                Registry.Remove(handlerId);
        }
    }
}
