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
        public string Feedback { get; private set; } = "Ziel: Kanister fuellen und Regolith versenden.";
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
                Hint = terminal.IsReturningCanister
                    ? (game.Session.Buffer.Amount > 0 && game.Session.Container.Free > 0 ? "E: persoenlichen Kanister entleeren" : "E: vollen Versandkanister abschicken")
                    : terminal.IsElevator
                    ? $"E: vollen Container versenden (+{game.Session.ShipmentPrice} Credits)"
                    : "E: Regolith vom Puffer in den 1-m³-Container füllen";
                if (player.InteractPressed) UseTerminal(terminal);
                return;
            }
            if (hit.collider.GetComponentInParent<VoxelMoon>() == moon)
            {
                Hint = game.Session.Buffer.Free == 0 ? "Kanister voll! Am cyanfarbenen Container entleeren." : $"LMB: {game.Session.MiningVolume / 1000000f:0.##} m³ Regolith abbauen";
                if (player.MinePressed) Mine(hit);
            }
            else Hint = "Blicke auf Regolith, Container oder Elevator-Terminal.";
        }

        public bool Mine(RaycastHit hit)
        {
            if (hit.collider == null || hit.collider.GetComponentInParent<VoxelMoon>() != moon) return false;
            bool mined = moon.TryMine(game.Session, moon.CellAt(hit));
            Feedback = mined ? $"{game.Session.MiningVolume / 1000000f:0.##} m³ Regolith gewonnen." : "Abbau blockiert: Kanister voll oder Luft.";
            return mined;
        }

        public bool UseTerminal(HarvestTerminal terminal)
        {
            if (terminal == null) return false;
            if (game.Session.IsShipping)
            {
                Feedback = "Versandkanister unterwegs. Bitte auf die leere Rueckkehr warten.";
                return false;
            }
            bool success;
            if (terminal.IsElevator || (terminal.IsReturningCanister &&
                (game.Session.Buffer.Amount == 0 || game.Session.Container.Free == 0)))
            {
                success = game.Session.TryShip();
                Feedback = success ? (game.Session.IsShipping ? "Versand gestartet. Der Kanister kommt leer zurueck." : $"Versendet! +{game.Session.ShipmentPrice} Credits. Container wieder leer.") : "Versand blockiert: Container muss voll sein.";
            }
            else
            {
                success = game.Session.Transfer() > 0;
                Feedback = success ? (terminal.IsReturningCanister ? "Umgefuellt. Vollen Versandkanister mit E abschicken." : "Regolith im Container. Jetzt am orangefarbenen Elevator versenden.") : "Transfer blockiert: Kanister leer oder Container voll.";
            }
            return success;
        }
    }
}
