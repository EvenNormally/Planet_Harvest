using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PlanetHarvest.Core;
using PlanetHarvest.Mining;
using PlanetHarvest.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlanetHarvest.Tests
{
    /// <summary>
    /// QA 001: two drills dig in parallel on an automatic cycle while the editor hosts a crew link with a
    /// minimal guest client. Measures frame time, mesh rebuilds per frame, save size after N removed cells
    /// and network bytes per drilling second. Writes a Markdown report; the numbers are not pass criteria,
    /// material conservation is.
    /// Report path: Logs/Profiling/QA-001-Profiling.md, or the path in PLANET_HARVEST_QA_REPORT.
    /// </summary>
    [Category("Profiling")]
    public class ProfilingScenarioTests
    {
        private const float DrillSeconds = 20;
        private const int SaveCheckpointCells = 1000;
        private const string ReportVariable = "PLANET_HARVEST_QA_REPORT";

        private sealed class Drill
        {
            public readonly string Name;
            public readonly HarvestInventory Pack = new HarvestInventory(20000 * HarvestInventory.CubicMetre);
            public readonly Func<int, Vector3Int> Contact;
            public readonly Vector3 Axis;
            public readonly DrillProfile Profile;
            public int Strokes, Hits;
            public float Next;

            public Drill(string name, Func<int, Vector3Int> contact, Vector3 axis, DrillProfile profile)
            {
                Name = name;
                Contact = contact;
                Axis = axis;
                Profile = profile;
            }
        }

        [UnityTest]
        public IEnumerator TwoParallelDrillsAutomaticDigCycleReport()
        {
            yield return SceneManager.LoadSceneAsync(EarthTraversalTests.ScenePath);
            yield return null;
            var miner = Object.FindFirstObjectByType<EarthMining>();
            var moon = miner.Planet;
            var crew = miner.Crew;
            // Interval of the fastest hand tool with the highest speed upgrade: the heaviest player load.
            var upgrades = miner.Equipment.UpgradeCatalog;
            float interval = MiningTools.Spec(MiningTool.Drill).RepeatSeconds /
                upgrades.Step(UpgradeAxis.Speed, upgrades.MaxLevel(UpgradeAxis.Speed)).Value;
            float scale = 1.45f * upgrades.Step(UpgradeAxis.Radius, upgrades.MaxLevel(UpgradeAxis.Radius)).Value;

            var guest = new CrewConnection();
            long guestReceived = 0;
            guest.Opened += () => guest.Send("{\"kind\":\"hello\",\"world\":" + Quote(moon.WorldKey) + "}");
            guest.Received += json => guestReceived += Encoding.UTF8.GetByteCount(json) + 4;
            var drills = new[]
            {
                // Tunnel rows deep below the surface: x-runs advancing in z, and z-runs advancing in x.
                new Drill("Bohrer A", k => new Vector3Int(-36 + k % 24 * 3, 6, -24 + k / 24 % 16 * 3), Vector3.right, DrillProfile.Bore),
                new Drill("Bohrer B", k => new Vector3Int(-24 + k / 24 % 16 * 3, -8, -36 + k % 24 * 3), Vector3.forward, DrillProfile.Bore)
            };
            var frameMs = new List<float>(4096);
            var rebuildsPerFrame = new List<int>(4096);
            var saves = new List<(int cells, long raw, long packed)>();
            var changes = new List<Vector3Int>(256);
            string savePath = Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".json.gz");
            long hostSentStart = 0, hostReceivedStart = 0, guestStart = 0;
            float drilling = 0;
            int removedStart = moon.Data.InitialCount - moon.Data.SolidCount;
            try
            {
                crew.Host();
                guest.Join("127.0.0.1", ExpeditionCrew.Port, Time.unscaledTime);
                float limit = Time.realtimeSinceStartup + 15;
                while (!crew.Paired && Time.realtimeSinceStartup < limit)
                {
                    guest.Tick(Time.unscaledTime);
                    yield return null;
                }
                Assert.IsTrue(crew.Paired, crew.Status);
                for (int i = 0; i < 5; i++) { guest.Tick(Time.unscaledTime); yield return null; }
                hostSentStart = crew.NetworkBytesSent;
                hostReceivedStart = crew.NetworkBytesReceived;
                guestStart = guestReceived;

                int lastRebuilds = moon.MeshRebuilds;
                double rebuildMsStart = moon.RebuildMilliseconds;
                int nextCheckpoint = SaveCheckpointCells;
                while (drilling < DrillSeconds)
                {
                    foreach (var drill in drills)
                    {
                        if (drilling < drill.Next) continue;
                        drill.Next += interval;
                        DrillStroke.Cut(moon, drill.Pack, drill.Contact(drill.Strokes++), drill.Axis, drill.Profile, scale,
                            _ => false, changes);
                        drill.Hits += changes.Count > 0 ? 1 : 0;
                    }
                    int removed = moon.Data.InitialCount - moon.Data.SolidCount - removedStart;
                    if (removed >= nextCheckpoint)
                    {
                        saves.Add(MeasureSave(moon, miner, savePath));
                        nextCheckpoint += SaveCheckpointCells;
                    }
                    guest.Tick(Time.unscaledTime);
                    yield return null;
                    float dt = Time.unscaledDeltaTime;
                    drilling += dt;
                    frameMs.Add(dt * 1000);
                    rebuildsPerFrame.Add(moon.MeshRebuilds - lastRebuilds);
                    lastRebuilds = moon.MeshRebuilds;
                }
                int backlog = moon.PendingRebuilds;
                double rebuildMs = moon.RebuildMilliseconds - rebuildMsStart;
                // Let the host flush the last update packets before reading the counters.
                for (int i = 0; i < 10; i++) { guest.Tick(Time.unscaledTime); yield return null; }
                long hostSent = crew.NetworkBytesSent - hostSentStart;
                long hostReceived = crew.NetworkBytesReceived - hostReceivedStart;
                long guestBytes = guestReceived - guestStart;
                saves.Add(MeasureSave(moon, miner, savePath));

                int totalRemoved = moon.Data.InitialCount - moon.Data.SolidCount - removedStart;
                long booked = drills.Sum(d => d.Pack.Amount);
                Assert.AreEqual(totalRemoved * HarvestInventory.CubicMetre, booked, "Every removed cell is booked exactly once.");
                Assert.Greater(totalRemoved, 0);
                Assert.Greater(hostSent, 0, "The host must stream terrain edits to the guest.");
                var roundtrip = ExpeditionSnapshot.Read(savePath);
                Assert.AreEqual(moon.Data.InitialCount - moon.Data.SolidCount, roundtrip.removed.Length);

                string report = Report(drills, interval, scale, frameMs, rebuildsPerFrame, rebuildMs, backlog, saves,
                    totalRemoved, drilling, hostSent, hostReceived, guestBytes);
                string path = Environment.GetEnvironmentVariable(ReportVariable);
                if (string.IsNullOrWhiteSpace(path)) path = Path.Combine("Logs", "Profiling", "QA-001-Profiling.md");
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                File.WriteAllText(path, report, new UTF8Encoding(false));
                TestContext.Out.WriteLine(report);
            }
            finally
            {
                guest.Dispose();
                if (miner && miner.Crew) miner.Crew.StopSession();
                File.Delete(savePath);
                File.Delete(savePath + ".bak");
            }
        }

        private static (int, long, long) MeasureSave(Voxel.VoxelPlanet moon, EarthMining miner, string path)
        {
            // Same layout the crew writes; only the removed cells grow with the number of modifications.
            var state = new ExpeditionSnapshot
            {
                version = ExpeditionSnapshot.CurrentVersion, world = moon.WorldKey,
                removed = moon.Data.Removed.ToArray(), host = new MinerSnapshot(), guest = new MinerSnapshot(),
                freight = miner.Elevator.Freight.Capture()
            };
            ExpeditionSnapshot.Write(path, state);
            return (state.removed.Length, Encoding.UTF8.GetByteCount(JsonUtility.ToJson(state)), new FileInfo(path).Length);
        }

        private static string Report(Drill[] drills, float interval, float scale, List<float> frameMs, List<int> rebuilds,
            double rebuildMs, int backlog, List<(int cells, long raw, long packed)> saves, int removed, float seconds,
            long hostSent, long hostReceived, long guestBytes)
        {
            var c = CultureInfo.InvariantCulture;
            var sorted = frameMs.OrderBy(v => v).ToArray();
            float Percentile(float p) => sorted[Mathf.Clamp(Mathf.CeilToInt(p * sorted.Length) - 1, 0, sorted.Length - 1)];
            var text = new StringBuilder();
            text.AppendLine("# QA 001 · Profiling-Szenario: zwei parallele Bohrer");
            text.AppendLine();
            text.AppendLine($"Erzeugt: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", c)} UTC · Unity {Application.unityVersion} · " +
                $"{(Application.isBatchMode ? "Batchmodus" : "Editor")} · {SystemInfo.processorType.Trim()} · " +
                $"{SystemInfo.graphicsDeviceName} · {SystemInfo.systemMemorySize} MB RAM");
            text.AppendLine();
            text.AppendLine("Ablauf: Erd-Szene laden, Editor hostet die Crew, ein minimaler Gast-Client tritt per TCP bei. " +
                $"Zwei Bohrer schneiden {seconds.ToString("F1", c)} s lang automatisch Tunnelreihen (Profil Tunnel, " +
                $"Schnittfaktor {scale.ToString("F2", c)}, ein Schlag je {interval.ToString("F3", c)} s und Bohrer = " +
                "Kernbohrer mit höchster Radius- und Tempostufe). Messwerte sind Ist-Werte dieses Laufs, keine Grenzwerte.");
            text.AppendLine();
            text.AppendLine("## Abbau");
            text.AppendLine();
            text.AppendLine("| Bohrer | Schläge | Schläge mit Abtrag | gebucht m³ |");
            text.AppendLine("| --- | ---: | ---: | ---: |");
            foreach (var d in drills)
                text.AppendLine($"| {d.Name} | {d.Strokes} | {d.Hits} | {d.Pack.Amount / HarvestInventory.CubicMetre} |");
            text.AppendLine();
            text.AppendLine($"Entfernte Zellen: {removed} ({(removed / seconds).ToString("F0", c)} pro Sekunde). " +
                "Materialbilanz: jede entfernte Zelle genau einmal gebucht (geprüft).");
            text.AppendLine();
            text.AppendLine("## Framezeit");
            text.AppendLine();
            text.AppendLine("| Frames | Mittel ms | Median ms | P95 ms | P99 ms | Max ms | Mittel FPS |");
            text.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
            float mean = frameMs.Average();
            text.AppendLine($"| {frameMs.Count} | {mean.ToString("F2", c)} | {Percentile(.5f).ToString("F2", c)} | " +
                $"{Percentile(.95f).ToString("F2", c)} | {Percentile(.99f).ToString("F2", c)} | {sorted[sorted.Length - 1].ToString("F2", c)} | " +
                $"{(1000 / mean).ToString("F0", c)} |");
            text.AppendLine();
            text.AppendLine($"Frames über 16,7 ms: {frameMs.Count(v => v > 1000f / 60)}, über 33,3 ms: {frameMs.Count(v => v > 1000f / 30)}. " +
                "Die Bildrate ist im Test nicht begrenzt; Mittelwerte pro Frame hängen daher von der Bildrate ab.");
            text.AppendLine();
            text.AppendLine("## Mesh-Rebuilds");
            text.AppendLine();
            text.AppendLine($"Chunk-Rebuilds gesamt {rebuilds.Sum()}, pro Frame im Mittel {rebuilds.Average().ToString("F2", c)}, " +
                $"maximal {rebuilds.Max()} (Budget 4 pro Frame in `VoxelPlanet.LateUpdate`). " +
                $"Frames mit Rebuild: {rebuilds.Count(r => r > 0)} von {rebuilds.Count}. Offene Rebuilds am Ende: {backlog}.");
            text.AppendLine();
            int total = Math.Max(1, rebuilds.Sum());
            text.AppendLine($"Rebuilds pro Sekunde: {(rebuilds.Sum() / seconds).ToString("F1", c)}. Rechenzeit Meshing+Collider: " +
                $"{rebuildMs.ToString("F0", c)} ms gesamt, {(rebuildMs / total).ToString("F2", c)} ms je Chunk, " +
                $"{(rebuildMs / seconds / 10).ToString("F2", c)} % der Laufzeit. Ein Frame mit vollem Budget kostet damit etwa " +
                $"{(4 * rebuildMs / total).ToString("F1", c)} ms allein für Meshing.");
            text.AppendLine();
            text.AppendLine("## Spielstandgröße");
            text.AppendLine();
            text.AppendLine("| entfernte Zellen | JSON Bytes | GZip Bytes | GZip Bytes je Zelle |");
            text.AppendLine("| ---: | ---: | ---: | ---: |");
            foreach (var s in saves)
                text.AppendLine($"| {s.cells} | {s.raw} | {s.packed} | {((double)s.packed / Math.Max(1, s.cells)).ToString("F2", c)} |");
            text.AppendLine();
            text.AppendLine("## Netzwerk");
            text.AppendLine();
            text.AppendLine("Gemessen auf der Host-Seite ab dem Ende des Beitritts (ohne den vollständigen Welt-Snapshot), inklusive 4-Byte-Rahmen, " +
                "ohne TCP/IP-Overhead. Der Host sendet alle 0,1 s ein Update mit den neu entfernten Zellen.");
            text.AppendLine();
            text.AppendLine("| Richtung | Bytes | Bytes pro Bohrsekunde | Bytes je entfernter Zelle |");
            text.AppendLine("| --- | ---: | ---: | ---: |");
            text.AppendLine($"| Host → Gast | {hostSent} | {(hostSent / seconds).ToString("F0", c)} | {((double)hostSent / Math.Max(1, removed)).ToString("F1", c)} |");
            text.AppendLine($"| Gast → Host | {hostReceived} | {(hostReceived / seconds).ToString("F0", c)} | – |");
            text.AppendLine();
            text.AppendLine($"Beim Gast angekommen: {guestBytes} Bytes.");
            text.AppendLine();
            text.AppendLine("## Einordnung");
            text.AppendLine();
            text.AppendLine("- Im Batchmodus ohne sichtbares Fenster fehlt die GPU-Last; die Framezeit zeigt vor allem CPU-Kosten von Abbau, " +
                "Meshing und Netzwerk. Für den 60-FPS-Zielwert im Editor oder Player auf dem Referenz-PC wiederholen.");
            text.AppendLine("- Beide Bohrer laufen auf dem Host; die Gast-Bedienabsichten (Raycast-Prüfung) sind nicht Teil der Last.");
            text.AppendLine("- Wiederholen: `unity test . --mode PlayMode --filter TwoParallelDrillsAutomaticDigCycleReport`, " +
                $"Zielpfad über die Umgebungsvariable `{ReportVariable}`.");
            return text.ToString();
        }

        private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
