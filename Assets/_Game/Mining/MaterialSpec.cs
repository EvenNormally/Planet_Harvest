using System;
using UnityEngine;

namespace PlanetHarvest.Mining
{
    /// <summary>Silhouette of the elevator container that receives a material.</summary>
    public enum ContainerKind
    {
        PressTub = 0,
        Barrel = 1,
        OreHopper = 2,
        MagneticBin = 3,
        PrismBox = 4,
        GlassSphere = 5,
        OrganicTank = 6
    }

    /// <summary>Immutable runtime copy of a <see cref="MaterialDefinition"/>.</summary>
    public readonly struct MaterialSpec
    {
        private const long MilliPerCredit = 1000;
        public const float MaxCreditsPerCubicMetre = 10000;

        public readonly MaterialId Id;
        public readonly string Name;
        public readonly float DensityKgPerCubicMetre;
        /// <summary>Delivery value in thousandths of a credit per m³, kept integral to avoid drift.</summary>
        public readonly long MilliCreditsPerCubicMetre;
        public readonly int ContainerCubicMetres;
        public readonly ContainerKind Container;
        public readonly Color ContainerColor;

        public MaterialSpec(MaterialId id, string name, float densityKgPerCubicMetre, float creditsPerCubicMetre,
            int containerCubicMetres, ContainerKind container, Color containerColor)
        {
            if (!HarvestInventory.IsResource(id)) throw new ArgumentOutOfRangeException(nameof(id));
            if (!(densityKgPerCubicMetre > 0) || float.IsInfinity(densityKgPerCubicMetre))
                throw new ArgumentOutOfRangeException(nameof(densityKgPerCubicMetre));
            if (!(creditsPerCubicMetre >= 0) || creditsPerCubicMetre > MaxCreditsPerCubicMetre)
                throw new ArgumentOutOfRangeException(nameof(creditsPerCubicMetre));
            if (containerCubicMetres < 1) throw new ArgumentOutOfRangeException(nameof(containerCubicMetres));
            Id = id;
            Name = string.IsNullOrWhiteSpace(name) ? id.Name() : name;
            DensityKgPerCubicMetre = densityKgPerCubicMetre;
            MilliCreditsPerCubicMetre = (long)Math.Round(creditsPerCubicMetre * (double)MilliPerCredit);
            ContainerCubicMetres = containerCubicMetres;
            Container = container;
            ContainerColor = containerColor;
        }

        public long ContainerCapacity => ContainerCubicMetres * HarvestInventory.CubicMetre;

        /// <summary>Whole credits earned by a cumulative delivered volume (cm³). Rounds down.</summary>
        public long CreditsFor(long deliveredCubicCentimetres)
        {
            if (deliveredCubicCentimetres <= 0) return 0;
            // Split into whole m³ and remainder so the product cannot overflow; the floor stays exact.
            long whole = deliveredCubicCentimetres / HarvestInventory.CubicMetre;
            long part = deliveredCubicCentimetres % HarvestInventory.CubicMetre;
            long milli = whole * MilliCreditsPerCubicMetre + part * MilliCreditsPerCubicMetre / HarvestInventory.CubicMetre;
            return milli / MilliPerCredit;
        }

        /// <summary>Mass in kilograms of a volume given in cm³.</summary>
        public double MassKg(long cubicCentimetres) => cubicCentimetres * (double)DensityKgPerCubicMetre / HarvestInventory.CubicMetre;
    }
}
