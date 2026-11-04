using System;
using PlanetHarvest.Mining;
using PlanetHarvest.Voxel;
using UnityEngine;

namespace PlanetHarvest.Core
{
    public sealed class HarvestSession
    {
        public HarvestInventory Buffer { get; } = new HarvestInventory();
        public HarvestInventory Container { get; } = new HarvestInventory();
        public long Credits { get; private set; }
        public long Shipments { get; private set; }
        public int ShipmentPrice { get; }

        public HarvestSession(int shipmentPrice)
        {
            if (shipmentPrice <= 0) throw new ArgumentOutOfRangeException(nameof(shipmentPrice));
            ShipmentPrice = shipmentPrice;
        }

        public bool TryMine(VoxelData terrain, Vector3Int cell)
        {
            if (terrain == null || Buffer.Free < HarvestInventory.CubicMetre || !terrain.IsSolid(cell))
                return false;
            // Synchronous transaction: capacity is reserved before removing any terrain.
            if (!terrain.Remove(cell)) return false;
            Buffer.TryAdd(HarvestInventory.CubicMetre);
            return true;
        }

        public long Transfer() => Buffer.TransferTo(Container);

        public bool TryShip()
        {
            if (Container.Amount != Container.Capacity || Credits > long.MaxValue - ShipmentPrice)
                return false;
            Container.Empty();
            Credits += ShipmentPrice;
            Shipments++;
            return true;
        }
    }
}
