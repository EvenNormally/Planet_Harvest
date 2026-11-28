using System.Collections;
using System.Linq;
using NUnit.Framework;
using PlanetHarvest.Logistics;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using PlanetHarvest.Player;
using PlanetHarvest.UI;
using PlanetHarvest.Voxel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlanetHarvest.Tests
{
    public class EarthEquipmentTests
    {
        [UnityTest]
        public IEnumerator SelectedDrillCutsSoilAndResourceSlotDoesNotMine()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
                yield return new WaitForSeconds(.4f);
                var mining = Object.FindFirstObjectByType<EarthMining>();
                InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = Vector2.down });
                yield return new WaitForSeconds(.8f);
                InputSystem.QueueStateEvent(pad, new GamepadState());
                yield return null;
                Assert.AreEqual(MaterialId.Soil, mining.Target);
                yield return Press(pad, GamepadButton.RightShoulder);
                yield return Press(pad, GamepadButton.West);
                Assert.AreEqual(0, mining.Inventory.Amount);
                yield return Press(pad, GamepadButton.LeftShoulder);
                yield return Press(pad, GamepadButton.West);
                Assert.GreaterOrEqual(mining.Inventory.AmountOf(MaterialId.Soil), HarvestInventory.CubicMetre);
                Assert.Greater(mining.GetComponentInChildren<ParticleSystem>().particleCount, 0);
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [UnityTest]
        public IEnumerator HotbarAndElevatorWorkThroughPlayerInput()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
                yield return new WaitForSeconds(.4f);
                var mining = Object.FindFirstObjectByType<EarthMining>();
                var dock = mining.Elevator;
                var soil = dock.GetComponentsInChildren<EarthCargoBin>().Single(b => b.Material == MaterialId.Soil);
                Assert.IsTrue(mining.Equipment.ShowingTool);
                Assert.AreEqual(1, mining.GetComponentsInChildren<Canvas>().Length);
                var glyphs = mining.GetComponentsInChildren<EquipmentGlyph>();
                Assert.AreEqual(4, glyphs.Length);
                foreach (var glyph in glyphs) Assert.IsNotNull(glyph.GetComponent<CanvasRenderer>());
                Assert.IsTrue(mining.Inventory.TryAdd(MaterialId.Soil, dock.Freight.Container(MaterialId.Soil).Capacity));
                yield return Press(pad, GamepadButton.RightShoulder);
                Assert.AreEqual(1, mining.Equipment.Slot);
                Assert.IsFalse(mining.Equipment.ShowingTool);
                Assert.IsTrue(mining.Equipment.transform.GetChild(1).gameObject.activeSelf);

                // Approach the labelled front of the soil bin. The normal player ray must hit it.
                Vector3 up = dock.transform.up;
                var body = mining.GetComponent<Rigidbody>();
                body.position = soil.transform.position - dock.transform.forward * 2.5f + up * .35f;
                body.rotation = Quaternion.LookRotation(dock.transform.forward, up);
                body.linearVelocity = Vector3.zero;
                var camera = mining.GetComponentInChildren<Camera>();
                // A short look-down input puts the crosshair on the container front.
                InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = Vector2.down });
                yield return new WaitForSeconds(.22f);
                InputSystem.QueueStateEvent(pad, new GamepadState());
                yield return null;
                yield return null;
                Assert.AreSame(soil,mining.Bin,$"Camera {camera.transform.position} pointing {camera.transform.forward}");
                Vector3 start = soil.transform.position;
                yield return Press(pad, GamepadButton.North);
                yield return new WaitForSeconds(.3f);
                Assert.AreEqual(MaterialId.Soil,dock.Freight.Flying);
                Assert.IsFalse(soil.GetComponent<Collider>().enabled);
                Assert.Greater(Vector3.Dot(soil.transform.position - start, up), .1f);
                Assert.AreEqual(0,mining.Inventory.Amount);
                Assert.IsFalse(mining.Equipment.transform.GetChild(1).gameObject.activeSelf);
                yield return new WaitForSeconds(EarthFreight.LegSeconds * 2 + .2f);
                Assert.AreEqual(dock.Freight.Container(MaterialId.Soil).Capacity, dock.Freight.Delivered(MaterialId.Soil));
                Assert.AreEqual(0,dock.Freight.CompletedContracts, "Soil alone does not fulfil the landing contract.");
                Assert.IsTrue(soil.GetComponent<Collider>().enabled);
                Assert.Less(Vector3.Distance(start,soil.transform.position), .01f);
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [UnityTest]
        public IEnumerator InventoryAndWorkshopUsePlayerInputAndPurchasedDrillMinesContinuously()
        {
#if UNITY_EDITOR
            bool previousBackground = Application.runInBackground;
            var previousBehavior = InputSystem.settings.backgroundBehavior;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var previousRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
                yield return new WaitForSeconds(.5f);
                var mining = Object.FindFirstObjectByType<EarthMining>();
                yield return PressKey(keyboard, Key.I);
                Assert.AreEqual(FieldMenu.Inventory, mining.Menu);
                Assert.IsFalse(mining.GetComponent<RadialPlayer>().Captured);
                Assert.IsFalse(mining.ChooseTool(MiningTool.Drill));
                yield return PressKey(keyboard, Key.Escape);
                Assert.AreEqual(FieldMenu.Closed, mining.Menu);
                Assert.IsTrue(mining.GetComponent<RadialPlayer>().Captured);
                var freight = mining.Elevator.Freight;
                foreach (var type in new[] { MaterialId.Soil, MaterialId.Stone })
                {
                    long load = freight.Container(type).Capacity;
                    Assert.IsTrue(mining.Inventory.TryAdd(type, load));
                    freight.Load(mining.Inventory,type);
                    freight.Advance(EarthFreight.LegSeconds * 2);
                }
                Assert.AreEqual(1, freight.CompletedContracts);
                long earned = freight.Credits;
                var station = mining.Workshop.transform;
                var body = mining.GetComponent<Rigidbody>();
                body.position = station.position - station.forward * 2.8f + station.up * 1.2f;
                body.rotation = Quaternion.LookRotation(station.forward, station.up);
                body.linearVelocity = Vector3.zero;
                yield return new WaitForSeconds(.2f);
                Assert.IsTrue(mining.AtWorkshop);
                yield return Press(pad, GamepadButton.North);
                Assert.AreEqual(FieldMenu.Workshop, mining.Menu);
                yield return PressKey(keyboard, Key.Digit3);
                Assert.AreEqual(MiningTool.Drill, mining.Equipment.Tools.Active);
                Assert.AreEqual(earned - MiningTools.Spec(MiningTool.Drill).Price, freight.Credits);
                yield return PressKey(keyboard, Key.Escape);
                InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = Vector2.down });
                yield return new WaitForSeconds(.8f);
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.West));
                yield return new WaitForSeconds(.65f);
                Assert.GreaterOrEqual(mining.Inventory.Amount, HarvestInventory.CubicMetre * 2);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = previousRouting;
                InputSystem.settings.backgroundBehavior = previousBehavior;
                Application.runInBackground = previousBackground;
#endif
                InputSystem.RemoveDevice(pad);
            }
        }

        [UnityTest]
        public IEnumerator WorkshopUpgradesCostSharedCreditsAndSurviveSaveAndLoad()
        {
#if UNITY_EDITOR
            bool previousBackground = Application.runInBackground;
            var previousBehavior = InputSystem.settings.backgroundBehavior;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var previousRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var pad = InputSystem.AddDevice<Gamepad>();
            string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid() + ".gz");
            try
            {
                yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
                yield return new WaitForSeconds(.5f);
                var mining = Object.FindFirstObjectByType<EarthMining>();
                var freight = mining.Elevator.Freight;
                var upgrades = mining.Equipment.Upgrades;
                Assert.AreEqual(EarthMining.PackCapacity, mining.Inventory.Capacity);
                Assert.IsFalse(mining.ChooseUpgrade(UpgradeAxis.Capacity), "Upgrades are only sold at the workbench.");
                // Real terrain only: the save checks that every booked m³ was removed from the planet.
                foreach (var type in new[] { MaterialId.Soil, MaterialId.Stone })
                {
                    Harvest(mining.Planet, mining.Inventory, type, freight.Container(type).Capacity / HarvestInventory.CubicMetre);
                    freight.Load(mining.Inventory, type);
                    freight.Advance(EarthFreight.LegSeconds * 2);
                }
                Assert.AreEqual(1, freight.CompletedContracts);
                long earned = freight.Credits;
                var station = mining.Workshop.transform;
                var body = mining.GetComponent<Rigidbody>();
                body.position = station.position - station.forward * 2.8f + station.up * 1.2f;
                body.rotation = Quaternion.LookRotation(station.forward, station.up);
                body.linearVelocity = Vector3.zero;
                yield return new WaitForSeconds(.2f);
                Assert.IsTrue(mining.AtWorkshop);
                yield return Press(pad, GamepadButton.North);
                Assert.AreEqual(FieldMenu.Workshop, mining.Menu);
                yield return PressKey(keyboard, Key.Digit6);
                Assert.AreEqual(1, upgrades.Level(UpgradeAxis.Capacity));
                Assert.AreEqual(upgrades.Capacity, mining.Inventory.Capacity);
                Assert.Greater(mining.Inventory.Capacity, EarthMining.PackCapacity);
                yield return PressKey(keyboard, Key.Digit4);
                Assert.AreEqual(1, upgrades.Level(UpgradeAxis.Radius));
                long spent = upgrades.Catalog.Step(UpgradeAxis.Capacity, 1).Price + upgrades.Catalog.Step(UpgradeAxis.Radius, 1).Price;
                Assert.AreEqual(earned - spent, freight.Credits);
                yield return PressKey(keyboard, Key.Escape);
                Harvest(mining.Planet, mining.Inventory, MaterialId.Stone, mining.Inventory.Capacity / HarvestInventory.CubicMetre);

                mining.Crew.Save(path);
                Assert.IsTrue(System.IO.File.Exists(path), mining.Crew.Status);
                long capacity = mining.Inventory.Capacity;
                // Reset the player's state, then load: levels, the bigger pack and its contents return.
                mining.Equipment.Upgrades.Restore(new int[UpgradeCatalog.AxisCount], null);
                mining.Crew.Load(path);
                Assert.AreEqual("Expedition geladen", mining.Crew.Status);
                Assert.AreEqual(1, upgrades.Level(UpgradeAxis.Capacity));
                Assert.AreEqual(1, upgrades.Level(UpgradeAxis.Radius));
                Assert.AreEqual(capacity, mining.Inventory.Capacity);
                Assert.AreEqual(capacity, mining.Inventory.AmountOf(MaterialId.Stone));
                Assert.AreEqual(earned - spent, freight.Credits);
            }
            finally
            {
                System.IO.File.Delete(path);
                System.IO.File.Delete(path + ".bak");
                InputSystem.RemoveDevice(keyboard);
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = previousRouting;
                InputSystem.settings.backgroundBehavior = previousBehavior;
                Application.runInBackground = previousBackground;
#endif
                InputSystem.RemoveDevice(pad);
            }
        }

        [UnityTest]
        public IEnumerator UpgradedRadiusCutsMoreTerrainWithExactBooking()
        {
            yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
            yield return null;
            var mining = Object.FindFirstObjectByType<EarthMining>();
            var moon = mining.Planet;
            var changes = new System.Collections.Generic.List<Vector3Int>();
            var basePack = new HarvestInventory(4000 * HarvestInventory.CubicMetre);
            var upgradedPack = new HarvestInventory(basePack.Capacity);
            DrillStroke.Cut(moon, basePack, new Vector3Int(-10, 0, 0), Vector3.up, DrillProfile.Bowl, 1, _ => false, changes);
            int baseCells = changes.Count;
            DrillStroke.Cut(moon, upgradedPack, new Vector3Int(10, 0, 0), Vector3.up, DrillProfile.Bowl, 1.65f, _ => false, changes);
            int upgradedCells = changes.Count;
            Assert.Greater(upgradedCells, baseCells);
            Assert.AreEqual(baseCells * HarvestInventory.CubicMetre, basePack.Amount);
            Assert.AreEqual(upgradedCells * HarvestInventory.CubicMetre, upgradedPack.Amount);
            Assert.AreEqual(moon.Data.InitialCount - baseCells - upgradedCells, moon.Data.SolidCount);
        }

        /// <summary>Mines exactly the given volume of one material on the far side of the planet into a pack.</summary>
        private static void Harvest(VoxelPlanet moon, HarvestInventory into, MaterialId material, long cubicMetres)
        {
            var scoop = new HarvestInventory(cubicMetres * HarvestInventory.CubicMetre);
            var changes = new System.Collections.Generic.List<Vector3Int>();
            int r = moon.Data.Radius;
            Vector3 spawn = moon.Earth.SpawnDirection;
            for (int x = -r; x < r && scoop.Free > 0; x++)
            for (int y = -r; y < r && scoop.Free > 0; y++)
            for (int z = -r; z < r && scoop.Free > 0; z++)
            {
                var cell = new Vector3Int(x, y, z);
                if (Vector3.Dot(cell, spawn) > -8 || moon.Data.MaterialAt(cell) != material) continue;
                DrillStroke.Cut(moon, scoop, cell, Vector3.up, DrillProfile.Bowl, 1, c => moon.Data.MaterialAt(c) != material, changes);
            }
            Assert.AreEqual(0, scoop.Free, $"Not enough {material} found.");
            Assert.AreEqual(scoop.Amount, scoop.TransferTo(into, material));
        }

        private static IEnumerator PressKey(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            Assert.IsTrue(keyboard[key].isPressed, $"Synthetic {key} was not delivered. Device enabled={keyboard.enabled}, focus={Application.isFocused}.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private static IEnumerator Press(Gamepad pad, GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null;
        }
    }
}
