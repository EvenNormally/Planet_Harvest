using System.Text;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using TMPro;
using UnityEngine;

namespace PlanetHarvest.UI
{
    /// <summary>Final sequence: statistics of the expedition and slowly rising credits. Esc returns to free mining.</summary>
    public sealed class EndingScreen : MonoBehaviour
    {
        private EarthMining mining;
        private TMP_FontAsset font;
        private RectTransform panel, crawl;
        private TextMeshProUGUI title, subtitle, stats, credits, hint;
        private float crawlOffset;

        public void Initialize(EarthMining owner, RectTransform screen, TMP_FontAsset typeface)
        {
            mining = owner;
            font = typeface;
            panel = UiKit.Panel(screen, "Endsequenz", new Vector2(760, 560), UiKit.Ink);
            UiKit.Box(panel, 0, 0, 760, 5, UiKit.Mint);
            title = UiKit.Label(panel, font, 30, 22, 700, 60, 44, UiKit.Mint, true, TextAlignmentOptions.Center);
            subtitle = UiKit.Label(panel, font, 30, 86, 700, 40, 18, UiKit.Paper, false, TextAlignmentOptions.Center);
            stats = UiKit.Label(panel, font, 60, 140, 640, 240, 18, UiKit.Paper, false, TextAlignmentOptions.TopLeft);
            crawl = (RectTransform)UiKit.Box(panel, 30, 390, 700, 120, new Color(0, 0, 0, 0)).transform;
            crawl.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            credits = UiKit.Label(crawl, font, 0, 120, 700, 240, 16, UiKit.Muted, false, TextAlignmentOptions.Top);
            hint = UiKit.Label(panel, font, 30, 520, 700, 30, 15, UiKit.Muted, false, TextAlignmentOptions.Center);
            panel.gameObject.SetActive(false);
        }

        /// <summary>Fills the screen from the current expedition state.</summary>
        public string Describe(EarthFreight freight, PlanetProgress progress, float playSeconds)
        {
            var text = new StringBuilder();
            text.AppendLine($"{Localization.Text("ending.orbit")}:   {progress.OrbitFraction * 100:F2} %   ({progress.InOrbit / HarvestInventory.CubicMetre} m³)");
            text.AppendLine($"{Localization.Text("ending.credits")}:   {freight.Credits}");
            text.AppendLine($"{Localization.Text("ending.contracts")}:   {freight.Contracts.Completed} / {freight.Contracts.Count}");
            text.AppendLine($"{Localization.Text("ending.time")}:   {(int)(playSeconds / 3600)} h {(int)(playSeconds / 60 % 60)} min");
            text.AppendLine();
            foreach (var material in freight.Program.Materials)
                text.AppendLine($"   {material.Name}   {freight.Delivered(material.Id) / HarvestInventory.CubicMetre} m³");
            return text.ToString();
        }

        private void LateUpdate()
        {
            bool open = mining && mining.Menu == FieldMenu.Ending;
            if (panel.gameObject.activeSelf != open)
            {
                panel.gameObject.SetActive(open);
                crawlOffset = 0;
                if (open)
                {
                    var freight = mining.Elevator.Freight;
                    title.text = Localization.Text("ending.title");
                    subtitle.text = Localization.Text("ending.subtitle");
                    stats.text = Describe(freight, PlanetProgress.Measure(mining.Planet.Data.InitialCount, freight, 0), Time.timeSinceLevelLoad);
                    credits.text = Localization.Text("ending.crew") + "\n\n" + Localization.Text("ending.title");
                    hint.text = Localization.Text("ending.continue");
                }
            }
            if (!open) return;
            crawlOffset = Mathf.Repeat(crawlOffset + Time.unscaledDeltaTime * 18, 360);
            credits.rectTransform.anchoredPosition = new Vector2(0, -120 + crawlOffset);
        }
    }
}
