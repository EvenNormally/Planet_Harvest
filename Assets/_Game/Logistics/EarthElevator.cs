using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Logistics
{
    public sealed class EarthElevator : MonoBehaviour
    {
        [SerializeField] private EarthCargoBin soil;
        [SerializeField] private EarthCargoBin stone;
        public EarthFreight Freight { get; } = new EarthFreight();

        public void Place(VoxelMoon planet)
        {
            Vector3 spawn = planet.Earth.SpawnDirection;
            Vector3 local = (spawn * planet.Earth.SurfaceRadius(spawn) + new Vector3(6, 0, 9)).normalized;
            Vector3 up = planet.transform.TransformDirection(local);
            Vector3 origin = planet.transform.position + up * (planet.Earth.Extent + 4);
            Vector3 foot = planet.transform.TransformPoint(local * planet.Earth.SurfaceRadius(local));
            if (Physics.Raycast(origin, -up, out var hit, planet.Earth.Extent, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<VoxelMoon>() == planet) foot = hit.point;
            transform.SetPositionAndRotation(foot, Quaternion.LookRotation(Vector3.ProjectOnPlane(planet.transform.forward, up), up));
        }

        private void Update()
        {
            Freight.Advance(Time.deltaTime);
            float fraction = Mathf.SmoothStep(0, 1, Freight.LegProgress);
            float height = 65 * (Freight.Returning ? 1 - fraction : fraction);
            Position(soil, height);
            Position(stone, height);
        }

        private void Position(EarthCargoBin bin, float height)
        {
            bool flying = Freight.Flying == bin.Material;
            bin.SetHeight(flying ? height : 0, !flying);
        }
    }
}
