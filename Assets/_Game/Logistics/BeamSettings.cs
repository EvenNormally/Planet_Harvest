using UnityEngine;

namespace PlanetHarvest.Logistics
{
    /// <summary>Tuning of the tractor-beam masts. Prices are placeholders.</summary>
    [CreateAssetMenu(menuName = "Planet Harvest/Traktorstrahl")]
    public sealed class BeamSettings : ScriptableObject
    {
        [SerializeField, Min(0)] private int hopperPrice = 30;
        [SerializeField, Min(0)] private int mastPrice = 40;
        [SerializeField, Min(0), Tooltip("Abgeschlossene Aufträge bis zur Freischaltung")] private int unlockAfterContracts = 1;
        [SerializeField, Range(3, 16)] private float range = 8;
        [SerializeField, Range(1, 4)] private float minimumSpacing = 2;
        [SerializeField, Range(1, 8)] private int slotsPerSegment = 4;
        [SerializeField, Range(.1f, 2)] private float stepSeconds = .5f;
        [SerializeField, Range(1, 24)] private int hopperCubicMetres = 6;
        [SerializeField, Range(2, 32)] private int maximumMasts = 12;

        public int HopperPrice => hopperPrice;
        public int MastPrice => mastPrice;
        public int UnlockAfterContracts => unlockAfterContracts;
        public float Range => range;
        public float MinimumSpacing => minimumSpacing;
        public int SlotsPerSegment => slotsPerSegment;
        public float StepSeconds => stepSeconds;
        public int HopperCubicMetres => hopperCubicMetres;
        public int MaximumMasts => maximumMasts;
    }
}
