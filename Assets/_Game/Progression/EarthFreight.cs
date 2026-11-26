using System;
using PlanetHarvest.Mining;

namespace PlanetHarvest.Progression
{
    public sealed class EarthFreight
    {
        public const int SoilGoal = 20;
        public const int StoneGoal = 12;
        public const float LegSeconds = 3;
        private const long Unit = HarvestInventory.CubicMetre;
        private readonly HarvestInventory soil = new HarvestInventory(SoilGoal * Unit, MaterialId.Soil);
        private readonly HarvestInventory stone = new HarvestInventory(StoneGoal * Unit, MaterialId.Stone);
        public long SoilDelivered { get; private set; }
        public long StoneDelivered { get; private set; }
        public int CompletedLevels => SoilDelivered < soil.Capacity ? 0 : StoneDelivered < stone.Capacity ? 1 : 2;
        public MaterialId Flying { get; private set; }
        public bool Returning { get; private set; }
        public float LegProgress { get; private set; }

        public HarvestInventory Container(MaterialId type) => type switch
        {
            MaterialId.Soil => soil,
            MaterialId.Stone => stone,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        public long Load(HarvestInventory pack, MaterialId type)
        {
            long moved = pack.TransferTo(Container(type), type);
            if (moved > 0 && Flying == MaterialId.None) LaunchNext();
            return moved;
        }

        private void LaunchNext()
        {
            Flying = soil.Free == 0 ? MaterialId.Soil : stone.Free == 0 ? MaterialId.Stone : MaterialId.None;
            if (Flying != MaterialId.None) Container(Flying).InTransit = true;
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
                LegProgress = 0;
                var container = Container(Flying);
                if (!Returning)
                {
                    if (Flying == MaterialId.Soil) SoilDelivered += container.Amount;
                    else StoneDelivered += container.Amount;
                    container.Empty();
                    Returning = true;
                }
                else
                {
                    container.InTransit = false;
                    Returning = false;
                    LaunchNext();
                }
            }
        }
    }
}
