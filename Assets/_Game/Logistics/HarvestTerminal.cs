using UnityEngine;

namespace PlanetHarvest.Logistics
{
    public sealed class HarvestTerminal : MonoBehaviour
    {
        [SerializeField] private bool elevator;
        [SerializeField] private bool returningCanister;
        public bool IsElevator => elevator;
        public bool IsReturningCanister => returningCanister;
    }
}
