using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Wind sway without reparenting: rotates this transform and assigned
/// siblings around a virtual hinge point (in parent-local space).
/// Safe to use on Prefab instances.
/// </summary>
public class WindSwayGroup : MonoBehaviour
{
    [Tooltip("Vector from this object's origin up to the hook/hinge, in parent space")]
    public Vector3 hingeOffset = new Vector3(0f, 0.55f, 0f);

    [Tooltip("Siblings that must swing together (Point Light, particles...)")]
    public Transform[] swingTogether;

    [Header("Wind")]
    public float maxAngle = 6f;
    public float gustSpeed = 0.7f;
    public float maxOffset = 0f;                 // optional extra positional sway
    public Vector3 axisMask = new Vector3(1f, 0f, 1f);

    struct Base { public Transform tr; public Vector3 pos; public Quaternion rot; }
    readonly List<Base> items = new List<Base>();
    Vector3 pivot;
    float seed;

    void Awake()
    {
        Capture(transform);
        foreach (var t in swingTogether)
        {
            if (t == null) continue;
            if (t.parent == transform.parent) Capture(t);
            else Debug.LogWarning($"WindSwayGroup: {t.name} is not a sibling of {name}, skipped.");
        }

        pivot = items[0].pos + hingeOffset;

        // Deterministic per-lamp seed: different lamps desync, parts of one lamp stay in sync
        Vector3 p = transform.parent != null ? transform.parent.position : transform.position;
        seed = Mathf.Repeat(Mathf.Abs(p.x * 12.9898f + p.z * 78.233f), 100f);
    }

    void Capture(Transform tr) =>
        items.Add(new Base { tr = tr, pos = tr.localPosition, rot = tr.localRotation });

    void Update()
    {
        float t = Time.time * gustSpeed;
        float nx = (Mathf.PerlinNoise(t + seed, 0f) * 2f - 1f) * axisMask.x;
        float nz = (Mathf.PerlinNoise(0f, t + seed + 137.5f) * 2f - 1f) * axisMask.z;

        Quaternion q = Quaternion.Euler(nx * maxAngle, 0f, nz * maxAngle);
        Vector3 off = new Vector3(nx, 0f, nz) * maxOffset;

        foreach (var b in items)
        {
            b.tr.localRotation = q * b.rot;
            b.tr.localPosition = pivot + q * (b.pos - pivot) + off;
        }
    }
}