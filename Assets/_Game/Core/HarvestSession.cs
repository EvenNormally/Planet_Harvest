using System;
using PlanetHarvest.Mining;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Core
{
    public sealed class HarvestSession
    {
        public HarvestInventory Buffer { get; }
        public HarvestInventory Container { get; }
        public long MiningVolume { get; }
        public float ShipmentDuration { get; }
        public float ShippingRemaining { get; private set; }
        public bool IsShipping => ShippingRemaining > 0;
        public long Credits { get; private set; }
        public long Shipments { get; private set; }
        public int ShipmentPrice { get; }

        public HarvestSession(int shipmentPrice, long miningVolume = HarvestInventory.CubicMetre,
            int containerLoads = 1, float shipmentDuration = 0)
        {
            if (shipmentPrice <= 0) throw new ArgumentOutOfRangeException(nameof(shipmentPrice));
            if (miningVolume <= 0 || containerLoads < 1 || shipmentDuration < 0 || float.IsNaN(shipmentDuration) || float.IsInfinity(shipmentDuration))
                throw new ArgumentOutOfRangeException(nameof(miningVolume));
            ShipmentPrice = shipmentPrice;
            MiningVolume = miningVolume;
            Buffer = new HarvestInventory(miningVolume);
            Container = new HarvestInventory(checked(miningVolume * containerLoads));
            ShipmentDuration = shipmentDuration;
        }

        public bool TryMine(VoxelData terrain, Vector3Int cell)
        {
            if (terrain == null || Buffer.Free < MiningVolume || !terrain.IsSolid(cell))
                return false;
            // Synchronous transaction: capacity is reserved before removing any terrain.
            if (!terrain.Remove(cell)) return false;
            Buffer.TryAdd(MiningVolume);
            return true;
        }

        public long Transfer() => IsShipping ? 0 : Buffer.TransferTo(Container);

        public bool TryShip()
        {
            if (IsShipping || Container.Amount != Container.Capacity || Credits > long.MaxValue - ShipmentPrice)
                return false;
            if (ShipmentDuration > 0) ShippingRemaining = ShipmentDuration;
            else CompleteShipment();
            return true;
        }

        public void AdvanceShipment(float seconds)
        {
            if (!IsShipping || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            ShippingRemaining = Mathf.Max(0, ShippingRemaining - seconds);
            if (!IsShipping) CompleteShipment();
        }

        private void CompleteShipment()
        {
            Container.Empty();
            Credits += ShipmentPrice;
            Shipments++;
        }
    }
}
