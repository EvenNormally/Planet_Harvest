using System;
using PlanetHarvest.Mining;

namespace PlanetHarvest.Logistics
{
    /// <summary>A finite body of water. No fluid simulation: only the remaining volume is tracked.</summary>
    public sealed class WaterReservoir
    {
        public long Capacity { get; }
        public long Remaining { get; private set; }
        public double Fill => Capacity == 0 ? 0 : (double)Remaining / Capacity;
        public long Drawn => Capacity - Remaining;

        public WaterReservoir(int cubicMetres)
        {
            if (cubicMetres < 1) throw new ArgumentOutOfRangeException(nameof(cubicMetres));
            Capacity = cubicMetres * HarvestInventory.CubicMetre;
            Remaining = Capacity;
        }

        /// <summary>Takes up to <paramref name="volume"/> and returns what was actually available.</summary>
        public long Draw(long volume)
        {
            long taken = Math.Max(0, Math.Min(volume, Remaining));
            Remaining -= taken;
            return taken;
        }

        public void Restore(long remaining)
        {
            if (remaining < 0 || remaining > Capacity) throw new ArgumentOutOfRangeException(nameof(remaining));
            Remaining = remaining;
        }
    }

    public enum PumpState
    {
        NotBuilt = 0,
        Stopped = 1,
        Pumping = 2,
        /// <summary>Tank full; waits for unloading.</summary>
        Full = 3,
        /// <summary>Reservoir empty.</summary>
        Dry = 4
    }

    [Serializable]
    public sealed class WaterSnapshot
    {
        public long reservoirRemaining = -1;
        public PumpState pump;
        public long tank;
        public float timer;
    }

    /// <summary>
    /// Pump at a reservoir: moves water in whole cubic metres from the reservoir into its tank. Water is only
    /// ever moved between the reservoir, the tank and inventories, so the total stays constant.
    /// </summary>
    public sealed class WaterPump
    {
        public WaterReservoir Reservoir { get; }
        public HarvestInventory Tank { get; }
        public PumpState State { get; private set; }
        public float IntervalSeconds { get; }
        public int Revision { get; private set; }
        private float timer;

        public WaterPump(WaterReservoir reservoir, int tankCubicMetres, float intervalSeconds)
        {
            Reservoir = reservoir ?? throw new ArgumentNullException(nameof(reservoir));
            if (!(intervalSeconds > 0)) throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            Tank = new HarvestInventory(tankCubicMetres * HarvestInventory.CubicMetre, MaterialId.Water);
            IntervalSeconds = intervalSeconds;
        }

        public bool Built => State != PumpState.NotBuilt;

        public void Build()
        {
            if (Built) return;
            State = PumpState.Stopped;
            Revision++;
        }

        public void Toggle()
        {
            if (!Built) return;
            State = State == PumpState.Stopped ? PumpState.Pumping : State == PumpState.Pumping || State == PumpState.Full
                ? PumpState.Stopped : State;
            timer = 0;
            Revision++;
        }

        public void Advance(float seconds)
        {
            if (!(seconds > 0)) return;
            if (State == PumpState.Full && Tank.Free >= HarvestInventory.CubicMetre) { State = PumpState.Pumping; Revision++; }
            if (State != PumpState.Pumping) return;
            timer += seconds;
            while (timer >= IntervalSeconds && State == PumpState.Pumping)
            {
                timer -= IntervalSeconds;
                if (Reservoir.Remaining <= 0) { State = PumpState.Dry; timer = 0; }
                else if (Tank.Free < HarvestInventory.CubicMetre) { State = PumpState.Full; timer = 0; }
                else
                {
                    long volume = Reservoir.Draw(HarvestInventory.CubicMetre);
                    Tank.TryAdd(MaterialId.Water, volume);
                }
                Revision++;
            }
        }

        public WaterSnapshot Capture() => new WaterSnapshot
            { reservoirRemaining = Reservoir.Remaining, pump = State, tank = Tank.Amount, timer = timer };

        public void Restore(WaterSnapshot state)
        {
            Tank.Empty();
            if (state == null || state.reservoirRemaining < 0)
            {
                Reservoir.Restore(Reservoir.Capacity);
                State = PumpState.NotBuilt;
                timer = 0;
            }
            else
            {
                Reservoir.Restore(state.reservoirRemaining);
                State = state.pump;
                timer = state.timer;
                if (state.tank > 0) Tank.TryAdd(MaterialId.Water, state.tank);
            }
            Revision++;
        }

        public static void Validate(WaterSnapshot state, long reservoirCapacity, long tankCapacity, float interval)
        {
            if (state == null || state.reservoirRemaining < 0) return;
            if (state.reservoirRemaining > reservoirCapacity || state.tank < 0 || state.tank > tankCapacity ||
                (uint)state.pump > (uint)PumpState.Dry || !(state.timer >= 0 && state.timer < interval) ||
                (state.pump == PumpState.NotBuilt && state.tank != 0))
                throw new System.IO.InvalidDataException("Ungültiger Pumpenzustand.");
        }

        /// <summary>Water removed from the reservoir according to a snapshot (0 when absent).</summary>
        public static long Drawn(WaterSnapshot state, long reservoirCapacity) =>
            state == null || state.reservoirRemaining < 0 ? 0 : reservoirCapacity - state.reservoirRemaining;
    }
}
