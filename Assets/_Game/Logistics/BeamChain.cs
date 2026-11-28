using System;
using System.Collections.Generic;
using PlanetHarvest.Mining;

namespace PlanetHarvest.Logistics
{
    /// <summary>Receiver at the head of a beam chain. Returns the volume it actually took.</summary>
    public interface IBeamSink
    {
        long Accept(HarvestInventory source, MaterialId material, long volume);
    }

    [Serializable]
    public sealed class BeamChainSnapshot
    {
        public int segments;
        /// <summary>Material per slot, index 0 at the hopper.</summary>
        public MaterialId[] slots;
        /// <summary>Hopper contents indexed by <see cref="MaterialId"/>.</summary>
        public long[] hopper;
        public float timer;
        public bool blocked;
    }

    /// <summary>
    /// Tractor-beam transport as pure logic. Material travels as lumps of exactly 1 m³ through a row of slots;
    /// each segment between two masts has the same number of slots. The volume of every lump is held by a real
    /// inventory (<see cref="Lane"/>), so material can only move, never appear or vanish. A lump advances only
    /// into a free slot; when the sink refuses, the head stays and everything behind it backs up.
    /// </summary>
    public sealed class BeamChain
    {
        public const long Lump = HarvestInventory.CubicMetre;
        private readonly List<MaterialId> slots = new List<MaterialId>();
        private readonly List<bool> moved = new List<bool>();
        private float timer;

        public HarvestInventory Hopper { get; }
        /// <summary>Holds the volume of all lumps currently in the beam.</summary>
        public HarvestInventory Lane { get; }
        public int SlotsPerSegment { get; }
        public float StepSeconds { get; }
        public int Segments => slots.Count / SlotsPerSegment;
        public int SlotCount => slots.Count;
        public float StepProgress => timer / StepSeconds;
        /// <summary>True when the head lump could not be delivered on the last step.</summary>
        public bool Blocked { get; private set; }
        public int Revision { get; private set; }
        public long Delivered { get; private set; }

        public BeamChain(int hopperCubicMetres, int slotsPerSegment, float stepSeconds)
        {
            if (hopperCubicMetres < 1) throw new ArgumentOutOfRangeException(nameof(hopperCubicMetres));
            if (slotsPerSegment < 1) throw new ArgumentOutOfRangeException(nameof(slotsPerSegment));
            if (!(stepSeconds > 0)) throw new ArgumentOutOfRangeException(nameof(stepSeconds));
            SlotsPerSegment = slotsPerSegment;
            StepSeconds = stepSeconds;
            Hopper = new HarvestInventory(hopperCubicMetres * Lump);
            Lane = new HarvestInventory(Lump);
        }

        public MaterialId Slot(int index) => slots[index];
        /// <summary>True if the lump in this slot arrived during the current step (for interpolation).</summary>
        public bool MovedIn(int index) => moved[index];

        public void AddSegment()
        {
            for (int i = 0; i < SlotsPerSegment; i++) { slots.Add(MaterialId.None); moved.Add(false); }
            Lane.Resize(Math.Max(Lump, slots.Count * Lump));
            Revision++;
        }

        /// <summary>Removes the last segment if it carries no lump.</summary>
        public bool TryRemoveSegment()
        {
            if (Segments == 0) return false;
            int start = slots.Count - SlotsPerSegment;
            for (int i = start; i < slots.Count; i++) if (slots[i] != MaterialId.None) return false;
            slots.RemoveRange(start, SlotsPerSegment);
            moved.RemoveRange(start, SlotsPerSegment);
            Lane.Resize(Math.Max(Lump, slots.Count * Lump));
            Revision++;
            return true;
        }

        public int LumpCount
        {
            get { int count = 0; foreach (var slot in slots) if (slot != MaterialId.None) count++; return count; }
        }

        public void Advance(float seconds, IBeamSink sink)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds)) return;
            timer += seconds;
            while (timer >= StepSeconds)
            {
                timer -= StepSeconds;
                Step(sink);
            }
        }

        private void Step(IBeamSink sink)
        {
            for (int i = 0; i < moved.Count; i++) moved[i] = false;
            int last = slots.Count - 1;
            if (last < 0) return;
            Blocked = false;
            if (slots[last] != MaterialId.None)
            {
                if (sink != null && sink.Accept(Lane, slots[last], Lump) == Lump)
                {
                    slots[last] = MaterialId.None;
                    Delivered += Lump;
                }
                else Blocked = true;
            }
            for (int i = last - 1; i >= 0; i--)
            {
                if (slots[i] == MaterialId.None || slots[i + 1] != MaterialId.None) continue;
                slots[i + 1] = slots[i];
                slots[i] = MaterialId.None;
                moved[i + 1] = true;
            }
            if (slots[0] == MaterialId.None)
            {
                for (int m = 1; m < HarvestInventory.MaterialSlots; m++)
                {
                    var material = (MaterialId)m;
                    if (Hopper.AmountOf(material) < Lump) continue;
                    if (Hopper.TransferTo(Lane, material, Lump) != Lump) break;
                    slots[0] = material;
                    moved[0] = true;
                    break;
                }
            }
            Revision++;
        }

        public BeamChainSnapshot Capture()
        {
            var state = new BeamChainSnapshot
            {
                segments = Segments, slots = slots.ToArray(), hopper = new long[HarvestInventory.MaterialSlots],
                timer = timer, blocked = Blocked
            };
            for (int m = 0; m < state.hopper.Length; m++) state.hopper[m] = Hopper.AmountOf((MaterialId)m);
            return state;
        }

        public void Restore(BeamChainSnapshot state)
        {
            Validate(state, Hopper.Capacity, SlotsPerSegment, StepSeconds);
            slots.Clear();
            moved.Clear();
            Hopper.Empty();
            Lane.Empty();
            Lane.Resize(Math.Max(Lump, state.slots.Length * Lump));
            foreach (var slot in state.slots)
            {
                slots.Add(slot);
                moved.Add(false);
                if (slot != MaterialId.None) Lane.TryAdd(slot, Lump);
            }
            for (int m = 1; m < state.hopper.Length; m++)
                if (state.hopper[m] > 0) Hopper.TryAdd((MaterialId)m, state.hopper[m]);
            timer = state.timer;
            Blocked = state.blocked;
            Revision++;
        }

        public static void Validate(BeamChainSnapshot state, long hopperCapacity, int slotsPerSegment, float stepSeconds)
        {
            if (state?.slots == null || state.hopper == null || state.hopper.Length != HarvestInventory.MaterialSlots ||
                state.segments < 0 || state.slots.Length != state.segments * slotsPerSegment ||
                !(state.timer >= 0 && state.timer < stepSeconds) || state.hopper[0] != 0)
                throw new System.IO.InvalidDataException("Ungültiger Traktorstrahl.");
            long total = 0;
            foreach (long amount in state.hopper)
            {
                if (amount < 0 || amount > hopperCapacity - total) throw new System.IO.InvalidDataException("Ungültiger Trichterinhalt.");
                total += amount;
            }
            foreach (var slot in state.slots)
                if (slot != MaterialId.None && !HarvestInventory.IsResource(slot))
                    throw new System.IO.InvalidDataException("Ungültiger Materialklumpen.");
        }

        /// <summary>Volume of one material held by hopper and beam, for the material balance.</summary>
        public static long Holding(BeamChainSnapshot state, MaterialId material)
        {
            if (state == null) return 0;
            long volume = state.hopper[(int)material];
            foreach (var slot in state.slots) if (slot == material) volume += Lump;
            return volume;
        }
    }
}
