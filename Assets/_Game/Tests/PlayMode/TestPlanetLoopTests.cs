using System.Collections;
using System.Linq;
using NUnit.Framework;
using PlanetHarvest.Core;
using PlanetHarvest.Logistics;
using PlanetHarvest.Mining;
using PlanetHarvest.Player;
using PlanetHarvest.Voxel;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace PlanetHarvest.Tests
{
    public class TestPlanetLoopTests
    {
        [UnityTest]
        public IEnumerator TwoShipmentsReturnEmptyAndMiningUpdatesTheCollider()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/Scenes/PlanetHarvest_Test200m.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("Assets/_Game/Scenes/PlanetHarvest_Test200m.unity");
#endif
            yield return null;
            var moon = Object.FindFirstObjectByType<VoxelMoon>();
            var game = Object.FindFirstObjectByType<HarvestGame>();
            var interaction = Object.FindFirstObjectByType<HarvestInteraction>();
            var terminals = Object.FindObjectsByType<HarvestTerminal>(FindObjectsSortMode.None);
            Assert.That(terminals.Length, Is.EqualTo(2));
            var player = Object.FindFirstObjectByType<RadialPlayer>();
            Assert.That(moon.Data.Radius * moon.CellSize, Is.EqualTo(200));
            Assert.That(moon.GetComponentsInChildren<SphereCollider>().Length, Is.Zero, "Preview must not block mining");
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                    Assert.That(component, Is.Not.Null, "Missing scene component");
            yield return new WaitForSeconds(0.5f);
            Assert.That(player.transform.position.magnitude, Is.InRange(200.5f, 202f));
            for (int trip = 0; trip < 2; trip++)
            {
                var material = trip == 0 ? MaterialId.Regolith : MaterialId.Stone;
                var terminal = terminals.Single(t => t.Material == material);
                var other = terminals.Single(t => t.Material != material);
                Vector3 dockPosition = terminal.transform.position;
                Vector3 otherDock = other.transform.position;
                for (int load = 0; load < 4; load++)
                {
                    var ray = new Ray(new Vector3(-1 - 2 * (load % 2), 206, -1 - 2 * (load / 2)), Vector3.down);
                    Assert.That(Physics.Raycast(ray, out var hit, 40), Is.True);
                    float height = hit.point.y;
                    Assert.That(interaction.Mine(hit), Is.True);
                    Assert.That(game.Session.Buffer.AmountOf(material), Is.EqualTo(8000000));
                    Assert.That(interaction.UseTerminal(other), Is.False);
                    Physics.SyncTransforms();
                    Assert.That(Physics.Raycast(ray, out var below, 40), Is.True);
                    Assert.That(below.point.y, Is.EqualTo(height - 2).Within(0.1f), "Rounded surface must expose the excavated cell floor");
                    Assert.That(interaction.Mine(below), Is.False, "Full personal canister blocks mining");
                    Assert.That(interaction.UseTerminal(terminal), Is.True);
                }
                Assert.That(game.Session.ContainerFor(material).Amount, Is.EqualTo(32000000));
                Assert.That(interaction.UseTerminal(terminal), Is.True);
                Assert.That(interaction.UseTerminal(terminal), Is.False, "No duplicate dispatch");
                yield return new WaitForSeconds(0.2f);
                Assert.That(terminal.GetComponent<Collider>().enabled, Is.False);
                Assert.That(terminal.transform.position.y, Is.GreaterThan(dockPosition.y));
                Assert.That(other.transform.position, Is.EqualTo(otherDock));
                Assert.That(other.GetComponent<Collider>().enabled, Is.True);
                Assert.That(game.Session.Credits, Is.EqualTo(trip == 0 ? 0 : game.Session.ShipmentPrice));
                float deadline = Time.realtimeSinceStartup + 12;
                while (game.Session.IsShipping && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(game.Session.IsShipping, Is.False, "Shipment must return in eight seconds");
                yield return null;
                Assert.That(game.Session.ContainerFor(material).Amount, Is.Zero);
                Assert.That(game.Session.Credits, Is.EqualTo(trip == 0 ? game.Session.ShipmentPrice :
                    game.Session.ShipmentPrice + game.Session.PriceFor(MaterialId.Stone)));
                Assert.That(game.Session.Shipments, Is.EqualTo(trip + 1));
                Assert.That(Vector3.Distance(terminal.transform.position, dockPosition), Is.LessThan(0.01f));
                Assert.That(terminal.GetComponent<Collider>().enabled, Is.True);
            }
        }
    }
}
