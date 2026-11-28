namespace PlanetHarvest.Progression
{
    /// <summary>
    /// End condition of the campaign (chapter 6): every contract fulfilled and the solid planet delivered to orbit.
    /// The threshold leaves room for the residue the scanner cannot reach; it is configuration, not a guess.
    /// </summary>
    public static class ExpeditionEnding
    {
        public const double OrbitThreshold = .995;

        public static bool IsComplete(PlanetProgress progress, ContractLedger contracts, double threshold = OrbitThreshold) =>
            contracts.AllComplete && progress.OrbitFraction >= threshold;
    }
}
