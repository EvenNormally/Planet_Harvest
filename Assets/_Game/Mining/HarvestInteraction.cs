using PlanetHarvest.Core;
using PlanetHarvest.Logistics;
using PlanetHarvest.Player;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Mining
{
    public sealed class HarvestInteraction : MonoBehaviour
    {
        [SerializeField] private HarvestGame game;
        [SerializeField] private VoxelMoon moon;
        [SerializeField] private RadialPlayer player;
        [SerializeField] private Camera view;
        public string Hint { get; private set; } = "Blicke auf den Boden und bohre mit LMB.";
        public string Feedback { get; private set; } = "Ziel: Kanister füllen und Regolith versenden.";
        public const float Reach = 5f;

        private void Update()
        {
            if (!player.Captured) { Hint = "Esc: Maus fangen und weiterspielen"; return; }
            Ray ray = new Ray(view.transform.position, view.transform.forward);
            if (!Physics.Raycast(ray, out var hit, Reach, ~0, QueryTriggerInteraction.Ignore))
            {
                Hint = "Kein Ziel in Reichweite (5 m).";
                return;
            }
            var terminal = hit.collider.GetComponent<HarvestTerminal>();
            if (terminal != null)
            {
                var material = terminal.Material;
                var session = game.Session;
                if (session.IsShipping) Hint = "Versand läuft. Warte auf die Rückkehr.";
                else if (IsDispatch(terminal))
                    Hint = $"E: {material.Name()} versenden (+{session.PriceFor(material)} Credits)";
                else if (session.Buffer.AmountOf(material) > 0)
                    Hint = $"E: {material.Name()} aus dem Puffer umfüllen";
                else Hint = $"Dieser Container nimmt nur {material.Name()} auf.";
                if (player.InteractPressed) UseTerminal(terminal);
                return;
            }
            if (hit.collider.GetComponentInParent<VoxelMoon>() == moon)
            {
                Hint = game.Session.Buffer.Free == 0 ? "Puffer voll! Am passenden Container entleeren."
                    : moon.Data.MaterialAt(moon.CellAt(hit)) == MaterialId.Stone ? "LMB: Stein abbauen" : "LMB: Regolith abbauen";
                if (player.MinePressed) Mine(hit);
            }
            else Hint = "Blicke auf den Boden oder einen Versandcontainer.";
        }

        private bool IsDispatch(HarvestTerminal terminal) => terminal.IsElevator ||
            (terminal.IsReturningCanister && game.Session.ContainerFor(terminal.Material).Free == 0);

        public bool Mine(RaycastHit hit)
        {
            if (hit.collider == null || hit.collider.GetComponentInParent<VoxelMoon>() != moon) return false;
            bool mined = moon.TryMine(game.Session, moon.CellAt(hit), out var material);
            Feedback = mined ? $"{game.Session.MiningVolume / (double)HarvestInventory.CubicMetre:0.##} m³ {material.Name()} gewonnen."
                : "Abbau blockiert: Puffer voll oder Luft.";
            return mined;
        }

        public bool UseTerminal(HarvestTerminal terminal)
        {
            if (terminal == null) return false;
            var session = game.Session;
            if (session.IsShipping)
            {
                Feedback = "Versandkanister unterwegs. Bitte auf die Rückkehr warten.";
                return false;
            }
            var material = terminal.Material;
            if (IsDispatch(terminal))
            {
                bool started = session.TryShip(material);
                Feedback = started ? $"{material.Name()} versendet · {session.PriceFor(material)} Credits bei Ankunft."
                    : "Versand blockiert: Container muss voll sein.";
                return started;
            }
            long moved = session.Transfer(material);
            Feedback = moved > 0 ? $"{moved / (double)HarvestInventory.CubicMetre:0.##} m³ {material.Name()} umgefüllt."
                : $"Transfer blockiert: {material.Name()} im Puffer und Platz im Container erforderlich.";
            return moved > 0;
        }
    }
}
