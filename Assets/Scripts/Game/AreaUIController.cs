using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Displays the active area name and controls its assigned UI object.</summary>
    public sealed class AreaUIController : MonoBehaviour
    {
        [SerializeField] GameObject areaUI;
        [SerializeField] Text areaNameText;
        [SerializeField, Min(0f)] float displayDuration = 2f;

        Coroutine _displayRoutine;

        void Awake()
        {
            SetUIVisible(false);
        }

        void OnEnable()
        {
            AreaManager.OnAreaUIChanged += HandleAreaUIChanged;

            if (AreaManager.Instance != null)
                HandleAreaUIChanged(AreaManager.Instance.AreaName, AreaManager.Instance.IsAreaUIVisible);
        }

        void OnDisable()
        {
            AreaManager.OnAreaUIChanged -= HandleAreaUIChanged;

            if (_displayRoutine != null)
                StopCoroutine(_displayRoutine);

            _displayRoutine = null;
            SetUIVisible(false);
        }

        /// <summary>Shows the assigned UI object. Can be called directly from a UnityEvent.</summary>
        public void ShowUI() => SetUIVisible(true);

        /// <summary>Hides the assigned UI object. Can be called directly from a UnityEvent.</summary>
        public void HideUI() => SetUIVisible(false);

        public void SetUIVisible(bool visible)
        {
            if (areaUI != null)
                areaUI.SetActive(visible);
        }

        void HandleAreaUIChanged(string areaName, bool visible)
        {
            if (areaNameText != null)
                areaNameText.text = areaName;

            if (_displayRoutine != null)
                StopCoroutine(_displayRoutine);

            _displayRoutine = visible ? StartCoroutine(ShowForDuration(areaName)) : null;
            if (!visible)
                SetUIVisible(false);
        }

        IEnumerator ShowForDuration(string displayedAreaName)
        {
            SetUIVisible(true);
            yield return new WaitForSecondsRealtime(displayDuration);
            _displayRoutine = null;

            if (AreaManager.Instance != null && AreaManager.Instance.AreaName == displayedAreaName)
                AreaManager.Instance.HideAreaUI();
            else
                SetUIVisible(false);
        }
    }
}
