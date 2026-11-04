using System;

namespace PlanetHarvest.Mining
{
    /// <summary>Regolith only. Integer cm³ keep every transfer exact.</summary>
    public sealed class HarvestInventory
    {
        public const long CubicMetre = 1000000;
        public long Amount { get; private set; }
        public long Capacity { get; }
        public long Free => Capacity - Amount;

        public HarvestInventory(long capacity = CubicMetre)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
        }

        public bool TryAdd(long amount)
        {
            if (amount <= 0 || amount > Free) return false;
            Amount += amount;
            return true;
        }

        public long TransferTo(HarvestInventory destination)
        {
            if (destination == null || ReferenceEquals(this, destination)) return 0;
            long moved = Math.Min(Amount, destination.Free);
            if (moved == 0) return 0;
            destination.Amount += moved;
            Amount -= moved;
            return moved;
        }

        internal void Empty() => Amount = 0;
    }
}
