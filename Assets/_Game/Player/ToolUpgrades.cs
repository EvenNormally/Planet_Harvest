using System;
using System.Collections.Generic;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;

namespace PlanetHarvest.Player
{
    /// <summary>The three upgrade axes of the hand tools. Values are save and network data.</summary>
    public enum UpgradeAxis
    {
        Radius = 0,
        Speed = 1,
        Capacity = 2
    }

    public readonly struct UpgradeStep
    {
        /// <summary>Credits to reach this step; ignored for step 0 (starting equipment).</summary>
        public readonly int Price;
        /// <summary>Radius and speed: multiplier. Capacity: backpack volume in m³.</summary>
        public readonly float Value;

        public UpgradeStep(int price, float value)
        {
            if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
            if (!(value > 0) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Price = price;
            Value = value;
        }
    }

    /// <summary>Validated, immutable upgrade table built from a <see cref="ToolUpgradeSet"/> or in code.</summary>
    public sealed class UpgradeCatalog
    {
        public const int AxisCount = 3;
        private readonly UpgradeStep[][] steps = new UpgradeStep[AxisCount][];
        private readonly string[] names = new string[AxisCount];

        public UpgradeCatalog(IReadOnlyList<UpgradeStep> radius, IReadOnlyList<UpgradeStep> speed,
            IReadOnlyList<UpgradeStep> capacity, string radiusName = "Abbauradius", string speedName = "Abbaugeschwindigkeit",
            string capacityName = "Aufnahmekapazität")
        {
            Set(UpgradeAxis.Radius, radius, radiusName);
            Set(UpgradeAxis.Speed, speed, speedName);
            Set(UpgradeAxis.Capacity, capacity, capacityName);
            if (Step(UpgradeAxis.Radius, 0).Value != 1 || Step(UpgradeAxis.Speed, 0).Value != 1)
                throw new ArgumentException("Stufe 0 von Radius und Geschwindigkeit muss Faktor 1 sein.");
        }

        private void Set(UpgradeAxis axis, IReadOnlyList<UpgradeStep> values, string label)
        {
            if (values == null || values.Count == 0) throw new ArgumentException($"{axis}: keine Stufen.");
            var copy = new UpgradeStep[values.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = values[i];
                if (i > 0 && copy[i].Value <= copy[i - 1].Value)
                    throw new ArgumentException($"{axis}: Stufen müssen ansteigen.");
                if (axis == UpgradeAxis.Capacity && copy[i].Value % 1 != 0)
                    throw new ArgumentException("Kapazität in ganzen m³ angeben.");
            }
            steps[(int)axis] = copy;
            names[(int)axis] = string.IsNullOrWhiteSpace(label) ? axis.ToString() : label;
        }

        public string Name(UpgradeAxis axis) => names[(int)axis];
        public int MaxLevel(UpgradeAxis axis) => steps[(int)axis].Length - 1;
        public UpgradeStep Step(UpgradeAxis axis, int level) => steps[(int)axis][level];
        public long CapacityAt(int level) => (long)Step(UpgradeAxis.Capacity, level).Value * HarvestInventory.CubicMetre;
        public long LargestCapacity => CapacityAt(MaxLevel(UpgradeAxis.Capacity));

        public bool IsValid(int[] levels)
        {
            if (levels == null || levels.Length != AxisCount) return false;
            for (int i = 0; i < AxisCount; i++)
                if (levels[i] < 0 || levels[i] > MaxLevel((UpgradeAxis)i)) return false;
            return true;
        }
    }

    /// <summary>
    /// One crew member's purchased upgrade levels. Credits are shared through the elevator wallet,
    /// the levels themselves belong to the player.
    /// </summary>
    public sealed class ToolUpgrades
    {
        private readonly int[] levels = new int[UpgradeCatalog.AxisCount];
        public UpgradeCatalog Catalog { get; }
        public int Revision { get; private set; }

        public ToolUpgrades(UpgradeCatalog catalog)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public int Level(UpgradeAxis axis) => levels[(int)axis];
        public bool IsMaxed(UpgradeAxis axis) => Level(axis) >= Catalog.MaxLevel(axis);
        public int NextPrice(UpgradeAxis axis) => IsMaxed(axis) ? 0 : Catalog.Step(axis, Level(axis) + 1).Price;
        public float RadiusMultiplier => Catalog.Step(UpgradeAxis.Radius, Level(UpgradeAxis.Radius)).Value;
        public float SpeedMultiplier => Catalog.Step(UpgradeAxis.Speed, Level(UpgradeAxis.Speed)).Value;
        public long Capacity => Catalog.CapacityAt(Level(UpgradeAxis.Capacity));

        /// <summary>Strike interval of a tool after the speed upgrade.</summary>
        public float RepeatSeconds(float baseSeconds) => baseSeconds / SpeedMultiplier;

        /// <summary>Buys the next level at the workbench if the shared wallet covers it.</summary>
        public bool TryBuy(UpgradeAxis axis, EarthFreight wallet, bool atShop, HarvestInventory pack)
        {
            if ((uint)axis >= UpgradeCatalog.AxisCount || !atShop || wallet == null || IsMaxed(axis)) return false;
            if (!wallet.Spend(NextPrice(axis))) return false;
            levels[(int)axis]++;
            Revision++;
            if (axis == UpgradeAxis.Capacity) pack?.Expand(Capacity);
            return true;
        }

        public int[] Capture() => (int[])levels.Clone();

        /// <summary>Restores saved levels and resizes the pack. The pack must be empty or fit the capacity.</summary>
        public void Restore(int[] saved, HarvestInventory pack)
        {
            if (!Catalog.IsValid(saved)) throw new ArgumentException("Ungültige Upgradestufen.", nameof(saved));
            Array.Copy(saved, levels, levels.Length);
            Revision++;
            pack?.Resize(Capacity);
        }
    }
}
