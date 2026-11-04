using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlanetHarvest.Tests
{
    public class ProjectConfigurationTests
    {
        [Test]
        public void AssetsUseTextSerialization()
        {
            Assert.That(EditorSettings.serializationMode, Is.EqualTo(SerializationMode.ForceText));
            Assert.That(VersionControlSettings.mode, Is.EqualTo("Visible Meta Files"));
        }

        [Test]
        public void Windows64IsTheActiveBuildTarget()
        {
            Assert.That(EditorUserBuildSettings.activeBuildTarget, Is.EqualTo(BuildTarget.StandaloneWindows64));
        }

        [Test]
        public void UniversalPipelineIsAssigned()
        {
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.Not.Null);
            Assert.That(GraphicsSettings.defaultRenderPipeline.GetType().Name, Is.EqualTo("UniversalRenderPipelineAsset"));
        }

        [Test]
        public void EnabledBuildScenesExistAndResolveTheirGuids()
        {
            int enabledCount = 0;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                enabledCount++;
                Assert.That(File.Exists(scene.path), Is.True, scene.path);
                Assert.That(AssetDatabase.AssetPathToGUID(scene.path), Is.Not.Empty, scene.path);
            }
            Assert.That(enabledCount, Is.GreaterThan(0));
        }
    }
}
