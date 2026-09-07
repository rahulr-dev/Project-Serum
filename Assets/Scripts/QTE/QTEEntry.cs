using UnityEngine;

namespace QTE
{
    public abstract class QTEEntry : MonoBehaviour
    {
        [SerializeField] protected QTEGraph graph;

        public QTEGraph Graph => graph;

        public virtual void Trigger()
        {
            if (graph == null)
                return;

            QTEManager.Instance?.StartQTE(graph);
        }
    }
}
