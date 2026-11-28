using System;
using System.Collections.Generic;
using PlanetHarvest.Mining;
using UnityEngine;

namespace PlanetHarvest.Machines
{
    public enum RobotState
    {
        Stopped = 0,
        Working = 1,
        /// <summary>Hopper full; waits for unloading.</summary>
        Full = 2,
        /// <summary>Work area cleared.</summary>
        Finished = 3
    }

    /// <summary>
    /// Box-shaped work area on the planet surface in planet-local cell space. The box is aligned with the local
    /// up direction at its centre; it is excavated in horizontal tranches from the top down.
    /// </summary>
    public sealed class RobotWorkArea
    {
        public const float Above = 2f;
        private readonly List<Vector3Int> contacts = new List<Vector3Int>();
        public Vector3Int Centre { get; }
        public int Width { get; }
        public int Length { get; }
        public int Depth { get; }
        public Vector3 Up { get; }
        public Vector3 Right { get; }
        public Vector3 Forward { get; }
        public IReadOnlyList<Vector3Int> Contacts => contacts;
        public int Tranches => Depth + Mathf.CeilToInt(Above);

        public RobotWorkArea(Vector3Int centre, int width, int length, int depth)
        {
            if (width < 2 || length < 2 || depth < 1 || width > 32 || length > 32 || depth > 16)
                throw new ArgumentOutOfRangeException(nameof(width));
            Vector3 position = (Vector3)centre + Vector3.one * .5f;
            if (position.sqrMagnitude < 1) throw new ArgumentOutOfRangeException(nameof(centre));
            Centre = centre;
            Width = width;
            Length = length;
            Depth = depth;
            Up = position.normalized;
            Right = Vector3.Cross(Up, Mathf.Abs(Up.y) < .9f ? Vector3.up : Vector3.right).normalized;
            Forward = Vector3.Cross(Right, Up);
            // Tranches from the top: every layer is visited on a 2-cell grid, rows alternating like a plough.
            for (int layer = 0; layer < Tranches; layer++)
            {
                float height = Above - layer;
                int row = 0;
                for (float b = -length * .5f + 1; b <= length * .5f - 1 + 1e-3f; b += 2, row++)
                for (int i = 0; i <= width / 2 - 1; i++)
                {
                    float a = row % 2 == 0 ? -width * .5f + 1 + i * 2 : width * .5f - 1 - i * 2;
                    contacts.Add(Cell(a, b, height));
                }
            }
        }

        public Vector3 Point(float right, float forward, float up) =>
            (Vector3)Centre + Vector3.one * .5f + Right * right + Forward * forward + Up * up;

        private Vector3Int Cell(float right, float forward, float up) => Vector3Int.FloorToInt(Point(right, forward, up));

        public bool Contains(Vector3Int cell)
        {
            Vector3 d = (Vector3)(cell - Centre);
            float a = Vector3.Dot(d, Right), b = Vector3.Dot(d, Forward), h = Vector3.Dot(d, Up);
            return Mathf.Abs(a) <= Width * .5f && Mathf.Abs(b) <= Length * .5f && h >= -Depth && h <= Above + .5f;
        }
    }

    [Serializable]
    public sealed class RobotSnapshot
    {
        public bool placed;
        public Vector3Int centre;
        public int cursor;
        public RobotState state;
        public float timer;
        /// <summary>Hopper indexed by <see cref="MaterialId"/>.</summary>
        public long[] hopper;
    }

    /// <summary>
    /// The first machine. Uses the same terrain cut as the players through a delegate, so it can only remove
    /// cells that are really solid, and books every cell into its own hopper. Pure logic without Unity objects.
    /// </summary>
    public sealed class DrillRobot
    {
        /// <summary>Cuts at a contact cell along an axis and returns the number of removed cells.</summary>
        public delegate int Cutter(Vector3Int contact, Vector3 axis, HarvestInventory hopper, Predicate<Vector3Int> blocked);

        private readonly Predicate<Vector3Int> protectedCell;
        public RobotWorkArea Area { get; private set; }
        public HarvestInventory Hopper { get; }
        public RobotState State { get; private set; }
        public int Cursor { get; private set; }
        public float StrokeSeconds { get; }
        public int Revision { get; private set; }
        public int Strokes { get; private set; }
        private float timer;

        public DrillRobot(int hopperCubicMetres, float strokeSeconds, Predicate<Vector3Int> protectedCell)
        {
            if (!(strokeSeconds > 0)) throw new ArgumentOutOfRangeException(nameof(strokeSeconds));
            Hopper = new HarvestInventory(hopperCubicMetres * HarvestInventory.CubicMetre);
            StrokeSeconds = strokeSeconds;
            this.protectedCell = protectedCell ?? (_ => false);
        }

        public bool Placed => Area != null;
        public float Progress => Placed ? (float)Cursor / Area.Contacts.Count : 0;
        public Vector3Int CurrentContact => Area.Contacts[Math.Min(Cursor, Area.Contacts.Count - 1)];

        public void Place(RobotWorkArea area)
        {
            Area = area ?? throw new ArgumentNullException(nameof(area));
            Cursor = 0;
            timer = 0;
            State = RobotState.Stopped;
            Revision++;
        }

        /// <summary>Removes the robot. Only allowed with an empty hopper so no material is lost.</summary>
        public bool TryRemove()
        {
            if (!Placed || Hopper.Amount != 0) return false;
            Area = null;
            State = RobotState.Stopped;
            Revision++;
            return true;
        }

        public bool Blocked(Vector3Int cell) => !Area.Contains(cell) || protectedCell(cell);

        public void Toggle()
        {
            if (!Placed) return;
            State = State == RobotState.Stopped ? (Cursor >= Area.Contacts.Count ? RobotState.Finished : RobotState.Working)
                : State == RobotState.Working || State == RobotState.Full ? RobotState.Stopped : State;
            Revision++;
        }

        public void Advance(float seconds, Cutter cut)
        {
            if (!Placed || !(seconds > 0)) return;
            if (State == RobotState.Full && Hopper.Free >= HarvestInventory.CubicMetre) { State = RobotState.Working; Revision++; }
            if (State != RobotState.Working) return;
            timer += seconds;
            while (timer >= StrokeSeconds && State == RobotState.Working)
            {
                timer -= StrokeSeconds;
                Stroke(cut);
            }
        }

        private void Stroke(Cutter cut)
        {
            if (Hopper.Free < HarvestInventory.CubicMetre) { BecomeFull(); return; }
            // Skip air and protected contacts in the same tick; one real cut per stroke.
            while (Cursor < Area.Contacts.Count)
            {
                int removed = cut(Area.Contacts[Cursor], Area.Up, Hopper, Blocked);
                if (removed > 0) Strokes++;
                // Full mid-cut: keep the cursor so the rest of this contact is cut after unloading.
                if (Hopper.Free < HarvestInventory.CubicMetre && removed > 0) { BecomeFull(); return; }
                Cursor++;
                if (removed > 0) { Revision++; return; }
            }
            State = RobotState.Finished;
            Revision++;
        }

        /// <summary>Waiting time is not banked: after unloading the robot resumes at its normal pace.</summary>
        private void BecomeFull()
        {
            State = RobotState.Full;
            timer = 0;
            Revision++;
        }

        public RobotSnapshot Capture()
        {
            var state = new RobotSnapshot { placed = Placed, centre = Placed ? Area.Centre : default, cursor = Cursor,
                state = State, timer = timer, hopper = new long[HarvestInventory.MaterialSlots] };
            for (int m = 0; m < state.hopper.Length; m++) state.hopper[m] = Hopper.AmountOf((MaterialId)m);
            return state;
        }

        public void Restore(RobotSnapshot state, Func<Vector3Int, RobotWorkArea> area)
        {
            Hopper.Empty();
            if (state == null || !state.placed)
            {
                Area = null;
                State = RobotState.Stopped;
                Cursor = 0;
                timer = 0;
                Revision++;
                return;
            }
            Area = area(state.centre);
            Cursor = Mathf.Clamp(state.cursor, 0, Area.Contacts.Count);
            State = state.state;
            timer = state.timer;
            for (int m = 1; m < state.hopper.Length; m++)
                if (state.hopper[m] > 0) Hopper.TryAdd((MaterialId)m, state.hopper[m]);
            Revision++;
        }

        public static void Validate(RobotSnapshot state, long hopperCapacity, float strokeSeconds)
        {
            if (state == null) return;
            if (state.hopper == null || state.hopper.Length != HarvestInventory.MaterialSlots || state.hopper[0] != 0 ||
                state.cursor < 0 || (uint)state.state > (uint)RobotState.Finished || !(state.timer >= 0 && state.timer < strokeSeconds))
                throw new System.IO.InvalidDataException("Ungültiger Bohrroboter.");
            long total = 0;
            foreach (long amount in state.hopper)
            {
                if (amount < 0 || amount > hopperCapacity - total) throw new System.IO.InvalidDataException("Ungültiger Robotertrichter.");
                total += amount;
            }
            if (!state.placed && total != 0) throw new System.IO.InvalidDataException("Roboterladung ohne Roboter.");
        }

        public static long Holding(RobotSnapshot state, MaterialId material) =>
            state?.hopper == null ? 0 : state.hopper[(int)material];
    }
}
