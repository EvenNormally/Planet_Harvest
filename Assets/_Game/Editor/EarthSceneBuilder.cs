using System;
using System.Linq;
using PlanetHarvest.Core;
using PlanetHarvest.Mining;
using PlanetHarvest.Planets;
using PlanetHarvest.Player;
using PlanetHarvest.UI;
using PlanetHarvest.Voxel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PlanetHarvest.Editor
{
    public static class EarthSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/PlanetHarvest_Earth.unity";
        private const string SettingsPath = "Assets/_Game/ScriptableObjects/StarterEarth.asset";

        [MenuItem("Planet Harvest/Create Starter Earth")]
        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException("The Earth scene already exists.");
            var previous = SceneManager.GetActiveScene();
            var mode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            SceneManager.SetActiveScene(scene);
            var settings = AssetDatabase.LoadAssetAtPath<EarthPlanetSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<EarthPlanetSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            var planet = new GameObject("Startplanet").AddComponent<VoxelMoon>();
            Set(planet, "earthSettings", settings);
            Set(planet, "material", Load<Material>("Assets/_Game/Art/Materials/EarthSoil.mat"));
            Set(planet, "stoneMaterial", Load<Material>("Assets/_Game/Art/Materials/EarthStone.mat"));
            Set(planet, "ironMaterial", Load<Material>("Assets/_Game/Art/Materials/EarthIron.mat"));

            var player = new GameObject("Spieler");
            var field = settings.CreateField();
            Vector3 up = field.SpawnDirection;
            player.transform.SetPositionAndRotation(up * (field.SurfaceRadius(up) + 1.25f),
                Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, up), up));
            var camera = new GameObject("Augen").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.SetParent(player.transform, false);
            camera.transform.localPosition = Vector3.up * 0.8f;
            camera.nearClipPlane = 0.04f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.49f, 0.79f, 0.93f);
            camera.gameObject.AddComponent<AudioListener>();
            var movement = player.AddComponent<RadialPlayer>();
            Set(movement, "settings", Load<HarvestSettings>("Assets/_Game/ScriptableObjects/MvpSettings.asset"));
            Set(movement, "moon", planet.transform);
            Set(movement, "eyes", camera.transform);
            Set(movement, "input", Load<InputActionAsset>("Assets/InputSystem_Actions.inputactions"));
            Set(player.AddComponent<EarthSpawn>(), "planet", planet);
            AddMining(player, planet, camera);

            var sun = new GameObject("Sonne").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(48, -40, 0);
            sun.intensity = 1.3f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.65f, 0.69f, 0.73f);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save the new Earth scene.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException(path);

        internal static void AddMining(GameObject player, VoxelMoon planet, Camera camera)
        {
            var mining = player.GetComponent<EarthMining>() ?? player.AddComponent<EarthMining>();
            Set(mining, "planet", planet);
            Set(mining, "view", camera);
            if (player.GetComponent<EarthHud>() == null) player.AddComponent<EarthHud>();
        }

        internal static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
