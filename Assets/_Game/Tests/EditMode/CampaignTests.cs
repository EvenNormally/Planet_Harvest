using System.Collections.Generic;
using NUnit.Framework;
using PlanetHarvest.Core;
using PlanetHarvest.Logistics;
using PlanetHarvest.Mining;
using PlanetHarvest.Planets;
using PlanetHarvest.Progression;
using PlanetHarvest.Voxel;
using UnityEditor;
using UnityEngine;

namespace PlanetHarvest.Tests
{
    public class CampaignTests
    {
        private const long Unit = HarvestInventory.CubicMetre;

        private static FreightProgram Program() =>
            AssetDatabase.LoadAssetAtPath<ElevatorContractSet>("Assets/_Game/ScriptableObjects/ElevatorContracts_Akt1.asset").CreateProgram();

        [Test]
        public void CrystalCaveIsHollowLinedWithCrystalsAndReachableByShaft()
        {
            var field = new EarthPlanetField(7219);
            Vector3 cave = field.CrystalCave;
            Assert.AreEqual(MaterialId.None, field.MaterialAt(cave));
            Assert.Greater(field.Depth(cave), 10, "The cave lies deep below the surface.");
            int crystals = 0;
            for (int x = -7; x <= 7; x++)
            for (int y = -7; y <= 7; y++)
            for (int z = -7; z <= 7; z++)
                if (field.MaterialAt(cave + new Vector3(x, y, z)) == MaterialId.Crystal) crystals++;
            Assert.Greater(crystals, 20);
            // The shaft is open all the way from the cave to the surface.
            Vector3 up = EarthPlanetField.CrystalDirection;
            for (float d = 0; d < EarthPlanetField.CrystalDepth; d += .5f)
                Assert.AreEqual(MaterialId.None, field.MaterialAt(cave + up * d), $"shaft blocked at {d} m");
            Assert.Greater(Vector3.Angle(up, field.SpawnDirection), 90, "Far from the landing site.");
        }

        [Test]
        public void EveryContractCanBeFulfilledWithTheFiniteWorld()
        {
            var program = Program();
            var required = new long[HarvestInventory.MaterialSlots];
            foreach (var contract in program.Contracts)
                foreach (var requirement in contract.Requirements)
                    required[(int)requirement.Material] += requirement.Volume;
            var terrain = new VoxelData(new EarthPlanetField(7219));
            var available = new long[HarvestInventory.MaterialSlots];
            int r = terrain.Radius;
            for (int x = -r; x < r; x++)
            for (int y = -r; y < r; y++)
            for (int z = -r; z < r; z++)
                available[(int)terrain.MaterialAt(new Vector3Int(x, y, z))] += Unit;
            var water = AssetDatabase.LoadAssetAtPath<WaterSettings>("Assets/_Game/ScriptableObjects/WaterAndPump.asset");
            available[(int)MaterialId.Water] = water.LakeCubicMetres * Unit;
            for (int m = 1; m < required.Length; m++)
            {
                if ((MaterialId)m == MaterialId.Biomass) continue; // plants are checked in the scene test
                Assert.GreaterOrEqual(available[m], required[m], $"{(MaterialId)m}: {available[m] / Unit} m³ for {required[m] / Unit} m³ required");
            }
            var acts = new HashSet<string>();
            foreach (var contract in program.Contracts) acts.Add(contract.Act);
            CollectionAssert.IsSubsetOf(new[] { "Akt 1", "Akt 2", "Akt 3" }, acts);
        }

        [Test]
        public void VersionFourArraysGrowForCrystal()
        {
            var state = new ExpeditionSnapshot
            {
                version = 4, host = new MinerSnapshot { amounts = new long[9] }, guest = new MinerSnapshot { amounts = new long[9] },
                freight = new FreightSnapshot { deliveredByMaterial = new long[9], loadsByMaterial = new long[9] }
            };
            state.freight.deliveredByMaterial[(int)MaterialId.Biomass] = 2 * Unit;
            state.Upgrade(TestFreight.Act1());
            Assert.AreEqual(ExpeditionSnapshot.CurrentVersion, state.version);
            Assert.AreEqual(HarvestInventory.MaterialSlots, state.freight.deliveredByMaterial.Length);
            Assert.AreEqual(2 * Unit, state.freight.deliveredByMaterial[(int)MaterialId.Biomass]);
            Assert.AreEqual(HarvestInventory.MaterialSlots, state.host.amounts.Length);
            Assert.IsTrue(MaterialId.Crystal.IsTerrain());
        }
    }
}
