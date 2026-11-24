using System.Collections.Generic;
using PlanetHarvest.Core;
using PlanetHarvest.Mining;
using PlanetHarvest.Planets;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlanetHarvest.Voxel
{
    public sealed class VoxelMoon : MonoBehaviour
    {
        [SerializeField] private HarvestSettings settings;
        [SerializeField] private Material material;
        [SerializeField] private Material stoneMaterial;
        [SerializeField] private EarthPlanetSettings earthSettings;
        [SerializeField] private Material ironMaterial;
        public EarthPlanetField Earth { get; private set; }
        private Material[] terrainMaterials;
        [SerializeField] private GameObject editorPreview;
        public int CellSize => earthSettings != null ? 1 : settings.CellSize;
        private readonly Dictionary<Vector3Int, MeshFilter> chunks = new Dictionary<Vector3Int, MeshFilter>();
        private readonly Dictionary<Collider, List<Vector3Int>> triangleCells = new Dictionary<Collider, List<Vector3Int>>();
        public VoxelData Data { get; private set; }
        private static readonly Vector3Int[] Directions =
        {
            Vector3Int.right, Vector3Int.left, Vector3Int.up,
            Vector3Int.down, Vector3Int.forward, Vector3Int.back
        };

        private void Awake()
        {
            if (editorPreview != null) editorPreview.SetActive(false);
            Earth = earthSettings != null ? earthSettings.CreateField() : null;
            terrainMaterials = Earth != null ? new[] { material, stoneMaterial, ironMaterial }
                : new[] { material, stoneMaterial != null ? stoneMaterial : material };
            Data = Earth != null ? new VoxelData(Earth) : new VoxelData(settings.MoonRadius / CellSize);
            int min = Mathf.FloorToInt(-Data.Radius / (float)VoxelData.ChunkSize);
            int max = Mathf.FloorToInt((Data.Radius - 1) / (float)VoxelData.ChunkSize);
            for (int x = min; x <= max; x++)
            for (int y = min; y <= max; y++)
            for (int z = min; z <= max; z++)
            {
                var key = new Vector3Int(x, y, z);
                if (Data.IsSurfaceChunk(key)) Rebuild(key);
            }
        }

        public bool TryMine(HarvestSession session, Vector3Int cell) => TryMine(session, cell, out _);

        public bool TryMine(HarvestSession session, Vector3Int cell, out MaterialId mined)
        {
            return TryMine(session.Buffer, session.MiningVolume, cell, out mined);
        }

        public bool TryMine(HarvestInventory inventory, Vector3Int cell, out MaterialId mined) =>
            TryMine(inventory, (long)CellSize * CellSize * CellSize * HarvestInventory.CubicMetre, cell, out mined);

        private bool TryMine(HarvestInventory inventory, long volume, Vector3Int cell, out MaterialId mined)
        {
            if (!inventory.TryMine(Data, cell, volume, out mined)) return false;
            var dirty = new HashSet<Vector3Int> { VoxelData.ChunkOf(cell) };
            foreach (var direction in Directions) dirty.Add(VoxelData.ChunkOf(cell + direction));
            foreach (var key in dirty) Rebuild(key);
            return true;
        }

        public Vector3Int CellAt(RaycastHit hit)
        {
            // Rounded faces no longer necessarily lie inside their original grid cell.
            if (triangleCells.TryGetValue(hit.collider, out var cells) && hit.triangleIndex >= 0 && hit.triangleIndex < cells.Count)
                return cells[hit.triangleIndex];
            return Vector3Int.FloorToInt(transform.InverseTransformPoint(hit.point - hit.normal * 0.01f) / CellSize);
        }

        private void Rebuild(Vector3Int key)
        {
            var vertices = new List<Vector3>();
            var colors = Earth != null ? new List<Color32>() : null;
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            var owners = new List<Vector3Int>();
            var stoneTriangles = new List<int>();
            var stoneOwners = new List<Vector3Int>();
            var ironTriangles = new List<int>();
            var ironOwners = new List<Vector3Int>();
            Vector3Int start = key * VoxelData.ChunkSize;
            for (int x = 0; x < VoxelData.ChunkSize; x++)
            for (int y = 0; y < VoxelData.ChunkSize; y++)
            for (int z = 0; z < VoxelData.ChunkSize; z++)
            {
                Vector3Int cell = start + new Vector3Int(x, y, z);
                var resource = Data.MaterialAt(cell);
                if (resource == MaterialId.None) continue;
                var faceTriangles = resource == MaterialId.Stone ? stoneTriangles : triangles;
                var faceOwners = resource == MaterialId.Stone ? stoneOwners : owners;
                if (resource == MaterialId.IronOre) { faceTriangles = ironTriangles; faceOwners = ironOwners; }
                foreach (var direction in Directions)
                {
                    if (Data.IsSolid(cell + direction)) continue;
                    Vector3 n = direction;
                    Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                    Vector3 v = Vector3.Cross(n, u);
                    Vector3 centre = (Vector3)cell + Vector3.one * 0.5f + n * 0.5f;
                    int index = vertices.Count;
                    vertices.Add(SurfaceVertex(centre - u * 0.5f - v * 0.5f));
                    vertices.Add(SurfaceVertex(centre + u * 0.5f - v * 0.5f));
                    vertices.Add(SurfaceVertex(centre + u * 0.5f + v * 0.5f));
                    vertices.Add(SurfaceVertex(centre - u * 0.5f + v * 0.5f));
                    bool exterior = Earth == null && settings.RoundedSurface && !Data.IsOriginalSolid(cell + direction);
                    Vector3 faceNormal = Vector3.Cross(vertices[index + 1] - vertices[index], vertices[index + 2] - vertices[index]).normalized;
                    if (colors != null)
                    {
                        float depth = Earth.Depth(centre);
                        bool turf = resource == MaterialId.Soil && depth < 1
                            && !Data.IsOriginalSolid(cell + direction) && Vector3.Dot(faceNormal, centre.normalized) > 0.48f;
                        Color32 color = new Color(turf ? 1 : 0, Mathf.Clamp01(depth / 8), 0, 1);
                        for (int i = 0; i < 4; i++) colors.Add(color);
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 vertex = vertices[index + i];
                        normals.Add(exterior ? vertex.normalized : faceNormal);
                        // Project in planet-local metres so neighbouring chunks share the same tile origin.
                        uv.Add(new Vector2(Vector3.Dot(vertex, u), Vector3.Dot(vertex, v)) * (CellSize / 2f));
                    }
                    AddTriangle(index, index + 1, index + 2, cell, vertices, faceTriangles, faceOwners);
                    AddTriangle(index, index + 2, index + 3, cell, vertices, faceTriangles, faceOwners);
                }
            }
            if (!chunks.TryGetValue(key, out var filter))
            {
                if (vertices.Count == 0) return;
                var chunk = new GameObject($"Chunk {key}", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                chunk.transform.SetParent(transform, false);
                chunk.transform.localScale = Vector3.one * CellSize;
                chunk.GetComponent<MeshRenderer>().sharedMaterials = terrainMaterials;
                filter = chunk.GetComponent<MeshFilter>();
                filter.sharedMesh = new Mesh { name = chunk.name, indexFormat = IndexFormat.UInt32 };
                chunks.Add(key, filter);
            }
            Mesh mesh = filter.sharedMesh;
            var collider = filter.GetComponent<MeshCollider>();
            // Raycast triangleIndex traverses the submeshes in index order.
            owners.AddRange(stoneOwners);
            owners.AddRange(ironOwners);
            triangleCells[collider] = owners;
            collider.sharedMesh = null;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            if (colors != null) mesh.SetColors(colors);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = terrainMaterials.Length;
            mesh.SetTriangles(triangles, 0);
            mesh.SetTriangles(stoneTriangles, 1);
            if (Earth != null) mesh.SetTriangles(ironTriangles, 2);
            mesh.RecalculateBounds();
            if (owners.Count > 0) collider.sharedMesh = mesh;
        }

        private Vector3 SurfaceVertex(Vector3 vertex) => Earth != null || settings.RoundedSurface
            ? Data.RoundSurfaceVertex(vertex) : vertex;

        public Vector3 SpawnPosition()
        {
            Vector3 up = transform.TransformDirection(Earth.SpawnDirection);
            Vector3 origin = transform.position + up * (Earth.Extent + 4);
            Physics.SyncTransforms();
            float nearest = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(origin, -up, Earth.Extent, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponentInParent<VoxelMoon>() == this)
                    nearest = Mathf.Min(nearest, hit.distance);
            if (!float.IsPositiveInfinity(nearest)) return origin - up * (nearest - 1.25f);
            throw new System.InvalidOperationException("The spawn has no terrain collider.");
        }

        private static void AddTriangle(int a, int b, int c, Vector3Int cell, List<Vector3> vertices,
            List<int> indices, List<Vector3Int> owners)
        {
            if (Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).sqrMagnitude < 0.00000001f) return;
            indices.Add(a); indices.Add(b); indices.Add(c);
            owners.Add(cell);
        }

        private void OnDestroy()
        {
            foreach (var filter in chunks.Values)
                if (filter != null && filter.sharedMesh != null) Destroy(filter.sharedMesh);
        }
    }
}
