using System;
using System.Collections.Generic;
using UnityEngine;

namespace Serum.Audio
{
    /// <summary>Switches between ambient audio states using simultaneous crossfades.</summary>
    public class GameAudioStateManager : MonoBehaviour
    {
        public static GameAudioStateManager Instance { get; private set; }
        public static event Action<GameAudioState, GameAudioState> OnStateChanged;

        [SerializeField] GameAudioState initialState = GameAudioState.Forest;
        [SerializeField] AudioStateController[] stateControllers;

        readonly Dictionary<GameAudioState, AudioStateController> controllers = new();

        public GameAudioState CurrentState { get; private set; }
        public GameAudioState PreviousState { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CacheControllers();
            CurrentState = initialState;
            PreviousState = initialState;
        }

        void Start()
        {
            ApplyState(CurrentState, true);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetState(GameAudioState state)
        {
            TrySetState(state);
        }

        public bool TrySetState(GameAudioState state)
        {
            if (CurrentState == state)
                return false;

            PreviousState = CurrentState;
            CurrentState = state;
            ApplyState(state, false);
            OnStateChanged?.Invoke(PreviousState, CurrentState);
            return true;
        }

        public bool IsState(GameAudioState state) => CurrentState == state;
        public void EnterForest() => SetState(GameAudioState.Forest);
        public void EnterIndoor() => SetState(GameAudioState.Indoor);
        public void EnterBattle() => SetState(GameAudioState.Battle);

        void CacheControllers()
        {
            controllers.Clear();
            if (stateControllers == null || stateControllers.Length == 0)
                stateControllers = GetComponentsInChildren<AudioStateController>(true);

            foreach (AudioStateController controller in stateControllers)
            {
                if (controller == null)
                    continue;

                if (controllers.ContainsKey(controller.State))
                {
                    Debug.LogWarning($"More than one AudioStateController is assigned to {controller.State}. Only the first will be used.", controller);
                    continue;
                }

                controllers.Add(controller.State, controller);
            }
        }

        void ApplyState(GameAudioState state, bool immediate)
        {
            foreach (KeyValuePair<GameAudioState, AudioStateController> entry in controllers)
                entry.Value.SetActive(entry.Key == state, immediate);
        }
    }
}
