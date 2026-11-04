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
        private readonly Dictionary<Vector3Int, MeshFilter> chunks = new Dictionary<Vector3Int, MeshFilter>();
        public VoxelData Data { get; private set; }
        private static readonly Vector3Int[] Directions =
        {
            Vector3Int.right, Vector3Int.left, Vector3Int.up,
            Vector3Int.down, Vector3Int.forward, Vector3Int.back
        };

        private void Awake()
        {
            Data = new VoxelData(settings.MoonRadius);
            int min = Mathf.FloorToInt(-Data.Radius / (float)VoxelData.ChunkSize);
            int max = Mathf.FloorToInt((Data.Radius - 1) / (float)VoxelData.ChunkSize);
            for (int x = min; x <= max; x++)
            for (int y = min; y <= max; y++)
            for (int z = min; z <= max; z++)
            {
                var key = new Vector3Int(x, y, z);
                var chunk = new GameObject($"Chunk {key}", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                chunk.transform.SetParent(transform, false);
                chunk.GetComponent<MeshRenderer>().sharedMaterial = material;
                var filter = chunk.GetComponent<MeshFilter>();
                filter.sharedMesh = new Mesh { name = chunk.name, indexFormat = IndexFormat.UInt32 };
                chunks.Add(key, filter);
                Rebuild(key);
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

        public Vector3Int CellAt(RaycastHit hit) => Vector3Int.FloorToInt(
            transform.InverseTransformPoint(hit.point - hit.normal * 0.01f));

        private void Rebuild(Vector3Int key)
        {
            if (!chunks.TryGetValue(key, out var filter)) return;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
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
                    vertices.Add(centre - u * 0.5f - v * 0.5f);
                    vertices.Add(centre + u * 0.5f - v * 0.5f);
                    vertices.Add(centre + u * 0.5f + v * 0.5f);
                    vertices.Add(centre - u * 0.5f + v * 0.5f);
                    for (int i = 0; i < 4; i++) normals.Add(n);
                    triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
                    triangles.Add(index); triangles.Add(index + 2); triangles.Add(index + 3);
                }
            }
            Mesh mesh = filter.sharedMesh;
            var collider = filter.GetComponent<MeshCollider>();
            collider.sharedMesh = null;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            if (vertices.Count > 0) collider.sharedMesh = mesh;
        }

        private void OnDestroy()
        {
            foreach (var filter in chunks.Values)
                if (filter != null && filter.sharedMesh != null) Destroy(filter.sharedMesh);
        }
    }
}
