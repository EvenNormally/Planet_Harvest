using NUnit.Framework;
using PlanetHarvest.Core;
using PlanetHarvest.Mining;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Tests
{
    public class TestPlanetRulesTests
    {
        [Test]
        public void RoundingKeepsSurfaceOnSphereAndDoesNotFillExcavations()
        {
            var terrain = new VoxelData(100);
            var corner = new Vector3(100, 1, 1);
            Vector3 rounded = terrain.RoundSurfaceVertex(corner);
            Assert.That(rounded.magnitude, Is.EqualTo(100).Within(0.0001f));
            Assert.That(terrain.RoundSurfaceVertex(new Vector3(50, 1, 1)), Is.EqualTo(new Vector3(50, 1, 1)));
            var session = new HarvestSession(100);
            Assert.That(session.TryMine(terrain, new Vector3Int(99, 0, 0)), Is.True);
            Assert.That(terrain.RoundSurfaceVertex(corner), Is.EqualTo(rounded), "Hole rim must keep the same vertices as neighbouring chunks");
            Assert.That(terrain.IsSolid(new Vector3Int(99, 0, 0)), Is.False);
        }

        [Test]
        public void LargeImplicitSphereCanBeMinedWithoutCreatingInteriorCells()
        {
            var terrain = new VoxelData(200);
            Assert.That(terrain.IsSolid(new Vector3Int(199, 0, 0)), Is.True);
            Assert.That(terrain.IsSolid(new Vector3Int(200, 0, 0)), Is.False);
            Assert.That(terrain.IsSurfaceChunk(Vector3Int.zero), Is.False);
            Assert.That(terrain.IsSurfaceChunk(new Vector3Int(24, 0, 0)), Is.True);
            var session = new HarvestSession(100);
            int count = terrain.SolidCount;
            Assert.That(session.TryMine(terrain, Vector3Int.zero), Is.True);
            Assert.That(terrain.SolidCount, Is.EqualTo(count - 1));
            Assert.That(terrain.IsSolid(Vector3Int.zero), Is.False);
        }

        [Test]
        public void ImplicitSphereCountMatchesSolidCells()
        {
            var terrain = new VoxelData(12);
            int count = 0;
            for (int x = -12; x < 12; x++)
            for (int y = -12; y < 12; y++)
            for (int z = -12; z < 12; z++)
                if (terrain.IsSolid(new Vector3Int(x, y, z))) count++;
            Assert.That(terrain.SolidCount, Is.EqualTo(count));
        }

        [Test]
        public void ShipmentLocksTransfersReturnsEmptyAndSupportsAnotherTrip()
        {
            long load = 8 * HarvestInventory.CubicMetre;
            var session = new HarvestSession(100, load, 4, 8);
            for (int trip = 0; trip < 2; trip++)
            {
                for (int i = 0; i < 4; i++)
                {
                    session.Buffer.TryAdd(load);
                    Assert.That(session.Transfer(), Is.EqualTo(load));
                    if (i < 3) Assert.That(session.TryShip(), Is.False);
                }
                Assert.That(session.TryShip(), Is.True);
                Assert.That(session.TryShip(), Is.False);
                session.Buffer.TryAdd(load);
                Assert.That(session.Transfer(), Is.Zero);
                session.AdvanceShipment(7);
                Assert.That(session.IsShipping, Is.True);
                Assert.That(session.Container.Amount, Is.EqualTo(4 * load));
                session.AdvanceShipment(1);
                Assert.That(session.IsShipping, Is.False);
                Assert.That(session.Container.Amount, Is.Zero);
                Assert.That(session.Credits, Is.EqualTo((trip + 1) * 100));
                session.AdvanceShipment(100);
                Assert.That(session.Shipments, Is.EqualTo(trip + 1));
                // Keep the mined load for the next trip: adding to a full buffer is harmless.
            }
        }
    }
}
