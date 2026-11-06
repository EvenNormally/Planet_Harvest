using System;
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
using UnityEngine.SceneManagement;

namespace PlanetHarvest.Editor
{
    public static class TestPlanetSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/PlanetHarvest_Test200m.unity";
        private const string SettingsPath = "Assets/_Game/ScriptableObjects/TestPlanet200m.asset";

        [MenuItem("Planet Harvest/Create 200m Test Scene")]
        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null ||
                AssetDatabase.LoadAssetAtPath<HarvestSettings>(SettingsPath) != null)
                throw new InvalidOperationException("Test assets already exist; preserve scene edits.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var settings = ScriptableObject.CreateInstance<HarvestSettings>();
            var config = new SerializedObject(settings);
            config.FindProperty("moonRadius").intValue = 200;
            config.FindProperty("cellSize").intValue = 2;
            config.FindProperty("roundedSurface").boolValue = true;
            config.FindProperty("containerLoads").intValue = 4;
            config.FindProperty("shipmentDuration").floatValue = 8;
            config.FindProperty("gravity").floatValue = 9.81f;
            config.FindProperty("moveSpeed").floatValue = 6;
            config.FindProperty("jumpSpeed").floatValue = 7;
            config.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(settings, SettingsPath);

            var ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Regolith.mat");
            var cyan = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Container.mat");
            if (ground == null || cyan == null) throw new InvalidOperationException("Existing materials missing.");
            var game = new GameObject("Harvest Game").AddComponent<HarvestGame>();
            Set(game, "settings", settings);
            var moon = new GameObject("Planet - Radius 200m - Regolith").AddComponent<VoxelMoon>();
            Set(moon, "settings", settings);
            Set(moon, "material", ground);

            // Lightweight editor preview; the real mineable chunk surface replaces it in Play Mode.
            var preview = Primitive("Planet Preview (Play Mode generates terrain)", PrimitiveType.Sphere,
                Vector3.zero, Vector3.one * 400, ground);
            UnityEngine.Object.DestroyImmediate(preview.GetComponent<Collider>());
            preview.transform.SetParent(moon.transform, true);
            Set(moon, "editorPreview", preview);

            Primitive("Dock", PrimitiveType.Cube, new Vector3(2, 200.1f, 2), new Vector3(5, 0.2f, 4), ground);
            var canister = Primitive("Versandkanister - E: fuellen / abschicken", PrimitiveType.Cylinder,
                new Vector3(1.5f, 201.1f, 2), new Vector3(1.4f, 0.9f, 1.4f), cyan);
            var terminal = canister.AddComponent<HarvestTerminal>();
            var terminalConfig = new SerializedObject(terminal);
            terminalConfig.FindProperty("returningCanister").boolValue = true;
            terminalConfig.ApplyModifiedPropertiesWithoutUndo();
            var flight = canister.AddComponent<ShipmentFlight>();
            Set(flight, "game", game); Set(flight, "planet", moon.transform);

            var player = new GameObject("Player");
            player.transform.position = new Vector3(0.5f, 201.3f, -2.5f);
            var capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 2.2f; capsule.radius = 0.3f;
            var body = player.AddComponent<Rigidbody>();
            body.useGravity = false; body.constraints = RigidbodyConstraints.FreezeRotation;
            var eyes = new GameObject("First Person Camera").transform;
            eyes.SetParent(player.transform, false); eyes.localPosition = new Vector3(0, 0.85f, 0);
            var camera = eyes.gameObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f; camera.farClipPlane = 1200;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f);
            eyes.gameObject.AddComponent<AudioListener>(); eyes.tag = "MainCamera";
            var movement = player.AddComponent<RadialPlayer>();
            Set(movement, "settings", settings); Set(movement, "moon", moon.transform); Set(movement, "eyes", eyes);
            Set(movement, "input", AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions"));
            var interaction = player.AddComponent<HarvestInteraction>();
            Set(interaction, "game", game); Set(interaction, "moon", moon);
            Set(interaction, "player", movement); Set(interaction, "view", camera);

            var personal = Primitive("Persoenlicher Kanister", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.13f, 0.14f, 0.13f), cyan);
            UnityEngine.Object.DestroyImmediate(personal.GetComponent<Collider>());
            personal.transform.SetParent(eyes, false);
            personal.transform.localPosition = new Vector3(0.28f, -0.26f, 0.55f);
            personal.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var hud = new GameObject("HUD").AddComponent<HarvestHud>();
            Set(hud, "game", game); Set(hud, "interaction", interaction);
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.8f;
            light.transform.rotation = Quaternion.Euler(40, -30, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.53f, 0.6f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = moon.gameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.Euler(25, 30, 0), 520);
        }

        private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name; go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
