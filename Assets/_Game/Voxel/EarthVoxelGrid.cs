using PlanetHarvest.Mining;
using PlanetHarvest.Planets;
using UnityEngine;

namespace PlanetHarvest.Voxel
{
    // Sample the immutable field once. Meshes on either side of a chunk boundary use
    // exactly the same eight cell centres to locate their shared surface-net vertex.
    internal sealed class EarthVoxelGrid
    {
        private readonly int extent, side;
        private readonly float[] densities;
        private readonly byte[] materials;
        public int SolidCount { get; }

        public EarthVoxelGrid(EarthPlanetField field)
        {
            extent = field.Extent;
            side = 2 * extent;
            densities = new float[side * side * side];
            materials = new byte[densities.Length];
            int count = 0;
            for (int x = -extent; x < extent; x++)
            for (int y = -extent; y < extent; y++)
            for (int z = -extent; z < extent; z++)
            {
                var cell = new Vector3Int(x, y, z);
                Vector3 centre = (Vector3)cell + Vector3.one * 0.5f;
                int i = Index(cell);
                float density = field.Density(centre);
                densities[i] = density;
                if (density <= 0) continue;
                materials[i] = (byte)field.SolidMaterial(centre);
                count++;
            }
            SolidCount = count;
        }

        private int Index(Vector3Int p) => ((p.x + extent) * side + p.y + extent) * side + p.z + extent;
        private bool Contains(Vector3Int p) => p.x >= -extent && p.x < extent && p.y >= -extent
            && p.y < extent && p.z >= -extent && p.z < extent;
        public MaterialId MaterialAt(Vector3Int p) => Contains(p) ? (MaterialId)materials[Index(p)] : MaterialId.None;
        private float Density(Vector3Int p) => Contains(p) ? densities[Index(p)] : -1;

        public bool HasBoundary(Vector3Int key)
        {
            Vector3Int start = key * VoxelData.ChunkSize - Vector3Int.one;
            bool first = MaterialAt(start) != MaterialId.None;
            for (int x = 0; x < VoxelData.ChunkSize + 2; x++)
            for (int y = 0; y < VoxelData.ChunkSize + 2; y++)
            for (int z = 0; z < VoxelData.ChunkSize + 2; z++)
                if ((MaterialAt(start + new Vector3Int(x, y, z)) != MaterialId.None) != first)
                    return true;
            return false;
        }

        public Vector3 SurfaceVertex(Vector3Int corner)
        {
            Vector3 sum = Vector3.zero;
            int crossings = 0;
            Vector3Int start = corner - Vector3Int.one;
            for (int i = 0; i < 8; i++)
            {
                var cell = start + new Vector3Int(i & 1, (i >> 1) & 1, i >> 2);
                float value = Density(cell);
                if ((i & 1) == 0) Accumulate(cell, Vector3Int.right, value, ref sum, ref crossings);
                if ((i & 2) == 0) Accumulate(cell, Vector3Int.up, value, ref sum, ref crossings);
                if ((i & 4) == 0) Accumulate(cell, Vector3Int.forward, value, ref sum, ref crossings);
            }
            return crossings == 0 ? (Vector3)corner : sum / crossings;
        }

        private void Accumulate(Vector3Int cell, Vector3Int axis, float from, ref Vector3 sum, ref int crossings)
        {
            float to = Density(cell + axis);
            if ((from > 0) == (to > 0)) return;
            float t = Mathf.Clamp(from / (from - to), 0.02f, 0.98f);
            sum += (Vector3)cell + Vector3.one * 0.5f + (Vector3)axis * t;
            crossings++;
        }
    }
}
