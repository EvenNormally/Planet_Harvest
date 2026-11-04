using NUnit.Framework;
using PlanetHarvest.Core;
using PlanetHarvest.Mining;
using PlanetHarvest.Planets;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Tests
{
    public class HarvestRulesTests
    {
        [TestCase(1, 0, 0)] [TestCase(-1, 0, 0)]
        [TestCase(0, 1, 0)] [TestCase(0, -1, 0)]
        [TestCase(0, 0, 1)] [TestCase(0, 0, -1)]
        public void GravityPointsToTranslatedCentre(int x, int y, int z)
        {
            Vector3 centre = new Vector3(13, -2, 4);
            Vector3 direction = new Vector3(x, y, z);
            Assert.That(RadialGravity.Acceleration(centre + direction * 12, centre, 6), Is.EqualTo(-direction * 6));
            Assert.That(RadialGravity.Up(centre + direction, centre), Is.EqualTo(direction));
        }

        [Test]
        public void CentreHasFiniteStableFallback()
        {
            Assert.That(RadialGravity.Up(Vector3.one, Vector3.one), Is.EqualTo(Vector3.up));
            Assert.That(RadialGravity.Acceleration(Vector3.one, Vector3.one, 6), Is.EqualTo(Vector3.zero));
        }

        [TestCase(-9, -2)] [TestCase(-8, -1)] [TestCase(-1, -1)]
        [TestCase(0, 0)] [TestCase(7, 0)] [TestCase(8, 1)]
        public void NegativeChunkCoordinatesUseFloor(int coordinate, int chunk)
        {
            Assert.That(VoxelData.ChunkOf(new Vector3Int(coordinate, 0, 0)).x, Is.EqualTo(chunk));
        }

        [Test]
        public void AirAndRepeatedMiningProduceNothing()
        {
            var terrain = new VoxelData(8);
            var session = new HarvestSession(100);
            Assert.That(session.TryMine(terrain, new Vector3Int(100, 0, 0)), Is.False);
            Assert.That(session.Buffer.Amount, Is.Zero);
            int before = terrain.SolidCount;
            Assert.That(session.TryMine(terrain, Vector3Int.zero), Is.True);
            session.Transfer();
            Assert.That(session.TryMine(terrain, Vector3Int.zero), Is.False);
            Assert.That(session.Buffer.Amount, Is.Zero);
            Assert.That(terrain.SolidCount, Is.EqualTo(before - 1));
        }

        [Test]
        public void FullBufferNeverRemovesAnotherCell()
        {
            var terrain = new VoxelData(8);
            var session = new HarvestSession(100);
            session.TryMine(terrain, Vector3Int.zero);
            int before = terrain.SolidCount;
            Assert.That(session.TryMine(terrain, Vector3Int.one), Is.False);
            Assert.That(terrain.IsSolid(Vector3Int.one), Is.True);
            Assert.That(terrain.SolidCount, Is.EqualTo(before));
            Assert.That(session.Buffer.Amount, Is.EqualTo(HarvestInventory.CubicMetre));
        }

        [Test]
        public void TransferPreservesAmountAndRejectsOverflow()
        {
            var source = new HarvestInventory();
            var destination = new HarvestInventory();
            source.TryAdd(1000000);
            destination.TryAdd(750000);
            long total = source.Amount + destination.Amount;
            Assert.That(source.TransferTo(destination), Is.EqualTo(250000));
            Assert.That(source.Amount + destination.Amount, Is.EqualTo(total));
            Assert.That(destination.Amount, Is.EqualTo(destination.Capacity));
            Assert.That(source.TransferTo(destination), Is.Zero);
            Assert.That(destination.TryAdd(1), Is.False);
            Assert.That(source.TryAdd(-1), Is.False);
            Assert.That(source.TryAdd(long.MaxValue), Is.False);
            Assert.That(source.TransferTo(source), Is.Zero);
            Assert.That(source.Amount + destination.Amount, Is.EqualTo(total));
        }

        [Test]
        public void ShipmentPaysExactlyOnceAndContainerCanBeReused()
        {
            var session = new HarvestSession(137);
            Assert.That(session.TryShip(), Is.False);
            session.Container.TryAdd(500000);
            Assert.That(session.TryShip(), Is.False);
            session.Buffer.TryAdd(500000);
            Assert.That(session.Transfer(), Is.EqualTo(500000));
            Assert.That(session.TryShip(), Is.True);
            Assert.That(session.Container.Amount, Is.Zero);
            Assert.That(session.TryShip(), Is.False);
            Assert.That(session.Credits, Is.EqualTo(137));
            session.Buffer.TryAdd(1000000);
            session.Transfer();
            Assert.That(session.TryShip(), Is.True);
            Assert.That(session.Credits, Is.EqualTo(274));
            Assert.That(session.Shipments, Is.EqualTo(2));
        }
    }
}
