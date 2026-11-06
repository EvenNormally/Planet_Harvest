using PlanetHarvest.Core;
using UnityEngine;

namespace PlanetHarvest.Logistics
{
    [RequireComponent(typeof(Collider))]
    public sealed class ShipmentFlight : MonoBehaviour
    {
        [SerializeField] private HarvestGame game;
        [SerializeField] private Transform planet;
        [SerializeField, Min(1)] private float height = 60;
        private Vector3 dockPosition;
        private Collider bodyCollider;

        private void Awake()
        {
            dockPosition = transform.position;
            bodyCollider = GetComponent<Collider>();
        }

        private void Update()
        {
            var session = game.Session;
            float progress = session.IsShipping ? 1 - session.ShippingRemaining / session.ShipmentDuration : 0;
            Vector3 up = (dockPosition - planet.position).normalized;
            transform.position = dockPosition + up * (Mathf.Sin(progress * Mathf.PI) * height);
            bodyCollider.enabled = !session.IsShipping;
        }
    }
}
