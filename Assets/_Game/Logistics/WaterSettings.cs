using UnityEngine;

namespace PlanetHarvest.Logistics
{
    /// <summary>Reservoir size and pump tuning. Placeholder values for play tests.</summary>
    [CreateAssetMenu(menuName = "Planet Harvest/Wasser und Pumpe")]
    public sealed class WaterSettings : ScriptableObject
    {
        [SerializeField, Range(1, 2000), Tooltip("Wasservolumen des Bergsees in m³")] private int lakeCubicMetres = 60;
        [SerializeField, Min(0)] private int pumpPrice = 60;
        [SerializeField, Min(0)] private int unlockAfterContracts = 1;
        [SerializeField, Range(.2f, 10)] private float pumpSeconds = 1.5f;
        [SerializeField, Range(1, 40)] private int tankCubicMetres = 10;
        [SerializeField, Range(2, 12)] private float unloadRange = 7;
        [SerializeField, Range(1, 4), Tooltip("Biomasse je geerntete Pflanze in m³")] private int biomassPerPlant = 1;

        public int LakeCubicMetres => lakeCubicMetres;
        public int PumpPrice => pumpPrice;
        public int UnlockAfterContracts => unlockAfterContracts;
        public float PumpSeconds => pumpSeconds;
        public int TankCubicMetres => tankCubicMetres;
        public float UnloadRange => unloadRange;
        public int BiomassPerPlant => Mathf.Max(1, biomassPerPlant);
    }
}
