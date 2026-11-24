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
        public HarvestInventory StoneContainer { get; }
        public long MiningVolume { get; }
        public float ShipmentDuration { get; }
        public float ShippingRemaining { get; private set; }
        public MaterialId ShippingMaterial { get; private set; }
        public bool IsShipping => ShippingMaterial != MaterialId.None;
        public long Credits { get; private set; }
        public long Shipments { get; private set; }
        public const long DeliveryGoal = 20 * HarvestInventory.CubicMetre;
        public long DeliveryProgress { get; private set; }
        public int ShipmentPrice { get; }
        private readonly int stoneShipmentPrice;

        public HarvestSession(int shipmentPrice, long miningVolume = HarvestInventory.CubicMetre,
            int containerLoads = 1, float shipmentDuration = 0, int stoneShipmentPrice = 150)
        {
            if (shipmentPrice <= 0) throw new ArgumentOutOfRangeException(nameof(shipmentPrice));
            if (stoneShipmentPrice <= 0) throw new ArgumentOutOfRangeException(nameof(stoneShipmentPrice));
            if (miningVolume <= 0 || containerLoads < 1 || shipmentDuration < 0 || float.IsNaN(shipmentDuration) || float.IsInfinity(shipmentDuration))
                throw new ArgumentOutOfRangeException(nameof(miningVolume));
            ShipmentPrice = shipmentPrice;
            this.stoneShipmentPrice = stoneShipmentPrice;
            MiningVolume = miningVolume;
            Buffer = new HarvestInventory(miningVolume);
            long capacity = checked(miningVolume * containerLoads);
            Container = new HarvestInventory(capacity, MaterialId.Regolith);
            StoneContainer = new HarvestInventory(capacity, MaterialId.Stone);
            ShipmentDuration = shipmentDuration;
        }

        public HarvestInventory ContainerFor(MaterialId material) => material switch
        {
            MaterialId.Regolith => Container,
            MaterialId.Stone => StoneContainer,
            _ => throw new ArgumentOutOfRangeException(nameof(material))
        };

        public int PriceFor(MaterialId material) => material switch
        {
            MaterialId.Regolith => ShipmentPrice,
            MaterialId.Stone => stoneShipmentPrice,
            _ => throw new ArgumentOutOfRangeException(nameof(material))
        };

        public bool TryMine(VoxelData terrain, Vector3Int cell) => TryMine(terrain, cell, out _);

        public bool TryMine(VoxelData terrain, Vector3Int cell, out MaterialId material)
        {
            return Buffer.TryMine(terrain, cell, MiningVolume, out material);
        }

        public long Transfer(MaterialId material = MaterialId.Regolith) =>
            IsShipping ? 0 : Buffer.TransferTo(ContainerFor(material), material);

        public bool TryShip(MaterialId material = MaterialId.Regolith)
        {
            var container = ContainerFor(material);
            if (IsShipping || container.Free != 0 || Credits > long.MaxValue - PriceFor(material))
                return false;
            ShippingMaterial = material;
            container.InTransit = true;
            ShippingRemaining = ShipmentDuration;
            if (ShippingRemaining == 0) CompleteShipment();
            return true;
        }

        public void AdvanceShipment(float seconds)
        {
            if (!IsShipping || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            ShippingRemaining = Mathf.Max(0, ShippingRemaining - seconds);
            if (ShippingRemaining == 0) CompleteShipment();
        }

        private void CompleteShipment()
        {
            var container = ContainerFor(ShippingMaterial);
            if (ShippingMaterial == MaterialId.Regolith)
                DeliveryProgress += Math.Min(DeliveryGoal - DeliveryProgress, container.Amount);
            Credits += PriceFor(ShippingMaterial);
            container.Empty();
            container.InTransit = false;
            ShippingMaterial = MaterialId.None;
            Shipments++;
        }
    }
}
