using System.Linq;
using PlanetHarvest.Core;
using PlanetHarvest.Logistics;
using PlanetHarvest.Mining;
using PlanetHarvest.Player;
using PlanetHarvest.UI;
using PlanetHarvest.Voxel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace PlanetHarvest.Editor
{
    public static class HarvestSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/PlanetHarvest.unity";

        [MenuItem("Planet Harvest/Create MVP Scene")]
        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new System.InvalidOperationException("MVP scene already exists; preserve existing scene edits.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            var settings = ScriptableObject.CreateInstance<HarvestSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/_Game/ScriptableObjects/MvpSettings.asset");
            var ground = Material("Regolith", new Color(0.42f, 0.44f, 0.49f));
            var cyan = Material("Container", new Color(0.05f, 0.7f, 0.8f));
            var orange = Material("Elevator", new Color(1, 0.42f, 0.08f));
            var game = new GameObject("Harvest Game").AddComponent<HarvestGame>();
            Set(game, "settings", settings);
            var moon = new GameObject("Voxel Moon").AddComponent<VoxelMoon>();
            Set(moon, "settings", settings); Set(moon, "material", ground);
            Cube("Landing Pad", new Vector3(2.5f, 12.1f, 2), new Vector3(6, 0.2f, 4), ground);
            var container = Cube("Container - E to fill", new Vector3(1.5f, 12.7f, 2), Vector3.one, cyan).AddComponent<HarvestTerminal>();
            var elevator = Cube("Elevator - E to ship", new Vector3(3.5f, 12.85f, 2), new Vector3(0.8f, 1.3f, 0.8f), orange).AddComponent<HarvestTerminal>();
            var serialized = new SerializedObject(elevator);
            serialized.FindProperty("elevator").boolValue = true; serialized.ApplyModifiedPropertiesWithoutUndo();
            Cube("Space Elevator Mast", new Vector3(4.7f, 21, 3), new Vector3(0.3f, 18, 0.3f), orange);
            var spawn = new GameObject("Spawn Point").transform;
            spawn.position = new Vector3(0.5f, 13.3f, -2.5f);
            var player = new GameObject("Player");
            player.transform.position = spawn.position;
            var controller = player.AddComponent<CharacterController>();
            controller.height = 2.2f; controller.radius = 0.3f; controller.center = Vector3.zero;
            controller.stepOffset = 1.05f; controller.slopeLimit = 80; controller.skinWidth = 0.03f;
            var eyes = new GameObject("First Person Camera").transform;
            eyes.SetParent(player.transform, false); eyes.localPosition = new Vector3(0, 0.85f, 0);
            var camera = eyes.gameObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f; camera.farClipPlane = 150;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.012f, 0.02f, 0.04f);
            eyes.gameObject.AddComponent<AudioListener>(); eyes.tag = "MainCamera";
            var movement = player.AddComponent<RadialPlayer>();
            Set(movement, "settings", settings); Set(movement, "moon", moon.transform); Set(movement, "eyes", eyes);
            Set(movement, "input", AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions"));
            var interaction = player.AddComponent<HarvestInteraction>();
            Set(interaction, "game", game); Set(interaction, "moon", moon); Set(interaction, "player", movement); Set(interaction, "view", camera);
            var hud = new GameObject("HUD").AddComponent<HarvestHud>();
            Set(hud, "game", game); Set(hud, "interaction", interaction);
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.8f;
            light.transform.rotation = Quaternion.Euler(40, -30, 0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.5f, 0.53f, 0.6f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }.Concat(EditorBuildSettings.scenes).ToArray();
            AssetDatabase.SaveAssets();
        }

        private static Material Material(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            AssetDatabase.CreateAsset(material, $"Assets/_Game/Art/Materials/{name}.mat");
            return material;
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name; cube.transform.position = position; cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
