using UnityEngine;

namespace InteractionSystem
{
    public class InteractionGraph : MonoBehaviour
    {
        [SerializeField]
        private InteractionSequenceSO sequence;

        [SerializeField]
        private SwitchEvent switchEvent;

        public InteractionSequenceSO Sequence
        {
            get => sequence;
            set => sequence = value;
        }

        public SwitchEvent SwitchEvent
        {
            get => switchEvent;
            set => switchEvent = value;
        }

        private void Awake()
        {
            if (switchEvent == null)
            {
                switchEvent = GetComponent<SwitchEvent>();
            }
        }

        public void Run()
        {
            if (sequence == null)
            {
                Debug.LogWarning($"[InteractionGraph] No InteractionSequenceSO assigned to {gameObject.name}.");
                return;
            }

            if (switchEvent == null)
            {
                switchEvent = GetComponent<SwitchEvent>();
            }

            InteractionGraphRunner.Run(sequence, switchEvent);
        }
    }
}
