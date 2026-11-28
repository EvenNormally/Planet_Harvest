using PlanetHarvest.Mining;

namespace PlanetHarvest.Progression
{
    /// <summary>
    /// Global extraction quota. Only volume that actually arrived in orbit counts; cargo in the cabin,
    /// in elevator containers and in backpacks is reported separately. All volumes in cm³.
    /// </summary>
    public readonly struct PlanetProgress
    {
        public readonly long InitialVolume;
        public readonly long InOrbit;
        public readonly long InFlight;
        public readonly long AtElevator;
        public readonly long Carried;

        public PlanetProgress(long initialVolume, long inOrbit, long inFlight, long atElevator, long carried)
        {
            InitialVolume = initialVolume;
            InOrbit = inOrbit;
            InFlight = inFlight;
            AtElevator = atElevator;
            Carried = carried;
        }

        /// <summary>Share of the original solid planet that reached orbit, 0..1.</summary>
        public double OrbitFraction => InitialVolume <= 0 ? 0 : System.Math.Min(1, (double)InOrbit / InitialVolume);

        /// <summary>Water and biomass are delivered too but do not count towards the solid-planet quota.</summary>
        public static PlanetProgress Measure(int initialSolidCells, EarthFreight freight, long carried)
        {
            long terrain = 0;
            foreach (var material in freight.Program.Materials)
                if (material.Id.IsTerrain()) terrain += freight.Delivered(material.Id);
            return new PlanetProgress(initialSolidCells * HarvestInventory.CubicMetre, terrain,
                freight.InFlight, freight.AtDock, carried);
        }
    }
}
