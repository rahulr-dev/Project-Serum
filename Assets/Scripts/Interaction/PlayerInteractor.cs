using Game;
using Interaction;
using UnityEngine;

namespace InteractionSystem
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Vector3 detectionOffset = new Vector3(0f, 0.8f, 0.8f);
        [SerializeField] float radius = 1.2f;
        [SerializeField] LayerMask layerMask = ~0;

        [Header("Gizmos")]
        [SerializeField] bool drawGizmos = true;
        [SerializeField] Color gizmoColor = new Color(1f, 0.85f, 0.15f, 1f);

        static readonly Collider[] Hits = new Collider[32];

        void OnEnable()
        {
            InteractionManager.OnInteractStarted += HandleInteractStarted;
        }

        void OnDisable()
        {
            InteractionManager.OnInteractStarted -= HandleInteractStarted;
        }

        void HandleInteractStarted()
        {
            if (!AllowsWorldInteract)
                return;

            Interactable target = FindClosestInteractable();
            if (target == null || target.IsRunning)
                return;

            target.Interact();
        }

        static bool AllowsWorldInteract
        {
            get
            {
                GameStateManager states = GameStateManager.Instance;
                if (states == null)
                    return true;

                GameState state = states.CurrentState;
                return state == GameState.Gameplay || state == GameState.GameplayDialogue;
            }
        }

        Interactable FindClosestInteractable()
        {
            Vector3 origin = DetectionOrigin;
            int count = Physics.OverlapSphereNonAlloc(origin, radius, Hits, layerMask, QueryTriggerInteraction.Collide);

            Interactable best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = Hits[i];
                if (col == null)
                    continue;

                if (col.transform == transform || col.transform.IsChildOf(transform))
                    continue;

                Interactable interactable = col.GetComponent<Interactable>();
                if (interactable == null)
                    interactable = col.GetComponentInParent<Interactable>();

                if (interactable == null)
                    continue;

                float dist = Vector3.SqrMagnitude(col.transform.position - origin);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = interactable;
                }
            }

            return best;
        }

        Vector3 DetectionOrigin => transform.TransformPoint(detectionOffset);

        void OnDrawGizmos()
        {
            if (!drawGizmos)
                return;

            DrawInteractionSphere(0.25f);
        }

        void OnDrawGizmosSelected()
        {
            DrawInteractionSphere(1f);
        }

        void DrawInteractionSphere(float alphaScale)
        {
            Vector3 origin = DetectionOrigin;
            Color color = gizmoColor;
            color.a *= alphaScale;

            Gizmos.color = color;
            Gizmos.DrawLine(transform.position, origin);
            Gizmos.DrawWireSphere(origin, radius);

            Color fill = color;
            fill.a *= 0.12f;
            Gizmos.color = fill;
            Gizmos.DrawSphere(origin, radius);
        }
    }
}
