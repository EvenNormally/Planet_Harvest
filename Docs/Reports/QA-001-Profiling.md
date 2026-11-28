# QA 001 · Profiling-Szenario: zwei parallele Bohrer

Erzeugt: 2026-11-28 02:07 UTC · Unity 6000.3.28f1 · Batchmodus · AMD Ryzen 7 3700X 8-Core Processor · NVIDIA GeForce RTX 3070 · 32694 MB RAM

Ablauf: Erd-Szene laden, Editor hostet die Crew, ein minimaler Gast-Client tritt per TCP bei. Zwei Bohrer schneiden 20.0 s lang automatisch Tunnelreihen (Profil Tunnel, Schnittfaktor 2.39, ein Schlag je 0.120 s und Bohrer = Kernbohrer mit höchster Radius- und Tempostufe). Messwerte sind Ist-Werte dieses Laufs, keine Grenzwerte.

## Abbau

| Bohrer | Schläge | Schläge mit Abtrag | gebucht m³ |
| --- | ---: | ---: | ---: |
| Bohrer A | 167 | 56 | 6216 |
| Bohrer B | 167 | 56 | 6216 |

Entfernte Zellen: 12432 (622 pro Sekunde). Materialbilanz: jede entfernte Zelle genau einmal gebucht (geprüft).

## Framezeit

| Frames | Mittel ms | Median ms | P95 ms | P99 ms | Max ms | Mittel FPS |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 52247 | 0.38 | 0.33 | 0.54 | 0.76 | 43.99 | 2612 |

Frames über 16,7 ms: 24, über 33,3 ms: 6. Die Bildrate ist im Test nicht begrenzt; Mittelwerte pro Frame hängen daher von der Bildrate ab.

## Mesh-Rebuilds

Chunk-Rebuilds gesamt 234, pro Frame im Mittel 0.00, maximal 4 (Budget 4 pro Frame in `VoxelPlanet.LateUpdate`). Frames mit Rebuild: 66 von 52247. Offene Rebuilds am Ende: 0.

Rebuilds pro Sekunde: 11.7. Rechenzeit Meshing+Collider: 867 ms gesamt, 3.71 ms je Chunk, 4.34 % der Laufzeit. Ein Frame mit vollem Budget kostet damit etwa 14.8 ms allein für Meshing.

## Spielstandgröße

| entfernte Zellen | JSON Bytes | GZip Bytes | GZip Bytes je Zelle |
| ---: | ---: | ---: | ---: |
| 1176 | 29436 | 3394 | 2.89 |
| 2058 | 49890 | 5720 | 2.78 |
| 3192 | 77274 | 8742 | 2.74 |
| 4032 | 96858 | 10953 | 2.72 |
| 5082 | 122058 | 13638 | 2.68 |
| 6132 | 147228 | 16349 | 2.67 |
| 7182 | 171978 | 18981 | 2.64 |
| 8022 | 192432 | 21159 | 2.64 |
| 9072 | 216972 | 23760 | 2.62 |
| 10122 | 241262 | 26505 | 2.62 |
| 11172 | 265466 | 29252 | 2.62 |
| 12012 | 284420 | 31356 | 2.61 |
| 12432 | 293912 | 32461 | 2.61 |

## Netzwerk

Gemessen auf der Host-Seite ab dem Ende des Beitritts (ohne den vollständigen Welt-Snapshot), inklusive 4-Byte-Rahmen, ohne TCP/IP-Overhead. Der Host sendet alle 0,1 s ein Update mit den neu entfernten Zellen.

| Richtung | Bytes | Bytes pro Bohrsekunde | Bytes je entfernter Zelle |
| --- | ---: | ---: | ---: |
| Host → Gast | 536354 | 26818 | 43.1 |
| Gast → Host | 0 | 0 | – |

Beim Gast angekommen: 536354 Bytes.

## Einordnung

- Im Batchmodus ohne sichtbares Fenster fehlt die GPU-Last; die Framezeit zeigt vor allem CPU-Kosten von Abbau, Meshing und Netzwerk. Für den 60-FPS-Zielwert im Editor oder Player auf dem Referenz-PC wiederholen.
- Beide Bohrer laufen auf dem Host; die Gast-Bedienabsichten (Raycast-Prüfung) sind nicht Teil der Last.
- Wiederholen: `unity test . --mode PlayMode --filter TwoParallelDrillsAutomaticDigCycleReport`, Zielpfad über die Umgebungsvariable `PLANET_HARVEST_QA_REPORT`.
