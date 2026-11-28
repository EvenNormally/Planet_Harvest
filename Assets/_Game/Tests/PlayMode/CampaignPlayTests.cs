using System.Collections;
using NUnit.Framework;
using PlanetHarvest.Mining;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlanetHarvest.Tests
{
    public class CampaignPlayTests
    {
        [UnityTest]
        public IEnumerator ConfettiMushroomPopsAndPlantsCoverTheBiomassContracts()
        {
            yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
            yield return null;
            var miner = Object.FindFirstObjectByType<EarthMining>();
            var landscape = miner.Landscape;
            Assert.NotNull(landscape.Mushroom);
            Vector3 cap = landscape.Mushroom.position + landscape.Mushroom.up * .5f;
            Vector3 eye = cap + landscape.Mushroom.up * 2;
            Assert.IsTrue(landscape.TryPopMushroom(eye, -landscape.Mushroom.up, 5));
            Assert.AreEqual(1, landscape.ConfettiPops);
            Assert.IsFalse(landscape.TryPopMushroom(eye, -landscape.Mushroom.up, 5), "It needs a moment to recover.");
            Assert.IsFalse(landscape.TryPopMushroom(eye, landscape.Mushroom.up, 5), "Looking away does nothing.");
            yield return new WaitForSeconds(1.4f);
            Assert.IsTrue(landscape.TryPopMushroom(eye, -landscape.Mushroom.up, 5));
            Assert.AreEqual(2, landscape.ConfettiPops);

            long biomass = 0;
            foreach (var contract in miner.Elevator.Freight.Program.Contracts)
                foreach (var requirement in contract.Requirements)
                    if (requirement.Material == MaterialId.Biomass) biomass += requirement.CubicMetres;
            Assert.GreaterOrEqual(landscape.PlantCount * miner.Elevator.WaterSettings.BiomassPerPlant, biomass);
            Assert.AreEqual(8, miner.Elevator.Freight.Program.Materials.Count, "Eight elevator containers incl. crystal.");
        }
    }
}
