using NUnit.Framework;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;

namespace PlanetHarvest.Tests
{
    public class EarthFreightTests
    {
        private const long Unit = HarvestInventory.CubicMetre;

        [Test]
        public void PartialLoadsAndSurplusStayInTheCorrectInventories()
        {
            var freight = new EarthFreight();
            var pack = new HarvestInventory(100 * Unit);
            pack.TryAdd(MaterialId.Soil, 7 * Unit);
            pack.TryAdd(MaterialId.Stone, 15 * Unit);
            pack.TryAdd(MaterialId.IronOre, 2 * Unit);
            Assert.AreEqual(7 * Unit, freight.Load(pack, MaterialId.Soil));
            Assert.AreEqual(MaterialId.None, freight.Flying);
            Assert.AreEqual(12 * Unit, freight.Load(pack, MaterialId.Stone));
            Assert.AreEqual(MaterialId.Stone, freight.Flying);
            Assert.AreEqual(0, freight.Load(pack, MaterialId.Stone));
            Assert.AreEqual(7 * Unit, freight.Container(MaterialId.Soil).Amount);
            Assert.AreEqual(3 * Unit, pack.AmountOf(MaterialId.Stone));
            Assert.AreEqual(2 * Unit, pack.AmountOf(MaterialId.IronOre));
            Assert.IsFalse(freight.Container(MaterialId.Stone).TryAdd(MaterialId.Soil, Unit));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FullBinsQueueAndOnlyCountOnceOnArrival(bool soilFirst)
        {
            var freight = new EarthFreight();
            var pack = new HarvestInventory(32 * Unit);
            pack.TryAdd(MaterialId.Soil, 20 * Unit);
            pack.TryAdd(MaterialId.Stone, 12 * Unit);
            var first = soilFirst ? MaterialId.Soil : MaterialId.Stone;
            var second = soilFirst ? MaterialId.Stone : MaterialId.Soil;
            freight.Load(pack, first);
            freight.Load(pack, second);
            freight.Advance(EarthFreight.LegSeconds / 2);
            Assert.AreEqual(0, freight.CompletedLevels);
            Assert.AreEqual(first, freight.Flying);
            freight.Advance(EarthFreight.LegSeconds / 2);
            Assert.IsTrue(freight.Returning);
            Assert.AreEqual(soilFirst ? 1 : 0, freight.CompletedLevels);
            Assert.AreEqual(0, freight.Container(first).Amount);
            freight.Advance(EarthFreight.LegSeconds);
            Assert.AreEqual(second, freight.Flying);
            freight.Advance(90);
            Assert.AreEqual(2, freight.CompletedLevels);
            Assert.AreEqual(20 * Unit, freight.SoilDelivered);
            Assert.AreEqual(12 * Unit, freight.StoneDelivered);
            Assert.AreEqual(MaterialId.None, freight.Flying);
            Assert.AreEqual(0, pack.Amount);
            Assert.IsTrue(pack.TryAdd(MaterialId.Soil, Unit));
            Assert.AreEqual(Unit, freight.Load(pack, MaterialId.Soil));
        }
    }
}
