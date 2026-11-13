---
tags: [typ/story, status/idee, bereich/gamification, rolle/student]
aliases: [Bonus-Runde ohne Item-Schnappschuss]
status: idee
prio: P3
art: Defekt
groesse: S
wo: backend
migration: ja
vertragsbruch: nein
quelle: "B-543, Entscheidung 6 (zurückgestellt), Bau im Backlog-Lauf 2026-11-13 (docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-533]
wartet_auf: ""
---

# B-545 · Eine Bonus-Runde wird gegen die aktuellen Items gewertet, nicht gegen die beim Start

`ExtraTrainingService.BuildRun` und `preview.CheckAsync` lesen die Items der Übung erst bei der Abgabe;
`ExtraTrainingRun.ItemCount` wird geschrieben, aber nirgends gelesen. Ändert der Autor die Übung zwischen Start und
Abgabe, zählen neue oder verschobene Items als falsch, und eine fortgesetzte Runde zeigt andere Karten.

Seit [B-543](B-543-bonus-deckel-verbrennt-einmal-uebung.md) kann eine Einmal-Runde über Nacht offen bleiben (die
Abgabe wartet bei vollem Deckel bis morgen) – das Fenster für diesen Fall ist dadurch länger geworden. Eine echte
Lösung braucht einen Item-Schnappschuss an der Runde (Migration, nur angehängt); ein Vergleich über `ItemCount`
erkennt eine Änderung bei gleicher Anzahl nicht.

## Verlauf

- **2026-11-13** — angelegt aus B-543 (Entscheidung 6, zurückgestellt) beim Bau im Backlog-Lauf 2026-11-13.
