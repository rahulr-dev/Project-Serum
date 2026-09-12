using UnityEngine;

namespace NpcAi
{
    public class NpcStateMachineBridge : MonoBehaviour
    {
        [SerializeField] NpcStateMachine machine;
        [SerializeField] string interruptNodeId = "";

        public NpcStateMachine Machine => machine;
        public string InterruptNodeId => interruptNodeId;

        void Reset()
        {
            machine = GetComponent<NpcStateMachine>();
        }

        void Awake()
        {
            if (machine == null)
                machine = GetComponent<NpcStateMachine>();
        }

        public void InterruptIfCurrent()
        {
            if (machine == null)
                machine = GetComponent<NpcStateMachine>();

            machine?.TryInterrupt(interruptNodeId);
        }

        public void InterruptIfCurrent(string nodeIdOrName)
        {
            if (machine == null)
                machine = GetComponent<NpcStateMachine>();

            machine?.TryInterrupt(nodeIdOrName);
        }

        public void InterruptCurrent()
        {
            if (machine == null)
                machine = GetComponent<NpcStateMachine>();

            if (machine == null)
                return;

            NpcStateMachineNodeData node = machine.CurrentNode;
            if (node == null)
                return;

            machine.TryInterrupt(node.id);
        }

        public void Bind(NpcStateMachine stateMachine, string nodeId)
        {
            machine = stateMachine;
            interruptNodeId = nodeId ?? "";
        }
    }
}
