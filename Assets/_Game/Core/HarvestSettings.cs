using UnityEngine;

namespace PlanetHarvest.Core
{
    [CreateAssetMenu(menuName = "Planet Harvest/MVP Settings")]
    public sealed class HarvestSettings : ScriptableObject
    {
        [SerializeField, Range(8, 200)] private int moonRadius = 12;
        [SerializeField, Range(1, 2)] private int cellSize = 1;
        [SerializeField] private bool roundedSurface;
        [SerializeField, Min(1)] private int containerLoads = 1;
        [SerializeField, Min(0)] private float shipmentDuration;
        [SerializeField, Min(1)] private float gravity = 12f;
        [SerializeField, Min(1)] private float moveSpeed = 4f;
        [SerializeField, Min(1)] private float jumpSpeed = 6f;
        [SerializeField, Min(1)] private int shipmentPrice = 100;

        public int MoonRadius => Mathf.Clamp(moonRadius, 8, 200);
        public int CellSize => Mathf.Clamp(cellSize, 1, 2);
        public bool RoundedSurface => roundedSurface;
        public int ContainerLoads => Mathf.Max(1, containerLoads);
        public float ShipmentDuration => Mathf.Max(0, shipmentDuration);
        public float Gravity => Mathf.Max(1, gravity);
        public float MoveSpeed => Mathf.Max(1, moveSpeed);
        public float JumpSpeed => Mathf.Max(1, jumpSpeed);
        public int ShipmentPrice => Mathf.Max(1, shipmentPrice);
    }
}
