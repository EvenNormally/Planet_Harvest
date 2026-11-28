namespace PlanetHarvest.Mining
{
    /// <summary>Stable material keys. The numeric values are save and network data: append, never renumber.</summary>
    public enum MaterialId
    {
        None = 0,
        Regolith = 1,
        Stone = 2,
        Soil = 3,
        IronOre = 4,
        CopperOre = 5,
        Quartz = 6,
        /// <summary>Pumped from finite reservoirs, not part of the voxel terrain.</summary>
        Water = 7,
        /// <summary>Harvested from plants, not part of the voxel terrain.</summary>
        Biomass = 8
    }

    public static class HarvestMaterial
    {
        public static string Name(this MaterialId material) => material switch
        {
            MaterialId.Regolith => "Regolith",
            MaterialId.Stone => "Stein",
            MaterialId.Soil => "Erde",
            MaterialId.IronOre => "Eisenerz",
            MaterialId.CopperOre => "Kupfererz",
            MaterialId.Quartz => "Quarz",
            MaterialId.Water => "Wasser",
            MaterialId.Biomass => "Biomasse",
            _ => "Luft"
        };

        /// <summary>True for materials of the solid voxel planet, which define the global extraction quota.</summary>
        public static bool IsTerrain(this MaterialId material) => material >= MaterialId.Regolith && material <= MaterialId.Quartz;
    }
}
