using System.Collections;
using Game;
using UnityEngine;

namespace InteractionSystem
{
    public class Interactable : MonoBehaviour
    {
        [SerializeField] InteractionGraph graph;

        [Header("Gizmos")]
        [SerializeField] bool drawGizmos = true;
        [SerializeField] Color gizmoColor = new Color(0.2f, 0.85f, 1f, 1f);

        Coroutine _running;

        public bool IsRunning => _running != null;
        public InteractionGraph Graph => graph;

        public void Interact()
        {
            if (graph == null)
            {
                Debug.LogWarning($"[Interactable] No InteractionGraph assigned on {gameObject.name}.");
                return;
            }

            if (_running != null)
                return;

            _running = StartCoroutine(RunGraph());
        }

        IEnumerator RunGraph()
        {
            GameStateManager states = GameStateManager.Instance;
            if (states != null)
                states.SetState(graph.playState);

            yield return InteractionGraphRunner.Run(graph);

            if (states != null && states.CurrentState == graph.playState)
                states.SetState(graph.endState);

            _running = null;
        }

        void OnDrawGizmos()
        {
            if (!drawGizmos)
                return;

            DrawInteractableGizmos(0.35f);
        }

        void OnDrawGizmosSelected()
        {
            DrawInteractableGizmos(1f);
        }

        void DrawInteractableGizmos(float alphaScale)
        {
            Color color = gizmoColor;
            color.a *= alphaScale;
            Gizmos.color = color;

            Collider[] colliders = GetComponentsInChildren<Collider>();
            if (colliders != null && colliders.Length > 0)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider col = colliders[i];
                    if (col == null || !col.enabled)
                        continue;

                    DrawCollider(col, color);
                }
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 0.35f);
            }

            Color fill = color;
            fill.a *= 0.2f;
            Gizmos.color = fill;
            Gizmos.DrawSphere(transform.position, 0.12f);
        }

        static void DrawCollider(Collider col, Color color)
        {
            Gizmos.color = color;
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = col.transform.localToWorldMatrix;

            switch (col)
            {
                case BoxCollider box:
                    Gizmos.DrawWireCube(box.center, box.size);
                    break;
                case SphereCollider sphere:
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                    break;
                case CapsuleCollider capsule:
                    Gizmos.DrawWireSphere(capsule.center + Vector3.up * (capsule.height * 0.5f - capsule.radius), capsule.radius);
                    Gizmos.DrawWireSphere(capsule.center - Vector3.up * (capsule.height * 0.5f - capsule.radius), capsule.radius);
                    break;
                default:
                    Gizmos.matrix = Matrix4x4.identity;
                    Bounds bounds = col.bounds;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                    break;
            }

            Gizmos.matrix = prev;
        }
    }
}
