using UnityEngine;

namespace PlanetHarvest.Core
{
    [CreateAssetMenu(menuName = "Planet Harvest/MVP Settings")]
    public sealed class HarvestSettings : ScriptableObject
    {
        [SerializeField, Range(8, 24)] private int moonRadius = 12;
        [SerializeField, Min(1)] private float gravity = 12f;
        [SerializeField, Min(1)] private float moveSpeed = 4f;
        [SerializeField, Min(1)] private float jumpSpeed = 6f;
        [SerializeField, Min(1)] private int shipmentPrice = 100;

        public int MoonRadius => Mathf.Clamp(moonRadius, 8, 24);
        public float Gravity => Mathf.Max(1, gravity);
        public float MoveSpeed => Mathf.Max(1, moveSpeed);
        public float JumpSpeed => Mathf.Max(1, jumpSpeed);
        public int ShipmentPrice => Mathf.Max(1, shipmentPrice);
    }
}
