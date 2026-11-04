# Unity Project Context

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
