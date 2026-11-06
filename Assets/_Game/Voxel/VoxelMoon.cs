using System.Collections.Generic;
using PlanetHarvest.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlanetHarvest.Voxel
{
    public sealed class VoxelMoon : MonoBehaviour
    {
        [SerializeField] private HarvestSettings settings;
        [SerializeField] private Material material;
        [SerializeField] private GameObject editorPreview;
        public int CellSize => settings.CellSize;
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
            Data = new VoxelData(settings.MoonRadius / CellSize);
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

        public bool TryMine(HarvestSession session, Vector3Int cell)
        {
            if (!session.TryMine(Data, cell)) return false;
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
            if (!chunks.TryGetValue(key, out var filter))
            {
                var chunk = new GameObject($"Chunk {key}", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                chunk.transform.SetParent(transform, false);
                chunk.transform.localScale = Vector3.one * CellSize;
                chunk.GetComponent<MeshRenderer>().sharedMaterial = material;
                filter = chunk.GetComponent<MeshFilter>();
                filter.sharedMesh = new Mesh { name = chunk.name, indexFormat = IndexFormat.UInt32 };
                chunks.Add(key, filter);
            }
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            var owners = new List<Vector3Int>();
            Vector3Int start = key * VoxelData.ChunkSize;
            for (int x = 0; x < VoxelData.ChunkSize; x++)
            for (int y = 0; y < VoxelData.ChunkSize; y++)
            for (int z = 0; z < VoxelData.ChunkSize; z++)
            {
                Vector3Int cell = start + new Vector3Int(x, y, z);
                if (!Data.IsSolid(cell)) continue;
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
                    bool exterior = settings.RoundedSurface && !Data.IsOriginalSolid(cell + direction);
                    Vector3 faceNormal = Vector3.Cross(vertices[index + 1] - vertices[index], vertices[index + 2] - vertices[index]).normalized;
                    for (int i = 0; i < 4; i++) normals.Add(exterior ? vertices[index + i].normalized : faceNormal);
                    AddTriangle(index, index + 1, index + 2, cell, vertices, triangles, owners);
                    AddTriangle(index, index + 2, index + 3, cell, vertices, triangles, owners);
                }
            }
            Mesh mesh = filter.sharedMesh;
            var collider = filter.GetComponent<MeshCollider>();
            triangleCells[collider] = owners;
            collider.sharedMesh = null;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            if (triangles.Count > 0) collider.sharedMesh = mesh;
        }

        private Vector3 SurfaceVertex(Vector3 vertex) => settings.RoundedSurface ? Data.RoundSurfaceVertex(vertex) : vertex;

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
