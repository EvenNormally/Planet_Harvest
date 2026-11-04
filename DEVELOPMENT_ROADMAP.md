# Entwicklungsroadmap

Jede Phase endet mit Review, nachvollziehbaren Tests und einem spielbaren Prüfstand.
Zahlen für Performance erst auf gemeinsam festgelegter Referenzhardware messen.

| Phase | Voraussetzungen | Aufgaben | Überprüfbare Akzeptanzkriterien |
| --- | --- | --- | --- |
| 0 – Initialisierung | Vorhandenes Unity-Projekt und GitHub-Zugriff | Git/LFS, Dokumentation, Teamregeln, CLI, Prüfungen | Editor importiert und kompiliert; Tests ausgeführt; Commit auf main synchron; zweiter PC nach SETUP einrichtbar |
| 1 – First Person und Planet | Phase 0 | Mond, Input, Kamera, Bewegung, radiale Gravitation | Spieler kann den Mond umrunden; lokales Oben bleibt korrekt; keine Pol-Sprünge oder Durchfälle; Gravitation an mehreren Positionen getestet |
| 2 – Voxel-Mining | Phase 1 | Dichtefeld, Chunks, Meshing, Collider, Materialgewinn, Save-Grundlage | Durchgehendes Loch über Chunk-Grenze; keine Randspalten; Luftabbau erzeugt nichts; Volumenerhaltung und Save-Roundtrip getestet |
| 3 – Container und Logistik | Phase 2 | Sortierung, 1/5/20/100-m³-Container, Elevator | Keine Überfüllung; Material bleibt bei Transfers erhalten; Sendung exakt einmal verbucht; volle/leere Container getestet |
| 4 – Wirtschaft und Upgrades | Phase 3 | Credits, Bohrer, Förderrate, Kapazität, Tiefe | Kauf nur bei ausreichendem Guthaben; Upgrade wirkt nach Laden weiter; keine doppelten Auszahlungen; Transaktionen getestet |
| 5 – Automatisierung | Phase 4 | Förderbänder, automatische Bohrer, Sortierer | Anlage arbeitet ohne Spielerinteraktion; voller Ausgang erzeugt Rückstau; Pause/Laden dupliziert keine Rohstoffe |
| 6 – Zweiter Planet | Phase 5 | Erdähnlicher Planet, Biome, Materialverteilung, Reise | Planeten reproduzierbar aus Seeds; Reise erhält beide Weltzustände; Gravitation und Saveversionen korrekt |
| 7 – Optimierung und Visuals | Phase 6 | Profiling, Streaming/LOD, Sounds, VFX, UI | Vereinbarte Framezeit-/RAM-Budgets in festem Benchmark eingehalten; Windows-Build und Langzeittest bestanden; keine neuen Fehler |

Phase 0 umfasst kein fertiges Mining, keinen Controller und keine Produktionsanlagen.
