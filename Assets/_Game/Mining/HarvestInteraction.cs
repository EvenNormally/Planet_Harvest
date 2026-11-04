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
        public string Feedback { get; private set; } = "Ziel: 1 m³ Regolith versenden.";
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
                Hint = terminal.IsElevator
                    ? $"E: vollen Container versenden (+{game.Session.ShipmentPrice} Credits)"
                    : "E: Regolith vom Puffer in den 1-m³-Container füllen";
                if (player.InteractPressed) UseTerminal(terminal);
                return;
            }
            if (hit.collider.GetComponentInParent<VoxelMoon>() == moon)
            {
                Hint = game.Session.Buffer.Free == 0 ? "Puffer voll! Am cyanfarbenen Container entleeren." : "LMB: 1 m³ Regolith bohren";
                if (player.MinePressed) Mine(hit);
            }
            else Hint = "Blicke auf Regolith, Container oder Elevator-Terminal.";
        }

        public bool Mine(RaycastHit hit)
        {
            if (hit.collider == null || hit.collider.GetComponentInParent<VoxelMoon>() != moon) return false;
            bool mined = moon.TryMine(game.Session, moon.CellAt(hit));
            Feedback = mined ? "1 m³ Regolith gewonnen." : "Abbau blockiert: Puffer voll oder Luft.";
            return mined;
        }

        public bool UseTerminal(HarvestTerminal terminal)
        {
            bool success;
            if (terminal.IsElevator)
            {
                success = game.Session.TryShip();
                Feedback = success ? $"Versendet! +{game.Session.ShipmentPrice} Credits. Container wieder leer." : "Versand blockiert: Container muss voll sein.";
            }
            else
            {
                success = game.Session.Transfer() > 0;
                Feedback = success ? "Regolith im Container. Jetzt am orangefarbenen Elevator versenden." : "Transfer blockiert: Puffer leer oder Container voll.";
            }
            return success;
        }
    }
}
