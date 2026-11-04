# Planet Harvest – Spielidee

Die ausführliche Fassung mit Spielregeln, erstem spielbaren Umfang und offenen Entscheidungen steht in [Docs/GAME_CONCEPT.md](Docs/GAME_CONCEPT.md). Einzelne Codex-Aufträge stehen in [Docs/IMPLEMENTATION_PLAN.md](Docs/IMPLEMENTATION_PLAN.md). Dieses Dokument bleibt die kurze Übersicht.

First-Person-Mining und Automatisierung auf kugelförmigen Planeten für Windows PC.
Der Spieler beginnt auf einem kleinen Mond mit einfachem Bohrer und kleinen Containern.
Ein erdähnlicher Planet mit eigenen Biomen und Materialien folgt später.

## Kernschleife

1. Auf dem Planeten landen und Oberfläche erkunden.
2. In First Person bewegen und echte Löcher in das Terrain bohren.
3. Abgebautes Material nach Zusammensetzung erkennen, sammeln und sortieren.
4. Rohstoffe in geeignete Container füllen.
5. Container per Space Elevator versenden und Credits erhalten.
6. Werkzeuge, Maschinen und Kapazitäten verbessern.
7. Mining und Transport schrittweise automatisieren.

## Materialien und Container

Regolith/Erde, Stein, Eisen, Kupfer, Gold und seltene Materialien.
Materialdefinitionen enthalten stabile ID, Dichte, Verkaufspreis und Darstellung.
Startkapazitäten: **1 m³, 5 m³, 20 m³ und 100 m³**, später industrielle Größen.
Füllstand nach realem Volumen begrenzen; Masse bei Bedarf aus Dichte berechnen.
Mischungen dürfen beim Sortieren kein Material verlieren oder vervielfachen.

## Fortschritt

Credits verbessern Bohrleistung, Fördergeschwindigkeit, Kapazität und erreichbare Tiefe.
Spätere Anlagen nutzen Förderbänder, automatische Bohrer und Sortierer.
Der Space Elevator bildet den kontrollierten Übergang von physischem Material zu Einnahmen.

## Offene Designentscheidungen

Bohrreichweite, Energiekosten, Preise, Schwerkraftstärke, Planetengrößen und Produktionsraten
werden erst anhand spielbarer Prototypen festgelegt. Keine Premium-Assets erforderlich.
Kein vollständiges Gameplay in Phase 0 implementieren.
