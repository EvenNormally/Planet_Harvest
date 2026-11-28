using System;
using PlanetHarvest.Mining;

namespace PlanetHarvest.Progression
{
    /// <summary>
    /// Tracks orbit deliveries per material and derives contract completion from them.
    /// Every delivered cm³ is assigned to at most one contract, in contract order: a contract
    /// consumes exactly its minimum amounts, anything beyond stays available for later contracts.
    /// Deliveries between stages are therefore never wasted and never counted twice.
    /// </summary>
    public sealed class ContractLedger
    {
        private readonly FreightProgram program;
        private readonly long[] delivered = new long[HarvestInventory.MaterialSlots];
        private readonly long[] unassigned = new long[HarvestInventory.MaterialSlots];

        /// <summary>Number of contracts whose minimums are met, counted from the first.</summary>
        public int Completed { get; private set; }
        /// <summary>Number of contracts whose completion bonus has already been paid.</summary>
        public int Rewarded { get; private set; }
        public int Count => program.Contracts.Count;
        public bool AllComplete => Completed == Count;
        public ContractSpec Current => AllComplete ? null : program.Contracts[Completed];

        public ContractLedger(FreightProgram program)
        {
            this.program = program ?? throw new ArgumentNullException(nameof(program));
        }

        public long Delivered(MaterialId material) => HarvestInventory.IsResource(material) ? delivered[(int)material] : 0;

        public long TotalDelivered
        {
            get { long sum = 0; foreach (long amount in delivered) sum += amount; return sum; }
        }

        /// <summary>Volume credited towards the current contract for one material, capped at its minimum.</summary>
        public long Progress(MaterialId material)
        {
            var contract = Current;
            if (contract == null) return 0;
            foreach (var requirement in contract.Requirements)
                if (requirement.Material == material) return Math.Min(requirement.Volume, unassigned[(int)material]);
            return 0;
        }

        /// <summary>Total volume credited towards the current contract across all its materials.</summary>
        public long CurrentProgress
        {
            get
            {
                var contract = Current;
                if (contract == null) return 0;
                long sum = 0;
                foreach (var requirement in contract.Requirements) sum += Progress(requirement.Material);
                return sum;
            }
        }

        /// <summary>Books an orbit arrival. Returns the completion bonus that became due by it.</summary>
        public long Record(MaterialId material, long volume)
        {
            if (!program.Contains(material)) throw new ArgumentOutOfRangeException(nameof(material));
            if (volume < 0) throw new ArgumentOutOfRangeException(nameof(volume));
            if (volume == 0) return 0;
            delivered[(int)material] = checked(delivered[(int)material] + volume);
            Recount();
            return CollectBonus();
        }

        /// <summary>Restores totals from a save. <paramref name="rewarded"/> must not exceed the completed count.</summary>
        public void Restore(long[] deliveredByMaterial, int rewarded)
        {
            if (deliveredByMaterial == null || deliveredByMaterial.Length != delivered.Length)
                throw new ArgumentException("Ungültige Liefermengen.", nameof(deliveredByMaterial));
            for (int i = 0; i < delivered.Length; i++)
            {
                if (deliveredByMaterial[i] < 0) throw new ArgumentOutOfRangeException(nameof(deliveredByMaterial));
                delivered[i] = deliveredByMaterial[i];
            }
            Recount();
            if (rewarded < 0 || rewarded > Completed) throw new ArgumentOutOfRangeException(nameof(rewarded));
            Rewarded = rewarded;
        }

        public long[] CaptureDelivered() => (long[])delivered.Clone();

        /// <summary>Completed contracts for given totals, without changing any ledger.</summary>
        public static int CountCompleted(FreightProgram program, long[] deliveredByMaterial)
        {
            var probe = new ContractLedger(program);
            probe.Restore(deliveredByMaterial, 0);
            return probe.Completed;
        }

        private long CollectBonus()
        {
            long bonus = 0;
            while (Rewarded < Completed) bonus += program.Contracts[Rewarded++].BonusCredits;
            return bonus;
        }

        private void Recount()
        {
            Array.Copy(delivered, unassigned, delivered.Length);
            Completed = 0;
            foreach (var contract in program.Contracts)
            {
                foreach (var requirement in contract.Requirements)
                    if (unassigned[(int)requirement.Material] < requirement.Volume) return;
                foreach (var requirement in contract.Requirements)
                    unassigned[(int)requirement.Material] -= requirement.Volume;
                Completed++;
            }
        }
    }
}
