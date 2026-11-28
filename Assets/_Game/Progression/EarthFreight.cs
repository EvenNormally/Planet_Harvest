using System;
using PlanetHarvest.Mining;

namespace PlanetHarvest.Progression
{
    [Serializable]
    public sealed class FreightSnapshot
    {
        /// <summary>Material order of the version 1 save format, which stored arrays per order index.</summary>
        private static readonly MaterialId[] VersionOneOrder =
            { MaterialId.Soil, MaterialId.Stone, MaterialId.IronOre, MaterialId.CopperOre, MaterialId.Quartz };

        public long credits;
        /// <summary>Version 1 only: per order index. Empty from version 2 on.</summary>
        public long[] delivered, loads;
        /// <summary>Version 2: indexed by <see cref="MaterialId"/>, length <see cref="HarvestInventory.MaterialSlots"/>.</summary>
        public long[] deliveredByMaterial, loadsByMaterial;
        public int rewardedContracts;
        public MaterialId flying;
        public bool returning;
        public float progress;

        /// <summary>
        /// Converts the version 1 layout. Contracts already satisfied by the old totals count as rewarded:
        /// the old save never paid completion bonuses, and loading must not mint credits retroactively.
        /// </summary>
        public void UpgradeFromVersionOne(FreightProgram program)
        {
            if (delivered == null || loads == null || delivered.Length != VersionOneOrder.Length || loads.Length != VersionOneOrder.Length)
                throw new System.IO.InvalidDataException("Ungültiger Liftzustand (Version 1).");
            deliveredByMaterial = new long[HarvestInventory.MaterialSlots];
            loadsByMaterial = new long[HarvestInventory.MaterialSlots];
            for (int i = 0; i < VersionOneOrder.Length; i++)
            {
                if (delivered[i] < 0 || loads[i] < 0) throw new System.IO.InvalidDataException("Ungültiger Frachtbestand.");
                deliveredByMaterial[(int)VersionOneOrder[i]] = delivered[i];
                loadsByMaterial[(int)VersionOneOrder[i]] = loads[i];
            }
            delivered = loads = null;
            rewardedContracts = ContractLedger.CountCompleted(program, deliveredByMaterial);
        }
    }

    /// <summary>
    /// Elevator logistics: one container per deliverable material, a single lift cabin and the orbit ledger.
    /// Credits are paid per delivered volume on arrival plus a one-time bonus per completed contract.
    /// </summary>
    public sealed class EarthFreight
    {
        public const float LegSeconds = 3;

        private readonly HarvestInventory[] bins;
        public FreightProgram Program { get; }
        public ContractLedger Contracts { get; }
        public int Revision { get; private set; }
        public long Credits { get; private set; }
        public int CompletedContracts => Contracts.Completed;
        public MaterialId Flying { get; private set; }
        public bool Returning { get; private set; }
        public float LegProgress { get; private set; }

        public EarthFreight(FreightProgram program)
        {
            Program = program ?? throw new ArgumentNullException(nameof(program));
            Contracts = new ContractLedger(program);
            bins = new HarvestInventory[program.Materials.Count];
            for (int i = 0; i < bins.Length; i++)
                bins[i] = new HarvestInventory(program.Materials[i].ContainerCapacity, program.Materials[i].Id);
        }

        private int Index(MaterialId type)
        {
            int index = Program.IndexOf(type);
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(type));
            return index;
        }

        public HarvestInventory Container(MaterialId type) => bins[Index(type)];

        public long Delivered(MaterialId material) => Contracts.Delivered(material);
        public long TotalDelivered => Contracts.TotalDelivered;

        /// <summary>Volume currently travelling up in the cabin (not yet in orbit).</summary>
        public long InFlight => Flying != MaterialId.None && !Returning ? Container(Flying).Amount : 0;

        /// <summary>Volume waiting in containers at the elevator, excluding the cabin load.</summary>
        public long AtDock
        {
            get { long sum = 0; foreach (var bin in bins) sum += bin.Amount; return sum - InFlight; }
        }

        public FreightSnapshot Capture()
        {
            var state = new FreightSnapshot
            {
                credits = Credits, flying = Flying, returning = Returning, progress = LegProgress,
                deliveredByMaterial = Contracts.CaptureDelivered(), loadsByMaterial = new long[HarvestInventory.MaterialSlots],
                rewardedContracts = Contracts.Rewarded
            };
            for (int i = 0; i < bins.Length; i++) state.loadsByMaterial[(int)Program.Materials[i].Id] = bins[i].Amount;
            return state;
        }

        public void Restore(FreightSnapshot state)
        {
            if (state?.loadsByMaterial == null || state.loadsByMaterial.Length != HarvestInventory.MaterialSlots)
                throw new ArgumentException("Ungültiger Liftzustand.", nameof(state));
            Contracts.Restore(state.deliveredByMaterial, state.rewardedContracts);
            for (int i = 0; i < bins.Length; i++)
            {
                var material = Program.Materials[i].Id;
                bins[i].InTransit = false;
                bins[i].Empty();
                bins[i].TryAdd(material, state.loadsByMaterial[(int)material]);
                bins[i].InTransit = material == state.flying;
            }
            Credits = state.credits;
            Flying = state.flying;
            Returning = state.returning;
            LegProgress = state.progress;
            Revision++;
        }

        internal bool Spend(int amount)
        {
            if (amount < 0 || Credits < amount) return false;
            Credits -= amount;
            Revision++;
            return true;
        }

        public long Load(HarvestInventory pack, MaterialId type)
        {
            long moved = pack.TransferTo(Container(type), type);
            if (moved > 0)
            {
                Revision++;
                if (Flying == MaterialId.None) LaunchNext();
            }
            return moved;
        }

        public bool Dispatch(MaterialId type)
        {
            var bin = Container(type);
            if (Flying != MaterialId.None || bin.Amount == 0) return false;
            Flying = type;
            bin.InTransit = true;
            Revision++;
            return true;
        }

        private void LaunchNext()
        {
            Flying = MaterialId.None;
            for (int i = 0; i < bins.Length; i++)
            {
                if (bins[i].Free != 0) continue;
                Flying = Program.Materials[i].Id;
                bins[i].InTransit = true;
                break;
            }
        }

        public void Advance(float seconds)
        {
            while (seconds > 0 && Flying != MaterialId.None)
            {
                float remaining = (1 - LegProgress) * LegSeconds;
                if (seconds < remaining)
                {
                    LegProgress += seconds / LegSeconds;
                    return;
                }
                seconds -= remaining;
                Revision++;
                LegProgress = 0;
                int i = Index(Flying);
                if (!Returning)
                {
                    Arrive(i);
                    Returning = true;
                }
                else
                {
                    bins[i].InTransit = false;
                    Returning = false;
                    LaunchNext();
                }
            }
        }

        private void Arrive(int index)
        {
            var spec = Program.Materials[index];
            var bin = bins[index];
            long before = Contracts.Delivered(spec.Id);
            long bonus = Contracts.Record(spec.Id, bin.Amount);
            Credits += spec.CreditsFor(before + bin.Amount) - spec.CreditsFor(before) + bonus;
            bin.Empty();
        }
    }
}
