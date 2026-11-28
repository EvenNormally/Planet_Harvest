using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PlanetHarvest.Machines;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlanetHarvest.Tests
{
    public class DrillRobotPlayTests
    {
        private const long Unit = HarvestInventory.CubicMetre;

        [UnityTest]
        public IEnumerator RobotDigsOnlyItsAreaRespectsTheBaseAndSurvivesSaveLoad()
        {
            yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
            yield return null;
            var miner = Object.FindFirstObjectByType<EarthMining>();
            var freight = miner.Elevator.Freight;
            var unit = miner.Robot;
            var moon = miner.Planet;
            foreach (var (type, volume) in new[] { (MaterialId.Soil, 20L), (MaterialId.Stone, 12L), (MaterialId.Stone, 12L) })
            {
                EarthEquipmentTests.Harvest(moon, miner.Inventory, type, volume);
                freight.Load(miner.Inventory, type);
                freight.Advance(EarthFreight.LegSeconds * 2);
            }
            Assert.IsFalse(unit.TryPlace(miner.Elevator.transform.position + miner.Elevator.transform.forward * 3, out var reason));
            StringAssert.Contains("Schutzzone", reason);

            Vector3 down = moon.transform.TransformDirection(-moon.Earth.SpawnDirection);
            Assert.IsTrue(Physics.Raycast(moon.transform.position + down * (moon.Earth.Extent + 10), -down, out var ground, 40));
            long credits = freight.Credits;
            Assert.IsTrue(unit.TryPlace(ground.point, out reason), reason);
            Assert.AreEqual(credits - unit.Settings.Price, freight.Credits);
            Assert.AreEqual(RobotState.Stopped, unit.Robot.State);
            var before = moon.Data.Removed.ToHashSet();
            StringAssert.Contains("läuft", unit.Use(miner.Inventory));
            yield return new WaitForSeconds(unit.Settings.StrokeSeconds * 6.5f);
            Assert.Greater(unit.Robot.Strokes, 0, "The robot cut terrain.");
            var dug = moon.Data.Removed.Where(c => !before.Contains(c)).ToArray();
            Assert.IsNotEmpty(dug);
            foreach (var cell in dug) Assert.IsTrue(unit.Robot.Area.Contains(cell), $"{cell} outside the work area");
            Assert.AreEqual(dug.Length * Unit, unit.Robot.Hopper.Amount, "Every robot cell is booked in its hopper.");

            string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".gz");
            try
            {
                miner.Crew.Save(path);
                Assert.AreEqual("Expedition gespeichert", miner.Crew.Status);
                int cursor = unit.Robot.Cursor;
                long load = unit.Robot.Hopper.Amount;
                var savedState = unit.Robot.State;
                yield return new WaitForSeconds(unit.Settings.StrokeSeconds * 3);
                miner.Crew.Load(path);
                Assert.AreEqual("Expedition geladen", miner.Crew.Status);
                Assert.AreEqual(cursor, unit.Robot.Cursor);
                Assert.AreEqual(load, unit.Robot.Hopper.Amount);
                Assert.AreEqual(savedState, unit.Robot.State);

                StringAssert.Contains("übernommen", unit.Use(miner.Inventory));
                Assert.AreEqual(load, miner.Inventory.Amount);
                unit.Use(miner.Inventory);
                Assert.AreEqual(RobotState.Stopped, unit.Robot.State);
                Assert.IsTrue(unit.TryRemove());
                Assert.IsFalse(unit.Robot.Placed);
            }
            finally
            {
                File.Delete(path);
                File.Delete(path + ".bak");
            }
        }
    }
}
