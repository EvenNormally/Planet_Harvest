using System.Collections.Generic;
using PlanetHarvest.Art;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Planets
{
    public sealed class EarthLandscape : MonoBehaviour
    {
        private VoxelMoon planet;
        private Camera view;
        private Material sky, water, foliage;
        private Mesh treeMesh, lakeMesh;
        private readonly Dictionary<Vector3Int, GameObject> trees = new Dictionary<Vector3Int, GameObject>();

        public void Populate(VoxelMoon moon, Camera camera)
        {
            planet = moon;
            view = camera;
            transform.SetPositionAndRotation(moon.transform.position, moon.transform.rotation);
            foliage = FieldProps.Finish();
            water = new Material(Resources.Load<Shader>("LakeWater"));
            sky = new Material(Resources.Load<Shader>("EarthSky"));
            var sun = RenderSettings.sun;
            if (!sun) sun = FindFirstObjectByType<Light>();
            sky.SetVector("_SunDirection", sun ? -sun.transform.forward : Vector3.up);
            var backdrop = view.gameObject.AddComponent<Skybox>();
            backdrop.material = sky;
            view.clearFlags = CameraClearFlags.Skybox;
            lakeMesh = Lake(moon.Earth);
            Model("Bergsee", lakeMesh, water);
            treeMesh = Pine();
            Physics.SyncTransforms();
            for (int i = 0; i < 200; i++)
            {
                float y = 1 - 2 * (i + .5f) / 200;
                float ring = Mathf.Sqrt(1 - y * y);
                float angle = i * 2.399963f;
                Plant(new Vector3(ring * Mathf.Cos(angle), y, ring * Mathf.Sin(angle)), i);
            }
            // A small grove frames the lake from the landing site.
            for (int i = 0; i < 10; i++)
            {
                float angle = i * 2.399963f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (8.5f + i % 3);
                Plant((EarthPlanetField.LakeDirection * moon.Earth.Radius + offset).normalized, i);
            }
            planet.CellMined += RemoveUnsupportedTree;
            planet.CellRestored += RestoreTree;
        }

        private void LateUpdate()
        {
            if (view) sky.SetVector("_LocalUp", (view.transform.position - transform.position).normalized);
        }

        private void Plant(Vector3 normal, int index)
        {
            var field = planet.Earth;
            Vector3 point = normal * field.SurfaceRadius(normal);
            if ((normal - EarthPlanetField.LakeDirection).sqrMagnitude * field.Radius * field.Radius < 53 ||
                (point.y > field.Radius * .55f && point.z > -14 && point.z < 31 && point.x > -6 && point.x < 16)) return;
            var ray = new Ray(transform.TransformPoint(normal * (field.Extent + 3)), transform.TransformDirection(-normal));
            if (!Physics.Raycast(ray, out var hit, 23) || hit.collider.GetComponentInParent<VoxelMoon>() != planet ||
                Vector3.Dot(hit.normal, -ray.direction) < .7f) return;
            Vector3Int cell = planet.CellAt(hit);
            if (trees.ContainsKey(cell)) return;
            var tree = Model("Kiefer", treeMesh, foliage);
            tree.transform.position = hit.point + ray.direction * .09f;
            tree.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0,index * 37,0);
            tree.transform.localScale = Vector3.one * (.72f + .13f * (index % 6));
            trees.Add(cell, tree);
        }

        private void RemoveUnsupportedTree(Vector3Int cell)
        {
            if (!trees.TryGetValue(cell, out var tree)) return;
            tree.SetActive(false);
        }

        private void RestoreTree(Vector3Int cell)
        {
            if (trees.TryGetValue(cell, out var tree)) tree.SetActive(true);
        }

        private GameObject Model(string label, Mesh mesh, Material finish)
        {
            var obj = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(transform, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial = finish;
            return obj;
        }

        private static Mesh Lake(EarthPlanetField field)
        {
            const int sides = 48, rings = 8;
            var up = EarthPlanetField.LakeDirection;
            var rotation = Quaternion.FromToRotation(Vector3.up, up);
            var vertices = new Vector3[1 + sides * rings];
            var uv = new Vector2[vertices.Length];
            var triangles = new List<int>();
            vertices[0] = up * field.WaterRadius;
            for (int ring = 0; ring < rings; ring++)
            for (int j = 0; j < sides; j++)
            {
                float angle = j * 2 * Mathf.PI / sides;
                var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (ring + 1);
                int a = 1 + ring * sides + j, b = 1 + ring * sides + (j + 1) % sides;
                vertices[a] = (up * field.WaterRadius + rotation * offset).normalized * field.WaterRadius;
                uv[a] = new Vector2(offset.x, offset.z);
                int inner = ring == 0 ? 0 : a - sides;
                triangles.Add(inner); triangles.Add(b); triangles.Add(a);
                if (ring == 0) continue;
                triangles.Add(inner); triangles.Add(b - sides); triangles.Add(b);
            }
            var mesh = new Mesh { name = "Seeoberfläche", vertices = vertices, uv = uv, triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh Pine()
        {
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var indices = new List<int>();
            void Tier(float bottom, float top, float width, float tip, Color color, float twist)
            {
                for (int j = 0; j < 7; j++)
                {
                    Vector3 Ring(int k) => new Vector3(Mathf.Cos(k * 2 * Mathf.PI / 7 + twist), 0,
                        Mathf.Sin(k * 2 * Mathf.PI / 7 + twist));
                    Vector3 a = Ring(j), b = Ring(j + 1);
                    int n = vertices.Count;
                    vertices.Add(a * width + Vector3.up * bottom);
                    vertices.Add(a * tip + Vector3.up * top);
                    vertices.Add(b * tip + Vector3.up * top);
                    vertices.Add(b * width + Vector3.up * bottom);
                    for (int k = 0; k < 4; k++) colors.Add(color);
                    indices.Add(n); indices.Add(n+1); indices.Add(n+3);
                    if (tip > 0) { indices.Add(n+1); indices.Add(n+2); indices.Add(n+3); }
                }
            }
            Tier(0,1.8f,.17f,.1f,new Color(.36f,.21f,.11f),0);
            Tier(.85f,2.55f,1.05f,0,new Color(.09f,.34f,.21f),0);
            Tier(1.49f,3.08f,.85f,0,new Color(.12f,.44f,.27f),.39f);
            Tier(2.13f,3.66f,.61f,0,new Color(.23f,.55f,.32f),.14f);
            var mesh = new Mesh { name = "Etagenkiefer" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();
            return mesh;
        }

        private void OnDestroy()
        {
            if (planet) { planet.CellMined -= RemoveUnsupportedTree; planet.CellRestored -= RestoreTree; }
            Destroy(treeMesh); Destroy(lakeMesh);
            Destroy(foliage); Destroy(water); Destroy(sky);
        }
    }
}
