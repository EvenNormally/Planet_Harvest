using System;
using System.Collections.Generic;
using PlanetHarvest.Mining;
using PlanetHarvest.Planets;
using UnityEngine;

namespace PlanetHarvest.Voxel
{
    public sealed class VoxelData
    {
        public const int ChunkSize = 8;
        // The sphere is implicit. Store only mined cells, never its filled interior.
        private readonly HashSet<Vector3Int> removed = new HashSet<Vector3Int>();
        private readonly int initialCount;
        private readonly EarthVoxelGrid earth;
        public int InitialCount => initialCount;
        public IEnumerable<Vector3Int> Removed => removed;
        public int SolidCount => initialCount - removed.Count;
        public int Radius { get; }
        public int CellsPerChunk { get; } = ChunkSize;

        public VoxelData(int radius)
        {
            if (radius < 1 || radius > 200) throw new ArgumentOutOfRangeException(nameof(radius));
            Radius = radius;
            for (int x = -radius; x < radius; x++)
            for (int y = -radius; y < radius; y++)
            {
                float remaining = radius * radius - (x + 0.5f) * (x + 0.5f) - (y + 0.5f) * (y + 0.5f);
                if (remaining >= 0) initialCount += 2 * Mathf.Max(0, Mathf.FloorToInt(Mathf.Sqrt(remaining) + 0.5f));
            }
        }

        public VoxelData(EarthPlanetField field, int cellsPerChunk = 16)
        {
            if (cellsPerChunk < 1) throw new ArgumentOutOfRangeException(nameof(cellsPerChunk));
            CellsPerChunk = cellsPerChunk;
            earth = new EarthVoxelGrid(field);
            Radius = field.Extent;
            initialCount = earth.SolidCount;
        }

        public bool IsOriginalSolid(Vector3Int cell) => earth != null
            ? earth.MaterialAt(cell) != MaterialId.None
            : ((Vector3)cell + Vector3.one * 0.5f).sqrMagnitude <= Radius * Radius;
        public bool IsSolid(Vector3Int cell) => IsOriginalSolid(cell) && !removed.Contains(cell);
        public MaterialId MaterialAt(Vector3Int cell) => removed.Contains(cell) ? MaterialId.None : OriginalMaterial(cell);

        internal MaterialId OriginalMaterial(Vector3Int cell)
        {
            if (!IsOriginalSolid(cell)) return MaterialId.None;
            if (earth != null) return earth.MaterialAt(cell);
            float stoneRadius = Radius - 1;
            return ((Vector3)cell + Vector3.one * 0.5f).sqrMagnitude <= stoneRadius * stoneRadius
                ? MaterialId.Stone : MaterialId.Regolith;
        }

        internal bool TryRemove(Vector3Int cell, out MaterialId material)
        {
            material = MaterialAt(cell);
            if (material == MaterialId.None) return false;
            removed.Add(cell);
            earth?.SetRemoved(cell, true);
            return true;
        }

        internal void RestoreCell(Vector3Int cell)
        {
            if (removed.Remove(cell)) earth?.SetRemoved(cell, false);
        }

        public Vector3 RoundSurfaceVertex(Vector3 vertex)
        {
            Vector3Int corner = Vector3Int.RoundToInt(vertex);
            if (earth != null) return earth.SurfaceVertex(corner);
            bool solid = false, air = false;
            for (int x = -1; x <= 0; x++)
            for (int y = -1; y <= 0; y++)
            for (int z = -1; z <= 0; z++)
            {
                if (IsOriginalSolid(corner + new Vector3Int(x, y, z))) solid = true;
                else air = true;
            }
            // Shared, immutable mapping keeps adjacent chunks AND mined hole rims watertight.
            // Only the original outer boundary is rounded; underground excavation stays volumetric.
            return solid && air ? vertex.normalized * Radius : vertex;
        }

        public bool IsSurfaceChunk(Vector3Int key)
        {
            if (earth != null) return earth.HasBoundary(key, CellsPerChunk);
            Vector3 min = key * ChunkSize;
            Vector3 max = min + Vector3.one * ChunkSize;
            var bounds = new Bounds((min + max) * 0.5f, Vector3.one * (ChunkSize + 2));
            float closest = bounds.SqrDistance(Vector3.zero);
            Vector3 farthest = new Vector3(Mathf.Max(Mathf.Abs(min.x), Mathf.Abs(max.x)),
                Mathf.Max(Mathf.Abs(min.y), Mathf.Abs(max.y)), Mathf.Max(Mathf.Abs(min.z), Mathf.Abs(max.z)));
            return closest <= Radius * Radius && farthest.sqrMagnitude >= (Radius - 1) * (Radius - 1);
        }

        internal Vector3Int ChunkKey(Vector3Int cell) => ChunkOf(cell, CellsPerChunk);

        public static Vector3Int ChunkOf(Vector3Int cell, int chunkSize = ChunkSize) => new Vector3Int(
            Mathf.FloorToInt((float)cell.x / chunkSize),
            Mathf.FloorToInt((float)cell.y / chunkSize),
            Mathf.FloorToInt((float)cell.z / chunkSize));
    }
}
