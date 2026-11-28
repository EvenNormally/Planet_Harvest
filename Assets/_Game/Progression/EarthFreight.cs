using System;
using PlanetHarvest.Mining;

namespace PlanetHarvest.Progression
{
    public readonly struct FreightOrder
    {
        public readonly MaterialId Material;
        public readonly string Title;
        public readonly int Volume;
        public readonly int Credits;

        public FreightOrder(MaterialId material, string title, int volume, int credits)
        {
            Material = material;
            Title = title;
            Volume = volume;
            Credits = credits;
        }
    }

    [Serializable]
    public sealed class FreightSnapshot
    {
        public long credits;
        public long[] delivered, loads;
        public MaterialId flying;
        public bool returning;
        public float progress;
    }

    public sealed class EarthFreight
    {
        public const int SoilGoal = 20;
        public const int StoneGoal = 12;
        public const float LegSeconds = 3;
        private const long Unit = HarvestInventory.CubicMetre;
        private static readonly FreightOrder[] Orders =
        {
            new FreightOrder(MaterialId.Soil, "Bodenprobe", SoilGoal, 20),
            new FreightOrder(MaterialId.Stone, "Gesteinslieferung", StoneGoal, 30),
            new FreightOrder(MaterialId.IronOre, "Tragwerk", 4, 45),
            new FreightOrder(MaterialId.CopperOre, "Stromleitungen", 5, 60),
            new FreightOrder(MaterialId.Quartz, "Orbitalsensoren", 3, 80)
        };

        private readonly HarvestInventory[] bins = new HarvestInventory[Orders.Length];
        private readonly long[] delivered = new long[Orders.Length];
        public long SoilDelivered => delivered[0];
        public long StoneDelivered => delivered[1];
        public int Revision { get; private set; }
        public long Credits { get; private set; }
        public int CompletedLevels { get; private set; }
        public static int LevelCount => Orders.Length;
        public static FreightOrder Order(int index) => Orders[index];
        public MaterialId Flying { get; private set; }
        public bool Returning { get; private set; }
        public float LegProgress { get; private set; }

        public EarthFreight()
        {
            for (int i = 0; i < Orders.Length; i++)
                bins[i] = new HarvestInventory(Orders[i].Volume * Unit, Orders[i].Material);
        }

        private static int Index(MaterialId type)
        {
            for (int i = 0; i < Orders.Length; i++)
                if (Orders[i].Material == type) return i;
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        public HarvestInventory Container(MaterialId type) => bins[Index(type)];

        public long Delivered(MaterialId material) => delivered[Index(material)];
        public long TotalDelivered
        {
            get { long sum = 0; foreach (var amount in delivered) sum += amount; return sum; }
        }

        public FreightSnapshot Capture()
        {
            var state = new FreightSnapshot { credits = Credits, flying = Flying, returning = Returning,
                progress = LegProgress, delivered = (long[])delivered.Clone(), loads = new long[bins.Length] };
            for (int i = 0; i < bins.Length; i++) state.loads[i] = bins[i].Amount;
            return state;
        }

        public void Restore(FreightSnapshot state)
        {
            CompletedLevels = 0;
            for (int i = 0; i < bins.Length; i++)
            {
                bins[i].InTransit = false;
                bins[i].Empty();
                bins[i].TryAdd(Orders[i].Material, state.loads[i]);
                bins[i].InTransit = Orders[i].Material == state.flying;
                delivered[i] = state.delivered[i];
                if (CompletedLevels == i && delivered[i] >= bins[i].Capacity) CompletedLevels++;
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
                Flying = Orders[i].Material;
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
                    long previousValue = delivered[i] * Orders[i].Credits / bins[i].Capacity;
                    delivered[i] += bins[i].Amount;
                    Credits += delivered[i] * Orders[i].Credits / bins[i].Capacity - previousValue;
                    bins[i].Empty();
                    while (CompletedLevels < bins.Length && delivered[CompletedLevels] >= bins[CompletedLevels].Capacity)
                        CompletedLevels++;
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
    }
}
