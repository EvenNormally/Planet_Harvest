using UnityEngine;

namespace PlanetHarvest.Machines
{
    /// <summary>Lets the player ray find the robot that owns this collider.</summary>
    public sealed class DrillRobotMarker : MonoBehaviour
    {
        public DrillRobotUnit Unit { get; internal set; }
    }
}
