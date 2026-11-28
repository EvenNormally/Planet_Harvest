using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PlanetHarvest.Mining;
using PlanetHarvest.Player;
using PlanetHarvest.Voxel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlanetHarvest.Tests
{
    public class EarthTraversalTests
    {
        public const string ScenePath = "Assets/_Game/Scenes/PlanetHarvest_Earth.unity";

        [UnityTest]
        public IEnumerator PlayerWalksFromSpawnUnderRockAndBackIntoDaylight()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                yield return SceneManager.LoadSceneAsync(ScenePath);
                yield return new WaitForSeconds(0.6f);
                var player = Object.FindFirstObjectByType<RadialPlayer>();
                var planet = Object.FindFirstObjectByType<VoxelMoon>();
                Vector3 start = player.transform.position;
                Assert.IsTrue(player.IsGrounded);
                HoldStick(pad, Vector2.up);
                float deadline = Time.time + 15;
                while (planet.Earth.Depth(player.transform.position) < 6 && Time.time < deadline)
                    yield return null;
                Vector3 inside = player.transform.position;
                Assert.GreaterOrEqual(planet.Earth.Depth(inside), 6, $"Stopped at {inside}");
                Assert.IsTrue(Physics.Raycast(inside, inside.normalized, out var roof, 7));
                Assert.AreEqual(planet, roof.collider.GetComponentInParent<VoxelMoon>());
                HoldStick(pad, Vector2.down);
                deadline = Time.time + 15;
                while (Vector3.Distance(start, player.transform.position) > 1.6f && Time.time < deadline)
                    yield return null;
                Assert.Less(Vector3.Distance(start, player.transform.position), 1.7f);
                Assert.Less(planet.Earth.Depth(player.transform.position), 0);
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        private static void HoldStick(Gamepad pad, Vector2 direction)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = direction });
            InputSystem.Update();
        }

        [UnityTest]
        public IEnumerator PickedTrianglesYieldExactlyOneCellOfEachMaterial()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath);
            var planet = Object.FindFirstObjectByType<VoxelMoon>();
            var inventory = Object.FindFirstObjectByType<EarthMining>().Inventory;
            var types = new[] { MaterialId.Soil, MaterialId.Stone, MaterialId.IronOre, MaterialId.CopperOre, MaterialId.Quartz };
            for (int submesh = 0; submesh < types.Length; submesh++)
            {
                bool mined = false;
                foreach (var filter in planet.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh = filter.sharedMesh;
                    var triangles = mesh.GetTriangles(submesh);
                    if (triangles.Length == 0) continue;
                    var points = mesh.vertices;
                    var collider = filter.GetComponent<MeshCollider>();
                    Vector3 a = points[triangles[0]], b = points[triangles[1]], c = points[triangles[2]];
                    Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                    var ray = new Ray((a + b + c) / 3 + n * 0.05f, -n);
                    if (!collider.Raycast(ray, out var hit, 0.1f)) continue;
                    var cell = planet.CellAt(hit);
                    Assert.AreEqual(types[submesh], planet.Data.MaterialAt(cell));
                    Assert.IsTrue(planet.TryMine(inventory, cell, out var material));
                    Assert.AreEqual(types[submesh], material);
                    Assert.AreEqual(HarvestInventory.CubicMetre, inventory.AmountOf(material));
                    Assert.IsFalse(planet.TryMine(inventory, cell, out var air));
                    Assert.AreEqual(MaterialId.None, air);
                    mined = true;
                    break;
                }
                Assert.IsTrue(mined, types[submesh].ToString());
            }
            Assert.IsFalse(planet.TryMine(inventory, Vector3Int.one * planet.Data.Radius, out _));
            Assert.AreEqual(types.Length * HarvestInventory.CubicMetre, inventory.Amount);
        }

        [UnityTest]
        public IEnumerator RenderedSurfaceAndCaveAreClosedAcrossChunkBoundaries()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath);
            var planet = Object.FindFirstObjectByType<VoxelMoon>();
            var edges = new Dictionary<(Vector3, Vector3), int>();
            foreach (var filter in planet.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                var indices = mesh.triangles;
                if (indices.Length == 0) continue;
                Assert.AreSame(mesh, filter.GetComponent<MeshCollider>().sharedMesh);
                var points = mesh.vertices;
                for (int i = 0; i < indices.Length; i += 3)
                for (int j = 0; j < 3; j++)
                {
                    Vector3 a = points[indices[i + j]], b = points[indices[i + (j + 1) % 3]];
                    bool ordered = a.x != b.x ? a.x < b.x : a.y != b.y ? a.y < b.y : a.z < b.z;
                    var key = ordered ? (a, b) : (b, a);
                    edges.TryGetValue(key, out int balance);
                    edges[key] = balance + (ordered ? 1 : -1);
                }
            }
            Assert.Greater(edges.Count, 1000);
            foreach (int balance in edges.Values) Assert.AreEqual(0, balance, "Open terrain edge");
        }
    }
}
