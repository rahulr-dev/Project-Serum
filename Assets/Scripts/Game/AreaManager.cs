using System;
using UnityEngine;

namespace Game
{
    /// <summary>Coordinates the visibility and name of the current area's UI.</summary>
    public sealed class AreaManager : MonoBehaviour
    {
        public static AreaManager Instance { get; private set; }

        /// <summary>Raised when the area UI changes, with its display name and visibility.</summary>
        public static event Action<string, bool> OnAreaUIChanged;

        [SerializeField] string areaName;

        public string AreaName => areaName;
        public bool IsAreaUIVisible { get; private set; }

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

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Shows the UI using the Area Name configured in the Inspector.</summary>
        public void ShowAreaUI() => SetAreaUIVisible(areaName, true);

        /// <summary>Shows the UI and sends this display name to the UI controller.</summary>
        public void ShowAreaUI(string newAreaName) => SetAreaUIVisible(newAreaName, true);

        /// <summary>Hides the UI.</summary>
        public void HideAreaUI() => SetAreaUIVisible(areaName, false);

        /// <summary>Switches the assigned area UI between visible and hidden.</summary>
        public void ToggleAreaUI() => SetAreaUIVisible(areaName, !IsAreaUIVisible);

        public void SetAreaName(string newAreaName)
        {
            areaName = newAreaName ?? string.Empty;
        }

        public void SetAreaUIVisible(bool visible) => SetAreaUIVisible(areaName, visible);

        public void SetAreaUIVisible(string newAreaName, bool visible)
        {
            areaName = newAreaName ?? string.Empty;
            IsAreaUIVisible = visible;
            OnAreaUIChanged?.Invoke(areaName, visible);
        }
    }
}
