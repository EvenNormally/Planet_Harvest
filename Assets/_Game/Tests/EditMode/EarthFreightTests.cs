using NUnit.Framework;
using PlanetHarvest.Player;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using UnityEngine;

namespace PlanetHarvest.Tests
{
    /// <summary>Act 1 program built in code so the rules are tested independently of the asset.</summary>
    internal static class TestFreight
    {
        public static FreightProgram Act1(int landingBonus = 10) => new FreightProgram(
            new[]
            {
                new MaterialSpec(MaterialId.Soil, "Erde", 1500, 1, 20, ContainerKind.PressTub, Color.white),
                new MaterialSpec(MaterialId.Stone, "Stein", 2600, 2.5f, 12, ContainerKind.Barrel, Color.white),
                new MaterialSpec(MaterialId.IronOre, "Eisenerz", 5000, 11.25f, 4, ContainerKind.MagneticBin, Color.white),
                new MaterialSpec(MaterialId.CopperOre, "Kupfererz", 4200, 12, 5, ContainerKind.OreHopper, Color.white),
                new MaterialSpec(MaterialId.Quartz, "Quarz", 2650, 26.67f, 3, ContainerKind.PrismBox, Color.white)
            },
            new[]
            {
                new ContractSpec("landevertrag", "Landevertrag", "Akt 1", landingBonus,
                    new MaterialRequirement(MaterialId.Soil, 20), new MaterialRequirement(MaterialId.Stone, 12)),
                new ContractSpec("tragwerk", "Tragwerk", "Akt 1", 15,
                    new MaterialRequirement(MaterialId.IronOre, 4), new MaterialRequirement(MaterialId.Stone, 12)),
                new ContractSpec("stromleitungen", "Stromleitungen", "Akt 1", 20,
                    new MaterialRequirement(MaterialId.CopperOre, 5), new MaterialRequirement(MaterialId.Soil, 10)),
                new ContractSpec("orbitalsensoren", "Orbitalsensoren", "Akt 1", 30,
                    new MaterialRequirement(MaterialId.Quartz, 3), new MaterialRequirement(MaterialId.CopperOre, 2))
            });

        /// <summary>Fills the material's container from a fresh pack and flies it up and back.</summary>
        public static void Deliver(EarthFreight freight, MaterialId material, int cubicMetres)
        {
            var pack = new HarvestInventory(cubicMetres * HarvestInventory.CubicMetre);
            Assert.IsTrue(pack.TryAdd(material, pack.Capacity));
            while (pack.Amount > 0)
            {
                freight.Load(pack, material);
                if (freight.Flying == MaterialId.None) Assert.IsTrue(freight.Dispatch(material));
                freight.Advance(2 * EarthFreight.LegSeconds);
            }
        }
    }

    public class EarthFreightTests
    {
        private const long Unit = HarvestInventory.CubicMetre;

        [Test]
        public void PartialLoadsAndSurplusStayInTheCorrectInventories()
        {
            var freight = new EarthFreight(TestFreight.Act1());
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
        public void LandingContractNeedsBothSoilAndStoneAndPaysItsBonusOnce()
        {
            var freight = new EarthFreight(TestFreight.Act1());
            TestFreight.Deliver(freight, MaterialId.Soil, 20);
            Assert.AreEqual(0, freight.CompletedContracts, "Soil alone must not fulfil the landing contract.");
            Assert.AreEqual(20, freight.Credits);
            Assert.AreEqual(20 * Unit, freight.Contracts.Progress(MaterialId.Soil));
            Assert.AreEqual(0, freight.Contracts.Progress(MaterialId.Stone));
            TestFreight.Deliver(freight, MaterialId.Stone, 12);
            Assert.AreEqual(1, freight.CompletedContracts);
            Assert.AreEqual(20 + 30 + 10, freight.Credits);
            Assert.AreEqual("tragwerk", freight.Contracts.Current.Id);
            // Later deliveries pay volume credits but never the landing bonus again.
            TestFreight.Deliver(freight, MaterialId.Soil, 1);
            Assert.AreEqual(61, freight.Credits);
            Assert.AreEqual(1, freight.Contracts.Rewarded);
        }

        [Test]
        public void AllAct1ContractsCompleteAndOwnedToolsCanBeReequipped()
        {
            var freight = new EarthFreight(TestFreight.Act1());
            var tools = new MiningTools();
            Assert.IsFalse(tools.Select(MiningTool.ProspectingPick, freight, true));
            foreach (var contract in freight.Program.Contracts)
            {
                Assert.AreSame(contract, freight.Contracts.Current);
                foreach (var requirement in contract.Requirements)
                    TestFreight.Deliver(freight, requirement.Material, requirement.CubicMetres);
            }
            Assert.IsTrue(freight.Contracts.AllComplete);
            Assert.IsNull(freight.Contracts.Current);
            // 20+12 soil, 24 stone, 4 iron, 7 copper, 3 quartz + four bonuses.
            long expected = 30 + 60 + 45 + 84 + 80 + 10 + 15 + 20 + 30;
            Assert.AreEqual(expected, freight.Credits);
            Assert.IsFalse(tools.Select(MiningTool.Drill, freight, false));
            Assert.IsTrue(tools.Select(MiningTool.Drill, freight, true));
            Assert.IsTrue(tools.Select(MiningTool.Pick, freight, false));
            Assert.IsTrue(tools.Select(MiningTool.Drill, freight, false));
            Assert.AreEqual(expected - MiningTools.Spec(MiningTool.Drill).Price, freight.Credits);
        }

        [Test]
        public void SplitStoneShipmentsEarnTheSameCreditsAsAFullContainer()
        {
            var cargo = new EarthFreight(TestFreight.Act1(landingBonus: 0));
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
            Assert.AreEqual(0, cargo.CompletedContracts);
            Assert.IsTrue(pack.TryAdd(MaterialId.Soil, 12 * Unit));
            cargo.Load(pack, MaterialId.Soil);
            Assert.IsTrue(cargo.Dispatch(MaterialId.Soil));
            cargo.Advance(2 * EarthFreight.LegSeconds);
            Assert.AreEqual(0, cargo.CompletedContracts);
            Assert.IsTrue(pack.TryAdd(MaterialId.Soil, 8 * Unit));
            cargo.Load(pack, MaterialId.Soil);
            cargo.Dispatch(MaterialId.Soil);
            cargo.Advance(2 * EarthFreight.LegSeconds);
            Assert.AreEqual(1, cargo.CompletedContracts);
            Assert.AreEqual(50, cargo.Credits);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FullBinsQueueAndOnlyCountOnceOnArrival(bool soilFirst)
        {
            var freight = new EarthFreight(TestFreight.Act1());
            var pack = new HarvestInventory(32 * Unit);
            pack.TryAdd(MaterialId.Soil, 20 * Unit);
            pack.TryAdd(MaterialId.Stone, 12 * Unit);
            var first = soilFirst ? MaterialId.Soil : MaterialId.Stone;
            var second = soilFirst ? MaterialId.Stone : MaterialId.Soil;
            freight.Load(pack, first);
            freight.Load(pack, second);
            freight.Advance(EarthFreight.LegSeconds / 2);
            Assert.AreEqual(0, freight.CompletedContracts);
            Assert.AreEqual(first, freight.Flying);
            Assert.AreEqual(freight.Container(first).Capacity, freight.InFlight);
            Assert.AreEqual(0, freight.TotalDelivered);
            freight.Advance(EarthFreight.LegSeconds / 2);
            Assert.IsTrue(freight.Returning);
            Assert.AreEqual(0, freight.InFlight);
            Assert.AreEqual(0, freight.CompletedContracts);
            Assert.AreEqual(0, freight.Container(first).Amount);
            freight.Advance(EarthFreight.LegSeconds);
            Assert.AreEqual(second, freight.Flying);
            freight.Advance(90);
            Assert.AreEqual(1, freight.CompletedContracts);
            Assert.AreEqual(20 * Unit, freight.Delivered(MaterialId.Soil));
            Assert.AreEqual(12 * Unit, freight.Delivered(MaterialId.Stone));
            Assert.AreEqual(60, freight.Credits);
            Assert.AreEqual(MaterialId.None, freight.Flying);
            Assert.AreEqual(0, pack.Amount);
            Assert.IsTrue(pack.TryAdd(MaterialId.Soil, Unit));
            Assert.AreEqual(Unit, freight.Load(pack, MaterialId.Soil));
        }
    }
}
