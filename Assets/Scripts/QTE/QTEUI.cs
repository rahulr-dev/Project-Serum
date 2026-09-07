using UnityEngine;
using UnityEngine.UI;

namespace QTE
{
    public class QTEUI : MonoBehaviour
    {
        [SerializeField] QTEManager manager;
        [SerializeField] GameObject rootPanel;
        [SerializeField] Text promptText;
        [SerializeField] Text inputHintText;
        [SerializeField] Image timerFill;
        [SerializeField] Image progressFill;
        [SerializeField] Text sequenceText;
        [SerializeField] bool hideOnEnd = true;

        void Awake()
        {
            ResolveReferences();
            SetRootVisible(false);
        }

        void ResolveReferences()
        {
            Transform panel = transform.Find("QTE_Canvas/QTE_Panel");
            if (panel == null)
                panel = transform.Find("QTE_Panel");

            if (rootPanel == null && panel != null)
                rootPanel = panel.gameObject;

            if (panel == null)
                return;

            promptText ??= panel.Find("Prompt_Text")?.GetComponent<Text>();
            inputHintText ??= panel.Find("InputHint_Text")?.GetComponent<Text>();
            sequenceText ??= panel.Find("Sequence_Text")?.GetComponent<Text>();
            timerFill ??= panel.Find("Timer_Bar/Timer_Fill")?.GetComponent<Image>();
            progressFill ??= panel.Find("Progress_Bar/Progress_Fill")?.GetComponent<Image>();
        }

        void Start()
        {
            BindManager();
        }

        void OnEnable()
        {
            BindManager();
        }

        void BindManager()
        {
            UnbindManager();
            if (manager == null)
                manager = QTEManager.Instance;
            if (manager == null)
                return;

            manager.OnQTEStarted += HandleStarted;
            manager.OnPromptUpdated += HandlePromptUpdated;
            manager.OnProgressUpdated += HandleProgressUpdated;
            manager.OnSequenceStepUpdated += HandleSequenceStepUpdated;
            manager.OnRequiredInputUpdated += HandleRequiredInputUpdated;
            QTEManager.OnQTECompleted += HandleCompletedStatic;
        }

        void UnbindManager()
        {
            if (manager == null)
                return;

            manager.OnQTEStarted -= HandleStarted;
            manager.OnPromptUpdated -= HandlePromptUpdated;
            manager.OnProgressUpdated -= HandleProgressUpdated;
            manager.OnSequenceStepUpdated -= HandleSequenceStepUpdated;
            manager.OnRequiredInputUpdated -= HandleRequiredInputUpdated;
            QTEManager.OnQTECompleted -= HandleCompletedStatic;
        }

        void OnDisable()
        {
            UnbindManager();
        }

        void HandleStarted(QTEGraph graph)
        {
            SetRootVisible(true);
            SetText(promptText, "");
            SetText(inputHintText, "");
            SetFill(timerFill, 0f);
            SetFill(progressFill, 0f);
            SetText(sequenceText, "");
        }

        void HandlePromptUpdated(string prompt, float normalizedTime)
        {
            SetText(promptText, prompt);
            SetFill(timerFill, normalizedTime);
        }

        void HandleProgressUpdated(float progress)
        {
            SetFill(progressFill, progress);
        }

        void HandleSequenceStepUpdated(int current, int total)
        {
            if (sequenceText != null)
                sequenceText.text = total > 0 ? $"{current}/{total}" : "";
        }

        void HandleRequiredInputUpdated(QTEInputKind? required, QTEInputKind? sequenceExpected)
        {
            QTEInputKind? kind = sequenceExpected ?? required;
            if (!kind.HasValue)
            {
                SetText(inputHintText, "");
                return;
            }

            SetText(inputHintText, QTEInputBindings.GetPromptHint(kind.Value));
        }

        void HandleCompletedStatic(QTEOutcome outcome)
        {
            if (hideOnEnd)
                SetRootVisible(false);
        }

        void SetRootVisible(bool visible)
        {
            if (rootPanel != null)
                rootPanel.SetActive(visible);
        }

        static void SetText(Text target, string value)
        {
            if (target != null)
                target.text = value ?? "";
        }

        static void SetFill(Image target, float normalized)
        {
            if (target != null)
                target.fillAmount = Mathf.Clamp01(normalized);
        }
    }
}
