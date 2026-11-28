using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PlanetHarvest.Logistics;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlanetHarvest.Tests
{
    public class WaterPlayTests
    {
        private const long Unit = HarvestInventory.CubicMetre;

        [UnityTest]
        public IEnumerator PumpDrainsTheLakePlantsGiveBiomassAndBothSurviveSaveLoad()
        {
            yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
            yield return null;
            var miner = Object.FindFirstObjectByType<EarthMining>();
            var freight = miner.Elevator.Freight;
            var station = miner.Pump;
            Assert.IsTrue(freight.Program.Contains(MaterialId.Water));
            Assert.IsTrue(freight.Program.Contains(MaterialId.Biomass));
            StringAssert.Contains("Landevertrag", station.Use(miner.Inventory));
            foreach (var (type, volume) in new[] { (MaterialId.Soil, 20L), (MaterialId.Stone, 12L), (MaterialId.Stone, 12L) })
            {
                EarthEquipmentTests.Harvest(miner.Planet, miner.Inventory, type, volume);
                freight.Load(miner.Inventory, type);
                freight.Advance(EarthFreight.LegSeconds * 2);
            }
            long credits = freight.Credits;
            StringAssert.Contains("gebaut", station.Use(miner.Inventory));
            Assert.AreEqual(credits - station.Settings.PumpPrice, freight.Credits);
            StringAssert.Contains("läuft", station.Use(miner.Inventory));
            yield return new WaitForSeconds(station.Settings.PumpSeconds * 3.3f);
            long pumped = station.Pump.Tank.Amount;
            Assert.GreaterOrEqual(pumped, 3 * Unit);
            Assert.AreEqual(station.Pump.Reservoir.Capacity - pumped, station.Pump.Reservoir.Remaining);
            Assert.Less(miner.Landscape.LakeSurface.localScale.x, 1, "The lake surface sinks.");
            StringAssert.Contains("übernommen", station.Use(miner.Inventory));
            long water = miner.Inventory.AmountOf(MaterialId.Water);
            Assert.GreaterOrEqual(water, pumped);

            var plant = miner.Landscape.PlantKeys.First(key => miner.HarvestPlant(miner.Inventory, key));
            Assert.AreEqual(Unit, miner.Inventory.AmountOf(MaterialId.Biomass));
            Assert.IsFalse(miner.HarvestPlant(miner.Inventory, plant), "A plant can only be harvested once.");

            string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".gz");
            try
            {
                StringAssert.Contains("gestoppt", station.Use(miner.Inventory));
                long tank = station.Pump.Tank.Amount;
                long remaining = station.Pump.Reservoir.Remaining;
                miner.Crew.Save(path);
                Assert.AreEqual("Expedition gespeichert", miner.Crew.Status);
                long pack = miner.Inventory.Amount;
                freight.Load(miner.Inventory, MaterialId.Water);
                freight.Load(miner.Inventory, MaterialId.Biomass);
                Assert.AreEqual(water, freight.Container(MaterialId.Water).Amount, "Water goes into the glass sphere.");
                miner.Crew.Load(path);
                Assert.AreEqual("Expedition geladen", miner.Crew.Status);
                Assert.AreEqual(pack, miner.Inventory.Amount);
                Assert.AreEqual(remaining, station.Pump.Reservoir.Remaining);
                Assert.AreEqual(tank, station.Pump.Tank.Amount);
                Assert.AreEqual(PumpState.Stopped, station.Pump.State);
                CollectionAssert.Contains(miner.Landscape.Harvested.ToArray(), plant);
                Assert.IsFalse(miner.HarvestPlant(miner.Inventory, plant));
            }
            finally
            {
                File.Delete(path);
                File.Delete(path + ".bak");
            }
        }
    }
}
