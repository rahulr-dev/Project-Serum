using System.Collections.Generic;
using UnityEngine;

namespace InteractionSystem
{
    public static class InteractionGraphRunner
    {
        public static void Run(InteractionSequenceSO sequence, SwitchEvent defaultSwitchEvent)
        {
            if (sequence == null)
            {
                Debug.LogWarning("[InteractionGraphRunner] InteractionSequenceSO is null.");
                return;
            }

            // 1. Find Start Node
            InteractionNode startNode = null;
            if (sequence.Nodes != null)
            {
                foreach (var node in sequence.Nodes)
                {
                    if (node != null && node.NodeType == InteractionNodeType.Start)
                    {
                        startNode = node;
                        break;
                    }
                }
            }

            if (startNode == null)
            {
                Debug.LogWarning("[InteractionGraphRunner] No Start node found in sequence.");
                return;
            }

            // 2. Traverse Graph
            HashSet<string> visited = new HashSet<string>();
            Queue<InteractionNode> queue = new Queue<InteractionNode>();
            queue.Enqueue(startNode);
            visited.Add(startNode.ID);

            while (queue.Count > 0)
            {
                InteractionNode current = queue.Dequeue();

                // Process Node
                switch (current.NodeType)
                {
                    case InteractionNodeType.Start:
                        // Start node begins flow
                        break;

                    case InteractionNodeType.SwitchEvent:
                        SwitchEvent targetEvent = (current.SwitchEvent != null) ? current.SwitchEvent : defaultSwitchEvent;
                        if (targetEvent != null)
                        {
                            targetEvent.Play();
                        }
                        else
                        {
                            Debug.LogWarning("[InteractionGraphRunner] SwitchEvent node encountered but no SwitchEvent component was found.");
                        }
                        break;

                    case InteractionNodeType.End:
                        // End node stops execution of this branch
                        continue;
                }

                // Follow outgoing edges
                List<InteractionEdge> outgoing = sequence.GetOutgoingEdges(current.ID);
                if (outgoing != null)
                {
                    for (int i = 0; i < outgoing.Count; i++)
                    {
                        string targetID = outgoing[i].ToNodeID;
                        if (!visited.Contains(targetID))
                        {
                            InteractionNode targetNode = sequence.GetNodeByID(targetID);
                            if (targetNode != null)
                            {
                                visited.Add(targetID);
                                queue.Enqueue(targetNode);
                            }
                        }
                    }
                }
            }
        }
    }
}
