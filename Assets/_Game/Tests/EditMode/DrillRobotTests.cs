using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PlanetHarvest.Machines;
using PlanetHarvest.Mining;
using UnityEngine;

namespace PlanetHarvest.Tests
{
    public class DrillRobotTests
    {
        private const long Unit = HarvestInventory.CubicMetre;

        /// <summary>Fake terrain: a set of solid stone cells. Every cut removes the contact and its neighbours.</summary>
        private sealed class FakeTerrain
        {
            public readonly HashSet<Vector3Int> Solid = new HashSet<Vector3Int>();
            public int Removed;

            public int Cut(Vector3Int contact, Vector3 axis, HarvestInventory hopper, System.Predicate<Vector3Int> blocked)
            {
                if (!Solid.Contains(contact)) return 0;
                int count = 0;
                for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                {
                    var cell = contact + new Vector3Int(x, 0, z);
                    if (blocked(cell) || !Solid.Contains(cell) || hopper.Free < Unit) continue;
                    Solid.Remove(cell);
                    hopper.TryAdd(MaterialId.Stone, Unit);
                    count++;
                }
                Removed += count;
                return count;
            }
        }

        private static RobotWorkArea Area() => new RobotWorkArea(new Vector3Int(0, 40, 0), 8, 8, 4);

        [Test]
        public void WorkAreaIsExcavatedInTranchesFromTheTop()
        {
            var area = Area();
            Assert.AreEqual(6, area.Tranches);
            Assert.AreEqual(6 * 16, area.Contacts.Count);
            float previous = float.MaxValue;
            for (int layer = 0; layer < area.Tranches; layer++)
            {
                float mean = 0;
                for (int i = 0; i < 16; i++)
                {
                    var contact = area.Contacts[layer * 16 + i];
                    Assert.IsTrue(area.Contains(contact), $"{contact} lies inside the area.");
                    mean += Vector3.Dot((Vector3)(contact - area.Centre), area.Up) / 16;
                }
                Assert.Less(mean, previous, "Each tranche lies below the previous one.");
                previous = mean;
            }
            Assert.IsFalse(area.Contains(area.Centre + Vector3Int.right * 6));
            Assert.IsFalse(area.Contains(area.Centre + Vector3Int.down * 6));
        }

        [Test]
        public void RobotOnlyCutsInsideItsAreaAndNeverProtectedCells()
        {
            var area = Area();
            var terrain = new FakeTerrain();
            for (int x = -8; x <= 8; x++) for (int y = 34; y <= 42; y++) for (int z = -8; z <= 8; z++)
                terrain.Solid.Add(new Vector3Int(x, y, z));
            var shielded = new Vector3Int(1, 40, 1);
            var robot = new DrillRobot(500, .5f, cell => cell == shielded);
            robot.Place(area);
            Assert.AreEqual(RobotState.Stopped, robot.State);
            robot.Advance(10, terrain.Cut);
            Assert.AreEqual(0, terrain.Removed, "A stopped robot does nothing.");
            robot.Toggle();
            robot.Advance(1000, terrain.Cut);
            Assert.AreEqual(RobotState.Finished, robot.State);
            Assert.Greater(terrain.Removed, 0);
            Assert.AreEqual(terrain.Removed * Unit, robot.Hopper.Amount, "Every removed cell is booked once.");
            Assert.IsTrue(terrain.Solid.Contains(shielded));
            Assert.IsTrue(terrain.Solid.Contains(new Vector3Int(8, 40, 8)), "Cells outside the box stay.");
        }

        [Test]
        public void FullHopperPausesUntilUnloadedAndStopKeepsTheCursor()
        {
            var terrain = new FakeTerrain();
            for (int x = -8; x <= 8; x++) for (int y = 34; y <= 42; y++) for (int z = -8; z <= 8; z++)
                terrain.Solid.Add(new Vector3Int(x, y, z));
            var robot = new DrillRobot(6, .5f, _ => false);
            robot.Place(Area());
            robot.Toggle();
            robot.Advance(30, terrain.Cut);
            Assert.AreEqual(RobotState.Full, robot.State);
            int removed = terrain.Removed;
            robot.Advance(30, terrain.Cut);
            Assert.AreEqual(removed, terrain.Removed, "No cutting while full.");
            var pack = new HarvestInventory(100 * Unit);
            robot.Hopper.TransferTo(pack, MaterialId.Stone);
            robot.Advance(.5f, terrain.Cut);
            Assert.AreEqual(RobotState.Working, robot.State);
            int cursor = robot.Cursor;
            robot.Toggle();
            Assert.AreEqual(RobotState.Stopped, robot.State);
            robot.Advance(30, terrain.Cut);
            Assert.AreEqual(cursor, robot.Cursor);
            Assert.AreEqual(terrain.Removed * Unit, robot.Hopper.Amount + pack.Amount);
            Assert.IsFalse(robot.TryRemove(), "A loaded robot cannot be removed.");
        }

        [Test]
        public void SnapshotRoundtripAndValidation()
        {
            var terrain = new FakeTerrain();
            for (int x = -8; x <= 8; x++) for (int y = 34; y <= 42; y++) for (int z = -8; z <= 8; z++)
                terrain.Solid.Add(new Vector3Int(x, y, z));
            var robot = new DrillRobot(40, .5f, _ => false);
            robot.Place(Area());
            robot.Toggle();
            robot.Advance(3.2f, terrain.Cut);
            var json = JsonUtility.ToJson(robot.Capture());
            var state = JsonUtility.FromJson<RobotSnapshot>(json);
            DrillRobot.Validate(state, 40 * Unit, .5f);
            var copy = new DrillRobot(40, .5f, _ => false);
            copy.Restore(state, centre => new RobotWorkArea(centre, 8, 8, 4));
            Assert.AreEqual(robot.Cursor, copy.Cursor);
            Assert.AreEqual(robot.State, copy.State);
            Assert.AreEqual(robot.Hopper.Amount, copy.Hopper.Amount);
            Assert.AreEqual(robot.Hopper.Amount, DrillRobot.Holding(state, MaterialId.Stone));
            state.hopper[(int)MaterialId.Stone] = 41 * Unit;
            Assert.Throws<InvalidDataException>(() => DrillRobot.Validate(state, 40 * Unit, .5f));
            copy.Restore(null, centre => null);
            Assert.IsFalse(copy.Placed);
        }
    }
}
