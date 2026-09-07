using UnityEngine;
using UnityEngine.Events;

namespace QTE
{
    public class QTEExit : MonoBehaviour
    {
        [SerializeField] UnityEvent onSuccess;
        [SerializeField] UnityEvent onFailure;
        [SerializeField] UnityEvent onTimedOut;
        [SerializeField] UnityEvent onCancelled;

        void OnEnable()
        {
            QTEManager.OnQTECompleted += HandleCompleted;
        }

        void OnDisable()
        {
            QTEManager.OnQTECompleted -= HandleCompleted;
        }

        void HandleCompleted(QTEOutcome outcome)
        {
            switch (outcome)
            {
                case QTEOutcome.Success:
                    onSuccess?.Invoke();
                    break;
                case QTEOutcome.Failure:
                    onFailure?.Invoke();
                    break;
                case QTEOutcome.TimedOut:
                    onTimedOut?.Invoke();
                    break;
                case QTEOutcome.Cancelled:
                    onCancelled?.Invoke();
                    break;
            }
        }
    }
}
