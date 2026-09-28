using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Smoothly fades renderers that sit between the gameplay camera and the player.
/// Add this component to the gameplay camera (or any persistent scene object), assign
/// the player, and put the walls that may obscure the player on the Occluders layer.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraOcclusionHider : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera gameplayCamera;

    [Header("Detection")]
    [Tooltip("Only colliders on these layers can fade. Keep the player off these layers.")]
    [SerializeField] private LayerMask occluderLayers = ~0;
    [Tooltip("A small radius makes thin walls reliably register as obstructions.")]
    [SerializeField, Min(0f)] private float castRadius = 0.15f;
    [SerializeField, Min(0f)] private float playerHeightOffset = 1f;
    [Header("Fade")]
    [Tooltip("Seconds required to fade a wall fully in or out.")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;
    [SerializeField, Range(0f, 1f)] private float hiddenOpacity = 0f;

    private readonly HashSet<Renderer> nextHiddenRenderers = new();
    private readonly Dictionary<Renderer, FadeState> fadingRenderers = new();
    private readonly RaycastHit[] castResults = new RaycastHit[64];

    private void Awake()
    {
        FindMissingReferences();
    }

    private void LateUpdate()
    {
        FindMissingReferences();
        if (player == null || gameplayCamera == null)
        {
            RestoreAllImmediately();
            return;
        }

        UpdateOccluders();
    }

    private void OnDisable()
    {
        RestoreAllImmediately();
    }

    private void FindMissingReferences()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    private void UpdateOccluders()
    {
        nextHiddenRenderers.Clear();

        Vector3 target = player.position + Vector3.up * playerHeightOffset;
        Vector3 origin = gameplayCamera.transform.position;
        Vector3 toTarget = target - origin;
        float distance = toTarget.magnitude;
        if (distance > Mathf.Epsilon)
        {
            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                castRadius,
                toTarget / distance,
                castResults,
                distance,
                occluderLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = castResults[i].collider;
                if (hitCollider == null || hitCollider.transform.IsChildOf(player))
                    continue;

                Renderer renderer = FindOwningRenderer(hitCollider.transform);
                if (renderer != null)
                    nextHiddenRenderers.Add(renderer);
            }
        }

        foreach (KeyValuePair<Renderer, FadeState> pair in fadingRenderers)
        {
            if (pair.Key != null)
                pair.Value.targetOpacity = nextHiddenRenderers.Contains(pair.Key) ? hiddenOpacity : 1f;
        }

        foreach (Renderer renderer in nextHiddenRenderers)
        {
            if (renderer != null && !fadingRenderers.ContainsKey(renderer))
                fadingRenderers.Add(renderer, new FadeState(renderer, hiddenOpacity));
        }

        UpdateFades();
    }

    private void UpdateFades()
    {
        List<Renderer> completedRestores = null;
        foreach (KeyValuePair<Renderer, FadeState> pair in fadingRenderers)
        {
            Renderer renderer = pair.Key;
            FadeState state = pair.Value;
            if (renderer == null)
            {
                (completedRestores ??= new List<Renderer>()).Add(renderer);
                continue;
            }

            state.opacity = Mathf.MoveTowards(
                state.opacity,
                state.targetOpacity,
                Time.deltaTime / fadeDuration);
            state.ApplyOpacity();

            if (state.targetOpacity >= 1f && Mathf.Approximately(state.opacity, 1f))
            {
                state.RestoreOriginalMaterials();
                (completedRestores ??= new List<Renderer>()).Add(renderer);
            }
        }

        if (completedRestores == null)
            return;

        foreach (Renderer renderer in completedRestores)
            fadingRenderers.Remove(renderer);
    }

    private static Renderer FindOwningRenderer(Transform colliderTransform)
    {
        Transform current = colliderTransform;
        while (current != null)
        {
            Renderer renderer = current.GetComponent<Renderer>();
            if (renderer != null)
                return renderer;

            current = current.parent;
        }

        return null;
    }

    private void RestoreAllImmediately()
    {
        foreach (FadeState state in fadingRenderers.Values)
            state.RestoreOriginalMaterials();

        fadingRenderers.Clear();
    }

    private sealed class FadeState
    {
        private readonly Renderer renderer;
        private readonly Material[] originalMaterials;
        private readonly Material[] fadeMaterials;
        private readonly Color[] originalColors;

        public float opacity = 1f;
        public float targetOpacity;

        public FadeState(Renderer targetRenderer, float initialTargetOpacity)
        {
            renderer = targetRenderer;
            originalMaterials = renderer.sharedMaterials;
            fadeMaterials = new Material[originalMaterials.Length];
            originalColors = new Color[originalMaterials.Length];
            targetOpacity = initialTargetOpacity;

            for (int i = 0; i < originalMaterials.Length; i++)
            {
                Material original = originalMaterials[i];
                if (original == null)
                    continue;

                Material fadeMaterial = new Material(original) { name = original.name + " (Occlusion Fade)" };
                ConfigureTransparent(fadeMaterial);
                fadeMaterials[i] = fadeMaterial;
                originalColors[i] = GetMainColor(fadeMaterial);
            }

            renderer.sharedMaterials = fadeMaterials;
        }

        public void ApplyOpacity()
        {
            for (int i = 0; i < fadeMaterials.Length; i++)
            {
                Material material = fadeMaterials[i];
                if (material == null)
                    continue;

                Color color = originalColors[i];
                color.a *= opacity;
                SetMainColor(material, color);
            }
        }

        public void RestoreOriginalMaterials()
        {
            if (renderer != null)
                renderer.sharedMaterials = originalMaterials;

            foreach (Material material in fadeMaterials)
            {
                if (material != null)
                    Object.Destroy(material);
            }
        }

        private static void ConfigureTransparent(Material material)
        {
            // These are the URP Lit/Unlit properties. Setting only properties that
            // exist keeps this safe for custom shaders too.
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static Color GetMainColor(Material material)
        {
            if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color")) return material.GetColor("_Color");
            return Color.white;
        }

        private static void SetMainColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }
    }
}
