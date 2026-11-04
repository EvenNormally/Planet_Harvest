using System.Collections;
using NUnit.Framework;
using PlanetHarvest.Core;
using PlanetHarvest.Logistics;
using PlanetHarvest.Mining;
using PlanetHarvest.Player;
using PlanetHarvest.Voxel;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlanetHarvest.Tests
{
    public class HarvestLoopTests
    {
        [UnityTest]
        public IEnumerator MineTransferShipAndRepeatThroughSceneComponents()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Game/Scenes/PlanetHarvest.unity", LoadSceneMode.Single);
            yield return null;
            var moon = Object.FindFirstObjectByType<VoxelMoon>();
            var game = Object.FindFirstObjectByType<HarvestGame>();
            var interaction = Object.FindFirstObjectByType<HarvestInteraction>();
            var player = Object.FindFirstObjectByType<RadialPlayer>();
            Assert.That(player, Is.Not.Null);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                    Assert.That(component, Is.Not.Null, "Missing scene component");
            yield return new WaitForSeconds(0.5f);
            Assert.That(player.transform.position.magnitude, Is.InRange(11f, 15f), "Player must remain on moon");
            var ray = new Ray(new Vector3(-0.5f, 16, -0.5f), Vector3.down);
            Assert.That(Physics.Raycast(ray, out var hit, 10), Is.True);
            float surface = hit.point.y;
            Vector3Int cell = moon.CellAt(hit);
            int before = moon.Data.SolidCount;
            Assert.That(interaction.Mine(hit), Is.True);
            Assert.That(moon.Data.SolidCount, Is.EqualTo(before - 1));
            Assert.That(game.Session.Buffer.Amount, Is.EqualTo(1000000));
            Physics.SyncTransforms();
            Assert.That(Physics.Raycast(ray, out var below, 10), Is.True);
            Assert.That(below.point.y, Is.EqualTo(surface - 1).Within(0.01f), "Collider must expose the real hole");
            Assert.That(interaction.Mine(below), Is.False, "Full buffer must stop terrain removal");
            HarvestTerminal container = null, elevator = null;
            foreach (var terminal in Object.FindObjectsByType<HarvestTerminal>(FindObjectsSortMode.None))
                if (terminal.IsElevator) elevator = terminal; else container = terminal;
            Assert.That(interaction.UseTerminal(container), Is.True);
            Assert.That(game.Session.Buffer.Amount, Is.Zero);
            Assert.That(game.Session.Container.Amount, Is.EqualTo(1000000));
            Assert.That(moon.TryMine(game.Session, cell), Is.False, "Removed cell is air");
            Assert.That(interaction.UseTerminal(elevator), Is.True);
            Assert.That(interaction.UseTerminal(elevator), Is.False);
            Assert.That(game.Session.Credits, Is.EqualTo(game.Session.ShipmentPrice));
            Assert.That(interaction.Mine(below), Is.True);
            Assert.That(interaction.UseTerminal(container), Is.True);
            Assert.That(interaction.UseTerminal(elevator), Is.True);
            Assert.That(game.Session.Credits, Is.EqualTo(2 * game.Session.ShipmentPrice));
            Assert.That(game.Session.Container.Amount, Is.Zero);
            // Cell (-1,11,-1) borders two chunks: the neighbouring faces must be rebuilt too.
            Assert.That(moon.Data.IsSolid(cell), Is.False);
        }
    }
}
