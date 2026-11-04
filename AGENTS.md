# Verbindliche Projektregeln für Codex

## Kontext und Technologie

Planet Harvest: Unity 6 **6000.3.28f1**, C#, URP 17.3.0, neues Input System,
Unity Test Framework, Windows x64, GitHub. Vor Arbeit README, ARCHITECTURE,
CONTRIBUTING und Docs/AI/UnityProjectContext lesen. Tatsächlichen Projekt- und Git-Root prüfen.
Bestehende Dateien und lokalen Git-Zustand untersuchen, bevor Änderungen erfolgen.

## Code

- Klassen, Methoden und öffentliche Properties: PascalCase; lokale Variablen: camelCase.
- Aussagekräftige Namen, Namespace PlanetHarvest mit fachlichem Unterbereich.
- Private serialisierte Felder als camelCase mit [SerializeField].
- Kleine klar abgegrenzte Komponenten, geringe Kopplung, keine God Classes.
- Interfaces für sinnvoll austauschbare Systeme; keine vorsorgliche Abstraktionsflut.
- ScriptableObjects für Konfiguration; veränderliche Spielstände separat speichern.
- Reine Berechnungen möglichst unabhängig von MonoBehaviour halten.
- Keine unnötigen externen Abhängigkeiten, bezahlten Assets oder leeren Füllklassen.
- .editorconfig beachten. Bestehende funktionierende Systeme nur begründet ersetzen.

## Unity und Performance

- Ein GameObject je Voxel ist verboten. Terrain später in Chunks und Meshes aufbauen.
- Keine unnötigen Allokationen pro Frame. Mesh- und Collider-Aktualisierungen bündeln.
- Radiale Gravitation berücksichtigen; Welt-Y nicht als allgemeines Oben annehmen.
- Terrainänderungen, Materialien und Abbauvolumen konsistent halten.
- Szenen und Prefabs modular. Bestehende Szenen nicht ohne Bedarf neu speichern.
- GUIDs und Meta-Dateien erhalten. Assets über AssetDatabase/Editor verschieben.
- Neue Ordner brauchen .meta; leere Ordner werden über .gitkeep erhalten.
- Resources ist ein Unity-Spezialordner; keine großen Daten unbeabsichtigt dort ablegen.
- Force Text und Visible Meta Files erhalten. Unity-YAML bleibt Text in Git.
- Editor- und Paketversionen nur nach begründetem Auftrag ändern.
- Bevorzugt Unity CLI/Editor-APIs für Einstellungen und Assetoperationen nutzen.
- Editor-Werkzeuge gehören in Editor-Assemblies, niemals in Player-Abhängigkeiten.

## Git und Sicherheit

- Keine destruktiven Operationen: kein reset --hard, clean -fd oder Force-Push.
- Vorhandene Arbeit und Historie erhalten; keine verschachtelten Repositories.
- Keine Secrets, persönlichen Konfigurationen, Library, Temp, Logs oder Builds committen.
- Vor jedem Commit staged Diff, neue Dateien und Binärdateien prüfen.
- Feature-Branches und Pull Requests verwenden. Keine automatischen Pushes nach main.
- Einzige Ausnahme: ausdrücklich beauftragtes initiales Setup und dessen erster Push.
- Neue Binärformate/LFS-Regeln bewusst prüfen, keine pauschale LFS-Regel für YAML.
- Keine unbekannten Collaborators einladen oder Nachrichten an andere senden.

## Umsetzung und Prüfung

- Vor Features Architektur und vorhandene Implementierungen lesen.
- Noch nicht implementierte Roadmap-Systeme nicht als vorhanden darstellen.
- Nach Codeänderungen Unity kompilieren und passende EditMode-/PlayMode-Tests ausführen.
- Neue Funktionen mit sinnvollen Tests versehen, soweit sie Verhalten absichern.
- Compilerfehler und verursachte Regressionen beheben; keine Tests deaktivieren.
- Toolausgaben/Logs prüfen. Statische Prüfung ersetzt keinen Unity-Test.
- Nach Assetänderungen Meta-Paare und GUIDs prüfen; ursprüngliche Referenzen erhalten.
- powershell -File Tools/Validate-Repository.ps1 vor Übergabe ausführen.
- Abschlussbericht: tatsächliche Änderungen, exakte durchgeführte Tests, Ergebnisse,
  verbleibende Probleme und nicht geprüfte Bereiche. Keine erfundenen Testerfolge.
