using UnityEngine;

namespace QTE
{
    public class QTETriggerEntry : QTEEntry
    {
        [SerializeField] bool autoStart;
        [SerializeField] string playerTag = "Player";

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            if (autoStart)
                Trigger();
        }
    }
}
