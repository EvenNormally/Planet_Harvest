using PlanetHarvest.Logistics;
using PlanetHarvest.Player;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Mining
{
    [RequireComponent(typeof(RadialPlayer))]
    public sealed class EarthMining : MonoBehaviour
    {
        [SerializeField] private VoxelMoon planet;
        [SerializeField] private Camera view;
        [SerializeField] private FieldEquipment equipmentPrefab;
        [SerializeField] private EarthElevator elevatorPrefab;
        private RadialPlayer player;
        public FieldEquipment Equipment { get; private set; }
        public EarthElevator Elevator { get; private set; }
        public EarthCargoBin Bin { get; private set; }
        public HarvestInventory Inventory { get; } = new HarvestInventory(long.MaxValue);
        public MaterialId Target { get; private set; }

        private void Awake() => player = GetComponent<RadialPlayer>();

        private void Start()
        {
            Equipment = Instantiate(equipmentPrefab, view.transform, false);
            Equipment.Select(0, Inventory);
            Elevator = Instantiate(elevatorPrefab, planet.transform.parent);
            Elevator.Place(planet);
        }

        private void Update()
        {
            Target = MaterialId.None;
            Bin = null;
            if (!player.Captured) return;
            Equipment.ReadSelection(Inventory);
            if (!Physics.Raycast(view.transform.position, view.transform.forward, out var hit,
                HarvestInteraction.Reach, ~0, QueryTriggerInteraction.Ignore)) return;
            if (hit.collider.TryGetComponent(out EarthCargoBin bin))
            {
                Bin = bin;
                if (player.InteractPressed || (player.MinePressed && Equipment.HeldMaterial == bin.Material))
                {
                    Elevator.Freight.Load(Inventory, bin.Material);
                    Equipment.Select(Equipment.Slot, Inventory);
                }
                return;
            }
            if (hit.collider.GetComponentInParent<VoxelMoon>() != planet) return;
            var cell = planet.CellAt(hit);
            Target = planet.Data.MaterialAt(cell);
            if (player.MinePressed && Equipment.Slot == 0 && planet.TryMine(Inventory, cell, out _))
                Equipment.Strike();
        }
    }
}
