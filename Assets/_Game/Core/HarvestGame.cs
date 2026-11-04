using UnityEngine;

namespace PlanetHarvest.Core
{
    public sealed class HarvestGame : MonoBehaviour
    {
        [SerializeField] private HarvestSettings settings;
        public HarvestSession Session { get; private set; }
        private void Awake() => Session = new HarvestSession(settings.ShipmentPrice);
    }
}
