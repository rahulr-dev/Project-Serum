using UnityEngine;

namespace InteractionSystem
{
    public class InteractionGraph : MonoBehaviour
    {
        [SerializeField]
        private InteractionSequenceSO sequence;

        [SerializeField]
        private InvokeEvent invokeEvent;

        public InteractionSequenceSO Sequence
        {
            get => sequence;
            set => sequence = value;
        }

        public InvokeEvent InvokeEvent
        {
            get => invokeEvent;
            set => invokeEvent = value;
        }

        public SwitchEvent SwitchEvent
        {
            get => invokeEvent as SwitchEvent;
            set => invokeEvent = value;
        }

        private void Awake()
        {
            if (invokeEvent == null)
            {
                invokeEvent = GetComponent<InvokeEvent>();
            }
        }

        public void Run()
        {
            if (sequence == null)
            {
                Debug.LogWarning($"[InteractionGraph] No InteractionSequenceSO assigned to {gameObject.name}.");
                return;
            }

            if (invokeEvent == null)
            {
                invokeEvent = GetComponent<InvokeEvent>();
            }

            InteractionGraphRunner.Run(sequence, invokeEvent, this);
        }
    }
}
