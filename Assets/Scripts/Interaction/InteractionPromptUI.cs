using UnityEngine;

namespace InteractionSystem
{
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] GameObject promptPanel;

        void Awake()
        {
            SetPromptVisible(false);
        }

        void Start()
        {
            BindInteractor();
            SyncFromInteractor();
        }

        void OnEnable()
        {
            BindInteractor();
            SyncFromInteractor();
        }

        void OnDisable()
        {
            UnbindInteractor();
            SetPromptVisible(false);
        }

        void BindInteractor()
        {
            UnbindInteractor();

            if (interactor == null)
                interactor = PlayerInteractor.Instance;

            if (interactor == null)
                return;

            interactor.OnCurrentChanged += HandleCurrentChanged;
        }

        void UnbindInteractor()
        {
            if (interactor == null)
                return;

            interactor.OnCurrentChanged -= HandleCurrentChanged;
        }

        void SyncFromInteractor()
        {
            if (interactor == null)
                interactor = PlayerInteractor.Instance;

            HandleCurrentChanged(interactor != null ? interactor.Current : null);
        }

        void HandleCurrentChanged(Interactable current)
        {
            SetPromptVisible(current != null);
        }

        void SetPromptVisible(bool visible)
        {
            if (promptPanel != null)
                promptPanel.SetActive(visible);
        }
    }
}
