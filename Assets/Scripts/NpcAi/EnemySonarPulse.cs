using UnityEngine;

namespace NpcAi
{
    /// <summary>
    /// Displays a lightweight expanding shell using the radius of a SphereCollider.
    /// The visual has no collider and does not affect enemy detection.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemySonarPulse : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] SphereCollider rangeCollider;
        [Tooltip("Optional material using the Serum/Sonar Pulse shader. A runtime material is created if omitted.")]
        [SerializeField] Material pulseMaterial;

        [Header("Pulse")]
        [SerializeField, Min(0.05f)] float interval = 2f;
        [SerializeField, Min(0.05f)] float duration = 1.25f;
        [SerializeField, Range(0f, 1f)] float startRadiusPercent = 0.06f;
        [Tooltip("Normalized point in the pulse at which the shell starts fading out.")]
        [SerializeField, Range(0f, 0.95f)] float fadeOutStart = 0.55f;
        [SerializeField] Color color = new Color(0.2f, 0.85f, 1f, 1f);
        [SerializeField, Range(0f, 5f)] float intensity = 1.5f;
        [SerializeField] bool playOnEnable = true;

        MeshRenderer _renderer;
        Material _runtimeMaterial;
        float _startedAt;
        float _nextPulseAt;
        bool _isPulsing;

        void Reset()
        {
            rangeCollider = GetComponent<SphereCollider>();
        }

        void Awake()
        {
            if (rangeCollider == null)
                rangeCollider = GetComponent<SphereCollider>();

            CreateVisual();
        }

        void OnEnable()
        {
            _nextPulseAt = Time.time;
            _isPulsing = false;
        }

        void OnDestroy()
        {
            if (_runtimeMaterial != null)
                Destroy(_runtimeMaterial);
        }

        void Update()
        {
            if (rangeCollider == null || _renderer == null)
                return;

            if (!_isPulsing && playOnEnable && Time.time >= _nextPulseAt)
                PlayPulse();

            if (!_isPulsing)
                return;

            float progress = Mathf.Clamp01((Time.time - _startedAt) / duration);
            UpdateVisual(progress);

            if (progress >= 1f)
            {
                _isPulsing = false;
                _renderer.enabled = false;
                _nextPulseAt = Time.time + interval;
            }
        }

        /// <summary>Starts one sonar wave. Useful from an Animator event or enemy-alert script.</summary>
        public void PlayPulse()
        {
            if (rangeCollider == null || _renderer == null)
                return;

            _startedAt = Time.time;
            _isPulsing = true;
            _renderer.enabled = true;
            UpdateVisual(0f);
        }

        void CreateVisual()
        {
            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "Sonar Pulse Visual";
            shell.transform.SetParent(transform, false);

            Collider shellCollider = shell.GetComponent<Collider>();
            if (shellCollider != null)
            {
                shellCollider.enabled = false;
                Destroy(shellCollider);
            }

            _renderer = shell.GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("Serum/Sonar Pulse");
            if (pulseMaterial != null)
                _runtimeMaterial = new Material(pulseMaterial);
            else if (shader != null)
                _runtimeMaterial = new Material(shader);

            if (_runtimeMaterial != null)
            {
                _runtimeMaterial.SetColor("_PulseColor", color);
                _runtimeMaterial.SetFloat("_Intensity", intensity);
                _renderer.sharedMaterial = _runtimeMaterial;
            }
            else
            {
                Debug.LogWarning("EnemySonarPulse needs a material or the Serum/Sonar Pulse shader.", this);
            }

            _renderer.enabled = false;
        }

        void UpdateVisual(float progress)
        {
            float radius = Mathf.Max(0.01f, rangeCollider.radius);
            float scale = Mathf.Lerp(startRadiusPercent, 1f, progress) * radius * 2f;
            Transform visual = _renderer.transform;
            visual.localPosition = rangeCollider.center;
            visual.localScale = Vector3.one * scale;

            if (_runtimeMaterial != null)
            {
                _runtimeMaterial.SetColor("_PulseColor", color);
                _runtimeMaterial.SetFloat("_Intensity", intensity);
                // SmoothStep gives the end of the pulse a gentle fade rather than a visible pop.
                float fade = 1f - Mathf.SmoothStep(fadeOutStart, 1f, progress);
                _runtimeMaterial.SetFloat("_Fade", fade);
            }
        }
    }
}
