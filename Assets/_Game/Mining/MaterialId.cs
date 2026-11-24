namespace PlanetHarvest.Mining
{
    public enum MaterialId
    {
        None = 0,
        Regolith = 1,
        Stone = 2,
        Soil = 3,
        IronOre = 4
    }

    public static class HarvestMaterial
    {
        public static string Name(this MaterialId material) => material switch
        {
            MaterialId.Regolith => "Regolith",
            MaterialId.Stone => "Stein",
            MaterialId.Soil => "Erde",
            MaterialId.IronOre => "Eisenerz",
            _ => "Luft"
        };
    }
}
