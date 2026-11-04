using UnityEngine;

namespace PlanetHarvest.Logistics
{
    public sealed class HarvestTerminal : MonoBehaviour
    {
        [SerializeField] private bool elevator;
        public bool IsElevator => elevator;
    }
}
