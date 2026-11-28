# Phase 4 · Kurzkonzept: erster Förderer und erste Maschine

Stand 28. November 2026. Nur Planung, nichts davon ist umgesetzt. Grundlage: Designdokument 1.0
(Kapitel 7 „Fördereinheiten, alles außer Förderbänder“, Maschinenregeln) und Bens Vorgabe vom selben Tag:
keine Förderbänder, Transport soll zur chaotischen Alien-Crew mit Antigrav-Technik passen und sichtbar bleiben.

## Gemeinsame Regeln für jeden Transport

- **Materialklumpen statt Fluss.** Ein Klumpen ist genau 1 m³ eines Materials (`MaterialId`, 1 000 000 cm³) und
  existiert immer an genau einem Ort: Rucksack, Sammelpunkt, unterwegs auf Strecke X bei Fortschritt t, oder
  Container. Neue Inventarart `TransitLane` (reine Logik) mit fester Kapazität in Klumpen.
- **Übergabe nur per `TransferTo`.** Material wechselt nur zwischen bestehenden `HarvestInventory`-Instanzen.
  Erzeugt wird nur beim Abbau (`DrillStroke`), vernichtet nur bei Ankunft im Orbit. Die Vollprüfung im Spielstand
  zählt Strecken als weiteren Bestand: entfernt = Rucksäcke + Sammelpunkte + Strecken + Container + Orbit.
- **Rückstau.** Ein Klumpen rückt nur vor, wenn das nächste Glied frei ist. Ist der Container voll oder im Flug,
  bleibt die Kette stehen, sichtbar (Klumpen schweben und zittern, Statusleuchte gelb). Nichts fällt heraus.
- **Host simuliert, Gast sieht.** Fortschritt läuft in festen Ticks (10 Hz) auf dem Host. Das Update-Paket enthält
  pro Strecke nur die Belegung (Material je Slot, Kopf-Fortschritt). Der Gast interpoliert die Darstellung.
  Bedienung (bauen, abreißen, Start/Stopp) ist wie beim Bohren eine Absicht, die der Host prüft.
- **Gespeichert** werden Bauteile mit Position und Ausrichtung im Planetenraum, Belegung und Fortschritt.
  Spielstand Version 3 mit Migration (fehlende Logistik heißt leer).
- **Bauplatz**: nicht in geschützten Fundamenten (`EarthMining.IsFoundation`), auf festem Boden, Richtung zum
  Planetenzentrum als unten.

## Transportkandidaten

| Kandidat | Sichtbarkeit | Aufwand | Koop-Synchronisation | Bewertung |
| --- | --- | --- | --- | --- |
| 1 Traktorstrahl-Masten | Hoch: Klumpen schweben im Strahl von Mast zu Mast | Mittel: Mast-Prefab, Strahl-Shader, Slot-Kette | Einfach: diskrete Slots je Strecke, 1–2 Bytes je Slot | **Empfehlung Vertical Slice** |
| 2 Saugrüssel-Röhren | Mittel: Klumpen in transparenten Röhren, Beulen beim Durchsaugen | Mittel bis hoch: Röhrenverlauf auf unebenem Boden, Biegungen | Einfach, wie Masten | Zweiter Schritt, verlängert Akt 3 (Wasser als Flüssigkeitsmodus) |
| 3 Schlepp-Drohnen oder Lasttiere | Hoch und lustig | Hoch: Wegfindung um Krater, Kollision, Animation | Mittel: Position je Drohne, Ziel, Last; Fehlerbilder bei Löchern | Später, gut als T2/T4-Belohnung |
| 4 Gravitationsschleuder | Sehr hoch, Slapstick | Mittel: ballistische Bahn bei radialer Schwerkraft, Fangnetz | Mittel: Wurf deterministisch aus Startzeit und Ballen | Spaßiges Zusatzgerät, Ballen = 8 Klumpen gepresst |
| 5 Teleport-Pads | Gering, Transport unsichtbar | Niedrig | Trivial | Nur Spätspiel, widerspricht „was wir transportieren, sieht man“ |

### Empfehlung: Traktorstrahl-Masten

- **Sammeltrichter** (Startpunkt): Spieler oder Roboter laden per E wie am Lift; Puffer 6 Klumpen.
- **Mast**: steht auf dem Boden, Reichweite 8 m zum nächsten Mast, Sichtlinie per Raycast, Steigung beliebig, weil
  der Strahl antigrav ist. Jede Strecke zwischen zwei Masten hat 4 Slots. Bei 1 Slot pro Sekunde ergibt das
  etwa 1 m³/s, deutlich mehr als ein Spieler mit Rucksack.
- **Containeranschluss**: Der letzte Mast zielt auf einen Lift-Container und speist über `EarthFreight.Load` ein.
  Ein voller Container startet den Lift wie bisher.
- Darstellung: Klumpen als kleine facettierte Würfel in der Materialfarbe aus der `MaterialDefinition`,
  leichtes Taumeln, violetter Strahl (Crew-Akzentfarbe).
- Kosten (Platzhalter): Mast 40 Credits, Trichter 30 Credits. Freigabe nach Auftrag „Tragwerk“.

## Erste Maschine: Bohrroboter

- **Gleiche Terrain-API wie Spieler:** `DrillStroke.Cut(planet, robotHopper, contact, axis, profile, scale,
  protectedCell, changed)`. Der Roboter bekommt einen eigenen `HarvestInventory`-Trichter (z. B. 12 m³) und schneidet
  nur Zellen, die tatsächlich fest sind. Keine Maschine erzeugt Material.
- **Arbeitsbereich:** Der Spieler setzt einen Quader (Breite, Tiefe, Höhe in Zellen) mit Vorschau. Der Roboter
  arbeitet ihn schichtweise von oben ab (Profil Scheibe), läuft auf der lokalen Oberfläche mit radialer Schwerkraft.
  `protectedCell` umfasst Fundamente und alles außerhalb des Bereichs. So zerstört er weder Lift noch Werkbank.
- **Bedienung:** Start, Stopp und Statusleuchte (grün arbeitet, gelb Trichter voll oder Rückstau, rot blockiert oder
  kein Material). Ein voller Trichter wird an einem benachbarten Sammeltrichter oder Mast entladen, sonst wartet der Roboter.
- **Takt:** ein Schlag je 0,8 s mit Schnittfaktor 1,2, also langsamer als ein aufgerüsteter Spieler, aber dauerhaft.
  Der Takt läuft über denselben Tick wie die Logistik, damit Host und Gast identisch zählen.
- **Energie:** Laut Design brauchen Maschinen Energie. Für den Slice reicht eine Batterie mit Laufzeit, die an der
  Werkbank getauscht wird. Echte Energienetze kommen später.
- **Netz:** Schnitte des Roboters laufen auf dem Host durch denselben `CellMined`-Pfad wie Spielerschnitte und
  erreichen den Gast damit ohne neues Protokoll. Neu sind nur Roboterposition, Status und Trichterfüllung.
- **Performance:** QA 001 zeigt Meshing als Engpass (rund 3,7 ms je Chunk). Vor mehreren Robotern das
  Rebuild-Budget auf Zeit umstellen und QA 001 um einen dritten, maschinellen Bohrer erweitern.

## Tickets (Vorschlag)

1. LOG 001: `TransitLane` und Klumpen-Übergabe als reine Logik, EditMode-Tests für Erhaltung und Rückstau.
2. LOG 002: Sammeltrichter, Mast und Containeranschluss als Prefabs, Platzierung, Host-Absichten, Save v3.
3. MACH 001: Bohrroboter-Logik (Arbeitsbereich, Schichtfolge, Trichter, Status) als reine Logik plus Tests.
4. MACH 002: Roboter-Prefab, Vorschau, Start/Stopp, Koop-Sync, PlayMode-Test „Roboter leert Bereich, Masten
   liefern in Container, Bilanz stimmt“.
5. QA 002: Profiling mit zwei Spielern, einem Roboter und einer Mastkette.

## Offene Designfragen

- Wie viele Masten pro Strecke maximal (Kostenbremse oder Energie)?
- Dürfen Masten auf abgebautem Boden „nachrutschen“, oder kippen sie, wenn unter ihnen gegraben wird
  (Slapstick-Potenzial, aber Erhaltung der Klumpen muss gesichert bleiben)?
- Teilt sich ein Roboter die Credits-Freigabe mit der Crew oder ist er pro Spieler?
