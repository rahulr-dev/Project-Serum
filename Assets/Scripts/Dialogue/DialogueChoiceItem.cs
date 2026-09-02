using UnityEngine;
using UnityEngine.UI;

namespace Dialogue
{
    public class DialogueChoiceItem : MonoBehaviour
    {
        [SerializeField] GameObject highlight;
        [SerializeField] Image highlightImage;
        [SerializeField] Text label;
        [SerializeField] Button button;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color selectedColor = new Color(1f, 0.92f, 0.4f, 1f);

        int _index;
        DialogueUI _ui;

        public void Bind(DialogueUI ui, int index, string text)
        {
            _ui = ui;
            _index = index;
            if (label != null)
                label.text = text ?? "";
            SetSelected(false);
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
                button.onClick.AddListener(OnClicked);
            }
        }

        public void SetSelected(bool selected)
        {
            if (highlight != null)
                highlight.SetActive(selected);
            if (highlightImage != null)
                highlightImage.enabled = selected;
            if (label != null)
                label.color = selected ? selectedColor : normalColor;
        }

        void OnClicked()
        {
            if (_ui != null)
                _ui.ClickChoice(_index);
        }
    }
}
