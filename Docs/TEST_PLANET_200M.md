# Testplanet mit 200 Metern Radius

In Unity `Assets/_Game/Scenes/PlanetHarvest_Test200m.unity` öffnen und Play drücken.
Die Szene liegt im vorhandenen Szenenordner; es wurden keine Ordner angelegt.

- WASD: bewegen; Maus: umsehen; Leertaste: springen; Esc: Maus freigeben/einfangen.
- Auf den Boden schauen und links klicken: einen 2 × 2 × 2 m großen Block abbauen.
  Der persönliche Kanister fasst einen Block (8 m³). Volle Kanister verhindern weiteren Abbau.
- Zum cyanfarbenen Versandkanister auf der kleinen Plattform gehen, darauf schauen,
  E drücken: Inhalt umfüllen. Interaktionsreichweite: 5 m.
- Nach vier Füllungen ist der Versandkanister voll (32 m³). Nochmals E: abschicken.
  Er steigt auf und kehrt nach acht Sekunden leer zurück. Während der Reise sind
  Umfüllen und erneuter Versand gesperrt; weiter abbauen ist möglich.
- Das HUD zeigt Füllstände, Rückkehrzeit, Hinweise und abgeschlossene Lieferungen.

Konfiguration: `Assets/_Game/ScriptableObjects/TestPlanet200m.asset`.
Radius 200 m, radiale Beschleunigung 9,81 m/s², ein Material/Rohstoff Regolith.
Das Inventarvolumen wird in ganzzahligen cm³ pro abgebauter Rasterzelle geführt.
Die natürliche Oberfläche ist über gemeinsame Grenzvertices auf die Kugel gerundet,
damit die 2-m-Stufen nicht übersprungen werden müssen. Mesh und Collider stimmen überein.
Abbaulöcher bleiben echte, volumetrische Aussparungen; das Abbauraster bleibt 2 m.
Die Spielerkapsel dreht sich radial mit. Gravitation und Bodenprüfung verwenden
überall das Planetenzentrum, nicht die Welt-Y-Achse.
Eine einfache Kugel dient nur als Editor-Vorschau und wird beim Start durch
die tatsächlich abbaubaren Chunk-Meshes einschließlich Kollision ersetzt.
Die Daten speichern nur entfernte Zellen; das volle Planeteninnere bleibt implizit.
Die Plattform ist nicht abbaubar, damit der Versandplatz erhalten bleibt.

Alles ist ein lokaler Test ohne Speicherung. Stop/Play setzt Terrain und Kanister zurück.
Die ursprüngliche MVP-Szene und die Build-Szenenliste bleiben erhalten.
Kein Windows-Player-Build wurde für diese Aufgabe angefordert.

Tests: `TestPlanetRulesTests` prüft Kugeldaten, Volumen, Sperren und wiederholte Lieferungen.
`TestPlanetLoopTests` prüft die neue Szene, Abbau samt aktualisiertem MeshCollider,
vollen persönlichen Kanister und zwei echte zeitgesteuerte Versand-/Rückkehrzyklen.

## Prüfung der ursprünglichen Szene am 2026-11-06

- Unity-Editor-Kompilierung erfolgreich.
- `test run`, Modus `editor`, Assembly-Filter `PlanetHarvest`: 24/24 bestanden.
- `test run`, Modus `playmode`, Assembly `PlanetHarvest.PlayModeTests`: 3/3 bestanden.
  Zwei vollständige zeitgesteuerte Lieferungen; Abbau entfernt jeweils 2 m Oberflächenhöhe,
  volle Kanister verhindern weiteren Abbau, Rückkehr stellt leeren Behälter und Collider wieder her.
- Laufende Szene visuell geprüft: Terrain, beide Kanister und HUD sichtbar;
  Spieler bleibt auf der Oberfläche, Frames laufen weiter, Planet verwendet genau ein Material.
- `pwsh -NoProfile -File Tools/Validate-Repository.ps1`: erfolgreich,
  561 Dateien, 270 eindeutige Asset-GUIDs. Windows PowerShell 5 blockierte zunächst
  die Skriptausführung; dieselbe Prüfung lief unter PowerShell 7 erfolgreich.
- Keine Console-Fehler. Bestehende Unity-AI-Account-/Netzwerkwarnung unverändert vorhanden.
- Lokale Nachweise (nicht versioniert): `Logs/test200m-editmode.json`,
  `Logs/test200m-playmode.json`, `Logs/test200m-game.png`.
- Kein Player-Build und kein vollständiger Rundgang um den Planeten geprüft.
