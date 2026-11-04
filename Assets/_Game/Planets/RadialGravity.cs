using UnityEngine;

namespace PlanetHarvest.Planets
{
    public static class RadialGravity
    {
        // No preferred direction exists at the exact centre: zero force, stable up.
        public static Vector3 Up(Vector3 position, Vector3 centre)
        {
            Vector3 offset = position - centre;
            return offset.sqrMagnitude < 0.000001f ? Vector3.up : offset.normalized;
        }

        public static Vector3 Acceleration(Vector3 position, Vector3 centre, float strength)
        {
            Vector3 offset = centre - position;
            return offset.sqrMagnitude < 0.000001f ? Vector3.zero : offset.normalized * strength;
        }
    }
}
