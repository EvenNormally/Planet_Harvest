# Zweiten Windows-PC einrichten

1. Git für Windows von https://git-scm.com/download/win installieren.
   Git LFS mitinstallieren bzw. von https://git-lfs.com beziehen.
2. Einladung zum Repository mit dem eigenen GitHub-Konto annehmen.
   Git Credential Manager bei der ersten Schreiboperation im Browser anmelden.
   Keine Passwörter oder Zugriffstokens in Dateien oder Chats eintragen.
3. Eigene Commit-Identität setzen:
   `git config --global user.name "Dein Name"` und
   `git config --global user.email "deine GitHub-noreply-Adresse"`.
4. Klonen:

   ```powershell
   git clone https://github.com/EvenNormally/Planet_Harvest.git
   cd Planet_Harvest
   git lfs install --local
   git lfs pull
   git status
   ```

5. Unity Hub installieren/anmelden, persönliche oder passende vorhandene Lizenz aktivieren.
   Editor **6000.3.28f1** installieren. Falls nicht angeboten:
   https://unity.com/releases/editor/archive .
   Für spätere IL2CPP-Builds zusätzlich Windows Build Support (IL2CPP) und erforderliche
   Visual-Studio-C++-Werkzeuge installieren. Aktuelles Setup verlangt keinen IL2CPP-Wechsel.
6. Hub > Projects > Add > Add project from disk: geklonten Ordner mit Assets,
   Packages und ProjectSettings auswählen. Kein neues Projekt erstellen.
7. Mit exakt dieser Editorversion öffnen, Paketimport abwarten. File > Build Profiles:
   Windows, x86_64. SampleScene prüfen; Console ohne Compilerfehler.
8. Codex installieren/anmelden und den geklonten Repositoryordner als Projekt öffnen.
   Offizielles Unity-Plugin im Pluginverzeichnis installieren. Alternativ:
   `codex plugin marketplace add Unity-Technologies/unity-agent-plugin`,
   danach `codex plugin add unity@unity-agent-plugin`.
   https://github.com/Unity-Technologies/unity-agent-plugin
9. Unity CLI aus offizieller Quelle https://unity.com installieren
   (PowerShell-Installer https://unity.com/install.ps1 vorher prüfen).
   `unity --version`, `unity self-update --check`, `codex plugin list`.
   Pipeline-Paket ist bereits im Projekt festgelegt; nicht unnötig neu installieren.
10. Optional Editor-MCP lokal registrieren: `unity mcp configure codex --local`.
    Generierte .codex/config.toml ist rechnerbezogen und ignoriert. Codex danach neu starten,
    Projekt vertrauen und mit geöffnetem Editor `unity status --format json` prüfen.
    Über CLI funktionieren eval, Szenenbefehle und Recompile auch ohne geladenes MCP.
11. UnityYAMLMerge lokal einrichten; Pfad zuerst prüfen:

    ```powershell
    $mergeTool = 'C:/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Data/Tools/UnityYAMLMerge.exe'
    if (!(Test-Path $mergeTool)) { throw 'Editorpfad anpassen' }
    git config --local merge.unityyamlmerge.name 'Unity Smart Merge'
    git config --local merge.unityyamlmerge.driver ('"' + $mergeTool + '" merge -p %O %B %A %A')
    git config --local merge.unityyamlmerge.recursive binary
    ```

12. PowerShell für die Repositoryprüfung verwenden, `powershell -File Tools/Validate-Repository.ps1`
    ausführen. Für Unity-Tests zuerst Editor regulär schließen:

    ```powershell
    unity test . --mode EditMode --output Logs/editmode-results.xml
    unity test . --mode PlayMode --output Logs/playmode-results.xml
    ```

13. Eigenen Feature-Branch erstellen, zum Beispiel
    `git switch -c feature/first-person-controller`. Codex zuerst AGENTS.md lesen lassen,
    kleine Aufgabe vergeben, Prüfungen durchführen, Branch pushen und Pull Request öffnen.

## Wiederherstellung und lokale Dateien

Library/Temp/Logs/Builds/UserSettings, Testberichte und lokale Agent-Konfiguration gehören
nicht in Git. Pakete werden aus Manifest und Lockdatei wiederhergestellt.
Keine Lizenzdateien übertragen. Kein automatischer Editor- oder Paketupgrade.
Bei Konflikten siehe CONTRIBUTING; niemals mit Force-Push oder Löschen der Historie reparieren.
