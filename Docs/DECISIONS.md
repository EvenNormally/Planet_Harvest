# Entscheidungen

Getroffene Annahmen während der autonomen Umsetzung. Jede Zeile ist eine Empfehlung, die ohne Rückfrage
umgesetzt wurde; Änderungen bitte hier vermerken.

| Datum | Bereich | Entscheidung | Begründung |
| --- | --- | --- | --- |
| 2026-11-28 | Aufträge | Orbitlieferungen werden den Aufträgen der Reihe nach zugeordnet; Überschuss bleibt für Folgeaufträge stehen. | Lieferungen zwischen Stufen gehen nie verloren, keine Doppelzählung, Save braucht nur Summen. |
| 2026-11-28 | Aufträge | Abschlussboni 10/15/20/30 Credits für Akt 1 (von Ben bestätigt, Platzhalter). | Erstes Upgrade direkt nach dem Landevertrag erreichbar. |
| 2026-11-28 | Save | Version-1-Stände: bereits erfüllte Aufträge gelten als vergütet. | Laden darf keine Credits erzeugen. |
| 2026-11-28 | Upgrades | Drei Achsen mit je drei Kaufstufen, Preise 20–120 Credits (von Ben bestätigt, Platzhalter). | Kapitel 8: erstes Upgrade nach 10–15 Minuten. |
| 2026-11-28 | Upgrades | Credits gemeinsam, Upgradestufen pro Spieler. | Designdokument Kapitel 8: „Ausrüstung kann pro Spieler bleiben“. |
| 2026-11-28 | Transport | Eine Mastkette pro Expedition: Sammeltrichter, bis zu 12 Masten, Einspeisung in den passenden Lift-Container. | Hält den Vertical Slice klein; mehrere Ketten später über eine Liste derselben Logik. |
| 2026-11-28 | Transport | Klumpen = genau 1 m³; je Abschnitt 4 Plätze, ein Schritt je 0,5 s. | Diskrete Plätze sind trivial zu synchronisieren und zu speichern. |
| 2026-11-28 | Transport | Masten sind antigrav und bleiben stehen, wenn darunter gegraben wird. | Keine Erhaltungsrisiken durch umkippende Klumpen. |
| 2026-11-28 | Transport | Freischaltung nach dem Landevertrag, Trichter 30 und Mast 40 Credits; Abbau erstattet den Preis und geht nur bei leerem letzten Abschnitt. | Kein Materialverlust beim Rückbau. |
| 2026-11-28 | Transport | Bauen mit B (Gamepad Steuerkreuz hoch), Abbauen mit N (Steuerkreuz runter), Trichter befüllen mit E. | Freie Tasten, analog zur Ladeinteraktion am Lift. |
| 2026-11-28 | Save | Version 3 enthält die Mastkette; ältere Stände laden ohne Kette. | Rückwärtskompatibel. |
| 2026-11-28 | Bohrroboter | Ein Roboter pro Expedition, 80 Credits, Freischaltung nach dem Landevertrag, Platzieren/Abbauen mit J (Steuerkreuz links). | Erste Maschine im Vertical Slice; Abbau erstattet nur bei leerem Trichter. |
| 2026-11-28 | Bohrroboter | Arbeitsbereich 8 × 8 Zellen, 2 über bis 4 unter der Oberfläche, Abbau in Tranchen von oben mit Profil Scheibe, ein Schlag je 0,8 s. | Sichtbarer, planbarer Fortschritt; langsamer als ein aufgerüsteter Spieler. |
| 2026-11-28 | Bohrroboter | Schutzzone: kein Arbeitsbereich näher als 11 m an Lift oder Werkbank, zusätzlich schützt jeder Schnitt die Fundamente. | Doppelte Sicherung gegen Zerstörung der Basis. |
| 2026-11-28 | Bohrroboter | E am Roboter übergibt zuerst den Trichterinhalt, sonst Start/Stopp; steht ein Sammeltrichter in 7 m Nähe, entlädt der Roboter automatisch dorthin. | Keine zusätzliche Taste; Roboter und Traktorstrahl ergänzen sich. |
| 2026-11-28 | Bohrroboter | Energie entfällt im Slice. | Kurzkonzept sah Batterie vor; zurückgestellt, um Umfang zu halten. |
| 2026-11-28 | QA | Profiling als PlayMode-Test, Bericht nur mit Umgebungsvariable nach `Docs/Reports/`. | Normale Testläufe verändern das Repository nicht. |
