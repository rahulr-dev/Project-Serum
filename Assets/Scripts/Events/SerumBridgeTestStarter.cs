using QTE;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Events
{
    public class SerumBridgeTestStarter : MonoBehaviour
    {
        [SerializeField] QTEEntry qteEntry;
        [SerializeField] Key startKey = Key.T;

        void Update()
        {
            if (qteEntry == null || Keyboard.current == null)
                return;

            if (Keyboard.current[startKey].wasPressedThisFrame)
                qteEntry.Trigger();
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (qteEntry == null)
                return;

            GUI.Label(new Rect(12f, Screen.height - 28f, 420f, 22f),
                $"Press [{startKey}] to start QTE: {qteEntry.Graph?.name ?? "unassigned"}");
        }
#endif
    }
}
