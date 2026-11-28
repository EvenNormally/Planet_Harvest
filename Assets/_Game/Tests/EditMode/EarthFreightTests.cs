using NUnit.Framework;
using PlanetHarvest.Player;
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

        [Test]
        public void AllFiveLoadsPayOnArrivalAndOwnedToolsCanBeReequipped()
        {
            var freight = new EarthFreight();
            var pack = new HarvestInventory(100 * Unit);
            var tools = new MiningTools();
            Assert.IsFalse(tools.Select(MiningTool.ProspectingPick, freight, true));
            long expectedCredits = 0;
            for (int i = 0; i < EarthFreight.LevelCount; i++)
            {
                var order = EarthFreight.Order(i);
                Assert.IsTrue(pack.TryAdd(order.Material, order.Volume * Unit));
                Assert.AreEqual(order.Volume * Unit, freight.Load(pack, order.Material));
                Assert.AreEqual(expectedCredits, freight.Credits);
                expectedCredits += order.Credits;
                freight.Advance(2 * EarthFreight.LegSeconds);
                Assert.AreEqual(i + 1, freight.CompletedLevels);
                Assert.AreEqual(expectedCredits, freight.Credits);
            }
            Assert.AreEqual(5, freight.CompletedLevels);
            Assert.IsFalse(tools.Select(MiningTool.Drill, freight, false));
            Assert.IsTrue(tools.Select(MiningTool.Drill, freight, true));
            Assert.IsTrue(tools.Select(MiningTool.Pick, freight, false));
            Assert.IsTrue(tools.Select(MiningTool.Drill, freight, false));
            Assert.AreEqual(expectedCredits - MiningTools.Spec(MiningTool.Drill).Price, freight.Credits);
        }

        [Test]
        public void SplitStoneShipmentsEarnTheSameCreditsAsAFullContainer()
        {
            var cargo = new EarthFreight();
            var pack = new HarvestInventory(12 * Unit);
            for (int i = 0; i < 12; i++)
            {
                Assert.IsTrue(pack.TryAdd(MaterialId.Stone, Unit));
                cargo.Load(pack, MaterialId.Stone);
                Assert.IsTrue(cargo.Dispatch(MaterialId.Stone));
                Assert.IsFalse(cargo.Dispatch(MaterialId.Stone));
                cargo.Advance(2 * EarthFreight.LegSeconds);
            }
            Assert.AreEqual(30, cargo.Credits);
            Assert.AreEqual(12 * Unit, cargo.TotalDelivered);
            Assert.AreEqual(0, cargo.CompletedLevels);
            Assert.IsTrue(pack.TryAdd(MaterialId.Soil, 12 * Unit));
            cargo.Load(pack, MaterialId.Soil);
            Assert.IsTrue(cargo.Dispatch(MaterialId.Soil));
            cargo.Advance(2 * EarthFreight.LegSeconds);
            Assert.AreEqual(0, cargo.CompletedLevels);
            Assert.IsTrue(pack.TryAdd(MaterialId.Soil, 8 * Unit));
            cargo.Load(pack, MaterialId.Soil);
            cargo.Dispatch(MaterialId.Soil);
            cargo.Advance(2 * EarthFreight.LegSeconds);
            Assert.AreEqual(2, cargo.CompletedLevels);
            Assert.AreEqual(50, cargo.Credits);
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
