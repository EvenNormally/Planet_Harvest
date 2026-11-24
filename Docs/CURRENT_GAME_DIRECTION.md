# Planet Harvest aktuelle Spielrichtung

Stand: 24. November 2026. Die Entscheidungen in diesem Dokument ersetzen widersprechende Aussagen in `GAME_DESIGN.md`, `GAME_CONCEPT.md`, `IMPLEMENTATION_PLAN.md`, `DEVELOPMENT_ROADMAP.md` und `ARCHITECTURE.md`. Der vorhandene Mond mit Space Elevator ist ein technischer Prüfstand, nicht mehr die festgelegte Startwelt.

## Spielidee

Planet Harvest ist ein First-Person-Spiel über das Erkunden und Abbauen kugelförmiger Welten. Die erste neue Spielwelt ist ein **erdähnlicher Planet** mit sichtbaren Höhenunterschieden, begehbarer Oberfläche und echten Höhlen. Der Spieler findet Rohstoffe, baut sie aus dem veränderbaren Terrain ab und verwendet sie später zum Herstellen besserer Werkzeuge und Anlagen. Der Look ist klar lesbarer 3D-Cartoonstil, der auch auf niedrigen Grafikeinstellungen funktioniert.

Der Fortschritt entsteht durch **Ressourcen und Crafting, ohne Geld, Credits, Verkaufspreise oder Kaufmenüs**. Spätere Bohrer, größere Puffer, Container und Maschinen benötigen passende Materialmengen und gegebenenfalls bereits hergestellte Komponenten. Abbau und Crafting erhalten die Mengenbilanz: nur tatsächlich entfernte feste Materie erzeugt Rohstoff; ein Rezept entnimmt seine Zutaten genau einmal und erzeugt sein Ergebnis genau einmal.

Der vorhandene Space Elevator kann später als Transport- oder Verbindungssystem zwischen Orten und Planeten dienen. Seine bisherige Credit-Auszahlung gehört nicht zum neuen Zieldesign. Die laufende Testszene und ihre lokalen Änderungen werden für den neuen Planeten nicht destruktiv zurückgesetzt; ihre technische Nutzbarkeit wird vor einer Migration geprüft.

## Erster Planet als nächster Entwicklungsabschnitt

Zunächst wird **ein** erdähnlicher Planet spielbar erstellt. Weitere Planeten sollen später über dieselbe konfigurierbare Grundarchitektur entstehen. Der Planet hat stabile ID, Seed und Generatorversion; derselbe Seed erzeugt dieselbe unveränderte Grundwelt. Radius, Höhenprofil, Materialschichten und Höhlenparameter sind konfigurierbar. Der bestehende radiale Spielercontroller und die Chunk-Technik sollen soweit möglich wiederverwendet werden. Ein GameObject pro Voxel bleibt ausgeschlossen.

Die Oberfläche zeigt Hügel und Täler statt einer glatten Kugel. Ein sicherer Spawn und mindestens ein vom Spawn aus auffindbarer, begehbarer Höhleneingang sind Teil des ersten spielbaren Ergebnisses. Höhlen sind echte Hohlräume im Terrain mit passendem Mesh und Collider; sie dürfen nicht nur als dekorative Modelle vor einer geschlossenen Wand stehen. Luft enthält kein Material. Chunk-Grenzen bleiben bei Oberfläche, Höhle und Abbau geschlossen. Für den ersten Abschnitt genügt ein stilisiertes Terrain ohne Wasser-, Wetter- oder Ökosystemsimulation.

**Genau drei abbaubare Materialien auf diesem Planeten:**

| Material | Verteilung für den ersten Prototyp | Visuelle Rolle |
| --- | --- | --- |
| Erde | häufig nahe der Oberfläche | warmer Boden mit grüner Oberflächentönung |
| Stein | häufiger unter der Erdschicht | kühler, klar unterscheidbarer Untergrund |
| Eisenerz | seltene Vorkommen in größerer Tiefe; nahe der Oberfläche ausgeschlossen | kontrastreiche Erzadern oder Cluster |

Material-IDs bleiben stabil. Die Seltenheit des Erzes wird durch Seed und Tiefe deterministisch bestimmt, nicht durch zufällige Neuberechnung beim Laden eines Chunks. „Tiefer“ bedeutet Abstand unter der **lokalen** Oberfläche, auch an Hängen. Die konkreten Tiefenschwellen, Häufigkeiten, Planetengröße und Zellgröße sind Prototypwerte, die anhand eines spielbaren Beispiels und eines Leistungstests angepasst werden. Der bestehende Regolith des Mond-Prüfstands bleibt eine eigene ID; auf dem neuen Planeten sind nur Erde, Stein und Eisenerz abbaubar.

## Spätere Entwicklung

1. Materialgewinn und Inventar für die drei Planetmaterialien stabilisieren; Mengenbilanz testen.
2. Terrainänderungen, Höhlen, Spielerzustand und Inventar versioniert speichern.
3. Erste Crafting-Station und ein verbessertes Bohrerrezept mit realen Zutaten umsetzen.
4. Größere Werkzeuge, Container und Förderanlagen durch Crafting freischalten.
5. Automatische Bohrer, Förderbänder und Sortierer mit Rückstau und Mengenerhaltung ergänzen.
6. Weitere Biome, Rohstoffe und Planeten sowie Reisen und Transport ergänzen.
7. Cartoon-Artstil, Audio, UI und Qualitätsprofile auf gemessener Windows-Hardware ausarbeiten.

Der nächste Auftrag konzentriert sich auf den **spielbaren Planeten** und die drei Materialschichten samt Höhlen. Crafting wird dafür vorbereitet, aber noch nicht implementiert. Die alte Credit-Logik wird in der neuen Spielszene nicht als Fortschrittssystem verwendet; ihre spätere Entfernung oder Migration ist eine getrennte Aufgabe.
