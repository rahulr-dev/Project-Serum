using UnityEngine;

namespace InteractionSystem
{
    [RequireComponent(typeof(Collider))]
    public class Climbable : MonoBehaviour
    {
        [SerializeField] Transform bottomPoint;
        [SerializeField] Transform topPoint;
        [SerializeField, Min(0.1f)] float fallbackHeight = 3f;
        [SerializeField, Min(0.1f)] float climbSpeed = 2.5f;

        [Header("Gizmos")]
        [SerializeField] bool drawGizmos = true;
        [SerializeField] Color gizmoColor = new Color(0.2f, 0.75f, 1f, 1f);

        public float ClimbSpeed => climbSpeed;
        public float BottomY => bottomPoint != null ? bottomPoint.position.y : transform.position.y;
        public float TopY => topPoint != null ? topPoint.position.y : BottomY + fallbackHeight;
        public float ClimbX => bottomPoint != null ? bottomPoint.position.x : transform.position.x;

        void Awake()
        {
            Collider ladderCollider = GetComponent<Collider>();
            if (ladderCollider == null || !ladderCollider.enabled)
            {
                Debug.LogWarning(
                    $"[Climbable] '{name}' needs an enabled Collider for PlayerClimber to detect it.",
                    this);
                return;
            }

            if (!ladderCollider.isTrigger)
                Debug.LogWarning(
                    $"[Climbable] '{name}' Collider is not a trigger. It may block the Player while snapping onto or climbing the ladder.",
                    this);
        }

        void OnDrawGizmos()
        {
            if (drawGizmos)
                DrawClimbGizmo(0.3f);
        }

        void OnDrawGizmosSelected()
        {
            DrawClimbGizmo(1f);
        }

        void DrawClimbGizmo(float alphaScale)
        {
            Vector3 bottom = bottomPoint != null ? bottomPoint.position : transform.position;
            Vector3 top = topPoint != null
                ? topPoint.position
                : bottom + Vector3.up * fallbackHeight;

            Color color = gizmoColor;
            color.a *= alphaScale;
            Gizmos.color = color;
            Gizmos.DrawLine(bottom, top);
            Gizmos.DrawWireSphere(bottom, 0.12f);
            Gizmos.DrawWireSphere(top, 0.12f);
        }
    }
}
