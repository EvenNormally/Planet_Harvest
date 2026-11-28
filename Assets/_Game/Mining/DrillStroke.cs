using System;
using System.Collections.Generic;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Mining
{
    public enum DrillProfile { Bowl, Bore, Disc }

    public static class DrillStroke
    {
        public static string Name(DrillProfile profile) => profile switch
        {
            DrillProfile.Bore => "Tunnel",
            DrillProfile.Disc => "Scheibe",
            _ => "Mulde"
        };

        // Each solid sample represents one cubic metre in the prototype ledger.
        public static void Cut(VoxelPlanet moon, HarvestInventory pack, Vector3Int contact, Vector3 axis,
            DrillProfile profile, float scale, Predicate<Vector3Int> protectedCell, List<Vector3Int> changed)
        {
            changed.Clear();
            if (!moon.Data.IsSolid(contact) || axis.sqrMagnitude < .1f || pack.Free < HarvestInventory.CubicMetre) return;
            axis.Normalize();
            int extent = Mathf.CeilToInt(3 * scale);
            for (int x = -extent; x <= extent; x++)
            for (int y = -extent; y <= extent; y++)
            for (int z = -extent; z <= extent; z++)
            {
                var offset = new Vector3Int(x, y, z);
                Vector3 p = (Vector3)offset / scale;
                float along = Vector3.Dot(p, axis);
                float radial = Mathf.Max(0, p.sqrMagnitude - along * along);
                bool inside = profile switch
                {
                    DrillProfile.Bore => along >= -.4f && along <= 2.8f && radial <= 1.1f,
                    DrillProfile.Disc => Mathf.Abs(along) <= .65f && radial <= 3.1f,
                    _ => p.sqrMagnitude <= 2.1f
                };
                var cell = contact + offset;
                if (!inside || protectedCell(cell) || !pack.TryMine(moon.Data, cell, HarvestInventory.CubicMetre, out _)) continue;
                moon.Changed(cell, true);
                changed.Add(cell);
                if (pack.Free < HarvestInventory.CubicMetre) return;
            }
        }
    }
}
