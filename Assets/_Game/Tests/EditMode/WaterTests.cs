using System.IO;
using NUnit.Framework;
using PlanetHarvest.Core;
using PlanetHarvest.Logistics;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using UnityEngine;

namespace PlanetHarvest.Tests
{
    public class WaterTests
    {
        private const long Unit = HarvestInventory.CubicMetre;

        [Test]
        public void ReservoirIsFiniteAndPumpConservesWater()
        {
            var lake = new WaterReservoir(5);
            var pump = new WaterPump(lake, 3, 1);
            pump.Advance(10);
            Assert.AreEqual(0, pump.Tank.Amount, "A pump that is not built does nothing.");
            pump.Build();
            pump.Toggle();
            Assert.AreEqual(PumpState.Pumping, pump.State);
            pump.Advance(2.5f);
            Assert.AreEqual(2 * Unit, pump.Tank.Amount);
            pump.Advance(10);
            Assert.AreEqual(PumpState.Full, pump.State);
            Assert.AreEqual(3 * Unit, pump.Tank.Amount);
            var pack = new HarvestInventory(10 * Unit);
            Assert.AreEqual(3 * Unit, pump.Tank.TransferTo(pack, MaterialId.Water));
            pump.Advance(10);
            Assert.AreEqual(PumpState.Dry, pump.State);
            Assert.AreEqual(0, lake.Remaining);
            Assert.AreEqual(lake.Capacity, pack.Amount + pump.Tank.Amount, "Nothing is created or lost.");
            Assert.AreEqual(0, lake.Draw(Unit));
        }

        [Test]
        public void PumpSnapshotRoundtripAndValidation()
        {
            var pump = new WaterPump(new WaterReservoir(20), 10, 1.5f);
            pump.Build();
            pump.Toggle();
            pump.Advance(4);
            var state = JsonUtility.FromJson<WaterSnapshot>(JsonUtility.ToJson(pump.Capture()));
            WaterPump.Validate(state, 20 * Unit, 10 * Unit, 1.5f);
            var copy = new WaterPump(new WaterReservoir(20), 10, 1.5f);
            copy.Restore(state);
            Assert.AreEqual(pump.Tank.Amount, copy.Tank.Amount);
            Assert.AreEqual(pump.Reservoir.Remaining, copy.Reservoir.Remaining);
            Assert.AreEqual(PumpState.Pumping, copy.State);
            Assert.AreEqual(pump.Reservoir.Drawn, WaterPump.Drawn(state, 20 * Unit));
            copy.Restore(null);
            Assert.IsFalse(copy.Built);
            Assert.AreEqual(copy.Reservoir.Capacity, copy.Reservoir.Remaining);
            state.tank = 11 * Unit;
            Assert.Throws<InvalidDataException>(() => WaterPump.Validate(state, 20 * Unit, 10 * Unit, 1.5f));
        }

        [Test]
        public void WaterIsDeliveredButDoesNotCountAsSolidPlanet()
        {
            var program = new FreightProgram(new[]
            {
                new MaterialSpec(MaterialId.Stone, "Stein", 2600, 2.5f, 12, ContainerKind.Barrel, Color.white),
                new MaterialSpec(MaterialId.Water, "Wasser", 1000, 3, 10, ContainerKind.GlassSphere, Color.blue)
            }, new ContractSpec[0]);
            var freight = new EarthFreight(program);
            var pack = new HarvestInventory(30 * Unit);
            pack.TryAdd(MaterialId.Water, 10 * Unit);
            pack.TryAdd(MaterialId.Stone, 12 * Unit);
            freight.Load(pack, MaterialId.Water);
            freight.Load(pack, MaterialId.Stone);
            freight.Advance(60);
            Assert.AreEqual(30 + 30, freight.Credits);
            var progress = PlanetProgress.Measure(1200, freight, 0);
            Assert.AreEqual(12 * Unit, progress.InOrbit);
            Assert.AreEqual(.01, progress.OrbitFraction, 1e-12);
            Assert.IsFalse(MaterialId.Water.IsTerrain());
            Assert.IsTrue(MaterialId.Quartz.IsTerrain());
        }

        [Test]
        public void VersionThreeArraysArePaddedForWaterAndBiomass()
        {
            var state = new ExpeditionSnapshot
            {
                version = 3, host = new MinerSnapshot { amounts = new long[7] }, guest = new MinerSnapshot { amounts = new long[7] },
                freight = new FreightSnapshot { deliveredByMaterial = new long[7], loadsByMaterial = new long[7] },
                logistics = new BeamNetworkSnapshot { chain = new BeamChainSnapshot { hopper = new long[7], slots = new MaterialId[0] } },
                robot = new Machines.RobotSnapshot { hopper = new long[7] }
            };
            state.freight.deliveredByMaterial[(int)MaterialId.Quartz] = 3 * Unit;
            state.host.amounts[(int)MaterialId.Stone] = Unit;
            state.Upgrade(TestFreight.Act1());
            Assert.AreEqual(ExpeditionSnapshot.CurrentVersion, state.version);
            Assert.AreEqual(HarvestInventory.MaterialSlots, state.host.amounts.Length);
            Assert.AreEqual(HarvestInventory.MaterialSlots, state.guest.amounts.Length);
            Assert.AreEqual(HarvestInventory.MaterialSlots, state.freight.loadsByMaterial.Length);
            Assert.AreEqual(HarvestInventory.MaterialSlots, state.logistics.chain.hopper.Length);
            Assert.AreEqual(HarvestInventory.MaterialSlots, state.robot.hopper.Length);
            Assert.AreEqual(3 * Unit, state.freight.deliveredByMaterial[(int)MaterialId.Quartz]);
            Assert.AreEqual(Unit, state.host.amounts[(int)MaterialId.Stone]);
            Assert.IsNull(state.water);
        }
    }
}
