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
        private RadialPlayer player;
        public HarvestInventory Inventory { get; } = new HarvestInventory(long.MaxValue);
        public MaterialId Target { get; private set; }

        private void Awake() => player = GetComponent<RadialPlayer>();

        private void Update()
        {
            Target = MaterialId.None;
            if (!player.Captured || !Physics.Raycast(view.transform.position, view.transform.forward, out var hit,
                HarvestInteraction.Reach, ~0, QueryTriggerInteraction.Ignore)) return;
            if (hit.collider.GetComponentInParent<VoxelMoon>() != planet) return;
            var cell = planet.CellAt(hit);
            Target = planet.Data.MaterialAt(cell);
            if (player.MinePressed) planet.TryMine(Inventory, cell, out _);
        }
    }
}
