# Planet Harvest Implementierungsplan für Codex

Stand: 4. November 2026. Dieser Plan zerlegt das [Spielkonzept](GAME_CONCEPT.md) in kleine, überprüfbare Aufgaben. Das Repository enthält derzeit ein Unity-URP-Template ohne Gameplay. Phase 0 ist eingerichtet; mit Aufgabe 01 beginnt die Spielentwicklung. Die Nummern sind eine sinnvolle Reihenfolge, keine Zusage über Dauer oder fertige Architektur.

## Arbeitsweise pro Aufgabe

Codex erhält jeweils **eine** nummerierte Aufgabe. Vorher `AGENTS.md`, `README.md`, `ARCHITECTURE.md`, `CONTRIBUTING.md`, `Docs/AI/UnityProjectContext.md`, dieses Dokument und betroffene Implementierungen lesen. Den tatsächlichen Git-Root und Status prüfen. Auf einem passenden Feature-Branch arbeiten; vorhandene Arbeit erhalten. Nur die für die Aufgabe nötigen Szenen, Prefabs, Daten und Scripts ändern. Nach Code- oder Assetänderungen im Editor kompilieren, passende EditMode-/PlayMode-Tests ausführen, `powershell -File Tools/Validate-Repository.ps1` prüfen und die tatsächlichen Ergebnisse berichten. Ein Prüfstand gilt erst als fertig, wenn man die Funktion im Play Mode nachvollziehen kann. Tests und Messwerte nicht behaupten, wenn sie nicht ausgeführt wurden.

Jede Aufgabe endet mit: (1) spielbarem oder inspizierbarem Ergebnis, (2) passender Prüfung, (3) kurzem Hinweis zu offenen Punkten, (4) Aktualisierung dieses Plans, falls sich eine Designentscheidung geändert hat. Der zweite Entwickler reviewt den PR. Parallele Arbeit ist möglich, wenn Szenen- und Prefab-Eigentümer vorher abgesprochen werden; reine Berechnungen und UI können oft getrennt entstehen.

**Wiederverwendbare Codex-Anweisung:**

> Setze Aufgabe NN aus `Docs/IMPLEMENTATION_PLAN.md` um. Lies zuerst `Docs/GAME_CONCEPT.md` sowie die Projektregeln und den betroffenen Bestand. Halte den Umfang auf diese Aufgabe begrenzt. Implementiere die dortigen Akzeptanzkriterien, prüfe sie in Unity und mit passenden Tests, führe die Repositoryprüfung aus und berichte die tatsächlich durchgeführten Prüfungen und verbleibenden Risiken. Erstelle keinen Commit oder Push ohne meinen Auftrag.

Für die erste Aufgabe kann `NN` direkt durch `01` ersetzt werden. Die Umsetzung darf kleine technische Vorarbeiten enthalten, wenn sie für die jeweilige Abnahme nötig sind; größere neue Systeme werden als eigene Aufgabe geplant.

## Abschnitt A Spieler auf einem Mond

### 01 Spielszene und Mondkonfiguration

**Auftrag:** Unter `Assets/_Game/` eine neue, getrennte Gameplay-Szene und minimale Planetenkonfiguration anlegen. Die SampleScene nicht verändern. Ein einfacher sichtbarer Kugel-Prototyp genügt; Seed, Radius und Zentrum sind im Inspector nachvollziehbar. Keine Voxelimplementierung.

**Fertig wenn:** Szene lädt ohne Fehler, Mond und Spawnpunkt sind sichtbar, Konfiguration ist austauschbar, Testszene ist separat. Prüfen: Editor-Kompilierung, PlayMode-Smoke-Test und Repositoryprüfung.

### 02 Radiale Gravitation als getrennte Berechnung

**Auftrag:** Gravitationsrichtung und lokales Oben für genau einen Planeten berechnen. Zentrum und Stärke kommen aus Konfiguration. Nullvektor am Mittelpunkt behandeln. Eine kleine, fachlich sinnvolle Schnittstelle nur dann anlegen, wenn Controller und Planet sie tatsächlich gemeinsam benötigen.

**Fertig wenn:** Tests an mehreren Positionen einschließlich Pol und Mittelpunkt liefern endliche Werte; am Mittelpunkt gilt eine dokumentierte Fallback-Regel. Noch keine Spielerbewegung nötig.

### 03 First-Person-Controller und Kamera

**Auftrag:** Neues Input System anbinden, Bewegung auf lokaler Tangentialebene, Blick, Sprung, Bodenkontakt und Pause implementieren. Die Kamera richtet sich weich auf lokales Oben aus; Mausbewegung bleibt bei Polübergängen stabil. Der Controller nutzt die Berechnung aus 02.

**Fertig wenn:** Spieler umrundet den Mond, steht an verschiedenen Positionen aufrecht, springt und landet, fällt nicht durch die Oberfläche und erlebt keine Kamerasprünge. PlayMode-Test für kritische Bewegungszustände, manueller Umrundungstest.

**Meilenstein A:** Ein vollständiger Rundgang auf dem Mond funktioniert ohne Mining.

## Abschnitt B Zerstörbares Terrain

### 04 Terrain-Datenmodell und Chunk-Koordinaten

**Auftrag:** Planetlokale Koordinaten, Zell-/Chunk-Indizes, Seed und Generatorversion festlegen. Ein deterministisches Dichtefeld für Mondoberfläche und einfache Schichten erzeugen. Noch kein Unity-Mesh. Zellkante und Chunkgröße als Konfiguration; 1 m und 16 Zellen sind Startwerte.

**Fertig wenn:** derselbe Seed dieselben Samples liefert, Nachbarchunks dieselben Grenzwerte lesen und negative Koordinaten korrekt zugeordnet werden. EditMode-Tests decken Randfälle ab.

### 05 Mesher-Entscheidung am kleinen Prüfstand

**Auftrag:** Ein begrenztes Testvolumen mit einfachem Block-Mesher und einem glatten Ansatz (Marching Cubes oder ähnlich) darstellen. Aufwand, Nahtverhalten, Löcher und Laufzeit unter gleichen Daten vergleichen. Ergebnis und gewählte Methode in `ARCHITECTURE.md` dokumentieren; nicht beide Methoden dauerhaft pflegen.

**Fertig wenn:** sichtbare Oberfläche und absichtlich geöffnete Höhle vorliegen, Nachbarchunks ohne Spalt anschließen und die Entscheidung anhand konkreter Beobachtungen nachvollziehbar ist. Der Prüfstand ist von der Spielszene getrennt.

### 06 Chunk-Laden, Mesh und Kollision

**Auftrag:** Gewählten Mesher in einen begrenzten Bereich um den Spieler integrieren. CPU-Terraindaten, Mesh und Collider getrennt verwalten. Nicht benötigte Unity-Objekte freigeben oder wiederverwenden. Der Spieler steht auf dem erzeugten Terrain, nicht auf einer zweiten unsichtbaren Kugel.

**Fertig wenn:** Bewegen über Chunkgrenzen ohne Lücken und Durchfallen funktioniert; der aktive Bereich bleibt begrenzt. Manuelle Spielprüfung und ein geeigneter PlayMode-Smoke-Test.

### 07 Materialdefinition und Schichtenzuordnung

**Auftrag:** Stabile Material-IDs und Konfigurationsdaten für Regolith und Stein anlegen. Jede feste Zelle hat eine deterministische Materialzuordnung oder Anteile mit Summe 1. Tiefe wird relativ zur lokalen Mondoberfläche bestimmt. Darstellung und Materialdaten dürfen nicht auseinanderlaufen.

**Fertig wenn:** derselbe Seed dieselben Materialien liefert, Luft kein Material hat und die Oberfläche Regolith sowie darunter Stein zeigt. EditMode-Tests für IDs, Tiefe und Anteile.

### 08 Terrain-Edit und Abbaubeleg

**Auftrag:** Eine reine Edit-Operation für einen begrenzten Bohrbereich erstellen. Sie berechnet vorher/nachher entferntes Volumen je Material und gibt einen Beleg zurück. Wiederholtes Editieren von Luft gibt null. Betroffene Nachbarchunks werden markiert. Kein Werkzeug oder UI in dieser Aufgabe.

**Fertig wenn:** eine Bohrung über eine Chunkgrenze ein zusammenhängendes Loch ergibt; Summe der Materialanteile entspricht dem entfernten Volumen des diskreten Modells; kein Doppelertrag bei Wiederholung. EditMode-Tests für Luft, Grenzfälle und Materialmix.

### 09 Manueller Bohrer mit Kapazitätsgrenze

**Auftrag:** Eingabe, Reichweitenprüfung, sichtbares Feedback und Abbaurate an 08 anbinden. Ein vorläufiger Puffer nimmt die Belege auf. Der Edit muss vor Ausführung auf freien Pufferplatz begrenzt werden. Collider und Mesh werden nach dem Edit gebündelt aktualisiert.

**Fertig wenn:** Spieler bohrt ein Loch, betritt es ohne Colliderfehler und erhält nur das entfernte Volumen; bei vollem Puffer stoppt der Bohrer ohne weiteres Loch. PlayMode- und manuelle Prüfung einschließlich Chunkgrenze.

### 10 Terrain-Speicherung als isolierter Roundtrip

**Auftrag:** Schema-Version, Planet-ID, Seed, Generatorversion und Terrain-Deltas speichern und laden. Atomisches Schreiben mit Sicherung. Noch nicht die gesamte Spielökonomie speichern. Deltas an denselben Generator binden.

**Fertig wenn:** Loch nach Neustart identisch bleibt, unveränderte Chunks aus Seed regeneriert werden und unpassende Generatorversion klar abgelehnt wird. EditMode-Roundtrip und manuelles Neustart-Szenario.

**Meilenstein B:** Ein bleibendes Loch im Mond kann gebohrt werden. Materialmenge und Terrainänderung stimmen im diskreten Modell überein.

## Abschnitt C Vom Material zu Credits

### 11 Einheitlicher Materialbestand

**Auftrag:** Tragbaren Mehrmaterialpuffer und ganzzahlige Volumeneinheit (cm³, `long`) als Unity-unabhängige Regeln implementieren. Grenzen, Restmengen und Material-IDs prüfen. Den vorläufigen Puffer aus 09 ablösen, ohne Abbaubelege zu ändern.

**Fertig wenn:** Addieren, Entnehmen, voller Puffer, Teilmengen und wiederholte Kleinstmengen ohne unbemerkten Verlust funktionieren. EditMode-Tests beweisen Mengenbilanz.

### 12 Materialreine Container

**Auftrag:** Wiederverwendbare Container mit festen Größen 1, 5, 20 und 100 m³ als Datenmodell und einfache Prefabs. Container können leer einer Material-ID zugewiesen werden. Teilfüllung und Restkapazität sind sichtbar.

**Fertig wenn:** falsche Material-ID und Überfüllung abgewiesen werden; Grenze exakt eingehalten wird. Tests für leere, teilweise und volle Container.

### 13 Terminal und Transfer

**Auftrag:** Am Terminal Puffer und Container anzeigen und einen gewünschten Materialbetrag übertragen. Die Fachoperation ist atomar: Quelle minus X, Ziel plus X. Bei zu wenig Platz nur zulässige Teilmenge übertragen oder den Vorgang klar ablehnen; UI und Test müssen dieselbe Regel verwenden.

**Fertig wenn:** gemischter Puffer in passende Container getrennt wird, die Summe vor und nach jedem Transfer gleich ist und blockierte Transfers nichts verändern. PlayMode-Test und manueller Bedienungstest.

### 14 Elevator-Sendung

**Auftrag:** Versandmodell mit eindeutiger ID und Status, Interaktionspunkt am Elevator und Vorschau des Erlöses implementieren. Eine erfolgreiche Sendung leert den Container und erstellt genau einen Verkaufsbeleg. Die Buchung muss wiederholte Interaktion und Ladeversuche aushalten.

**Fertig wenn:** leerer Container nicht versendet wird, gefüllter Container nur einmal entnommen wird und die Verkaufssumme aus Material, Volumen und Preis nachvollziehbar ist. Tests für Doppelversand.

### 15 Guthaben und erster Auftrag

**Auftrag:** Credits als Transaktionskonto an 14 anbinden. Startauftrag 1 m³ Regolith, danach Ziel 20 m³ Regolith. Lieferungen sind frei möglich; Auftragsbonus wird höchstens einmal gezahlt. Startwerte für Preise und Boni in Daten halten.

**Fertig wenn:** ein neuer Spielstand 1 m³ abbauen, sortieren, versenden und eine korrekte Gutschrift sehen kann; erneutes Senden oder Laden verdoppelt sie nicht. Manuell die komplette Schleife durchspielen und Transaktionstests ausführen.

### 16 Container- und Werkzeug-Upgrades

**Auftrag:** Kauf und Freischaltung für 5-/20-m³-Container, Bohrerleistung und Puffergröße. Kosten und Effekte aus Daten lesen. Käufe prüfen Guthaben und Voraussetzungen atomar; Upgradeeffekte wirken sofort.

**Fertig wenn:** unerschwingliche Käufe nichts verändern, erfolgreiche Käufe genau einmal abbuchen und messbar helfen. Der 20-m³-Auftrag ist mit normalem Spiel erreichbar. EditMode- und PlayMode-Prüfung.

### 17 Vollständiger Spielstand

**Auftrag:** Den Save aus 10 um Spielerzustand, Puffer, Container, Credits, Aufträge, Versand-IDs und Upgrades erweitern. Temporäre Unity-Objekte nicht direkt serialisieren. Saveversion und Migrationsregel dokumentieren.

**Fertig wenn:** Speichern mitten im Mining-/Logistikablauf und Laden denselben Zustand wiederherstellt; kein Material oder Credit neu entsteht. Roundtrip-Tests und Neustart im Editor.

### 18 Bedienbarer vertikaler Schnitt

**Auftrag:** HUD, Interaktionshinweise, Pausenmenü, Fehlertexte und einfache Platzhalter-Sounds/-Effekte soweit für Verständnis nötig verbinden. Start, erster Auftrag, Upgrade, 20-m³-Lieferung und Save/Load bilden einen durchgehenden Ablauf. Balancing-Startwerte anhand eines dokumentierten Spieltests anpassen.

**Fertig wenn:** eine Person ohne Debug-Konsole den gesamten Ablauf spielen kann und erkennt, warum Bohren, Transfer oder Versand blockiert ist. Alle kritischen Tests grün, Windows-Build startet; keine Behauptung über Performance ohne Messung.

**Meilenstein C:** Vollständige manuelle Mondschleife von Bohrung bis Verkauf und Upgrade.

## Abschnitt D Automatisierung und zweiter Planet

### 19 Stationärer Bohrer

**Auftrag:** Eine Maschine nutzt dieselbe Terrain-Edit- und Beleglogik wie das Handwerkzeug. Sie besitzt begrenzten Ausgang und stoppt bei vollem Puffer. Keine Rohstoffgenerierung ohne Terrainänderung.

**Fertig wenn:** Maschine ohne Spielerbohren Material gewinnt und bei leerem Terrain oder vollem Ausgang anhält. Mengenbilanz testen.

### 20 Förderweg und Rückstau

**Auftrag:** Einen kurzen Förderweg mit Ein-/Ausgangsvolumen und begrenztem Durchsatz bauen. Transfers verwenden dieselben Bestandsregeln wie 13. Pause und Save/Load erhalten den Inhalt.

**Fertig wenn:** Material von 19 zum Ziel gelangt, voller Ausgang Rückstau erzeugt und wiederholtes Laden nichts dupliziert. Tests für Tick-Reihenfolge und Kapazität.

### 21 Sortierer und erste automatische Anlage

**Auftrag:** Nach Material-ID auf zwei Container verteilen. Eine Demoanlage aus Bohrer, Förderweg, Sortierer und zwei Containern in separater Szene aufbauen.

**Fertig wenn:** zwei Materialien korrekt getrennt werden, volle Ziele den jeweiligen Fluss stoppen und die Anlage ohne Spielerinteraktion arbeitet. PlayMode-Langlauf und Save/Load-Vergleich.

### 22 Zweiter Planet als reproduzierbare Welt

**Auftrag:** Erdähnlichen Planet mit eigenem Seed, Generatorversion, Biomen und Materialverteilung hinzufügen. Zunächst separat laden und testen, bevor Reise-UI entsteht.

**Fertig wenn:** gleiche Seeds gleiche Biome und Vorkommen erzeugen, Gravitation auf beiden Planeten stimmt und Mond-Daten nicht überschrieben werden.

### 23 Reise und getrennte Weltzustände

**Auftrag:** Wechsel zwischen Mond und zweitem Planeten, inklusive Spawn, Inventar und Planet-Deltas, integrieren. Reise braucht eine explizite UI-Aktion und Saveübergang.

**Fertig wenn:** auf beiden Planeten gebohrte Löcher nach mehrfachem Wechsel und Neustart bestehen bleiben; Container, Guthaben und Upgrades konsistent sind.

### 24 Optimierung und Windows-Abnahme

**Auftrag:** Auf einer benannten Referenzhardware typische Szenarien (Umrundung, Abbau an Chunkgrenze, Maschinenlauf, Planetenwechsel) messen. Auffällige Kosten bei Chunk-Meshing, Collider, Speicher und Garbage Collection gezielt beheben. UI, Audio und VFX ausarbeiten, ohne Regeln zu ändern.

**Fertig wenn:** dokumentierte Framezeit-/RAM-Budgets für den festgelegten Benchmark eingehalten werden, Windows-Build startet und ein längerer Spieltest ohne Mengenfehler oder Saveverlust durchläuft. Budgets erst vor diesem Schritt mit Hardwareangabe festlegen.

## Übergreifende Abnahmeregeln

| Invariante | Nachweis |
| --- | --- |
| Terrain erzeugt nur bei tatsächlicher Entfernung Material | Luft-/Wiederholungs- und Chunkgrenztest |
| Material geht beim Transfer nicht verloren und wird nicht kopiert | Summenvergleich vor/nach Transfer, Save/Load und Maschinen-Tick |
| Kein Behälter überschreitet seine m³-Grenze | Grenzwerttests für alle vier Größen |
| Ein Versand zahlt genau einmal | wiederholte Interaktion, Neustart, identische Versand-ID |
| Weltseed und Generatorversion sind Teil des Saves | reproduzierbarer Roundtrip, Versionsfehler |
| Radiales Oben funktioniert rund um die Kugel | Testpositionen an Polen und Äquator, manueller Rundgang |
| Unity-Assets bleiben referenzierbar | Meta-/GUID-Prüfung und PlayMode-Smoke-Test |

Die bestehende grobe Phasenübersicht bleibt in [DEVELOPMENT_ROADMAP.md](../DEVELOPMENT_ROADMAP.md). Dieser Plan ist die operative Reihenfolge für einzelne Codex-Aufträge.
