using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Serum.MenuUI
{
    public sealed class SerumMenuItem : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public SerumMainMenu menu;
        public TMP_Text label;
        public Color normalColor = new Color(.77f, .76f, .71f, 1);
        public Color selectedColor = new Color(1f, .91f, .74f, 1);
        public Color disabledColor = new Color(.53f, .53f, .51f, .65f);
        Button button;
        bool highlighted;

        void Awake()
        {
            button = GetComponent<Button>();
            if (label == null) label = GetComponentInChildren<TMP_Text>();
        }

        void OnEnable() => UpdateLabelColor();
        void LateUpdate() => UpdateLabelColor();

        void UpdateLabelColor()
        {
            if (button == null || label == null) return;
            Color color = !button.IsInteractable() ? disabledColor : (highlighted ? selectedColor : normalColor);
            if (label.color != color) label.color = color;
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (button.IsInteractable()) button.Select();
        }
        public void OnSelect(BaseEventData data)
        {
            if (button.IsInteractable()) menu.Highlight(this);
        }

        public void SetHighlighted(bool selected)
        {
            highlighted = selected;
            UpdateLabelColor();
        }
    }
}
