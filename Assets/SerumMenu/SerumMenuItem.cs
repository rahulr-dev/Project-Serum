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
        public void OnPointerEnter(PointerEventData data)
        {
            if (GetComponent<Button>().IsInteractable()) GetComponent<Button>().Select();
        }
        public void OnSelect(BaseEventData data) => menu.Highlight(this);
        public void SetHighlighted(bool selected) => label.color = selected ? selectedColor : normalColor;
    }
}
