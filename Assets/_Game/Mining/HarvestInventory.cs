using System;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Mining
{
    /// <summary>Shared capacity across all contents, measured in integer cm³.</summary>
    public sealed class HarvestInventory
    {
        public const long CubicMetre = 1000000;
        private readonly long[] amounts = new long[(int)MaterialId.IronOre + 1];
        private readonly MaterialId? acceptedMaterial;
        internal bool InTransit { get; set; }
        public long Amount => amounts[(int)MaterialId.Regolith] + amounts[(int)MaterialId.Stone]
            + amounts[(int)MaterialId.Soil] + amounts[(int)MaterialId.IronOre];
        public long Capacity { get; }
        public long Free => Capacity - Amount;

        public HarvestInventory(long capacity = CubicMetre, MaterialId? acceptedMaterial = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (acceptedMaterial.HasValue && !IsResource(acceptedMaterial.Value))
                throw new ArgumentOutOfRangeException(nameof(acceptedMaterial));
            Capacity = capacity;
            this.acceptedMaterial = acceptedMaterial;
        }

        private static bool IsResource(MaterialId material) =>
            material == MaterialId.Regolith || material == MaterialId.Stone
            || material == MaterialId.Soil || material == MaterialId.IronOre;

        public long AmountOf(MaterialId material) => IsResource(material) ? amounts[(int)material] : 0;

        public bool TryAdd(long amount) => TryAdd(MaterialId.Regolith, amount);

        internal bool TryMine(VoxelData terrain, Vector3Int cell, long volume, out MaterialId material)
        {
            material = MaterialId.None;
            if (terrain == null || InTransit || volume <= 0 || volume > Free) return false;
            var resource = terrain.MaterialAt(cell);
            if (!IsResource(resource) || (acceptedMaterial.HasValue && acceptedMaterial != resource)) return false;
            if (!terrain.TryRemove(cell, out material)) return false;
            amounts[(int)material] += volume;
            return true;
        }

        public bool TryAdd(MaterialId material, long amount)
        {
            if (InTransit || !IsResource(material) || (acceptedMaterial.HasValue && acceptedMaterial != material)
                || amount <= 0 || amount > Free) return false;
            amounts[(int)material] += amount;
            return true;
        }

        public long TransferTo(HarvestInventory destination, MaterialId material = MaterialId.Regolith)
        {
            if (InTransit || destination == null || ReferenceEquals(this, destination)) return 0;
            long moved = Math.Min(AmountOf(material), destination.Free);
            if (!destination.TryAdd(material, moved)) return 0;
            amounts[(int)material] -= moved;
            return moved;
        }

        internal void Empty() => Array.Clear(amounts, 0, amounts.Length);
    }
}
