using PlanetHarvest.Core;
using PlanetHarvest.Mining;
using TMPro;
using UnityEngine;

namespace PlanetHarvest.UI
{
    public sealed class HarvestHud : MonoBehaviour
    {
        [SerializeField] private HarvestGame game;
        [SerializeField] private HarvestInteraction interaction;
        private TextMeshProUGUI status;
        private TMP_FontAsset font;
        private float nextRefresh;

        private void Start()
        {
            font = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            var canvasObject = new GameObject("HUD Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            status = AddText(canvasObject.transform, "Status", new Vector2(0, 1), new Vector2(24, -20), new Vector2(1050, 240), 24);
            var crosshair = AddText(canvasObject.transform, "Crosshair", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30), 28);
            crosshair.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            crosshair.alignment = TextAlignmentOptions.Center;
            crosshair.text = "+";
        }

        private TextMeshProUGUI AddText(Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.raycastTarget = false;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return text;
        }

        private void Update()
        {
            if (status == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.1f;
            var session = game.Session;
            string shipment = session.IsShipping ? $"Rueckkehr in {session.ShippingRemaining:0.0} s" : $"Lieferungen: {session.Shipments}";
            status.text = $"PLANET HARVEST\nRegolith | Dein Kanister: {session.Buffer.Amount / 1000000f:0.##}/{session.Buffer.Capacity / 1000000f:0.##} m³ | Versand: {session.Container.Amount / 1000000f:0.##}/{session.Container.Capacity / 1000000f:0.##} m³\n{shipment} | Credits: {session.Credits}\nWASD: Laufen   Maus: Blick   Space: Springen   Esc: Maus freigeben\n{interaction.Hint}\n{interaction.Feedback}";
        }

    }
}
