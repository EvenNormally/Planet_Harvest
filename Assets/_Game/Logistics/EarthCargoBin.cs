using PlanetHarvest.Mining;
using UnityEngine;

namespace PlanetHarvest.Logistics
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EarthCargoBin : MonoBehaviour
    {
        [SerializeField] private MaterialId material;
        public MaterialId Material => material;
        private Vector3 rest;
        private BoxCollider hitBox;

        private void Awake()
        {
            rest = transform.localPosition;
            hitBox = GetComponent<BoxCollider>();
        }

        internal void SetHeight(float metres, bool atDock)
        {
            transform.localPosition = rest + Vector3.up * metres;
            hitBox.enabled = atDock;
        }
    }
}
