using System;
using System.Collections;
using UnityEngine;

namespace Game
{
    public class TransitionManager : MonoBehaviour
    {
        public static TransitionManager Instance { get; private set; }

        public static event Action OnTransitionInStarted;
        public static event Action OnTransitionInCompleted;
        public static event Action OnTransitionOutStarted;
        public static event Action OnTransitionOutCompleted;

        [SerializeField] float transitionDelay = 0.5f;

        Coroutine _running;

        public float TransitionDelay
        {
            get => transitionDelay;
            set => transitionDelay = Mathf.Max(0f, value);
        }

        public bool IsTransitioning => _running != null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            SerumSceneManager.OnSceneChangeRequested += HandleSceneChangeRequested;
        }

        void OnDisable()
        {
            SerumSceneManager.OnSceneChangeRequested -= HandleSceneChangeRequested;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void HandleSceneChangeRequested()
        {
            PlayIn();
        }

        public void PlayIn()
        {
            StartTransition(inTransition: true);
        }

        public void PlayOut()
        {
            StartTransition(inTransition: false);
        }

        void StartTransition(bool inTransition)
        {
            if (_running != null)
                StopCoroutine(_running);

            _running = StartCoroutine(RunTransition(inTransition));
        }

        IEnumerator RunTransition(bool inTransition)
        {
            if (inTransition)
                OnTransitionInStarted?.Invoke();
            else
                OnTransitionOutStarted?.Invoke();

            float delay = Mathf.Max(0f, transitionDelay);
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            if (inTransition)
            {
                OnTransitionInCompleted?.Invoke();
                if (SerumSceneManager.Instance != null)
                    SerumSceneManager.Instance.NotifyTransitionComplete();
            }
            else
            {
                OnTransitionOutCompleted?.Invoke();
            }

            _running = null;
        }
    }
}
