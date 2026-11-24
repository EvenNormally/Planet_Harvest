using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Player
{
    [RequireComponent(typeof(RadialPlayer))]
    public sealed class EarthSpawn : MonoBehaviour
    {
        [SerializeField] private VoxelMoon planet;

        private void Start()
        {
            var body = GetComponent<Rigidbody>();
            Vector3 position = planet.SpawnPosition();
            Vector3 up = (position - planet.transform.position).normalized;
            body.position = position;
            body.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(planet.transform.forward, up), up);
        }
    }
}
