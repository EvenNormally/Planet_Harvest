using System;
using System.IO;
using NUnit.Framework;
using PlanetHarvest.Core;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using UnityEditor;
using UnityEngine;

namespace PlanetHarvest.Tests
{
    public class ContractLedgerTests
    {
        private const long Unit = HarvestInventory.CubicMetre;
        private const string ContractAsset = "Assets/_Game/ScriptableObjects/ElevatorContracts_Akt1.asset";

        [Test]
        public void SurplusBetweenStagesCarriesOverButIsNeverCountedTwice()
        {
            var ledger = new ContractLedger(TestFreight.Act1());
            Assert.AreEqual(0, ledger.Record(MaterialId.Stone, 30 * Unit), "No contract is complete without soil.");
            Assert.AreEqual(12 * Unit, ledger.Progress(MaterialId.Stone), "Progress is capped at the minimum.");
            Assert.AreEqual(10, ledger.Record(MaterialId.Soil, 20 * Unit));
            Assert.AreEqual(1, ledger.Completed);
            // 30 stone: 12 consumed by the landing contract, 12 of the remaining 18 cover the next one.
            Assert.AreEqual("tragwerk", ledger.Current.Id);
            Assert.AreEqual(12 * Unit, ledger.Progress(MaterialId.Stone));
            Assert.AreEqual(0, ledger.Progress(MaterialId.IronOre));
            Assert.AreEqual(15, ledger.Record(MaterialId.IronOre, 4 * Unit));
            Assert.AreEqual(2, ledger.Completed);
            // The 6 m³ stone left over is not required by later contracts and must not complete anything.
            Assert.AreEqual(0, ledger.Record(MaterialId.Stone, 100 * Unit));
            Assert.AreEqual(2, ledger.Completed);
            Assert.AreEqual(130 * Unit, ledger.Delivered(MaterialId.Stone));
        }

        [Test]
        public void PartialDeliveriesAccumulateAndOneArrivalCanFinishSeveralStages()
        {
            var ledger = new ContractLedger(TestFreight.Act1());
            for (int i = 0; i < 30; i++) Assert.AreEqual(0, ledger.Record(MaterialId.Soil, Unit));
            ledger.Record(MaterialId.Stone, 11 * Unit);
            ledger.Record(MaterialId.IronOre, 4 * Unit);
            ledger.Record(MaterialId.CopperOre, 5 * Unit);
            Assert.AreEqual(0, ledger.Completed);
            Assert.AreEqual(20 * Unit + 11 * Unit, ledger.CurrentProgress);
            // A single stone shipment now closes the landing contract and, with the queued surplus, stage 2 is still open.
            Assert.AreEqual(10, ledger.Record(MaterialId.Stone, Unit));
            Assert.AreEqual(1, ledger.Completed);
            Assert.AreEqual(15 + 20, ledger.Record(MaterialId.Stone, 12 * Unit), "Stone completes Tragwerk, surplus soil+copper complete Stromleitungen.");
            Assert.AreEqual(3, ledger.Completed);
            Assert.AreEqual(3, ledger.Rewarded);
            Assert.AreEqual(0, ledger.Record(MaterialId.Quartz, 0));
        }

        [Test]
        public void RestoreKeepsRewardedBonusesAndRejectsImpossibleCounts()
        {
            var source = new ContractLedger(TestFreight.Act1());
            source.Record(MaterialId.Soil, 20 * Unit);
            source.Record(MaterialId.Stone, 12 * Unit);
            var copy = new ContractLedger(TestFreight.Act1());
            copy.Restore(source.CaptureDelivered(), source.Rewarded);
            Assert.AreEqual(1, copy.Completed);
            Assert.AreEqual(1, copy.Rewarded);
            Assert.AreEqual(0, copy.Record(MaterialId.Soil, Unit), "Restored bonus must not be paid again.");
            Assert.Throws<ArgumentOutOfRangeException>(() => copy.Restore(source.CaptureDelivered(), 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => copy.Record(MaterialId.Regolith, Unit));
        }

        [Test]
        public void FreightSnapshotRoundtripPreservesContractsCreditsAndFlight()
        {
            var freight = new EarthFreight(TestFreight.Act1());
            TestFreight.Deliver(freight, MaterialId.Soil, 20);
            TestFreight.Deliver(freight, MaterialId.Stone, 12);
            var pack = new HarvestInventory(20 * Unit);
            pack.TryAdd(MaterialId.Stone, 12 * Unit);
            freight.Load(pack, MaterialId.Stone);
            freight.Advance(EarthFreight.LegSeconds / 2);
            var json = JsonUtility.ToJson(freight.Capture());
            var restored = new EarthFreight(TestFreight.Act1());
            restored.Restore(JsonUtility.FromJson<FreightSnapshot>(json));
            Assert.AreEqual(freight.Credits, restored.Credits);
            Assert.AreEqual(1, restored.CompletedContracts);
            Assert.AreEqual(MaterialId.Stone, restored.Flying);
            Assert.AreEqual(12 * Unit, restored.InFlight);
            restored.Advance(EarthFreight.LegSeconds);
            freight.Advance(EarthFreight.LegSeconds);
            Assert.AreEqual(freight.Credits, restored.Credits);
            Assert.AreEqual(freight.CompletedContracts, restored.CompletedContracts);
            Assert.AreEqual(24 * Unit, restored.Delivered(MaterialId.Stone));
            Assert.AreEqual(freight.Contracts.Rewarded, restored.Contracts.Rewarded);
        }

        [Test]
        public void VersionOneSaveMigratesWithoutRetroactiveBonus()
        {
            // Literal version 1 layout as written by the previous build (arrays per order index).
            const string legacy = "{\"version\":1,\"world\":\"earth\",\"removed\":[],\"freight\":{\"credits\":57," +
                "\"delivered\":[22000000,12000000,0,0,0],\"loads\":[3000000,0,0,0,1000000],\"flying\":0,\"returning\":false,\"progress\":0.0}}";
            string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".json.gz");
            try
            {
                var file = JsonUtility.FromJson<ExpeditionSnapshot>(legacy);
                ExpeditionSnapshot.Write(path, file);
                var state = ExpeditionSnapshot.Read(path);
                var program = TestFreight.Act1();
                state.Upgrade(program);
                Assert.AreEqual(ExpeditionSnapshot.CurrentVersion, state.version);
                Assert.IsNull(state.freight.delivered);
                Assert.AreEqual(22 * Unit, state.freight.deliveredByMaterial[(int)MaterialId.Soil]);
                Assert.AreEqual(Unit, state.freight.loadsByMaterial[(int)MaterialId.Quartz]);
                Assert.AreEqual(1, state.freight.rewardedContracts);
                var freight = new EarthFreight(program);
                freight.Restore(state.freight);
                Assert.AreEqual(57, freight.Credits, "Migration must not change the balance.");
                Assert.AreEqual(1, freight.CompletedContracts);
                Assert.AreEqual(3 * Unit, freight.Container(MaterialId.Soil).Amount);
                // Further stone pays volume credits only; the landing bonus counts as already settled.
                TestFreight.Deliver(freight, MaterialId.Stone, 1);
                Assert.AreEqual(57 + 2, freight.Credits);
                state.Upgrade(program);
                Assert.AreEqual(ExpeditionSnapshot.CurrentVersion, state.version, "Upgrading twice is a no-op.");
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void PlanetProgressCountsOnlyOrbitArrivals()
        {
            var freight = new EarthFreight(TestFreight.Act1());
            var pack = new HarvestInventory(40 * Unit);
            pack.TryAdd(MaterialId.Soil, 25 * Unit);
            freight.Load(pack, MaterialId.Soil);
            var climbing = PlanetProgress.Measure(1000, freight, pack.Amount);
            Assert.AreEqual(20 * Unit, climbing.InFlight);
            Assert.AreEqual(0, climbing.AtElevator);
            Assert.AreEqual(5 * Unit, climbing.Carried);
            Assert.AreEqual(0, climbing.OrbitFraction);
            Assert.AreEqual(0, freight.Load(pack, MaterialId.Soil), "A container in transit accepts nothing.");
            freight.Advance(EarthFreight.LegSeconds);
            var arrived = PlanetProgress.Measure(1000, freight, pack.Amount);
            Assert.AreEqual(20 * Unit, arrived.InOrbit);
            Assert.AreEqual(0, arrived.InFlight);
            Assert.AreEqual(5 * Unit, arrived.Carried);
            Assert.AreEqual(.02, arrived.OrbitFraction, 1e-12);
        }

        [Test]
        public void ProgramRejectsInconsistentDefinitions()
        {
            var soil = new MaterialSpec(MaterialId.Soil, "Erde", 1500, 1, 20, ContainerKind.PressTub, Color.white);
            Assert.Throws<ArgumentException>(() => new FreightProgram(new[] { soil, soil }, new ContractSpec[0]));
            Assert.Throws<ArgumentException>(() => new FreightProgram(new[] { soil },
                new[] { new ContractSpec("x", "X", "", 0, new MaterialRequirement(MaterialId.Stone, 1)) }));
            Assert.Throws<ArgumentException>(() => new ContractSpec("y", "Y", "", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MaterialRequirement(MaterialId.Soil, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MaterialSpec(MaterialId.None, "Luft", 1, 1, 1, ContainerKind.Barrel, Color.white));
            Assert.AreEqual(1500, soil.MassKg(Unit), 1e-9);
            Assert.AreEqual("Erde", new MaterialSpec(MaterialId.Soil, "", 1500, 1, 20, ContainerKind.PressTub, Color.white).Name);
        }

        [Test]
        public void ConfiguredAct1AssetKeepsTheLandingContract()
        {
            var set = AssetDatabase.LoadAssetAtPath<ElevatorContractSet>(ContractAsset);
            Assert.NotNull(set, ContractAsset);
            var program = set.CreateProgram();
            var landing = program.Contracts[0];
            Assert.AreEqual("landevertrag", landing.Id);
            Assert.AreEqual(2, landing.Requirements.Count);
            Assert.AreEqual(MaterialId.Soil, landing.Requirements[0].Material);
            Assert.AreEqual(20, landing.Requirements[0].CubicMetres);
            Assert.AreEqual(MaterialId.Stone, landing.Requirements[1].Material);
            Assert.AreEqual(12, landing.Requirements[1].CubicMetres);
            Assert.GreaterOrEqual(program.Contracts.Count, 2);
            foreach (var material in program.Materials)
            {
                Assert.Greater(material.DensityKgPerCubicMetre, 0, material.Name);
                Assert.Greater(material.ContainerCubicMetres, 0, material.Name);
            }
            Assert.AreEqual(2600, program.Material(MaterialId.Stone).MassKg(Unit), 1e-6);
            Assert.AreEqual(80, program.Material(MaterialId.Quartz).CreditsFor(3 * Unit));
        }
    }
}
