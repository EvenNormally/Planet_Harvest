using System;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Mining
{
    /// <summary>Shared capacity across all contents, measured in integer cm³.</summary>
    public sealed class HarvestInventory
    {
        public const long CubicMetre = 1000000;
        private readonly long[] amounts = new long[MaterialSlots];
        private readonly MaterialId? acceptedMaterial;
        internal bool InTransit { get; set; }
        public long Amount { get; private set; }
        public int Revision { get; private set; }
        public long Capacity { get; private set; }
        public long Free => Capacity - Amount;

        public HarvestInventory(long capacity = CubicMetre, MaterialId? acceptedMaterial = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (acceptedMaterial.HasValue && !IsResource(acceptedMaterial.Value))
                throw new ArgumentOutOfRangeException(nameof(acceptedMaterial));
            Capacity = capacity;
            this.acceptedMaterial = acceptedMaterial;
        }

        /// <summary>Number of per-material slots; arrays indexed by <see cref="MaterialId"/> use this length.</summary>
        public const int MaterialSlots = (int)MaterialId.Quartz + 1;

        public static bool IsResource(MaterialId material) =>
            material >= MaterialId.Regolith && material <= MaterialId.Quartz;

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
            Amount += volume;
            Revision++;
            return true;
        }

        public bool TryAdd(MaterialId material, long amount)
        {
            if (InTransit || !IsResource(material) || (acceptedMaterial.HasValue && acceptedMaterial != material)
                || amount <= 0 || amount > Free) return false;
            amounts[(int)material] += amount;
            Amount += amount;
            Revision++;
            return true;
        }

        public long TransferTo(HarvestInventory destination, MaterialId material = MaterialId.Regolith)
        {
            if (InTransit || destination == null || ReferenceEquals(this, destination)) return 0;
            long moved = Math.Min(AmountOf(material), destination.Free);
            if (!destination.TryAdd(material, moved)) return 0;
            amounts[(int)material] -= moved;
            Amount -= moved;
            Revision++;
            return moved;
        }

        /// <summary>Grows the capacity; never shrinks it.</summary>
        public void Expand(long capacity)
        {
            if (capacity > Capacity) Resize(capacity);
        }

        /// <summary>Sets the capacity. It may not drop below the current contents.</summary>
        public void Resize(long capacity)
        {
            if (capacity <= 0 || capacity < Amount) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            Revision++;
        }

        internal void Empty()
        {
            Array.Clear(amounts, 0, amounts.Length);
            Amount = 0;
            Revision++;
        }
    }
}
