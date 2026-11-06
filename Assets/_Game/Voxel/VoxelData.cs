using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetHarvest.Voxel
{
    public sealed class VoxelData
    {
        public const int ChunkSize = 8;
        // The sphere is implicit. Store only mined cells, never its filled interior.
        private readonly HashSet<Vector3Int> removed = new HashSet<Vector3Int>();
        private readonly int initialCount;
        public int SolidCount => initialCount - removed.Count;
        public int Radius { get; }

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

        public bool IsOriginalSolid(Vector3Int cell) =>
            ((Vector3)cell + Vector3.one * 0.5f).sqrMagnitude <= Radius * Radius;
        public bool IsSolid(Vector3Int cell) => IsOriginalSolid(cell) && !removed.Contains(cell);
        internal bool Remove(Vector3Int cell) => IsSolid(cell) && removed.Add(cell);

        public Vector3 RoundSurfaceVertex(Vector3 vertex)
        {
            Vector3Int corner = Vector3Int.RoundToInt(vertex);
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
            Vector3 min = key * ChunkSize;
            Vector3 max = min + Vector3.one * ChunkSize;
            var bounds = new Bounds((min + max) * 0.5f, Vector3.one * (ChunkSize + 2));
            float closest = bounds.SqrDistance(Vector3.zero);
            Vector3 farthest = new Vector3(Mathf.Max(Mathf.Abs(min.x), Mathf.Abs(max.x)),
                Mathf.Max(Mathf.Abs(min.y), Mathf.Abs(max.y)), Mathf.Max(Mathf.Abs(min.z), Mathf.Abs(max.z)));
            return closest <= Radius * Radius && farthest.sqrMagnitude >= (Radius - 1) * (Radius - 1);
        }

        public static Vector3Int ChunkOf(Vector3Int cell) => new Vector3Int(
            Mathf.FloorToInt((float)cell.x / ChunkSize),
            Mathf.FloorToInt((float)cell.y / ChunkSize),
            Mathf.FloorToInt((float)cell.z / ChunkSize));
    }
}
