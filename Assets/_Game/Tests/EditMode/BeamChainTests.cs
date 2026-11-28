using System.IO;
using NUnit.Framework;
using PlanetHarvest.Logistics;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using UnityEngine;

namespace PlanetHarvest.Tests
{
    public class BeamChainTests
    {
        private const long Unit = HarvestInventory.CubicMetre;

        private sealed class CountingSink : IBeamSink
        {
            public readonly HarvestInventory Store = new HarvestInventory(1000 * Unit);
            public bool Open = true;
            public long Accept(HarvestInventory source, MaterialId material, long volume) =>
                Open ? source.TransferTo(Store, material, volume) : 0;
        }

        private static BeamChain Chain(int segments)
        {
            var chain = new BeamChain(6, 4, .5f);
            for (int i = 0; i < segments; i++) chain.AddSegment();
            return chain;
        }

        private static long Total(BeamChain chain, CountingSink sink) => chain.Hopper.Amount + chain.Lane.Amount + sink.Store.Amount;

        [Test]
        public void LumpsTravelSlotBySlotAndArriveWithoutLoss()
        {
            var chain = Chain(2);
            var sink = new CountingSink();
            Assert.IsTrue(chain.Hopper.TryAdd(MaterialId.Stone, 3 * Unit));
            chain.Advance(.5f, sink);
            Assert.AreEqual(MaterialId.Stone, chain.Slot(0));
            Assert.IsTrue(chain.MovedIn(0));
            Assert.AreEqual(2 * Unit, chain.Hopper.Amount);
            Assert.AreEqual(Unit, chain.Lane.Amount);
            // Eight slots: the first lump needs eight steps to reach the head and one more to be delivered.
            chain.Advance(.5f * 7, sink);
            Assert.AreEqual(0, sink.Store.Amount);
            Assert.AreEqual(MaterialId.Stone, chain.Slot(7));
            chain.Advance(.5f, sink);
            Assert.AreEqual(Unit, sink.Store.Amount);
            chain.Advance(30, sink);
            Assert.AreEqual(3 * Unit, sink.Store.AmountOf(MaterialId.Stone));
            Assert.AreEqual(0, chain.Lane.Amount);
            Assert.AreEqual(3 * Unit, chain.Delivered);
        }

        [Test]
        public void ClosedTargetBacksUpTheWholeChainAndNothingIsLost()
        {
            var chain = Chain(1);
            var sink = new CountingSink { Open = false };
            chain.Hopper.TryAdd(MaterialId.Soil, 6 * Unit);
            chain.Advance(20, sink);
            Assert.IsTrue(chain.Blocked);
            Assert.AreEqual(4, chain.LumpCount, "Every slot is occupied, no lump overlaps another.");
            Assert.AreEqual(2 * Unit, chain.Hopper.Amount);
            Assert.AreEqual(6 * Unit, Total(chain, sink));
            sink.Open = true;
            chain.Advance(20, sink);
            Assert.IsFalse(chain.Blocked);
            Assert.AreEqual(6 * Unit, sink.Store.Amount);
            Assert.AreEqual(6 * Unit, Total(chain, sink));
        }

        [Test]
        public void UnconnectedChainHoldsLumpsAtTheLastMast()
        {
            var chain = Chain(1);
            chain.Hopper.TryAdd(MaterialId.Stone, Unit);
            chain.Advance(10, null);
            Assert.AreEqual(MaterialId.Stone, chain.Slot(3));
            Assert.IsTrue(chain.Blocked);
            Assert.AreEqual(Unit, chain.Lane.Amount);
        }

        [Test]
        public void SegmentsCanOnlyBeRemovedWhenEmpty()
        {
            var chain = Chain(2);
            var sink = new CountingSink { Open = false };
            chain.Hopper.TryAdd(MaterialId.Stone, Unit);
            chain.Advance(10, sink);
            Assert.IsFalse(chain.TryRemoveSegment(), "The lump waits in the last segment.");
            sink.Open = true;
            chain.Advance(.5f, sink);
            Assert.IsTrue(chain.TryRemoveSegment());
            Assert.IsTrue(chain.TryRemoveSegment());
            Assert.IsFalse(chain.TryRemoveSegment());
            Assert.AreEqual(0, chain.SlotCount);
        }

        [Test]
        public void SnapshotRoundtripRestoresSlotsHopperAndTimer()
        {
            var chain = Chain(2);
            chain.Hopper.TryAdd(MaterialId.Stone, 2 * Unit);
            chain.Hopper.TryAdd(MaterialId.Soil, 3 * Unit);
            chain.Advance(1.7f, null);
            var json = JsonUtility.ToJson(chain.Capture());
            var copy = Chain(0);
            copy.Restore(JsonUtility.FromJson<BeamChainSnapshot>(json));
            Assert.AreEqual(chain.SlotCount, copy.SlotCount);
            for (int i = 0; i < chain.SlotCount; i++) Assert.AreEqual(chain.Slot(i), copy.Slot(i));
            Assert.AreEqual(chain.Hopper.Amount, copy.Hopper.Amount);
            Assert.AreEqual(chain.Lane.Amount, copy.Lane.Amount);
            Assert.AreEqual(chain.StepProgress, copy.StepProgress, 1e-6);
            var state = chain.Capture();
            Assert.AreEqual(5 * Unit, BeamChain.Holding(state, MaterialId.Stone) + BeamChain.Holding(state, MaterialId.Soil));
            state.slots = new MaterialId[3];
            Assert.Throws<InvalidDataException>(() => copy.Restore(state));
        }

        [Test]
        public void ElevatorSinkFillsTheMatchingContainerAndBacksUpDuringFlight()
        {
            var freight = new EarthFreight(TestFreight.Act1());
            var sink = new ElevatorBeamSink(freight);
            var chain = Chain(1);
            chain.Hopper.TryAdd(MaterialId.IronOre, 6 * Unit);
            // The iron container holds 4 m³: the fourth lump fills it and launches the cabin.
            chain.Advance(.5f * 8, sink);
            Assert.AreEqual(MaterialId.IronOre, freight.Flying);
            Assert.AreEqual(4 * Unit, freight.Container(MaterialId.IronOre).Amount);
            chain.Advance(.5f * 2, sink);
            Assert.IsTrue(chain.Blocked, "The container is in transit and refuses lumps.");
            Assert.AreEqual(6 * Unit, freight.Container(MaterialId.IronOre).Amount + chain.Lane.Amount + chain.Hopper.Amount);
            freight.Advance(2 * EarthFreight.LegSeconds);
            chain.Advance(10, sink);
            Assert.AreEqual(4 * Unit, freight.Delivered(MaterialId.IronOre));
            Assert.AreEqual(2 * Unit, freight.Container(MaterialId.IronOre).Amount);
            Assert.AreEqual(0, chain.LumpCount);
        }
    }
}
