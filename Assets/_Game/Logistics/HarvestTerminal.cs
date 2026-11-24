using PlanetHarvest.Mining;
using UnityEngine;

namespace PlanetHarvest.Logistics
{
    public sealed class HarvestTerminal : MonoBehaviour
    {
        [SerializeField] private bool elevator;
        [SerializeField] private bool returningCanister;
        [SerializeField] private MaterialId material = MaterialId.Regolith;
        public MaterialId Material => material;
        public bool IsElevator => elevator;
        public bool IsReturningCanister => returningCanister;
    }
}
