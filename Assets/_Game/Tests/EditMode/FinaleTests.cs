using System;
using System.IO;
using NUnit.Framework;
using PlanetHarvest.Core;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using PlanetHarvest.UI;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Tests
{
    public class FinaleTests
    {
        private const long Unit = HarvestInventory.CubicMetre;

        [Test]
        public void SettingsAreClampedSavedAndSurviveDamagedFiles()
        {
            var settings = new GameSettings { masterVolume = 3, uiScale = .1f, quality = 99, language = (GameLanguage)7 }.Sanitize(4);
            Assert.AreEqual(1, settings.masterVolume);
            Assert.AreEqual(GameSettings.MinUiScale, settings.uiScale);
            Assert.AreEqual(-1, settings.quality);
            Assert.AreEqual(GameLanguage.German, settings.language);
            string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".json");
            try
            {
                new GameSettings { masterVolume = .3f, uiScale = 1.25f, quality = 2, language = GameLanguage.English, bindings = "{}" }.Save(path);
                var loaded = GameSettings.Load(path, 4);
                Assert.AreEqual(.3f, loaded.masterVolume, 1e-6);
                Assert.AreEqual(1.25f, loaded.uiScale, 1e-6);
                Assert.AreEqual(2, loaded.quality);
                Assert.AreEqual(GameLanguage.English, loaded.language);
                Assert.AreEqual("{}", loaded.bindings);
                File.WriteAllText(path, "{ kaputt");
                var fallback = GameSettings.Load(path, 4);
                Assert.AreEqual(.8f, fallback.masterVolume, 1e-6);
                Assert.AreEqual(GameLanguage.German, fallback.language);
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void EveryTextExistsInGermanAndEnglish()
        {
            foreach (var entry in Localization.All)
            {
                Assert.IsNotEmpty(entry.Value.de, entry.Key);
                Assert.IsNotEmpty(entry.Value.en, entry.Key);
            }
            var previous = Localization.Language;
            try
            {
                Localization.Language = GameLanguage.English;
                Assert.AreEqual("Settings", Localization.Text("settings.title"));
                Localization.Language = GameLanguage.German;
                Assert.AreEqual("Einstellungen", Localization.Text("settings.title"));
                Assert.AreEqual("unbekannt.key", Localization.Text("unbekannt.key"));
                StringAssert.Contains("12", Localization.Format("scanner.found", "Stein", 12f));
            }
            finally { Localization.Language = previous; }
        }

        [Test]
        public void ResidueScannerFindsTheNearestRemainingCellAndNothingOnAnEmptyWorld()
        {
            var terrain = new VoxelData(6);
            Assert.IsTrue(ResidueScanner.TryFindNearest(terrain, new Vector3Int(0, 9, 0), 10, out var cell, out var material));
            Assert.AreNotEqual(MaterialId.None, material);
            Assert.AreEqual(0, cell.x, 1);
            Assert.AreEqual(0, cell.z, 1);
            Assert.GreaterOrEqual(cell.y, 4, "Nearest is the top of the sphere.");
            var session = new HarvestSession(100, Unit, 100000);
            for (int x = -6; x < 6; x++) for (int y = -6; y < 6; y++) for (int z = -6; z < 6; z++)
                if (session.TryMine(terrain, new Vector3Int(x, y, z), out var mined)) session.Transfer(mined);
            Assert.AreEqual(0, terrain.SolidCount);
            Assert.IsFalse(ResidueScanner.TryFindNearest(terrain, Vector3Int.zero, 10, out _, out _));
        }

        [Test]
        public void EndingNeedsAllContractsAndTheSolidPlanetInOrbit()
        {
            var freight = new EarthFreight(TestFreight.Act1());
            var nearlyAll = new PlanetProgress(1000 * Unit, 999 * Unit, 0, 0, 0);
            Assert.IsFalse(ExpeditionEnding.IsComplete(nearlyAll, freight.Contracts), "Contracts still open.");
            foreach (var contract in freight.Program.Contracts)
                foreach (var requirement in contract.Requirements)
                    TestFreight.Deliver(freight, requirement.Material, requirement.CubicMetres);
            Assert.IsTrue(ExpeditionEnding.IsComplete(nearlyAll, freight.Contracts));
            Assert.IsFalse(ExpeditionEnding.IsComplete(new PlanetProgress(1000 * Unit, 900 * Unit, 0, 0, 0), freight.Contracts));
        }
    }
}
