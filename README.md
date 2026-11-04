# Planet Harvest

3D-First-Person-Mining-, Automatisierungs- und Simulationsspiel für Windows x64.
Zwei Entwickler bauen gemeinsam kugelförmige Planeten, zerstörbares Terrain und eine Rohstoffwirtschaft.

**Stand:** Phase 0. Vorhandenes URP-Template mit SampleScene; Gameplay ist geplant, noch nicht implementiert.

## Voraussetzungen

| Werkzeug | Festgestellter Stand |
| --- | --- |
| Unity Editor | **6000.3.28f1**, Revision 8f082d2fe476 |
| URP | 17.3.0 |
| Input System | 1.20.1, aktiviert |
| Unity Test Framework | 1.6.0 |
| Pipeline für Editor-Automation | 0.10.0-exp.1 |
| Git / Git LFS | 2.53.0.windows.1 / 3.7.1 |
| Codex CLI | 0.163.0, ChatGPT-Anmeldung vorhanden |
| Offizielles Unity-Plugin | 0.1.8-beta, installiert und aktiviert |
| Unity CLI | 1.0.0-beta.16 |

Editorversion nicht eigenmächtig aktualisieren. Vollständige Paketauflösung steht in
`Packages/manifest.json` und `Packages/packages-lock.json`.
Vorhandene Template-Pakete, einschließlich AI-Preview-Paketen, bleiben erhalten.

## Einstieg

```powershell
git clone https://github.com/EvenNormally/Planet_Harvest.git
cd Planet_Harvest
git lfs install --local
git lfs pull
```

In Unity Hub diesen Ordner unter Projects hinzufügen und mit **6000.3.28f1** öffnen.
Die vollständige Anleitung steht in [SETUP.md](SETUP.md).

## Struktur

Eigener Code und eigene Assets: `Assets/_Game/`.
Bereiche: Core, Player, Planets, Voxel, Mining, Resources, Logistics, Economy,
Progression, UI, Audio, Art (Materials/Models/Textures/Shaders), Prefabs, Scenes,
ScriptableObjects, Editor, Tests (EditMode/PlayMode).
Drittanbieter gehören nach `Assets/ThirdParty/`; bestehende Template-Assets bleiben an ihrem Ort.

**Achtung:** Unity behandelt Ordner namens `Resources` speziell. Dieser gewünschte
Ordner bleibt vorerst leer; Material-Domaincode wird bevorzugt in Mining/Core organisiert.
Große Terrain- oder Art-Daten nicht versehentlich in Resources legen.

## Zusammenarbeit und Prüfung

`main` ist stabil; Features auf `feature/...`, Fehler auf `fix/...`, Dokumentation auf
`docs/...`. Änderungen per Pull Request mit Review des zweiten Entwicklers.
Details: [CONTRIBUTING.md](CONTRIBUTING.md), verbindliche Agent-Regeln: [AGENTS.md](AGENTS.md).

```powershell
powershell -File Tools/Validate-Repository.ps1
unity test . --mode EditMode --output Logs/editmode-results.xml
unity test . --mode PlayMode --output Logs/playmode-results.xml
```

Vor Batch-Tests den interaktiven Editor regulär schließen. CI prüft die Repositorystruktur;
Unity-Lizenz und Editor-Tests werden lokal geprüft. Kein Cloud-Unity-Test wird vorgetäuscht.

Weitere Dokumente: [Architektur](ARCHITECTURE.md), [Spielidee](GAME_DESIGN.md),
[Roadmap](DEVELOPMENT_ROADMAP.md), [Prüfbericht](Docs/SETUP_REPORT.md).
