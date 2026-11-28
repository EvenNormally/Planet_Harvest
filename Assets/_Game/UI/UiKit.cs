using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetHarvest.UI
{
    /// <summary>Small helpers to build the code-driven overlay panels in the HUD's flat style.</summary>
    internal static class UiKit
    {
        public static readonly Color32 Ink = new Color32(24, 43, 49, 245);
        public static readonly Color32 Slate = new Color32(52, 76, 79, 248);
        public static readonly Color32 Paper = new Color32(246, 244, 222, 255);
        public static readonly Color32 Muted = new Color32(186, 204, 200, 255);
        public static readonly Color32 Mint = new Color32(128, 229, 190, 255);

        public static RectTransform Panel(RectTransform parent, string name, Vector2 size, Color color)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        public static Image Box(RectTransform parent, float x, float y, float w, float h, Color color)
        {
            var rect = (RectTransform)new GameObject("Fläche", typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static TextMeshProUGUI Label(RectTransform parent, TMP_FontAsset font, float x, float y, float w, float h, int size,
            Color color, bool bold = false, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var rect = (RectTransform)new GameObject("Text", typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        public static (Button button, TextMeshProUGUI label) Button(RectTransform parent, TMP_FontAsset font, float x, float y,
            float w, float h, UnityEngine.Events.UnityAction action)
        {
            var image = Box(parent, x, y, w, h, Slate);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var label = Label(image.rectTransform, font, 4, 1, w - 8, h - 2, 15, Paper, true, TextAlignmentOptions.Center);
            return (button, label);
        }
    }
}
