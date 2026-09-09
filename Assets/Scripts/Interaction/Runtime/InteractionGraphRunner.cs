using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InteractionSystem
{
    public static class InteractionGraphRunner
    {
        public static void Run(InteractionSequenceSO sequence, InvokeEvent defaultInvokeEvent, MonoBehaviour coroutineRunner)
        {
            if (sequence == null)
            {
                Debug.LogWarning("[InteractionGraphRunner] InteractionSequenceSO is null.");
                return;
            }

            if (coroutineRunner != null)
            {
                coroutineRunner.StartCoroutine(RunSequenceCoroutine(sequence, defaultInvokeEvent));
            }
            else
            {
                Debug.LogWarning("[InteractionGraphRunner] No coroutine runner provided. Cannot execute WAIT nodes properly.");
            }
        }

        public static IEnumerator RunSequenceCoroutine(InteractionSequenceSO sequence, InvokeEvent defaultInvokeEvent)
        {
            if (sequence == null) yield break;

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
                yield break;
            }

            // 2. Traverse Graph sequentially
            HashSet<string> visited = new HashSet<string>();
            Queue<InteractionNode> queue = new Queue<InteractionNode>();
            queue.Enqueue(startNode);
            visited.Add(startNode.ID);

            while (queue.Count > 0)
            {
                InteractionNode current = queue.Dequeue();
                if (current == null) continue;

                // Process Node
                switch (current.NodeType)
                {
                    case InteractionNodeType.Start:
                        break;

                    case InteractionNodeType.InvokeEvent:
                        // Execute node actions defined in graph
                        if (current.Actions != null)
                        {
                            for (int i = 0; i < current.Actions.Count; i++)
                            {
                                if (current.Actions[i] != null)
                                {
                                    current.Actions[i].Execute();
                                }
                            }
                        }

                        // Execute assigned InvokeEvent component if present
                        InvokeEvent targetEvent = (current.InvokeEvent != null) ? current.InvokeEvent : defaultInvokeEvent;
                        if (targetEvent != null)
                        {
                            targetEvent.Play();
                        }
                        break;

                    case InteractionNodeType.Wait:
                        if (current.WaitDuration > 0f)
                        {
                            yield return new WaitForSeconds(current.WaitDuration);
                        }
                        break;

                    case InteractionNodeType.End:
                        yield break;
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
