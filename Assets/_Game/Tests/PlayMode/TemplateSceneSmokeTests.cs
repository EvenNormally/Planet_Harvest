using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlanetHarvest.Tests
{
    public class TemplateSceneSmokeTests
    {
        private Scene loadedScene;

        [UnityTest]
        public IEnumerator SampleSceneLoadsWithCameraAndNoMissingScripts()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            yield return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            loadedScene = SceneManager.GetSceneByPath(scenePath);
            Assert.That(loadedScene.isLoaded, Is.True);
            bool hasCamera = false;
            foreach (var root in loadedScene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<Camera>(true) != null) hasCamera = true;
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                    Assert.That(component, Is.Not.Null, "Missing component in SampleScene");
            }
            Assert.That(hasCamera, Is.True);
        }

        [UnityTearDown]
        public IEnumerator UnloadSampleScene()
        {
            if (loadedScene.IsValid() && loadedScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(loadedScene);
        }
    }
}
