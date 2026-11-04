# Planet Harvest Spielkonzept

Stand: 4. November 2026. Dieses Konzept entwickelt die bereitgestellte Ideenskizze zu einer programmierbaren Spielschleife. Werte mit dem Zusatz **Startwert** sind Vorschläge für Prototypen und werden nach Spieltests angepasst. Die Umsetzungsschritte stehen in [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md).

## 1 Spielidee und erste Version

Planet Harvest ist ein First-Person-Mining- und Automatisierungsspiel für Windows PC. Man landet auf einem kugelförmigen Mond, bohrt sichtbare Löcher, gewinnt Material, sortiert es in passende Container und versendet es per Space Elevator. Der Erlös finanziert stärkere Werkzeuge und größere Anlagen. Später folgt ein erdähnlicher Planet mit Biomen und neuen Materialien.

Der Reiz liegt im sichtbaren Umbau der Welt und im Weg jedes Kubikmeters vom Vorkommen bis zum Verkauf. Upgrades beseitigen konkrete Engpässe: der Bohrer erreicht mehr Tiefe, der Puffer füllt sich langsamer, Container erlauben größere Lieferungen, Maschinen nehmen Handgriffe ab.

**Erste vollständige Spielschleife:** ein Mond, radiale Gravitation, First-Person-Bewegung, manueller Bohrer, Regolith und Stein, tragbarer Materialpuffer, materialreine Container, Elevator, Credits, ein Bohrer- und ein Kapazitätsupgrade sowie Speichern und Laden. Das erste Ziel ist der Versand von 1 m³ Regolith. Ein späterer Auftrag verlangt 20 m³; ein 5-m³-Container dient als mögliche Zwischenstufe. 100 m³ ist das nächste industrielle Ziel.

Eisen, Kupfer, Gold, seltene Stoffe, Scanner, Förderbänder, automatische Bohrer, Sortierer, zweiter Planet, Biome und Reisen folgen nach der ersten Schleife. Kämpfe, Gegner, Sauerstoffverwaltung, Multiplayer, Crafting, Energieverbrauch und dynamische Marktpreise gehören vorerst nicht zum Umfang.

## 2 Kernschleife und Bedienung

1. Ein Vorkommen erkunden und sich auf der Kugeloberfläche dorthin bewegen.
2. In Reichweite bohren. Das Terrain und seine Kollision ändern sich sichtbar.
3. Nur tatsächlich entfernte Materie als Volumen nach Material erhalten.
4. Bei vollem Werkzeugpuffer den Abbau stoppen und eine klare Rückmeldung anzeigen.
5. Material am Terminal in Container der passenden Material-ID umfüllen.
6. Einen gefüllten Container am Elevator versenden; Inhalt entfernen und Credits genau einmal buchen.
7. Container wiederverwenden, Upgrades kaufen und den nächsten Engpass angehen.

**Startbelegung:** WASD entlang der lokalen Tangentialebene, Maus zum Blicken, Leertaste Springen, linke Maustaste Bohren, E Interagieren, Tab Inventar/Aufträge, Esc Pause. Sie wird über das vorhandene Input System umgesetzt. Das erste HUD zeigt Crosshair, Reichweite, Puffer pro Material, Containerfüllstand, Credits und aktuellen Auftrag. Gesperrte Aktionen zeigen ihren Grund. Materialien erhalten Text und Farbe, damit sie ohne Farberkennung unterscheidbar sind.

## 3 Planeten und Bewegung

Der erste Mond ist klein genug, dass seine Krümmung sichtbar und eine Umrundung möglich ist. Ein Seed erzeugt dieselbe Grundwelt erneut. **Startwerte für den Prototyp:** etwa 80 m Radius und 6 m/s² Oberflächengravitation. Regolith liegt überwiegend außen, Stein darunter, Erz in tieferen Vorkommen. Das sind Messparameter, kein fertiges Balancing.

Die Gravitation zeigt zum Planetenzentrum. Spielerbewegung und Kamera verwenden das lokale Oben aus der Gegenrichtung. Polübergänge dürfen die Kamera nicht springen lassen; am Zentrum wird eine ungültige Gravitationsrichtung abgefangen. Ein Planet hat eine stabile ID, Seed, Generatorversion, Radius, Schwerkraftparameter und Materialverteilung.

Der zweite, erdähnliche Planet erhält eigene Biome, Schichten und Ressourcen. Reisen lädt den Zustand jedes Planeten separat; Änderungen am Mond bleiben erhalten. Atmosphäre und Ökosystemsimulation sind nicht Teil dieses Schritts.

## 4 Terrain und Bohrer

Das Ziel ist veränderbares Terrain mit Kratern und Tunneln. Ein planetlokales Dichtefeld wird in Chunks gespeichert und zu Meshes verarbeitet. Ein GameObject pro Voxel ist ausgeschlossen. Der Technikprototyp vergleicht einen einfachen Block-Mesher als Referenz mit Marching Cubes oder einer vergleichbaren glatten Methode. Danach wird eine Methode für die erste Spielschleife festgelegt. Geteilte Rand-Samples oder Halo-Zellen halten Chunk-Grenzen geschlossen.

**Startwerte für den Techniktest:** 1 m Zellkante, 16 Zellen pro Chunk und eine begrenzte aktive Chunk-Reichweite. Nicht aktive Chunks bleiben als Daten verfügbar, aber nicht dauerhaft als Unity-Objekte. Dichtefeld, Materialdaten, Mesh und Collider sind getrennt. Edits markieren betroffene Nachbarchunks; Mesh- und Collideränderungen werden gebündelt. Leistungsziele werden erst auf einer benannten Windows-Referenzhardware festgelegt.

Ein Bohrvorgang hat Position, Radius, Reichweite, Rate und optional eine Tiefenfreigabe. Der Edit liefert einen **Abbaubeleg** mit Volumen je Material aus der Änderung des diskreten Terrainmodells. Luft oder schon entferntes Material liefern null. Der Edit wird nur in dem Umfang ausgeführt, den der Puffer aufnehmen kann; sonst wird er verkleinert oder gestoppt. Dadurch darf weder ein unsichtbares Loch ohne Gutschrift noch eine Gutschrift ohne Loch entstehen. Nach dem Laden darf erneutes Bohren in dieselbe leere Stelle keine Menge erzeugen.

Materialanteile einer Zelle ergeben zusammen 1. Die Aufteilung des Abbaubelegs ist deterministisch. Für Inventar, Container und Verkauf gilt **1 m³ = 1 000 000 cm³** als ganzzahlige Basiseinheit (`long`). Restmengen aus der Umrechnung werden materialweise mitgeführt, statt bei jedem Bohrschritt verloren zu gehen. Masse wird nur bei Bedarf aus `Volumen m³ × Dichte kg/m³` berechnet; Kapazität und Verkauf orientieren sich zunächst am Volumen.

## 5 Material, Container und Elevator

Jedes Material besitzt eine stabile technische ID, Anzeigenamen, Dichte, Darstellung, Preis pro m³ und Freischaltstufe. IDs dürfen nach Veröffentlichung eines Saves nicht umbenannt werden. Geplante Reihenfolge: Regolith, Stein, Eisen, Kupfer, Gold und seltene Stoffe. Preise und Häufigkeiten bleiben konfigurierbar.

Der tragbare Puffer kann mehrere Material-IDs enthalten. Container enthalten jeweils nur eine Material-ID; ein leerer Container kann neu zugewiesen werden. Standardgrößen sind **1, 5, 20 und 100 m³**. Teilbefüllung ist erlaubt, Überfüllung nicht. Eine Übertragung ist atomar: Quelle verliert genau die Menge, die das Ziel gewinnt. Bei fehlender Kapazität bleibt der Rest an der Quelle.

Der Space Elevator bildet die Grenze zwischen physischem Rohstoff und Geld. Ein Versand hat eindeutige ID, Container-ID, Material-ID, Volumen, Preis, Auszahlung und Status. Nach bestätigtem Versand ist der Inhalt entfernt und das Guthaben genau einmal erhöht. Der leere Container bleibt erhalten. Zunächst wird die Sendung sofort abgeschlossen; eine spätere Fahranimation ändert die Buchungsregel nicht. Die UI zeigt Erlös vor Bestätigung und einen Beleg danach.

## 6 Fortschritt und Wirtschaft

Der Start gibt Bohrer, kleinen Puffer, einen 1-m³-Regolithcontainer und Zugang zum Elevator. Der erste Auftrag lautet: 1 m³ Regolith fördern und versenden. Danach sind freie Lieferungen und ein Ziel für 20 m³ Regolith sichtbar. Der 5-m³-Container ist eine günstige Zwischeninvestition. Der 20-m³-Container erlaubt den zweiten Auftrag in einer Sendung; 100 m³ folgt später. Aufträge können einmalige Boni zahlen, aber dieselbe Lieferung darf keinen Bonus zweimal auslösen.

| Upgrade | Spürbarer Effekt | Regel |
| --- | --- | --- |
| Bohrerleistung | mehr m³ pro Sekunde | ohne Terrainänderung kein Ertrag |
| Reichweite und Tiefe | weiter entfernte und tiefere Vorkommen | Freigaben werden am Edit geprüft |
| Puffer | längeres Bohren ohne Entleeren | voller Puffer stoppt den Abbau |
| Transfer | schnelleres Umfüllen | Volumen bleibt erhalten |
| Container | größere Sendungen | feste Kapazitätsgrenze |

Die erste Wirtschaftsversion verwendet feste Materialpreise und Upgrade-Kosten aus Konfigurationsdaten. Käufe prüfen Guthaben und Voraussetzungen atomar. Gekaufte Effekte bleiben nach Laden erhalten. Konkrete Zahlen entstehen aus dem vollständigen 1-m³- und 20-m³-Spieltest.

## 7 Automatisierung

Ein stationärer Bohrer gewinnt später Material aus realem Terrain, ein Förderband transportiert vorhandene Menge, ein Sortierer leitet sie nach Material-ID und Container nehmen sie auf. Jede Maschine hat Ein- und Ausgangskapazität. Ein voller Ausgang erzeugt sichtbaren Rückstau. Pause, Save und Load dürfen Material weder verlieren noch duplizieren. Maschinen laufen zunächst nur in geladenen Bereichen; Offline-Simulation ist ein eigenes späteres Feature.

Die erste Automationsdemo besteht aus einem Bohrer, einem kurzen Förderweg, einem Sortierer und zwei Containern. Sie bewegt ohne Spielerinteraktion Material und hält bei vollem Ziel an. Erst danach folgen längere Netze und Durchsatz-Upgrades.

## 8 Save, Szenen und technische Grenzen

Ein Save enthält Schema-Version, Planet-ID und Seed mit Generatorversion, Terrainänderungen, Spielerposition und Blickrichtung, Puffer, Container, versendete IDs, Credits, Upgrades und Freischaltungen. Material wird über stabile IDs gespeichert, nicht über Szeneobjekt-Referenzen. Schreiben erfolgt atomisch mit Sicherung. Unbekannte Versionen geben einen klaren Fehler oder nutzen eine getestete Migration. Eine veränderte Generatorversion darf alte Terrain-Deltas nicht still auf eine andere Grundwelt anwenden.

Reine Regeln für Volumen, Transfer, Buchung und Upgrade-Kosten sollen ohne Unity-Szene testbar sein. Die bestehende SampleScene und Template-Assets bleiben Referenz; neue Gameplay-Szenen und Prefabs liegen unter `Assets/_Game/`. Systemgrenzen und technische Regeln stehen in [ARCHITECTURE.md](../ARCHITECTURE.md).

## 9 Noch zu entscheiden

| Frage | Vorläufige Richtung | Entscheidung nach |
| --- | --- | --- |
| Glatter Terrain-Mesher | Marching Cubes oder vergleichbar | Rand-, Abbau- und Laufzeittest |
| Bohrgefühl und Reichweite | konfigurierbare Startwerte | erstem spielbaren Abbau |
| Mondradius und Gravitation | obige Startwerte | Umrundungs- und Kameratest |
| Preise, Kosten und Vorkommen | feste Datenwerte | 1-m³- und 20-m³-Durchlauf |
| Chunk- und Collider-Budget | gemessene Grenzen | Test auf benannter Hardware |
| Zweiter Planet und Reiseablauf | eigener Abschnitt | stabiler Mond und Saveversion |

Diese Entscheidungen werden im jeweiligen Implementierungsschritt mit Messung oder Spielbeobachtung festgehalten.
