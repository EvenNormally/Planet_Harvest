using System;
using System.IO;
using NUnit.Framework;
using PlanetHarvest.Core;
using PlanetHarvest.Mining;
using PlanetHarvest.Player;
using PlanetHarvest.Progression;
using UnityEditor;
using UnityEngine;

namespace PlanetHarvest.Tests
{
    public class ToolUpgradeTests
    {
        private const long Unit = HarvestInventory.CubicMetre;

        private static UpgradeCatalog Catalog() => new UpgradeCatalog(
            new[] { new UpgradeStep(0, 1), new UpgradeStep(25, 1.2f), new UpgradeStep(60, 1.4f) },
            new[] { new UpgradeStep(0, 1), new UpgradeStep(20, 2) },
            new[] { new UpgradeStep(0, 24), new UpgradeStep(20, 32) });

        /// <summary>Wallet with 60 credits from the landing contract.</summary>
        private static EarthFreight Wallet()
        {
            var freight = new EarthFreight(TestFreight.Act1());
            TestFreight.Deliver(freight, MaterialId.Soil, 20);
            TestFreight.Deliver(freight, MaterialId.Stone, 12);
            Assert.AreEqual(60, freight.Credits);
            return freight;
        }

        [Test]
        public void PurchaseNeedsWorkbenchAndEnoughSharedCredits()
        {
            var wallet = Wallet();
            var upgrades = new ToolUpgrades(Catalog());
            var pack = new HarvestInventory(24 * Unit);
            Assert.IsFalse(upgrades.TryBuy(UpgradeAxis.Radius, wallet, false, pack), "Away from the workbench.");
            Assert.AreEqual(60, wallet.Credits);
            Assert.IsTrue(upgrades.TryBuy(UpgradeAxis.Radius, wallet, true, pack));
            Assert.AreEqual(35, wallet.Credits);
            Assert.AreEqual(1.2f, upgrades.RadiusMultiplier);
            Assert.IsFalse(upgrades.TryBuy(UpgradeAxis.Radius, wallet, true, pack), "60 credits needed, 35 left.");
            Assert.AreEqual(1, upgrades.Level(UpgradeAxis.Radius));
            Assert.AreEqual(35, wallet.Credits);
            Assert.IsTrue(upgrades.TryBuy(UpgradeAxis.Speed, wallet, true, pack));
            Assert.AreEqual(.65f / 2, upgrades.RepeatSeconds(.65f), 1e-6);
            Assert.IsTrue(upgrades.IsMaxed(UpgradeAxis.Speed));
            Assert.IsFalse(upgrades.TryBuy(UpgradeAxis.Speed, wallet, true, pack), "No level beyond the maximum.");
            Assert.AreEqual(15, wallet.Credits);
            Assert.IsFalse(upgrades.TryBuy((UpgradeAxis)7, wallet, true, pack));
        }

        [Test]
        public void CreditsAreSharedButEquipmentBelongsToEachPlayer()
        {
            var wallet = Wallet();
            var host = new ToolUpgrades(Catalog());
            var guest = new ToolUpgrades(Catalog());
            var hostPack = new HarvestInventory(24 * Unit);
            var guestPack = new HarvestInventory(24 * Unit);
            Assert.IsTrue(guest.TryBuy(UpgradeAxis.Capacity, wallet, true, guestPack));
            Assert.AreEqual(40, wallet.Credits);
            Assert.AreEqual(32 * Unit, guestPack.Capacity);
            Assert.AreEqual(24 * Unit, hostPack.Capacity);
            Assert.AreEqual(0, host.Level(UpgradeAxis.Capacity));
            Assert.IsTrue(host.TryBuy(UpgradeAxis.Radius, wallet, true, hostPack));
            Assert.AreEqual(15, wallet.Credits);
            Assert.AreEqual(1f, guest.RadiusMultiplier);
        }

        [Test]
        public void LevelsSurviveSaveRoundtripAndOldSavesLoadAsLevelZero()
        {
            var catalog = Catalog();
            var wallet = Wallet();
            var upgrades = new ToolUpgrades(catalog);
            var pack = new HarvestInventory(24 * Unit);
            upgrades.TryBuy(UpgradeAxis.Capacity, wallet, true, pack);
            upgrades.TryBuy(UpgradeAxis.Speed, wallet, true, pack);
            Assert.IsTrue(pack.TryAdd(MaterialId.Soil, 30 * Unit), "The bigger pack holds more than 24 m³.");
            var tools = new MiningTools();
            var saved = MinerSnapshot.Capture(pack, tools, upgrades, Vector3.zero, Quaternion.identity, DrillProfile.Bowl);
            var json = JsonUtility.ToJson(saved);
            var loaded = JsonUtility.FromJson<MinerSnapshot>(json);
            loaded.Validate(catalog);
            var restored = new ToolUpgrades(catalog);
            var restoredPack = new HarvestInventory(24 * Unit);
            loaded.Apply(restoredPack, new MiningTools(), restored);
            Assert.AreEqual(32 * Unit, restoredPack.Capacity);
            Assert.AreEqual(30 * Unit, restoredPack.AmountOf(MaterialId.Soil));
            Assert.AreEqual(2f, restored.SpeedMultiplier);
            Assert.AreEqual(0, restored.Level(UpgradeAxis.Radius));

            // Older saves have no upgrade field at all.
            var legacy = JsonUtility.FromJson<MinerSnapshot>(json.Replace(",\"upgrades\":[0,1,1]", ""));
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, legacy.upgrades);
            Assert.Throws<InvalidDataException>(() => legacy.Validate(catalog), "30 m³ does not fit the base pack.");

            loaded.upgrades = new[] { 0, 5, 1 };
            Assert.Throws<InvalidDataException>(() => loaded.Validate(catalog));
        }

        [Test]
        public void CatalogRejectsFallingOrFractionalSteps()
        {
            var one = new[] { new UpgradeStep(0, 1) };
            Assert.Throws<ArgumentException>(() => new UpgradeCatalog(
                new[] { new UpgradeStep(0, 1), new UpgradeStep(5, .8f) }, one, new[] { new UpgradeStep(0, 24) }));
            Assert.Throws<ArgumentException>(() => new UpgradeCatalog(one, one, new[] { new UpgradeStep(0, 24.5f) }));
            Assert.Throws<ArgumentException>(() => new UpgradeCatalog(new[] { new UpgradeStep(0, 2) }, one, new[] { new UpgradeStep(0, 24) }));
            var pack = new HarvestInventory(10 * Unit);
            pack.TryAdd(MaterialId.Stone, 8 * Unit);
            Assert.Throws<ArgumentOutOfRangeException>(() => pack.Resize(7 * Unit));
        }

        [Test]
        public void ConfiguredUpgradeAssetStartsAtTheBasePack()
        {
            var set = AssetDatabase.LoadAssetAtPath<ToolUpgradeSet>("Assets/_Game/ScriptableObjects/ToolUpgrades.asset");
            Assert.NotNull(set);
            var catalog = set.CreateCatalog();
            Assert.AreEqual(EarthMining.PackCapacity, catalog.CapacityAt(0));
            for (int axis = 0; axis < UpgradeCatalog.AxisCount; axis++)
                Assert.GreaterOrEqual(catalog.MaxLevel((UpgradeAxis)axis), 1);
        }
    }
}
