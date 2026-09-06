using Dialogue;
using Interaction;
using UnityEngine;

namespace QTE
{
    public class QTEInteractionEntry : QTEEntry
    {
        [SerializeField] string playerTag = "Player";
        bool _playerInRange;

        void OnEnable()
        {
            InteractionManager.OnInteractStarted += HandleInteract;
        }

        void OnDisable()
        {
            InteractionManager.OnInteractStarted -= HandleInteract;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
                _playerInRange = true;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag))
                _playerInRange = false;
        }

        void HandleInteract()
        {
            if (!_playerInRange)
                return;

            if (QTEManager.Instance != null && QTEManager.Instance.IsRunning)
                return;

            if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying)
                return;

            Trigger();
        }
    }
}
