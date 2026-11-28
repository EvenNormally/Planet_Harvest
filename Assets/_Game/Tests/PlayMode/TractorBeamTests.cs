using System;
using System.Collections;
using System.IO;
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
    public class TractorBeamTests
    {
        private const long Unit = HarvestInventory.CubicMetre;

        private static Vector3 Ground(EarthMining miner, Vector3 offset)
        {
            var dock = miner.Elevator.transform;
            Vector3 probe = dock.position + dock.TransformDirection(offset);
            Vector3 up = (probe - miner.Planet.transform.position).normalized;
            Assert.IsTrue(Physics.Raycast(probe + up * 12, -up, out var hit, 30, ~0, QueryTriggerInteraction.Ignore));
            return hit.point;
        }

        [UnityTest]
        public IEnumerator MastChainCarriesMinedStoneIntoTheElevatorAndSurvivesSaveLoad()
        {
            yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
            yield return null;
            var miner = Object.FindFirstObjectByType<EarthMining>();
            var freight = miner.Elevator.Freight;
            var beams = miner.Beams;
            Assert.IsFalse(beams.TryBuild(Ground(miner, new Vector3(0, 0, -12)), freight, out var locked));
            StringAssert.Contains("Landevertrag", locked);

            // Earn the unlock and enough credits with real terrain so the save balance holds.
            foreach (var (type, volume) in new[] { (MaterialId.Soil, 20L), (MaterialId.Stone, 12L), (MaterialId.Stone, 12L) })
            {
                EarthEquipmentTests.Harvest(miner.Planet, miner.Inventory, type, volume);
                freight.Load(miner.Inventory, type);
                freight.Advance(EarthFreight.LegSeconds * 2);
            }
            Assert.AreEqual(1, freight.CompletedContracts);
            long credits = freight.Credits;
            Assert.IsTrue(beams.TryBuild(Ground(miner, new Vector3(0, 0, -12)), freight, out var reason), reason);
            Assert.IsFalse(beams.TryBuild(Ground(miner, new Vector3(0, 0, -30)), freight, out reason), "Out of range.");
            Assert.IsTrue(beams.TryBuild(Ground(miner, new Vector3(0, 0, -6)), freight, out reason), reason);
            Assert.AreEqual(credits - beams.Settings.HopperPrice - beams.Settings.MastPrice, freight.Credits);
            Assert.IsTrue(beams.Connected, "The mast next to the elevator links to the intake.");
            yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<BeamHopper>());

            EarthEquipmentTests.Harvest(miner.Planet, miner.Inventory, MaterialId.Stone, 5);
            Assert.AreEqual(5 * Unit, beams.LoadHopper(miner.Inventory));
            yield return new WaitForSeconds(beams.Settings.StepSeconds * 3.2f);
            Assert.Greater(beams.Chain.LumpCount, 0, "Lumps float in the beam.");
            string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".gz");
            try
            {
                miner.Crew.Save(path);
                Assert.AreEqual("Expedition gespeichert", miner.Crew.Status);
                var saved = beams.Capture();
                long savedBin = freight.Container(MaterialId.Stone).Amount;
                yield return new WaitForSeconds(beams.Settings.StepSeconds * 12);
                Assert.AreEqual(5 * Unit, freight.Container(MaterialId.Stone).Amount, "All five lumps reached the stone container.");
                Assert.AreEqual(0, beams.Chain.LumpCount);

                miner.Crew.Load(path);
                Assert.AreEqual("Expedition geladen", miner.Crew.Status);
                Assert.AreEqual(1, beams.MastCount);
                Assert.AreEqual(BeamChain.Holding(saved.chain, MaterialId.Stone), beams.Chain.Lane.Amount + beams.Chain.Hopper.Amount);
                Assert.AreEqual(savedBin, freight.Container(MaterialId.Stone).Amount);
                yield return new WaitForSeconds(beams.Settings.StepSeconds * 12);
                Assert.AreEqual(5 * Unit, freight.Container(MaterialId.Stone).Amount, "After loading, delivery completes exactly once.");

                long before = freight.Credits;
                Assert.IsTrue(beams.TryRemoveLast(freight));
                Assert.AreEqual(before + beams.Settings.MastPrice, freight.Credits);
                Assert.IsFalse(beams.Connected);
            }
            finally
            {
                File.Delete(path);
                File.Delete(path + ".bak");
            }
        }
    }
}
