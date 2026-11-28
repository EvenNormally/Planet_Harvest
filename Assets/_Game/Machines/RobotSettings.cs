using UnityEngine;

namespace PlanetHarvest.Machines
{
    /// <summary>Tuning of the drill robot. Values are placeholders for play tests.</summary>
    [CreateAssetMenu(menuName = "Planet Harvest/Bohrroboter")]
    public sealed class RobotSettings : ScriptableObject
    {
        [SerializeField, Min(0)] private int price = 80;
        [SerializeField, Min(0)] private int unlockAfterContracts = 1;
        [SerializeField, Range(2, 32)] private int width = 8;
        [SerializeField, Range(2, 32)] private int length = 8;
        [SerializeField, Range(1, 16)] private int depth = 4;
        [SerializeField, Range(1, 48)] private int hopperCubicMetres = 12;
        [SerializeField, Range(.1f, 5)] private float strokeSeconds = .8f;
        [SerializeField, Range(.5f, 2)] private float cutScale = 1.2f;
        [SerializeField, Range(4, 30), Tooltip("Kein Arbeitsbereich näher an Lift oder Werkbank")] private float protectedRadius = 11;
        [SerializeField, Range(2, 12)] private float unloadRange = 7;

        public int Price => price;
        public int UnlockAfterContracts => unlockAfterContracts;
        public int Width => width;
        public int Length => length;
        public int Depth => depth;
        public int HopperCubicMetres => hopperCubicMetres;
        public float StrokeSeconds => strokeSeconds;
        public float CutScale => cutScale;
        public float ProtectedRadius => protectedRadius;
        public float UnloadRange => unloadRange;
    }
}
