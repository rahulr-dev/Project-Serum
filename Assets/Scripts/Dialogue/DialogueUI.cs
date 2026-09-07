using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dialogue
{
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] DialogueManager manager;
        [SerializeField] GameObject rootPanel;
        [SerializeField] GameObject linePanel;
        [SerializeField] GameObject choicesPanel;
        [SerializeField] Text speakerText;
        [SerializeField] Text bodyText;
        [SerializeField] Transform choicesRoot;
        [SerializeField] DialogueChoiceItem choicePrefab;
        [SerializeField] bool hideOnEnd = true;

        readonly List<DialogueChoiceItem> _spawned = new List<DialogueChoiceItem>();

        void Awake()
        {
            SetRootVisible(false);
            SetChoicesVisible(false);
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
                manager = DialogueManager.Instance;
            if (manager == null)
                return;

            manager.OnDialogueStarted += HandleStarted;
            manager.OnLineStarted += HandleLineStarted;
            manager.OnLineTextUpdated += HandleLineTextUpdated;
            manager.OnChoicesPresented += HandleChoicesPresented;
            manager.OnChoiceIndexChanged += HandleChoiceIndexChanged;
            manager.OnDialogueEnded += HandleEnded;
        }

        void UnbindManager()
        {
            if (manager == null)
                return;

            manager.OnDialogueStarted -= HandleStarted;
            manager.OnLineStarted -= HandleLineStarted;
            manager.OnLineTextUpdated -= HandleLineTextUpdated;
            manager.OnChoicesPresented -= HandleChoicesPresented;
            manager.OnChoiceIndexChanged -= HandleChoiceIndexChanged;
            manager.OnDialogueEnded -= HandleEnded;
        }

        void OnDisable()
        {
            UnbindManager();
        }

        public void ClickChoice(int index)
        {
            if (manager != null)
                manager.SelectChoice(index);
        }

        void HandleStarted(DialogueGraph graph)
        {
            SetRootVisible(true);
            if (linePanel != null)
                linePanel.SetActive(true);
            SetChoicesVisible(false);
            ClearChoices();
        }

        void HandleLineStarted(DialogueLineInfo info)
        {
            if (linePanel != null)
                linePanel.SetActive(true);
            SetChoicesVisible(false);
            ClearChoices();
            SetText(speakerText, info.Speaker);
            SetText(bodyText, "");
        }

        void HandleLineTextUpdated(string visible)
        {
            SetText(bodyText, visible);
        }

        void HandleChoicesPresented(IReadOnlyList<string> labels)
        {
            if (linePanel != null)
                linePanel.SetActive(true);
            SetChoicesVisible(true);
            ClearChoices();
            if (choicePrefab == null || choicesRoot == null || labels == null)
                return;

            for (int i = 0; i < labels.Count; i++)
            {
                DialogueChoiceItem item = Instantiate(choicePrefab, choicesRoot);
                item.gameObject.SetActive(true);
                item.Bind(this, i, labels[i]);
                _spawned.Add(item);
            }

            HandleChoiceIndexChanged(0);
        }

        void HandleChoiceIndexChanged(int index)
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    _spawned[i].SetSelected(i == index);
            }
        }

        void HandleEnded()
        {
            ClearChoices();
            SetChoicesVisible(false);
            if (hideOnEnd)
                SetRootVisible(false);
        }

        void SetRootVisible(bool visible)
        {
            if (rootPanel != null)
                rootPanel.SetActive(visible);
        }

        void SetChoicesVisible(bool visible)
        {
            if (choicesPanel != null)
                choicesPanel.SetActive(visible);
        }

        void ClearChoices()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Destroy(_spawned[i].gameObject);
            }

            _spawned.Clear();
        }

        static void SetText(Text target, string value)
        {
            if (target != null)
                target.text = value ?? "";
        }
    }
}
