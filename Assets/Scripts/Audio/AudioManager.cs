using System.Collections.Generic;
using UnityEngine;

namespace Serum.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField, Min(1)] int initialPoolSize = 12;
        readonly List<AudioEmitter> emitters = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            for (int i = 0; i < initialPoolSize; i++)
                CreateEmitter();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void Play(SoundEvent soundEvent, Vector3 position, Transform followTarget = null)
        {
            if (soundEvent == null)
                return;

            if (Instance == null)
            {
                GameObject manager = new("Audio Manager");
                Instance = manager.AddComponent<AudioManager>();
            }

            Instance.GetAvailableEmitter().Play(soundEvent, position, followTarget);
        }

        AudioEmitter GetAvailableEmitter()
        {
            foreach (AudioEmitter emitter in emitters)
                if (!emitter.IsPlaying)
                    return emitter;

            return CreateEmitter();
        }

        AudioEmitter CreateEmitter()
        {
            GameObject emitterObject = new("Pooled Audio Emitter");
            emitterObject.transform.SetParent(transform);
            AudioEmitter emitter = emitterObject.AddComponent<AudioEmitter>();
            emitters.Add(emitter);
            return emitter;
        }
    }
}
