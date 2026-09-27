using System;
using UnityEngine;

namespace Serum.Audio
{
    /// <summary>
    /// Owns every layer of one audio state. All assigned sources fade together,
    /// allowing a state to contain music, ambience, and detail loops.
    /// </summary>
    public class AudioStateController : MonoBehaviour
    {
        [Serializable]
        public class SourceLayer
        {
            public AudioSource source;
            [Range(0f, 1f)] public float volume = 1f;

            [NonSerialized] public float currentVolume;
        }

        [SerializeField] GameAudioState state;
        [SerializeField, Min(0f)] float transitionDuration = 1.5f;
        [SerializeField] SourceLayer[] sourceLayers;

        bool isActive;
        bool initialized;

        public GameAudioState State => state;
        public bool IsActive => isActive;
        public float TransitionDuration => transitionDuration;

        void Awake()
        {
            Initialize();
        }

        void Update()
        {
            Initialize();

            float duration = Mathf.Max(transitionDuration, 0.0001f);
            float step = Time.unscaledDeltaTime / duration;

            foreach (SourceLayer layer in sourceLayers)
            {
                if (layer == null || layer.source == null)
                    continue;

                float targetVolume = isActive ? layer.volume : 0f;
                layer.currentVolume = Mathf.MoveTowards(layer.currentVolume, targetVolume, step);
                layer.source.volume = layer.currentVolume;

                if (!isActive && layer.currentVolume <= 0f && layer.source.isPlaying)
                    layer.source.Stop();
            }
        }

        public void SetActive(bool active, bool immediate = false)
        {
            Initialize();
            isActive = active;

            foreach (SourceLayer layer in sourceLayers)
            {
                if (layer == null || layer.source == null)
                    continue;

                if (active && !layer.source.isPlaying)
                    layer.source.Play();

                if (!immediate)
                    continue;

                layer.currentVolume = active ? layer.volume : 0f;
                layer.source.volume = layer.currentVolume;
                if (!active)
                    layer.source.Stop();
            }
        }

        void Initialize()
        {
            if (initialized)
                return;

            sourceLayers ??= Array.Empty<SourceLayer>();
            foreach (SourceLayer layer in sourceLayers)
            {
                if (layer == null || layer.source == null)
                    continue;

                layer.source.loop = true;
                layer.currentVolume = 0f;
                layer.source.volume = 0f;
                layer.source.playOnAwake = false;
            }

            initialized = true;
        }
    }
}
