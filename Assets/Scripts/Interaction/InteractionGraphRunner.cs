using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InteractionSystem
{
    public static class InteractionGraphRunner
    {
        const int MaxNodes = 128;

        public static IEnumerator Run(InteractionGraph graph)
        {
            if (graph == null)
            {
                Debug.LogWarning("[InteractionGraphRunner] Graph is null.");
                yield break;
            }

            InteractionNodeData start = graph.FindStart();
            if (start == null)
            {
                Debug.LogWarning("[InteractionGraphRunner] No Start node found.");
                yield break;
            }

            HashSet<string> visited = new HashSet<string>();
            string nextId = graph.FindNext(start.id, 0);
            int steps = 0;

            while (!string.IsNullOrEmpty(nextId) && steps++ < MaxNodes)
            {
                if (!visited.Add(nextId))
                {
                    Debug.LogWarning("[InteractionGraphRunner] Cycle detected; stopping graph.");
                    yield break;
                }

                InteractionNodeData node = graph.FindNode(nextId);
                if (node == null)
                    yield break;

                switch (node.kind)
                {
                    case InteractionNodeKind.Wait:
                        float duration = Mathf.Max(0f, node.duration);
                        float elapsed = 0f;
                        while (elapsed < duration)
                        {
                            elapsed += Time.unscaledDeltaTime;
                            yield return null;
                        }

                        nextId = graph.FindNext(node.id, 0);
                        break;

                    case InteractionNodeKind.Action:
                        InteractionActionExecutor.Execute(node);
                        nextId = graph.FindNext(node.id, 0);
                        break;

                    case InteractionNodeKind.End:
                        yield break;

                    case InteractionNodeKind.Start:
                        nextId = graph.FindNext(node.id, 0);
                        break;

                    default:
                        nextId = graph.FindNext(node.id, 0);
                        break;
                }
            }
        }
    }
}
