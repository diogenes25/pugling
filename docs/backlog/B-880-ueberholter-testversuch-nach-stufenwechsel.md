---
tags: [typ/story, status/abgenommen, bereich/api, bereich/training, rolle/student]
aliases: [Überholter Testversuch, Versuch nach Stufenwechsel]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Review von B-849 im Nachtlauf 2026-11-23-e (docs/pm-sitzung-2026-11-23-e.md, pugling-reviewer 🟡)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-848]
wartet_auf: ""
---

# B-880 · Ein offener Testversuch von gestern wird nach einem Stufenwechsel gegen die falsche Übung gewertet

## Befund

B-848 sperrt den Stufenwechsel des Vaters nur, solange heute eine Runde oder ein Test offen ist
(`PlanPositionsController.InPlayTodayAsync`). Ein Versuch von **gestern** bleibt aber über seine Id beantwortbar
(`Next`/`Answer`/`Submit` prüfen den Tag nicht). Wechselt der Vater heute die Stufe, löst der alte Versuch auf die Übung
der neuen Stufe auf: seine eingefrorenen Fragen sind Indizes der alten Übung und werden gegen fremde Karten gewertet.
Seit B-849 kann ein so bestandener Versuch die Position zusätzlich aus der alten geprüften Stufe heben – über die
Entscheidung des Vaters hinweg. Nur über eine von Hand gebaute Anfrage erreichbar (die Sohn-App setzt nur Versuche von
heute fort), darum P2.

## Akzeptanzkriterien

1. Ein Versuch, dessen geprüfte Stufe (`TestAttempt.BuildLevel`) nicht mehr die ist, auf die sein Tag auflöst, wird in
   `Next`, `Answer` und `Submit` mit 404 abgewiesen; die Stufe der Position bleibt. Rot: einer der drei Pfade ohne Prüfung.

## Verlauf

- **2026-11-23** — Gefunden im Review von B-849 (Fund 1), im selben Sprint behoben (Freigabe 3): `IsStale` in
  `PositionTestsController`, Test `AufbauStufeAufstiegTests.Ueberholter_Versuch_Wird_Nicht_Mehr_Gewertet`
  (Mutanten M19 Answer, M20 Submit → rot). Entgangen bei B-848: dort fehlte schon die Prüfung, B-849 hat die Folge
  verschärft. Die Schwester für Übungsrunden ist [B-881](B-881-ueberholte-uebungsrunde-nach-stufenwechsel.md).
- **2026-11-23** — abgenommen (Nachtlauf E, Sprint 122), Commit `2bf20d88`. Rollengang maschinell: Sohn über die
  HTTP-Pipeline mit Kind-Login (`AufbauStufeAufstiegTests`, Berlin-Host mit eingefrorener Uhr über zwei Lerntage), Vater
  über die Supervisor-API (Bericht, Stufe setzen), Creator als Regressionszeuge (E2E `aufbau-weg-editor` grün).
  Mensch offen: Migration `BuildLevelPromotion` beim nächsten Heimnetz-Start (an einer Kopie geprobt).
