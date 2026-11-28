using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PlanetHarvest.Core;
using PlanetHarvest.Mining;
using PlanetHarvest.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlanetHarvest.Tests
{
    public class ExpeditionTests
    {
        [UnityTest]
        public IEnumerator MidFlightSaveRestoresCraterPacksAndExactlyOnePayment()
        {
            yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
            yield return null;
            var miner = Object.FindFirstObjectByType<EarthMining>();
            var moon = miner.Planet;
            var changes = new List<Vector3Int>();
            DrillStroke.Cut(moon, miner.Inventory, Vector3Int.zero, Vector3.forward, DrillProfile.Bore, 1, _ => false, changes);
            Assert.Greater(changes.Count, 1);
            var saved = changes.ToArray();
            var cargo = miner.Elevator.Freight;
            cargo.Load(miner.Inventory, MaterialId.Stone);
            if (cargo.Flying == MaterialId.None) Assert.IsTrue(cargo.Dispatch(MaterialId.Stone));
            cargo.Advance(.75f);
            var checkpoint = cargo.Capture();
            long pack = miner.Inventory.Amount;
            string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".gz");
            try
            {
                miner.Crew.Save(path);
                Assert.IsTrue(File.Exists(path), miner.Crew.Status);
                DrillStroke.Cut(moon, miner.Inventory, Vector3Int.left * 7, Vector3.up, DrillProfile.Disc, 1, _ => false, changes);
                Assert.IsNotEmpty(changes);
                cargo.Advance(10);
                miner.Crew.Load(path);
                Assert.AreEqual("Expedition geladen", miner.Crew.Status);
                CollectionAssert.AreEquivalent(saved, moon.Data.Removed);
                Assert.AreEqual(pack, miner.Inventory.Amount);
                Assert.AreEqual(checkpoint.progress, cargo.LegProgress);
                Assert.AreEqual(0, cargo.Credits);
                cargo.Advance(10);
                long paid = cargo.Program.Material(MaterialId.Stone).CreditsFor(checkpoint.loadsByMaterial[(int)MaterialId.Stone]);
                Assert.AreEqual(paid, cargo.Credits);
                cargo.Advance(10);
                Assert.AreEqual(paid, cargo.Credits);
                var invalid = ExpeditionSnapshot.Read(path);
                invalid.version = 999;
                ExpeditionSnapshot.Write(path, invalid);
                miner.Crew.Load(path);
                StringAssert.StartsWith("Laden fehlgeschlagen", miner.Crew.Status);
                Assert.AreEqual(paid, cargo.Credits);
                CollectionAssert.AreEquivalent(saved, moon.Data.Removed);
            }
            finally { File.Delete(path); File.Delete(path + ".bak"); }
        }

        [UnityTest]
        public IEnumerator TwoOverlappingDrillsConserveMaterialAcrossChunkBorders()
        {
            yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
            var miner = Object.FindFirstObjectByType<EarthMining>();
            var moon = miner.Planet;
            var a = new HarvestInventory(4000 * HarvestInventory.CubicMetre);
            var b = new HarvestInventory(a.Capacity);
            var changes = new List<Vector3Int>();
            var watch = Stopwatch.StartNew();
            int strokes = 0;
            for (int x = -16; x < 16; x += 3)
            for (int z = -12; z < 12; z += 3)
            {
                var contact = new Vector3Int(x,0,z);
                DrillStroke.Cut(moon, a, contact, Vector3.up, DrillProfile.Bowl, 1, _ => false, changes);
                DrillStroke.Cut(moon, b, contact, Vector3.right, DrillProfile.Bore, 1, _ => false, changes);
                Assert.IsEmpty(changes, "A second command on the removed sample minted material.");
                DrillStroke.Cut(moon, b, contact + Vector3Int.up * 2, Vector3.right, DrillProfile.Disc, 1, _ => false, changes);
                strokes += 3;
                if (strokes % 12 == 0) yield return null;
            }
            for (int i = 0; i < 12; i++) yield return null;
            int removed = moon.Data.InitialCount - moon.Data.SolidCount;
            Assert.Greater(removed, 1000);
            Assert.AreEqual(removed * HarvestInventory.CubicMetre, a.Amount + b.Amount);
            foreach (var filter in moon.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh.vertexCount > 0) Assert.AreSame(filter.sharedMesh, filter.GetComponent<MeshCollider>().sharedMesh);
            TestContext.Out.WriteLine($"{strokes} strokes, {removed} cells, {watch.ElapsedMilliseconds} ms including frame yields.");
        }

#if UNITY_EDITOR_WIN
        [UnityTest]
        public IEnumerator WindowsHostAcceptsGuestDrillAndRestoresItOnRejoin()
        {
            string exe = Path.GetFullPath("Builds/Prototyp/PlanetHarvest.exe");
            if (!File.Exists(exe)) Assert.Ignore("Erfordert den Windows-Build unter Builds/Prototyp.");
            yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
            yield return null;
            var miner = Object.FindFirstObjectByType<EarthMining>();
            Assert.NotNull(miner.Crew);
            string log = Path.GetFullPath("Logs/coop-player.log");
            using var host = Process.Start(new ProcessStartInfo(exe,
                $"-batchmode -nographics -harvestHost -logFile \"{log}\"") { UseShellExecute = false, CreateNoWindow = true });
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                float limit = Time.realtimeSinceStartup + 60;
                while (!miner.Crew.Paired && Time.realtimeSinceStartup < limit)
                {
                    Assert.IsFalse(host.HasExited, "Windows player exited. See " + log);
                    if (!miner.Crew.IsGuest) miner.Crew.Join("127.0.0.1");
                    yield return new WaitForSecondsRealtime(.4f);
                }
                Assert.IsTrue(miner.Crew.Paired, miner.Crew.Status);
                InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = Vector2.down });
                yield return new WaitForSecondsRealtime(.9f);
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.West));
                yield return new WaitForSecondsRealtime(1.4f);
                InputSystem.QueueStateEvent(pad, new GamepadState());
                yield return new WaitForSecondsRealtime(.4f);
                var removed = miner.Planet.Data.Removed.ToArray();
                long carried = miner.Inventory.Amount;
                Assert.IsNotEmpty(removed, "No host-confirmed terrain edit. " + miner.Crew.Status);
                Assert.AreEqual(removed.Length * HarvestInventory.CubicMetre, carried);
                miner.Crew.StopSession();
                Assert.AreEqual(0, miner.Inventory.Amount);
                Assert.IsEmpty(miner.Planet.Data.Removed);
                yield return new WaitForSecondsRealtime(.5f);
                miner.Crew.Join("127.0.0.1");
                limit = Time.realtimeSinceStartup + 10;
                while (!miner.Crew.Paired && Time.realtimeSinceStartup < limit) yield return null;
                Assert.IsTrue(miner.Crew.Paired, miner.Crew.Status);
                Assert.AreEqual(carried, miner.Inventory.Amount);
                CollectionAssert.AreEquivalent(removed, miner.Planet.Data.Removed);
            }
            finally
            {
                try
                {
                    if (miner && miner.Crew) miner.Crew.StopSession();
                    InputSystem.RemoveDevice(pad);
                }
                finally { if (!host.HasExited) { host.Kill(); host.WaitForExit(5000); } }
            }
        }
#endif
    }
}
