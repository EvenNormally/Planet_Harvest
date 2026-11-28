using UnityEngine;

namespace PlanetHarvest.Planets
{
    [CreateAssetMenu(menuName = "Planet Harvest/Erdähnlicher Planet")]
    public sealed class EarthPlanetSettings : ScriptableObject
    {
        [SerializeField] private string planetId = "earth-starter";
        [SerializeField] private int seed = 7219;
        [SerializeField, Range(26, 64)] private int radius = 48;
        [SerializeField, Range(0, 8)] private float hillHeight = 5;
        [SerializeField, Range(1, 5)] private float hillFrequency = 3;
        [SerializeField, Range(2.3f, 3.5f)] private float tunnelRadius = 2.8f;
        [SerializeField, Range(8, 14)] private float caveDepth = 10;
        [SerializeField, Range(1, 4)] private float soilDepth = 2;
        [SerializeField, Range(5, 10)] private float minimumOreDepth = 7;
        [SerializeField, Range(0.05f, 0.5f)] private float oreClusterChance = 0.28f;

        [SerializeField, Range(8, 24)] private int chunkCells = 16;
        public int ChunkCells => Mathf.Clamp(chunkCells, 8, 24);

        public string PlanetId => planetId;
        public EarthPlanetField CreateField() => new EarthPlanetField(seed, radius, hillHeight,
            hillFrequency, tunnelRadius, caveDepth, soilDepth, minimumOreDepth, oreClusterChance);
    }
}
