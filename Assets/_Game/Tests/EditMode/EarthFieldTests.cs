using NUnit.Framework;
using PlanetHarvest.Mining;
using PlanetHarvest.Planets;
using UnityEngine;

namespace PlanetHarvest.Tests
{
    public class EarthFieldTests
    {
        [Test]
        public void SeedReplaysHeightsCavesAndMaterialsRegardlessOfQueryOrder()
        {
            var a = new EarthPlanetField(7219);
            var b = new EarthPlanetField(7219);
            var other = new EarthPlanetField(-31);
            int different = 0;
            for (int x = -33; x <= 33; x += 3)
            for (int y = -33; y <= 33; y += 3)
            for (int z = -33; z <= 33; z += 3)
            {
                var p = new Vector3(x, y, z) + Vector3.one * 0.5f;
                b.MaterialAt(-p);
                Assert.AreEqual(a.Density(p), b.Density(p));
                Assert.AreEqual(a.MaterialAt(p), b.MaterialAt(p));
                if (a.MaterialAt(p) != other.MaterialAt(p)) different++;
            }
            Assert.Greater(different, 100);
        }

        [Test]
        public void LayersFollowLocalReliefAndOreIsRare()
        {
            var field = new EarthPlanetField(7219);
            int soil = 0, stone = 0, iron = 0, copper = 0, quartz = 0, caveAir = 0;
            for (int x = -field.Extent; x <= field.Extent; x += 2)
            for (int y = -field.Extent; y <= field.Extent; y += 2)
            for (int z = -field.Extent; z <= field.Extent; z += 2)
            {
                Vector3 p = new Vector3(x, y, z) + Vector3.one * 0.5f;
                var material = field.MaterialAt(p);
                float depth = field.Depth(p);
                switch (material)
                {
                    case MaterialId.None: if (depth > 3) caveAir++; break;
                    case MaterialId.Soil: Assert.That(depth, Is.InRange(0f, 2f)); soil++; break;
                    case MaterialId.Stone: Assert.Greater(depth, 2f); stone++; break;
                    case MaterialId.IronOre: Assert.GreaterOrEqual(depth, 7f); iron++; break;
                    case MaterialId.CopperOre: Assert.Greater(depth, 2f); copper++; break;
                    case MaterialId.Quartz: Assert.Greater(depth, 2f); quartz++; break;
                    default: Assert.Fail(material.ToString()); break;
                }
            }
            Assert.Greater(soil, 0);
            Assert.Greater(caveAir, 0);
            Assert.Greater(copper, 0);
            Assert.Greater(quartz, 0);
            Assert.Less(field.SurfaceRadius(EarthPlanetField.LakeDirection), field.WaterRadius);
            Assert.That(iron, Is.InRange(1, stone / 20));
        }
    }
}
