# Unity Project Context

## Verifizierter aktueller Stand: 2026-11-06

Projektroot: `C:/Users/henry/Downloads/Planet_Harvest/Planet_Harvest`.
Unity 6000.3.28f1, URP und Input System wie unten. Die Angaben weiter unten
beschreiben den historischen Setup-Zustand; Gameplay ist inzwischen implementiert.

- Runtime-Assembly `PlanetHarvest.Runtime`; separate Editor-, EditMode- und PlayMode-Assemblies.
- Vorhandene MVP-Szene: `Assets/_Game/Scenes/PlanetHarvest.unity` (12-m-Mond).
- Neue eigenständige Testszene: `Assets/_Game/Scenes/PlanetHarvest_Test200m.unity`.
  200 m Radius, 9,81 m/s² radiale Gravitation, 2-m-Abbauraster, ausschließlich Regolith.
  Die ursprünglichen Grenzvertices werden gemeinsam auf die Kugel projiziert;
  Mesh und MeshCollider sind abgerundet, Abbaulöcher bleiben volumetrisch.
- `VoxelData`: implizite Kugel plus entfernte Zellen; `VoxelMoon`: Oberflächen-Chunks
  mit MeshCollider, bei Abbau werden betroffene Nachbar-Chunks aktualisiert/angelegt.
- `HarvestSession` besitzt Inventare und Versandzustand; `HarvestGame` treibt die Zeit.
  Testkonfiguration: 8 m³ persönlicher Kanister, 32 m³ Versandkanister, 8 s Hin-/Rückflug.
  Bestehende MVP-Konfiguration behält 1-m-Voxel und sofortigen Versand.
- `RadialPlayer`: radial gedrehte Rigidbody-/CapsuleCollider-Kombination, Bodenprüfung
  entlang der lokalen Gravitation und Kräfte in FixedUpdate; geklonte Input-Actions.
  Alte CharacterController werden in älteren Szenen zur Laufzeit deaktiviert.
  Der vorherige CharacterController blieb trotz Transform-Drehung entlang Welt-Y und
  verlor auf anderen Hemisphären den Bodenkontakt. UI: uGUI/TextMeshPro.
  Kein Speichern, kein Netzwerkspiel. Szene generiert das Terrain beim Play-Start.
- Unity-Pipeline 0.10.0-exp.1 lokal erreichbar. Direkter HTTP-Aufruf über
  `Tools/Invoke-UnityPipeline.ps1` (PowerShell 7), da die Hub-CLI nicht ausführbar war.
  Verbindungstoken wird ausschließlich aus dem lokalen Descriptor gelesen.
- Keine neuen Ordner, Pakete oder Build-Szenen hinzugefügt. Neue Szene direkt im Editor testen.
- Bedienung und Grenzen: `Docs/TEST_PLANET_200M.md`.

## Historischer Setup-Bericht

Analysiert am 2026-11-04, vor initialem Commit. Root ist der Ordner mit Assets/Packages/ProjectSettings.

Bestätigt: Unity 6000.3.28f1 (ProjectVersion.txt), URP 17.3.0 (Manifest und GraphicsSettings),
Input System 1.20.1 mit activeInputHandler=1, Test Framework 1.6.0, Pipeline 0.10.0-exp.1.
Force Text (m_SerializationMode=2), Visible Meta Files. Windows x64 als Ziel.

Buildszene: Assets/Scenes/SampleScene.unity. Vorhandene Scripts ausschließlich
TutorialInfo/Readme und dessen Editor; keine Gameplayarchitektur vorhanden.
Template-Assets unter Settings, Scenes, TutorialInfo bleiben erhalten.
Neue Fachbereiche unter _Game; ThirdParty für externe Assets.
Editor-/Testassemblies werden getrennt von künftigem Runtime-Code gehalten.

Bestätigte Tools: Git 2.53.0, LFS 3.7.1, Codex 0.163.0 mit Anmeldung,
Unity-Plugin 0.1.8-beta aktiv; CLI auf 1.0.0-beta.16 aktualisiert.
Aktuelle Validierung und Editor-Verbindungsstatus: ../SETUP_REPORT.md.
Keine zusätzlichen fremden Agent-Plugins installiert.

Geplant: Chunk-Terrain, radiale Gravitation, Mining, Inventar, Logistik und Wirtschaft.
Keine implementierten Funktionen aus der Roadmap ableiten.
Konventionen: AGENTS.md; Abhängigkeiten: Packages/manifest.json und packages-lock.json.
