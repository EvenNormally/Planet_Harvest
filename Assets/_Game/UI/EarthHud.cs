using PlanetHarvest.Mining;
using UnityEngine;

namespace PlanetHarvest.UI
{
    [RequireComponent(typeof(EarthMining))]
    public sealed class EarthHud : MonoBehaviour
    {
        private EarthMining mining;
        private GUIStyle panel, crosshair;
        private long lastAmount = -1;
        private MaterialId lastTarget;
        private string text;
        private void Awake() => mining = GetComponent<EarthMining>();

        private void OnGUI()
        {
            if (panel == null)
            {
                panel = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, fontSize = 16,
                    padding = new RectOffset(14, 14, 8, 8) };
                panel.normal.textColor = Color.white;
                crosshair = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
                crosshair.normal.textColor = Color.white;
            }
            var pack = mining.Inventory;
            if (lastAmount != pack.Amount || lastTarget != mining.Target)
            {
                lastAmount = pack.Amount;
                lastTarget = mining.Target;
                long unit = HarvestInventory.CubicMetre;
                text = $"Erde   {pack.AmountOf(MaterialId.Soil) / unit} m³     Stein   {pack.AmountOf(MaterialId.Stone) / unit} m³     Eisenerz   {pack.AmountOf(MaterialId.IronOre) / unit} m³\n"
                    + (lastTarget == MaterialId.None ? "WASD  Gehen     Maus  Umsehen     LMB  Abbauen" : $"LMB  {lastTarget.Name()} abbauen");
            }
            GUI.Box(new Rect(14, 14, 390, 62), text, panel);
            GUI.Label(new Rect(Screen.width / 2f - 12, Screen.height / 2f - 15, 24, 30), "+", crosshair);
        }
    }
}
