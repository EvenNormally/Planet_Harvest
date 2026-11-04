# Technische Architektur

## Ist-Zustand

URP-Template, SampleScene und InputSystem_Actions sind vorhanden. Noch keine
Gameplay-Implementierung, keine Speicherung, kein Netzwerkspiel. Nachfolgend Zielarchitektur.

## Systemgrenzen

| Bereich | Verantwortung und Schnittstellen |
| --- | --- |
| Core | Einheiten, IDs, Lebenszyklus; keine Abhängigkeit von UI |
| Player | Input-Actions, Bewegung und Kamera; nutzt austauschbare Gravitation |
| Planets | Seed, Radius, Biome, radiales Gravitationsfeld |
| Voxel | Chunk-Koordinaten, Dichtefeld, Materialanteile, Meshing, Dirty-Queue |
| Mining | Werkzeugabfrage, Dichteänderung, tatsächliches entnommenes Volumen |
| Logistics | Container, Transport und Space-Elevator-Aufträge |
| Economy | Preise, Transaktionen, Credits; keine Mesh-Abhängigkeit |
| Progression | Freischaltungen und Upgrades über Konfigurationsdaten |
| UI/Audio/Art | Darstellung von Zuständen und Ereignissen |
| ScriptableObjects | Unveränderliche Definitionen für Material, Werkzeug und Planet |

Konkrete Interfaces erst bei Implementierung einführen, etwa IGravityProvider,
ITerrainEditor und IMaterialStorage. Runtime-Assemblies dürfen nicht von Editor/Test-Assemblies
abhängen. Fachliche asmdefs ergänzen, sobald wirklicher Code und Abhängigkeiten existieren.

## Terrain

Geplant ist ein planetlokales, in Chunks unterteiltes skalares Dichtefeld mit Materialdaten.
Marching Cubes zunächst gegen Alternativen prototypisieren. Kein GameObject pro Voxel.
Geteilte Rand-Samples oder Halo-Zellen sichern identische Chunk-Grenzen.
Mining verändert Dichte, markiert betroffene Nachbarchunks und erzeugt genau eine
Materialbuchung aus dem tatsächlich entnommenen Volumen. Wiederholtes Bohren in Luft
darf kein Material erzeugen. Materialdichte und m³/kg explizit unterscheiden.

CPU-Daten, Mesh und Collider getrennt verwalten. Meshing bei Bedarf über Jobs/Burst,
nach Messung; Unity-Objekte nur im zulässigen Thread aktualisieren.
Chunk-Größe und aktive Reichweite anhand eines dokumentierten PC-Benchmarks festlegen.
LOD-Übergänge und Kollisionsaktualisierungen sind eigene Akzeptanzpunkte.

## Bewegung und Gravitation

Richtung zum Planetenzentrum bestimmt unten, Kamera und Controller verwenden lokales oben.
Nahe dem Zentrum muss der Nullvektor behandelt werden. Zunächst ein aktiver Planet;
Wechsel mehrerer Gravitationsfelder später ausdrücklich entwerfen.

## Daten und Speicherung

Versioniertes Save-Schema: Welt-Seed + Generatorversion + Chunk-Deltas + Inventare +
Container + Wirtschaft/Upgrades. Stabile Material-IDs, keine Scene-Objekt-Referenzen.
Atomisches Schreiben mit Sicherung und Migration; Änderungen am Generator dürfen alte
Saves nicht still verändern. Determinismus, Chunk-Ränder, Masse/Volumen und Roundtrips testen.

## Szenen und Zusammenarbeit

Bestehende SampleScene bleibt als Template erhalten. Künftig getrennte Bootstrap-, Planet-
und Test-Szenen, modulare Player/Tool/Container-Prefabs. Szeneigentümer pro Aufgabe abstimmen.
Kein Mehrspieler-Netzwerkumfang beschlossen; zwei Entwickler bedeuten kein Multiplayer-Spiel.
