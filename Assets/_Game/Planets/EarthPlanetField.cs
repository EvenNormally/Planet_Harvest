using PlanetHarvest.Mining;
using UnityEngine;

namespace PlanetHarvest.Planets
{
    // Planet-local metres. Positive density is solid. The field has no mutable random state.
    public sealed class EarthPlanetField
    {
        public const int GeneratorVersion = 3;
        /// <summary>Direction of the crystal cave, far from the landing site.</summary>
        public static readonly Vector3 CrystalDirection = new Vector3(0.55f, -0.6f, 0.58f).normalized;
        public const float CrystalCaveRadius = 4.6f, CrystalDepth = 15, ShaftRadius = 1.7f, RockZoneDepth = 13;
        private readonly int seed;
        private readonly float radius, amplitude, frequency, tunnelRadius, soilDepth, oreDepth, oreChance;
        private readonly Vector3 phases;
        private readonly Vector3[] passage;
        private readonly Vector3 chamber, branchEnd, chamberVein, crystalCave, shaftTop;
        public int Extent => Mathf.CeilToInt(radius + amplitude + 1);
        public Vector3 SpawnDirection => new Vector3(0, radius, -7).normalized;
        public static readonly Vector3 LakeDirection = new Vector3(-0.34f, 1, 0.16f).normalized;
        public float WaterRadius => radius - 1.2f;
        public float Radius => radius;

        public EarthPlanetField(int seed, int radius = 48, float amplitude = 5, float frequency = 3,
            float tunnelRadius = 2.8f, float caveDepth = 10, float soilDepth = 2,
            float oreDepth = 7, float oreChance = 0.28f)
        {
            this.seed = seed;
            this.radius = Mathf.Clamp(radius, 26, 64);
            this.amplitude = Mathf.Clamp(amplitude, 0, 8);
            this.frequency = Mathf.Clamp(frequency, 1, 5);
            this.tunnelRadius = Mathf.Clamp(tunnelRadius, 2.3f, 3.5f);
            this.soilDepth = Mathf.Clamp(soilDepth, 1, 4);
            this.oreDepth = Mathf.Clamp(oreDepth, 5, 10);
            this.oreChance = Mathf.Clamp(oreChance, 0.05f, 0.5f);
            phases = new Vector3(UnitHash(1, 0, 0), UnitHash(0, 1, 0), UnitHash(0, 0, 1)) * (2 * Mathf.PI);

            // A descending gallery follows the curvature and ends in a larger chamber.
            // Its first section starts outside the hillside so the mouth always opens.
            float descent = Mathf.Clamp(caveDepth, 8, 14);
            float length = (descent + this.tunnelRadius) * 2.8f;
            passage = new Vector3[9];
            for (int i = 0; i < passage.Length; i++)
            {
                float t = i / (float)(passage.Length - 1);
                float angle = length * t / this.radius;
                Vector3 direction = new Vector3(0.06f * Mathf.Sin(t * Mathf.PI) * Mathf.Sin(phases.x),
                    Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
                passage[i] = direction * (SurfaceRadius(direction) + Mathf.Lerp(this.tunnelRadius, -descent, t));
            }
            chamber = passage[passage.Length - 1];
            branchEnd = passage[5] + Vector3.right * (7 + 2 * Mathf.Sin(phases.z));
            chamberVein = chamber - Vector3.right * (this.tunnelRadius + 1.2f);
            float surface = SurfaceRadius(CrystalDirection);
            crystalCave = CrystalDirection * (surface - CrystalDepth);
            shaftTop = CrystalDirection * (surface + 2);
        }

        /// <summary>Centre of the crystal cave in planet-local metres.</summary>
        public Vector3 CrystalCave => crystalCave;

        public float SurfaceRadius(Vector3 direction)
        {
            Vector3 p = direction.normalized * frequency;
            float broad = Mathf.Sin(p.x + phases.x) * Mathf.Cos(p.z + phases.z);
            float hills = Mathf.Sin(p.y * 2 + p.z + phases.y) * Mathf.Cos(2 * p.x - p.y);
            // Ease relief around the approach to keep the spawn and first ramp walkable.
            float relief = Mathf.SmoothStep(0.12f, 1, Mathf.Clamp01((1 - direction.normalized.y) * 2));
            float shoreDistance = (direction.normalized - LakeDirection).magnitude * radius;
            float basin = 3.6f * (1 - Mathf.SmoothStep(0, 1, shoreDistance / 8));
            return radius + amplitude * relief * (0.65f * broad + 0.35f * hills) - basin;
        }

        public float Depth(Vector3 p) => SurfaceRadius(p) - p.magnitude;

        public float Density(Vector3 p)
        {
            float depth = Depth(p);
            if (depth < -1 || depth > radius * 0.8f) return depth;
            float nearest = float.MaxValue;
            for (int i = 1; i < passage.Length; i++)
                nearest = Mathf.Min(nearest, SegmentDistanceSquared(p, passage[i - 1], passage[i]));
            nearest = Mathf.Min(nearest, SegmentDistanceSquared(p, passage[5], branchEnd));
            float gallery = Mathf.Sqrt(nearest) - tunnelRadius;
            float cavern = Vector3.Distance(p, chamber) - (tunnelRadius + 1.4f);
            float crystals = Vector3.Distance(p, crystalCave) - CrystalCaveRadius;
            float shaft = Mathf.Sqrt(SegmentDistanceSquared(p, shaftTop, crystalCave)) - ShaftRadius;
            return Mathf.Min(Mathf.Min(depth, Mathf.Min(gallery, cavern)), Mathf.Min(crystals, shaft));
        }

        public MaterialId MaterialAt(Vector3 p)
        {
            if (Density(p) <= 0) return MaterialId.None;
            return SolidMaterial(p);
        }

        // Called on known solid samples by the chunker.
        internal MaterialId SolidMaterial(Vector3 p)
        {
            float depth = Depth(p);
            if (depth <= soilDepth) return MaterialId.Soil;
            // Accessible seams in the gallery lead prospectors towards the deeper chamber.
            if ((p - (passage[4] - Vector3.right * tunnelRadius)).sqrMagnitude < 5) return MaterialId.CopperOre;
            if ((p - (chamber + Vector3.right * (tunnelRadius + 1.4f))).sqrMagnitude < 5) return MaterialId.Quartz;
            // The crystal cave walls glitter: a shell of crystal and quartz around the hollow.
            float shell = Vector3.Distance(p, crystalCave) - CrystalCaveRadius;
            if (shell < 1.6f)
            {
                Vector3Int facet = Vector3Int.FloorToInt(p / 1.5f);
                float sparkle = UnitHash(facet.x, facet.y, facet.z, 7);
                if (sparkle < 0.45f) return MaterialId.Crystal;
                if (sparkle < 0.7f) return MaterialId.Quartz;
            }
            if (depth < oreDepth) return MaterialId.Stone;
            if ((p - chamberVein).sqrMagnitude < 2.4f) return MaterialId.IronOre;
            Vector3Int key = Vector3Int.FloorToInt(p / 6);
            // Below the rock-zone depth iron veins are twice as frequent (Felszone).
            float chance = depth > RockZoneDepth ? Mathf.Min(0.6f, oreChance * 2) : oreChance;
            if (UnitHash(key.x, key.y, key.z) >= chance) return MaterialId.Stone;
            Vector3 offset = new Vector3(UnitHash(key.x, key.y, key.z, 1),
                UnitHash(key.x, key.y, key.z, 2), UnitHash(key.x, key.y, key.z, 3));
            Vector3 delta = p - ((Vector3)(key * 6) + Vector3.one * 2 + offset * 2);
            delta.y *= 1.35f;
            if (delta.sqrMagnitude >= 3.2f) return MaterialId.Stone;
            float mineral = UnitHash(key.x, key.y, key.z, 4);
            if (mineral < 0.36f) return MaterialId.CopperOre;
            return mineral < 0.60f ? MaterialId.Quartz : MaterialId.IronOre;
        }

        private static float SegmentDistanceSquared(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 segment = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, segment) / segment.sqrMagnitude);
            return (p - a - segment * t).sqrMagnitude;
        }

        private float UnitHash(int x, int y, int z, int salt = 0)
        {
            unchecked
            {
                uint h = (uint)seed ^ (uint)x * 0x8da6b343u ^ (uint)y * 0xd8163841u
                    ^ (uint)z * 0xcb1ab31fu ^ (uint)salt * 0x165667b1u;
                h ^= h >> 16;
                h *= 0x7feb352du;
                h ^= h >> 15;
                h *= 0x846ca68bu;
                return (h ^ (h >> 16)) % 16777216u / 16777216f;
            }
        }
    }
}
