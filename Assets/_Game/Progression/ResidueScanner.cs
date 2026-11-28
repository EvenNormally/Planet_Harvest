using PlanetHarvest.Mining;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Progression
{
    /// <summary>
    /// Late-game helper from chapter 6: finds the nearest remaining solid cell so the campaign never fails on an
    /// invisible crumb. Scans cubic shells outward from the origin and stops at the first shell with a hit.
    /// </summary>
    public static class ResidueScanner
    {
        public static bool Unlocked(EarthFreight freight) => freight.Contracts.AllComplete;

        public static bool TryFindNearest(VoxelData terrain, Vector3Int origin, int maxRadius, out Vector3Int nearest, out MaterialId material)
        {
            nearest = default;
            material = MaterialId.None;
            float best = float.MaxValue;
            for (int shell = 0; shell <= maxRadius; shell++)
            {
                for (int x = -shell; x <= shell; x++)
                for (int y = -shell; y <= shell; y++)
                for (int z = -shell; z <= shell; z++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Max(Mathf.Abs(y), Mathf.Abs(z))) != shell) continue;
                    var cell = origin + new Vector3Int(x, y, z);
                    var found = terrain.MaterialAt(cell);
                    if (found == MaterialId.None) continue;
                    float distance = new Vector3(x, y, z).sqrMagnitude;
                    if (distance >= best) continue;
                    best = distance;
                    nearest = cell;
                    material = found;
                }
                // Anything in a later shell is at least `shell + 1` away; stop once that cannot beat the best hit.
                if (best <= (shell + 1) * (shell + 1)) return true;
            }
            return material != MaterialId.None;
        }
    }
}
