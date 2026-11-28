# Messberichte

`QA-001-Profiling.md` erzeugt der PlayMode-Test `ProfilingScenarioTests.TwoParallelDrillsAutomaticDigCycleReport`
(Kategorie `Profiling`, Testassembly, nicht im Player-Build). Ohne Umgebungsvariable schreibt er nach
`Logs/Profiling/` (nicht versioniert); für einen versionierten Bericht:

```powershell
$env:PLANET_HARVEST_QA_REPORT = "$PWD\Docs\Reports\QA-001-Profiling.md"
unity test . --mode PlayMode --filter TwoParallelDrillsAutomaticDigCycleReport --output Logs/playmode-profiling.xml
Remove-Item Env:PLANET_HARVEST_QA_REPORT
```

## Befunde aus dem Lauf vom 28. November 2026 (Batchmodus, Ryzen 7 3700X)

- **Meshing ist der Engpass.** Ein Chunk-Rebuild (16³ Zellen, Mesh und MeshCollider) kostet rund 3,7 ms auf dem
  Hauptthread. Mit dem Budget von vier Rebuilds pro Frame entstehen Spitzen um 15 ms, also knapp am
  60-FPS-Budget, sobald noch GPU-Last dazukommt. Kandidaten: Budget nach Zeit statt Anzahl, Mesh-Erzeugung in
  Jobs/Burst, Collider nur für Chunks in Spielernähe zeitnah bauen.
- **Spielstand wächst linear.** Etwa 2,6 Bytes GZip pro entfernter Zelle (24 Bytes JSON). 100 000 entfernte
  Zellen ergeben damit grob 260 KB. Unkritisch für den Vertical Slice; langfristig Chunk-Bitmasken statt
  Zellkoordinaten.
- **Netzwerk:** rund 27 KB/s Host → Gast bei zwei voll aufgerüsteten Bohrern, etwa 43 Bytes je entfernter Zelle.
  Jede Zelle wird als JSON-Koordinate übertragen, zusätzlich alle 0,1 s beide Crew-Zustände und der Liftzustand.
  Für vier Spieler und Maschinen lohnen sich ein binäres Format und Deltas nur bei Änderung.
- Die Bildrate ist im Test nicht begrenzt, ohne Fenster fehlt die GPU-Last. Für die 60-FPS-Abnahme den Lauf im
  Editor-Playmode oder im Player auf dem Referenz-PC wiederholen.
