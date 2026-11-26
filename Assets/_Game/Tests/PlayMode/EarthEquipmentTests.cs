using System.Collections;
using System.Linq;
using NUnit.Framework;
using PlanetHarvest.Logistics;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using PlanetHarvest.UI;
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
        public IEnumerator SelectedPickMinesOneCellAndResourceSlotDoesNotMine()
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
                Assert.AreEqual(HarvestInventory.CubicMetre, mining.Inventory.AmountOf(MaterialId.Soil));
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
                Assert.IsTrue(mining.Equipment.transform.GetChild(0).gameObject.activeSelf);
                Assert.AreEqual(1, mining.GetComponentsInChildren<Canvas>().Length);
                var glyphs = mining.GetComponentsInChildren<EquipmentGlyph>();
                Assert.AreEqual(4, glyphs.Length);
                foreach (var glyph in glyphs) Assert.IsNotNull(glyph.GetComponent<CanvasRenderer>());
                Assert.IsTrue(mining.Inventory.TryAdd(MaterialId.Soil, EarthFreight.SoilGoal * HarvestInventory.CubicMetre));
                yield return Press(pad, GamepadButton.RightShoulder);
                Assert.AreEqual(1, mining.Equipment.Slot);
                Assert.IsFalse(mining.Equipment.transform.GetChild(0).gameObject.activeSelf);
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
                Assert.AreEqual(1,dock.Freight.CompletedLevels);
                Assert.IsTrue(soil.GetComponent<Collider>().enabled);
                Assert.Less(Vector3.Distance(start,soil.transform.position), .01f);
            }
            finally { InputSystem.RemoveDevice(pad); }
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
