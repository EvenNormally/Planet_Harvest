using PlanetHarvest.Logistics;
using PlanetHarvest.Mining;
using PlanetHarvest.Player;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlanetHarvest.Editor
{
    // Bake the kit as ordinary prefabs. Only the station's radial placement depends on the seed.
    public static class EarthEquipmentBuilder
    {
        private const string Art = "Assets/_Game/Art/Materials/";
        private const string Prefabs = "Assets/_Game/Prefabs/";
        private static Material frame, enamel, cream, soil, stone, iron, dark;

        public static void Install()
        {
            frame = Finish("FreightFrame", "#38505A");
            enamel = Finish("FreightMint", "#60D4B6");
            cream = Finish("FreightIvory", "#EEE4BC");
            soil = Finish("FreightOchre", "#DA8345");
            stone = Finish("FreightSlate", "#829BD0");
            iron = Finish("FreightIron", "#BA5742");
            dark = Finish("FreightGrip", "#23343E");
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "FieldEquipment.prefab")) MakeEquipment();
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "EarthElevator.prefab")) MakeDock();
            var scene = EditorSceneManager.OpenScene(EarthSceneBuilder.ScenePath);
            Bind(Object.FindFirstObjectByType<EarthMining>());
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.InvalidOperationException("Earth scene save failed.");
        }

        internal static void Bind(EarthMining mining)
        {
            var kit = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "FieldEquipment.prefab");
            var dock = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "EarthElevator.prefab");
            EarthSceneBuilder.Set(mining, "equipmentPrefab", kit.GetComponent<FieldEquipment>());
            EarthSceneBuilder.Set(mining, "elevatorPrefab", dock.GetComponent<EarthElevator>());
        }

        private static Material Finish(string name, string hex)
        {
            string path = Art + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ColorUtility.TryParseHtmlString(hex, out var color);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.24f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static GameObject Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject Part(Transform parent, string name, Vector3 pos, Vector3 size, Material mat,
            PrimitiveType shape = PrimitiveType.Cube, float roll = 0)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.transform.localRotation = Quaternion.Euler(0, 0, roll);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void MakeEquipment()
        {
            var root = new GameObject("Feldausrüstung");
            root.transform.position = new Vector3(0.25f, -0.22f, 0.56f);
            root.transform.localScale = Vector3.one * .78f;
            root.transform.rotation = Quaternion.Euler(5, -16, -18);
            var tool = root.AddComponent<FieldEquipment>();
            var pick = Group(root.transform, "Feldhacke").transform;
            Part(pick, "Schaft", Vector3.zero, new Vector3(.038f,.38f,.044f), enamel);
            Part(pick, "Griff", Vector3.down * .11f, new Vector3(.049f,.14f,.052f), dark);
            for (int i = 0; i < 3; i++)
                Part(pick, "Griffring", Vector3.down * (.08f + .032f * i), new Vector3(.053f,.011f,.056f), frame);
            Part(pick, "Kopf", new Vector3(-.01f,.18f,0), new Vector3(.23f,.055f,.063f), frame);
            Part(pick, "Blatt", new Vector3(.125f,.169f,0), new Vector3(.091f,.041f,.083f), cream, roll: -16);
            Part(pick, "Spitze", new Vector3(-.145f,.158f,0), new Vector3(.105f,.033f,.045f), cream, roll: 25);
            Part(pick, "Fassung", new Vector3(0,.169f,0), new Vector3(.061f,.079f,.074f), soil);
            var so = new SerializedObject(tool);
            var items = so.FindProperty("items");
            items.arraySize = 4;
            items.GetArrayElementAtIndex(0).objectReferenceValue = pick.gameObject;
            Material[] finishes = { soil, stone, iron };
            for (int i = 0; i < finishes.Length; i++)
            {
                var sample = Group(root.transform, "Rohstoffprobe " + (i + 1));
                Part(sample.transform, "Bruchstück", Vector3.zero, new Vector3(.16f,.12f,.14f), finishes[i], roll: 17);
                Part(sample.transform, "Bruchkante", new Vector3(-.04f,.045f,.008f), new Vector3(.09f,.06f,.11f),
                    i == 2 ? soil : finishes[i], roll: -12);
                items.GetArrayElementAtIndex(i + 1).objectReferenceValue = sample;
                sample.SetActive(false);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
            Bake(root, "FieldEquipment");
        }

        private static void MakeDock()
        {
            var root = new GameObject("Space Elevator");
            var station = root.AddComponent<EarthElevator>();
            var t = root.transform;
            var deck = Part(t, "Ladepodest", new Vector3(0,-.13f,0), new Vector3(4.9f,.48f,2.7f), frame);
            deck.AddComponent<BoxCollider>();
            for (int side = -1; side <= 1; side += 2)
            {
                float x = 2.05f * side;
                Part(t, "Stütze", new Vector3(x,2.8f,.85f), new Vector3(.25f,5.9f,.36f), dark);
                Part(t, "Leitstreifen", new Vector3(x,3.6f,.65f), new Vector3(.11f,2.4f,.045f), enamel);
                Part(t, "Tragseil", new Vector3(side * 1.08f,41,.58f), new Vector3(.064f,82,.064f), frame);
                Part(t, "Diagonalstrebe", new Vector3(side * 1.65f,4.91f,.85f), new Vector3(.15f,1.6f,.21f), cream, roll: -side * 38);
            }
            Part(t, "Querträger", new Vector3(0,5.65f,.78f), new Vector3(4.6f,.58f,.51f), frame);
            Label(t, "SPACE ELEVATOR", new Vector3(0,5.65f,.50f), .26f);
            MakeBin(t, station, "soil", -1.08f, MaterialId.Soil, soil);
            MakeBin(t, station, "stone", 1.08f, MaterialId.Stone, stone);
            Bake(root, "EarthElevator");
        }

        private static void MakeBin(Transform parent, EarthElevator station, string field, float x, MaterialId id, Material finish)
        {
            var go = Group(parent, id.Name() + "container");
            var t = go.transform;
            t.localPosition = new Vector3(x,.85f,-.12f);
            var bin = go.AddComponent<EarthCargoBin>();
            go.GetComponent<BoxCollider>().size = new Vector3(1.49f,1.5f,1.43f);
            Part(t, "Frachtkörper", Vector3.zero, new Vector3(1.36f,1.18f,1.32f), finish);
            foreach (float y in new[] { -.62f, .62f })
                Part(t, "Stoßrahmen", Vector3.up * y, new Vector3(1.49f,.18f,1.43f), frame);
            Part(t, "Fronttafel", new Vector3(0,.05f,-.67f), new Vector3(1.14f,.43f,.035f), dark);
            Label(t, id.Name().ToUpperInvariant(), new Vector3(0,.05f,-.70f), .23f);
            for (int i = 0; i < (id == MaterialId.Soil ? 1 : 2); i++)
                Part(t, "Kennstreifen", new Vector3(-.43f + i * .14f,-.36f,-.67f), new Vector3(.085f,.16f,.04f), cream, roll: -18);
            var so = new SerializedObject(bin);
            so.FindProperty("material").intValue = (int)id;
            so.ApplyModifiedPropertiesWithoutUndo();
            EarthSceneBuilder.Set(station, field, bin);
        }

        private static void Label(Transform parent, string text, Vector3 pos, float height)
        {
            var label = Group(parent, text).AddComponent<TextMeshPro>();
            label.transform.localPosition = pos;
            label.rectTransform.sizeDelta = new Vector2(4,.55f);
            label.text = text;
            label.fontSize = height * 10;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(.98f,.96f,.85f);
            label.raycastTarget = false;
        }

        private static void Bake(GameObject root, string name)
        {
            var asset = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + name + ".prefab");
            Object.DestroyImmediate(root);
            if (!asset) throw new System.InvalidOperationException(name + " could not be saved.");
        }
    }
}
