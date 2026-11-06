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
            var terminal = Object.FindFirstObjectByType<HarvestTerminal>();
            var player = Object.FindFirstObjectByType<RadialPlayer>();
            Assert.That(moon.Data.Radius * moon.CellSize, Is.EqualTo(200));
            Assert.That(moon.GetComponentsInChildren<SphereCollider>().Length, Is.Zero, "Preview must not block mining");
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                    Assert.That(component, Is.Not.Null, "Missing scene component");
            yield return new WaitForSeconds(0.5f);
            Assert.That(player.transform.position.magnitude, Is.InRange(200.5f, 202f));
            Vector3 dockPosition = terminal.transform.position;
            for (int trip = 0; trip < 2; trip++)
            {
                for (int load = 0; load < 4; load++)
                {
                    var ray = new Ray(new Vector3(-1, 206, -1), Vector3.down);
                    Assert.That(Physics.Raycast(ray, out var hit, 40), Is.True);
                    float height = hit.point.y;
                    Assert.That(interaction.Mine(hit), Is.True);
                    Physics.SyncTransforms();
                    Assert.That(Physics.Raycast(ray, out var below, 40), Is.True);
                    Assert.That(below.point.y, Is.EqualTo(height - 2).Within(0.05f), "Rounded surface must expose the excavated cell floor");
                    Assert.That(interaction.Mine(below), Is.False, "Full personal canister blocks mining");
                    Assert.That(interaction.UseTerminal(terminal), Is.True);
                }
                Assert.That(game.Session.Container.Amount, Is.EqualTo(32000000));
                Assert.That(interaction.UseTerminal(terminal), Is.True);
                Assert.That(interaction.UseTerminal(terminal), Is.False, "No duplicate dispatch");
                yield return new WaitForSeconds(0.2f);
                Assert.That(terminal.GetComponent<Collider>().enabled, Is.False);
                Assert.That(terminal.transform.position.y, Is.GreaterThan(dockPosition.y));
                float deadline = Time.realtimeSinceStartup + 12;
                while (game.Session.IsShipping && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(game.Session.IsShipping, Is.False, "Shipment must return in eight seconds");
                yield return null;
                Assert.That(game.Session.Container.Amount, Is.Zero);
                Assert.That(game.Session.Shipments, Is.EqualTo(trip + 1));
                Assert.That(Vector3.Distance(terminal.transform.position, dockPosition), Is.LessThan(0.01f));
                Assert.That(terminal.GetComponent<Collider>().enabled, Is.True);
            }
        }
    }
}
