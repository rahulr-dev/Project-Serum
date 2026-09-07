using UnityEngine;

namespace InteractionSystem
{
    public class InteractionInteractable : MonoBehaviour
    {
        [SerializeField]
        private InteractionGraph interactionGraph;

        private void Awake()
        {
            if (interactionGraph == null)
            {
                interactionGraph = GetComponent<InteractionGraph>();
            }
        }

        public void Interact()
        {
            if (interactionGraph != null)
            {
                interactionGraph.Run();
            }
            else
            {
                Debug.LogWarning($"[InteractionInteractable] No InteractionGraph found on {gameObject.name}.");
            }
        }
    }
}