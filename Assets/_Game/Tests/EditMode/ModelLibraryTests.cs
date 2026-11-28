using System;
using NUnit.Framework;
using PlanetHarvest.Art;
using PlanetHarvest.Mining;
using UnityEditor;
using UnityEngine;

namespace PlanetHarvest.Tests
{
    public class ModelLibraryTests
    {
        private static ModelLibrary Library()
        {
            var library = AssetDatabase.LoadAssetAtPath<ModelLibrary>("Assets/_Game/ScriptableObjects/ModelLibrary.asset");
            Assert.NotNull(library);
            return library;
        }

        private static Bounds Bounds(GameObject model)
        {
            var filters = model.GetComponentsInChildren<MeshFilter>();
            Assert.IsNotEmpty(filters, model.name);
            var bounds = new Bounds(model.transform.InverseTransformPoint(filters[0].transform.TransformPoint(filters[0].sharedMesh.bounds.center)), Vector3.zero);
            foreach (var filter in filters)
            {
                var b = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2 - Vector3.one);
                    bounds.Encapsulate(model.transform.InverseTransformPoint(filter.transform.TransformPoint(corner)));
                }
            }
            return bounds;
        }

        [Test]
        public void EveryModelIsAssignedAndCarriesVertexColours()
        {
            var library = Library();
            foreach (GameModel model in Enum.GetValues(typeof(GameModel)))
            {
                var prefab = library.Prefab(model);
                Assert.NotNull(prefab, model.ToString());
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>())
                    Assert.Greater(filter.sharedMesh.colors32.Length, 0, $"{model}/{filter.name} has no vertex colours");
            }
            foreach (ContainerKind kind in Enum.GetValues(typeof(ContainerKind)))
                Assert.NotNull(library.Container(kind), kind.ToString());
        }

        [Test]
        public void ModelsUseMetresYUpAndFaceForward()
        {
            var library = Library();
            var bench = Bounds(library.Prefab(GameModel.Workbench));
            Assert.AreEqual(2.4f, bench.max.y, .25f, "Workbench height in metres, Y up.");
            Assert.Greater(bench.min.y, -.1f, "Origin at the foot.");
            var drill = library.Prefab(GameModel.HandDrill).transform;
            var spindle = ModelLibrary.Find(drill, "Spindel");
            Assert.NotNull(spindle);
            Assert.Greater(Bounds(spindle.gameObject).center.z + drill.InverseTransformPoint(spindle.position).z, .1f, "Drill bit points along +Z.");
            var mast = library.Prefab(GameModel.TractorMast).transform;
            Assert.AreEqual(3.2f, mast.InverseTransformPoint(ModelLibrary.Find(mast, "Strahlkern").position).y, .05f);
            var robot = library.Prefab(GameModel.DrillRobot).transform;
            foreach (var part in new[] { "Bein0", "Bein1", "Bein2", "Bein3", "Bohrkopf", "Statusleuchte" })
                Assert.NotNull(ModelLibrary.Find(robot, part), part);
            Assert.NotNull(ModelLibrary.Find(library.Prefab(GameModel.Pump).transform, "Pumpenkopf"));
        }
    }
}
