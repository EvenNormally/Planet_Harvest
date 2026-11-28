# Planet Harvest

## Erster spielbarer Prototyp

Starte `Builds/Prototyp/PlanetHarvest.exe` oder öffne `Assets/_Game/Scenes/PlanetHarvest_Earth.unity` in Unity und drücke Play. Auf dem 48-m-Planeten beginnt die Crew vor dem Höhleneingang. Rechts steht der Space Elevator, links die Werkzeugstation. Der Landevertrag verlangt insgesamt 20 m³ Erde und 12 m³ Stein. Weitere Aufträge verwenden die bereits vorhandenen Eisen-, Kupfer- und Quarzadern.

| Eingabe | Aktion |
| --- | --- |
| WASD, Maus, Leertaste | Laufen, schauen, springen |
| Linke Maustaste halten | Mit dem ausgerüsteten Bohrer abbauen |
| R oder Steuerkreuz rechts | Zwischen Mulde, Tunnel und Scheibe wechseln |
| 1–4 oder Mausrad | Werkzeug oder Materialprobe in die Hand nehmen |
| E am passenden Behälter | Material aus dem 24-m³-Rucksack umladen. Volle Behälter starten automatisch. Erneut E mit leerem Materialfach verschickt eine Teilladung bei freiem Lift. |
| E an der Werkbank | Für gemeinsame Liefererlöse einen persönlichen Bohrer kaufen |
| Tab oder I | Inventar und Crew-Menü öffnen |
| F5 / F9 | Expedition speichern / letzten Speicherpunkt laden |
| F3 | Bildrate einblenden |

**Zu zweit spielen.** Beide starten dieselbe Version. Im Inventar wählt einer **Crew hosten**, der andere trägt die erreichbare IP des Hosts ein und wählt **IP beitreten**. Für zwei Instanzen auf demselben Rechner gilt `127.0.0.1`. Direktverbindung über TCP-Port 24781, im LAN über die lokale IP. Der Gast kann nach einer Unterbrechung denselben zweiten Crewplatz wieder übernehmen. Der Host verwaltet Gelände, Lift und Credits. Beide haben einen eigenen Rucksack und eigene Werkzeugkäufe.

**Speicherpunkte.** F5 sichert beide Crewplätze samt Inventaren, Positionen, Werkzeugen, Bohrformen und laufender Fracht. Beim Trennen einer verbundenen Crew legt der Host einen zusätzlichen automatischen Speicherpunkt an. F9 lädt den jüngsten manuellen oder automatischen Stand. Die Dateien liegen unter `Application.persistentDataPath` (Windows: LocalLow im Firmen-/Produktordner), im Batch-Test im temporären Unity-Cache. Nach dem Verlassen einer fremden Sitzung kehrt der Gast in seine vorherige lokale Welt zurück.

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

**Achtung:** Unity behandelt Ordner namens `Resources` speziell. `Assets/_Game/Resources`
enthält die Shader `EarthSky`, `FieldFinish` und `LakeWater`, die per `Resources.Load`
geladen werden und deshalb dort bleiben müssen. Große Terrain- oder Art-Daten nicht in
Resources legen.

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

Maßgebliches Design-Dokument ist
[Docs/Design/Planet_Harvest_Spieldesign_und_Entwicklungsplan.docx](Docs/Design/Planet_Harvest_Spieldesign_und_Entwicklungsplan.docx)
(Konzeptversion 1.0); bei Widersprüchen zu älteren Markdown-Dokumenten gilt dieses.

Weitere Dokumente: [Aktuelle Spielrichtung](Docs/CURRENT_GAME_DIRECTION.md), [Architektur](ARCHITECTURE.md),
[Spielkonzept](Docs/GAME_CONCEPT.md), [Codex-Implementierungsplan](Docs/IMPLEMENTATION_PLAN.md),
[Roadmap](DEVELOPMENT_ROADMAP.md), Archiv: [Spielidee (historisch)](Docs/Archive/GAME_DESIGN.md),
[Prüfbericht](Docs/Archive/SETUP_REPORT.md).
