using System.Collections.Generic;
using UnityEngine;

namespace NpcAi
{
    public class NpcMoveTarget : MonoBehaviour
    {
        [SerializeField] string targetId;

        static readonly Dictionary<string, NpcMoveTarget> Registry = new Dictionary<string, NpcMoveTarget>();

        public string TargetId => targetId;

        public void AssignTargetId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            Unregister();
            targetId = id;
            Register();
        }

        public void EnsureRegistered()
        {
            EnsureTargetId();
            Register();
        }

        void Reset()
        {
            EnsureTargetId();
        }

        void Awake()
        {
            EnsureTargetId();
            Register();
        }

        void OnEnable()
        {
            EnsureTargetId();
            Register();
        }

        void OnDisable()
        {
            Unregister();
        }

        public static NpcMoveTarget FindById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (Registry.TryGetValue(id, out NpcMoveTarget target) && target != null)
                return target;

            NpcMoveTarget[] targets = Object.FindObjectsByType<NpcMoveTarget>(FindObjectsSortMode.None);
            for (int i = 0; i < targets.Length; i++)
            {
                NpcMoveTarget candidate = targets[i];
                if (candidate == null || string.IsNullOrEmpty(candidate.targetId))
                    continue;

                if (candidate.targetId != id)
                    continue;

                candidate.Register();
                return candidate;
            }

            return null;
        }

        void EnsureTargetId()
        {
            if (!string.IsNullOrEmpty(targetId))
                return;

            targetId = System.Guid.NewGuid().ToString("N");
        }

        void Register()
        {
            if (string.IsNullOrEmpty(targetId))
                return;

            Registry[targetId] = this;
        }

        void Unregister()
        {
            if (string.IsNullOrEmpty(targetId))
                return;

            if (Registry.TryGetValue(targetId, out NpcMoveTarget current) && current == this)
                Registry.Remove(targetId);
        }
    }
}
