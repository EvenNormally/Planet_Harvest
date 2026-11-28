# Technische Architektur

## Spielbare Erd-Szene

`EarthVoxelGrid` sampelt das feste Dichtefeld. `VoxelData` führt entfernte Materialzellen und aktualisierte Dichten. Der Surface-Net-Mesher von `VoxelPlanet` berechnet gemeinsame Randvertices aus benachbarten Samples. Ein Schnitt markiert die betroffenen Chunks einschließlich ihrer Randnachbarn. Pro Frame werden bis zu vier Meshes und Collider aktualisiert. Vollständige Wiederherstellung aktualisiert die betroffene Geometrie vor dem Versetzen der Spieler.

`DrillStroke` bucht jeden abgebauten Kubikmeter in genau einen Rucksack. `EarthFreight` verbucht den Frachtwert bei Ankunft im Orbit und führt exportierte Volumen pro Material. `ExpeditionCrew` betreibt dieselben Regeln im Solo- und Hostbetrieb. Ein Gast sendet Bewegungszustand und Bedienabsicht. Der Host prüft den Zielstrahl, die Werkzeugreichweite, den Takt und die freie Kapazität. Eine geordnete TCP-Verbindung überträgt beim Beitritt einen vollständigen Snapshot und danach die neu entfernten Zellen. Beim Laden wechselt die Weltgeneration für ausstehende Netzwerkbefehle.

Der komprimierte Spielstand enthält Weltkennung mit Seed, Generatorversion und Einstellungen, Zelldeltas, zwei Crewplätze und die Liftbuchhaltung einschließlich Flugphase. Die Vollprüfung vergleicht die entfernte Menge jedes Materials mit den Rucksäcken, Behältern und ausgelieferten Mengen. Geschrieben wird zuerst in eine temporäre Datei, danach atomar mit Sicherung des Vorgängers.

## Materialien, Elevator-Aufträge und Planetenfortschritt

`MaterialDefinition` (ScriptableObject, `ScriptableObjects/Materials/`) beschreibt je Material Anzeigename,
Dichte in kg/m³, Credits pro m³, Containervolumen, Containertyp und Farbe. Schlüssel bleibt der
`MaterialId`-Enumwert; seine Zahlenwerte sind Save- und Netzformat und werden nie geändert.
`ElevatorContractSet` (`ScriptableObjects/ElevatorContracts_Akt1.asset`) listet die lieferbaren Materialien
(Reihenfolge = Container am Lift) und die geordneten Aufträge mit Mindestmengen mehrerer Materialien und
einmaligem Abschlussbonus. Der Elevator-Prefab referenziert das Asset und erzeugt daraus ein reines
`FreightProgram` ohne Unity-Objektbezüge.

`ContractLedger` führt die im Orbit angekommenen Volumen pro Material. Aufträge werden der Reihe nach aus
diesen Summen bedient: ein Auftrag verbraucht genau seine Mindestmengen, Überschuss bleibt für spätere
Aufträge stehen. Lieferungen zwischen Stufen sind damit erlaubt, zahlen Volumen-Credits und gehen weder
verloren noch werden sie doppelt gezählt. Der Bonus eines Auftrags wird genau einmal gezahlt; der Spielstand
speichert dafür die Zahl bereits vergüteter Aufträge. Volumen-Credits werden kumulativ und ganzzahlig
(Tausendstel-Credits) abgerundet, sodass Teillieferungen denselben Betrag wie volle Container ergeben.

`PlanetProgress` misst die globale Quote als Orbitvolumen relativ zum ursprünglichen festen Planetenvolumen
und weist Kabine (im Flug), Lift-Container und Rucksäcke getrennt aus. Das HUD zeigt Auftrag, Teilmengen,
Orbitquote und die getrennten Mengen.

Spielstand Version 2 speichert Liefer- und Containermengen indiziert nach `MaterialId`. Version-1-Stände
(Arrays pro alter Auftragsposition Erde, Stein, Eisen, Kupfer, Quarz) werden beim Laden migriert; bereits
erfüllte Aufträge gelten als vergütet, damit Laden keine Credits erzeugt.

## Weitere Entwicklungsrichtung

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

Die Erd-Szene ist die einzige vorhandene Spiel- und Build-Szene. Künftig getrennte Bootstrap-, Planet-
und Test-Szenen, modulare Player/Tool/Container-Prefabs. Szeneigentümer pro Aufgabe abstimmen.
Der aktuelle Netzwerkumfang sind ein Host und ein beitretender Gast.
