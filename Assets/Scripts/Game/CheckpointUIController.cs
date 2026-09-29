using System.Collections;
using UnityEngine;

namespace Game
{
    /// <summary>Shows a checkpoint UI object for a short time whenever progress is saved.</summary>
    public sealed class CheckpointUIController : MonoBehaviour
    {
        [SerializeField] GameObject checkpointUI;
        [SerializeField, Min(0f)] float displayDuration = 2f;

        Coroutine _displayRoutine;

        void Awake()
        {
            SetUIVisible(false);
        }

        void OnEnable()
        {
            GameProgressManager.OnLocationSaved += HandleLocationSaved;
        }

        void OnDisable()
        {
            GameProgressManager.OnLocationSaved -= HandleLocationSaved;
            if (_displayRoutine != null)
                StopCoroutine(_displayRoutine);

            _displayRoutine = null;
            SetUIVisible(false);
        }

        void HandleLocationSaved()
        {
            if (_displayRoutine != null)
                StopCoroutine(_displayRoutine);

            _displayRoutine = StartCoroutine(ShowForDuration());
        }

        IEnumerator ShowForDuration()
        {
            SetUIVisible(true);
            yield return new WaitForSecondsRealtime(displayDuration);
            SetUIVisible(false);
            _displayRoutine = null;
        }

        void SetUIVisible(bool visible)
        {
            if (checkpointUI != null)
                checkpointUI.SetActive(visible);
        }
    }
}
