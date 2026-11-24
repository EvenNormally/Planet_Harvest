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

namespace PlanetHarvest.Tests
{
    public class HarvestLoopTests
    {
        [UnityTest]
        public IEnumerator ExcavatedFloorKeepsTextureAndYieldsItsOwnMaterial()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Game/Scenes/PlanetHarvest_Test200m.unity", LoadSceneMode.Single);
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
            Assert.That(player.transform.position.magnitude, Is.InRange(200.5f, 202f), "Player must remain on moon");
            var ray = new Ray(new Vector3(-1, 206, -1), Vector3.down);
            Assert.That(Physics.Raycast(ray, out var hit, 10), Is.True);
            float surface = hit.point.y;
            Vector3Int cell = moon.CellAt(hit);
            int before = moon.Data.SolidCount;
            Assert.That(interaction.Mine(hit), Is.True);
            Assert.That(moon.Data.SolidCount, Is.EqualTo(before - 1));
            Assert.That(game.Session.Buffer.AmountOf(MaterialId.Regolith), Is.EqualTo(8000000));
            Physics.SyncTransforms();
            Assert.That(Physics.Raycast(ray, out var below, 10), Is.True);
            var floorRenderer = below.collider.GetComponent<MeshRenderer>();
            Assert.That(floorRenderer.sharedMaterials[1].GetTexture("_BaseMap"), Is.Not.Null,
                "The excavated stone floor must retain its texture.");
            var floorMesh = below.collider.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(floorMesh.uv.Length, Is.EqualTo(floorMesh.vertexCount));
            Assert.That(below.triangleIndex * 3, Is.GreaterThanOrEqualTo((int)floorMesh.GetIndexCount(0)),
                "Exposed stone floor must use the stone submesh");
            var uv = floorMesh.uv;
            var indices = floorMesh.triangles;
            int start = below.triangleIndex * 3;
            Vector2 edgeA = uv[indices[start + 1]] - uv[indices[start]];
            Vector2 edgeB = uv[indices[start + 2]] - uv[indices[start]];
            Assert.That(Mathf.Abs(edgeA.x * edgeB.y - edgeA.y * edgeB.x), Is.GreaterThan(0.001f),
                "Newly exposed triangles need a nonzero area on the texture.");
            Assert.That(below.point.y, Is.EqualTo(surface - 2).Within(0.05f), "Collider must expose the real hole");
            Assert.That(interaction.Mine(below), Is.False, "Full buffer must stop terrain removal");
            var terminals = Object.FindObjectsByType<HarvestTerminal>(FindObjectsSortMode.None);
            var regolith = terminals.Single(t => t.Material == MaterialId.Regolith);
            var stone = terminals.Single(t => t.Material == MaterialId.Stone);
            Assert.That(interaction.UseTerminal(regolith), Is.True);
            Assert.That(game.Session.Buffer.Amount, Is.Zero);
            Assert.That(game.Session.Container.Amount, Is.EqualTo(8000000));
            Assert.That(moon.TryMine(game.Session, cell), Is.False, "Removed cell is air");
            Assert.That(interaction.Mine(below), Is.True);
            Assert.That(game.Session.Buffer.AmountOf(MaterialId.Stone), Is.EqualTo(8000000));
            Assert.That(interaction.UseTerminal(regolith), Is.False);
            Assert.That(interaction.UseTerminal(stone), Is.True);
            Assert.That(game.Session.Container.Amount, Is.EqualTo(8000000));
            Assert.That(game.Session.StoneContainer.Amount, Is.EqualTo(8000000));
            Assert.That(moon.Data.IsSolid(cell), Is.False);
        }
    }
}
