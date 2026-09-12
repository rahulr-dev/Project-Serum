using System.Collections.Generic;
using Character;
using UnityEngine;
using UnityEngine.Events;

namespace Events
{
    [RequireComponent(typeof(Collider))]
    public class AreaTrigger : MonoBehaviour
    {
        [SerializeField] UnityEvent onEnter = new UnityEvent();
        [SerializeField] UnityEvent onExit = new UnityEvent();

        readonly HashSet<Collider> _inside = new HashSet<Collider>();

        void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
                return;

            if (!_inside.Add(other))
                return;

            if (_inside.Count == 1)
                onEnter?.Invoke();
        }

        void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
                return;

            if (!_inside.Remove(other))
                return;

            if (_inside.Count == 0)
                onExit?.Invoke();
        }

        void OnDisable()
        {
            _inside.Clear();
        }

        static bool IsPlayer(Collider other)
        {
            return other != null
                && other.GetComponentInParent<SideScrollerController>() != null;
        }
    }
}
