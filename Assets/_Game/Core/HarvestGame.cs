using UnityEngine;

namespace PlanetHarvest.Core
{
    public sealed class HarvestGame : MonoBehaviour
    {
        [SerializeField] private HarvestSettings settings;
        public HarvestSession Session { get; private set; }
        private void Awake() => Session = new HarvestSession(settings.ShipmentPrice,
            settings.CellSize * settings.CellSize * settings.CellSize * Mining.HarvestInventory.CubicMetre,
            settings.ContainerLoads, settings.ShipmentDuration);
        private void Update() => Session.AdvanceShipment(Time.deltaTime);
    }
}
