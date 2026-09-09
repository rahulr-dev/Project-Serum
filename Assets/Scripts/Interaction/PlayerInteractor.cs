using System.Collections.Generic;
using Game;
using Interaction;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        [Header("Overlay")]
        [SerializeField] bool showScreenOverlay = true;

        public const string OverlayPrefsKey = "Serum.InteractablesOverlay.Enabled";

        static readonly Collider[] Hits = new Collider[64];
        readonly List<OverlapHit> _overlaps = new List<OverlapHit>(16);
        readonly HashSet<int> _seenColliders = new HashSet<int>();

        public static PlayerInteractor Instance { get; private set; }

        public Interactable Current { get; private set; }
        public Collider CurrentCollider { get; private set; }
        public IReadOnlyList<OverlapHit> Overlaps => _overlaps;

        public struct OverlapHit
        {
            public Collider Collider;
            public Interactable Interactable;
        }

#if UNITY_EDITOR
        static readonly Color OverlayIdleBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        static readonly Color OverlayActiveBg = new Color(0.15f, 0.85f, 0.28f, 1f);
        static readonly Color OverlayIdleText = new Color(0.75f, 0.75f, 0.75f, 1f);
        Rect _overlayRect = new Rect(12f, 360f, 280f, 150f);
        GUIStyle _overlayKeyStyle;
#endif

        void OnEnable()
        {
            Instance = this;
            InteractionManager.OnInteractStarted += HandleInteractStarted;
        }

        void OnDisable()
        {
            InteractionManager.OnInteractStarted -= HandleInteractStarted;
            if (Instance == this)
                Instance = null;

            _overlaps.Clear();
            Current = null;
            CurrentCollider = null;
        }

        void FixedUpdate()
        {
            RefreshDetection();
        }

        void LateUpdate()
        {
            RefreshDetection();
        }

        void HandleInteractStarted()
        {
            if (!AllowsWorldInteract)
                return;

            RefreshDetection();

            Interactable target = Current;
            if (target == null || !target.isActiveAndEnabled || target.IsRunning)
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
                return state == GameState.Gameplay ||
                       state == GameState.GameplayNoJump ||
                       state == GameState.GameplayDialogue;
            }
        }

        public void RefreshDetection()
        {
            Physics.SyncTransforms();

            _overlaps.Clear();
            _seenColliders.Clear();
            Current = null;
            CurrentCollider = null;

            Vector3 origin = DetectionOrigin;
            float radiusSq = radius * radius;

            int count = Physics.OverlapSphereNonAlloc(origin, radius, Hits, layerMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
                TryAddHit(Hits[i], origin, radiusSq);

            Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < interactables.Length; i++)
            {
                Interactable interactable = interactables[i];
                if (interactable == null || !interactable.isActiveAndEnabled)
                    continue;

                Collider[] colliders = interactable.GetComponentsInChildren<Collider>(false);
                for (int c = 0; c < colliders.Length; c++)
                    TryAddHit(colliders[c], origin, radiusSq);
            }

            float bestDist = float.MaxValue;
            for (int i = 0; i < _overlaps.Count; i++)
            {
                OverlapHit hit = _overlaps[i];
                if (hit.Collider == null)
                    continue;

                float dist = Vector3.SqrMagnitude(hit.Collider.ClosestPoint(origin) - origin);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    Current = hit.Interactable;
                    CurrentCollider = hit.Collider;
                }
            }
        }

        void TryAddHit(Collider col, Vector3 origin, float radiusSq)
        {
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                return;

            if (((1 << col.gameObject.layer) & layerMask) == 0)
                return;

            if (col.gameObject == gameObject)
                return;

            int id = col.GetInstanceID();
            if (!_seenColliders.Add(id))
                return;

            Vector3 closest = col.ClosestPoint(origin);
            if ((closest - origin).sqrMagnitude > radiusSq)
                return;

            Interactable interactable = ResolveInteractable(col);
            if (interactable == null || !interactable.isActiveAndEnabled)
                return;

            if (interactable.gameObject == gameObject)
                return;

            _overlaps.Add(new OverlapHit { Collider = col, Interactable = interactable });
        }

        static Interactable ResolveInteractable(Collider col)
        {
            Interactable interactable = col.GetComponent<Interactable>();
            if (interactable != null)
                return interactable;

            interactable = col.GetComponentInParent<Interactable>();
            if (interactable != null)
                return interactable;

            return col.GetComponentInChildren<Interactable>();
        }

        Vector3 DetectionOrigin => transform.TransformPoint(detectionOffset);

#if UNITY_EDITOR
        void OnGUI()
        {
            if (!showScreenOverlay && !EditorPrefs.GetBool(OverlayPrefsKey, false))
                return;

            _overlayRect = GUI.Window(GetInstanceID(), _overlayRect, DrawOverlay, "Interactor");
        }

        void DrawOverlay(int windowId)
        {
            bool touching = Current != null;
            DrawRow(touching ? Current.gameObject.name : "Nothing", touching, 250f);

            InteractionGraph graph = Current != null ? Current.Graph : null;
            DrawRow(graph != null ? graph.name : "No graph", graph != null, 250f);

            if (CurrentCollider != null)
                DrawRow(CurrentCollider.isTrigger ? "Trigger" : "Collider", true, 250f);

            DrawRow($"Hits  {_overlaps.Count}", _overlaps.Count > 0, 250f);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        GUIStyle OverlayKeyStyle
        {
            get
            {
                if (_overlayKeyStyle == null)
                {
                    _overlayKeyStyle = new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        fontSize = 12
                    };
                    _overlayKeyStyle.normal.background = Texture2D.whiteTexture;
                    _overlayKeyStyle.hover.background = Texture2D.whiteTexture;
                    _overlayKeyStyle.active.background = Texture2D.whiteTexture;
                }

                return _overlayKeyStyle;
            }
        }

        void DrawRow(string label, bool active, float width)
        {
            Color previousBg = GUI.backgroundColor;
            GUI.backgroundColor = active ? OverlayActiveBg : OverlayIdleBg;
            OverlayKeyStyle.normal.textColor = active ? Color.black : OverlayIdleText;
            OverlayKeyStyle.hover.textColor = OverlayKeyStyle.normal.textColor;
            OverlayKeyStyle.active.textColor = OverlayKeyStyle.normal.textColor;
            GUILayout.Box(label, OverlayKeyStyle, GUILayout.Width(width), GUILayout.Height(22f));
            GUI.backgroundColor = previousBg;
        }
#endif

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
