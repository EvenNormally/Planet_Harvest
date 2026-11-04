using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetHarvest.Voxel
{
    public sealed class VoxelData
    {
        public const int ChunkSize = 8;
        private readonly HashSet<Vector3Int> solid = new HashSet<Vector3Int>();
        public int SolidCount => solid.Count;
        public int Radius { get; }

        public VoxelData(int radius)
        {
            if (radius < 1 || radius > 24) throw new ArgumentOutOfRangeException(nameof(radius));
            Radius = radius;
            for (int x = -radius; x < radius; x++)
            for (int y = -radius; y < radius; y++)
            for (int z = -radius; z < radius; z++)
            {
                var cell = new Vector3Int(x, y, z);
                if (((Vector3)cell + Vector3.one * 0.5f).sqrMagnitude <= radius * radius)
                    solid.Add(cell);
            }
        }

        public bool IsSolid(Vector3Int cell) => solid.Contains(cell);
        internal bool Remove(Vector3Int cell) => solid.Remove(cell);

        public static Vector3Int ChunkOf(Vector3Int cell) => new Vector3Int(
            Mathf.FloorToInt((float)cell.x / ChunkSize),
            Mathf.FloorToInt((float)cell.y / ChunkSize),
            Mathf.FloorToInt((float)cell.z / ChunkSize));
    }
}
