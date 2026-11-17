---
tags: [typ/story, status/idee, bereich/backend, rolle/student]
aliases: [Truhen-Prüfung doppelt, Kind-Tag auf dem Schreibweg]
status: idee
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Backend-Review von B-606 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-609 · Bis die Truhe vergeben ist, rechnet jede Abgabe den Tag zweimal

Nach jeder Übungsrunde und jedem Test ruft der Schreibweg erst `progress.EvaluateAndAwardAsync(plan, …)` (Ziel-Punkte
des gespielten Plans) und dann `dailyBox.EvaluateAndAwardAsync(childId, …)`, das über `ComputeChildDayAsync` denselben
Plan noch einmal voll rechnet – samt `ItemsOfAsync`/`DueItemIndicesAsync` je Position, die die Truhe nicht braucht
(`PositionPracticeController.cs` ~736, `PositionTestsController.cs` ~450). Das endet, sobald die Truhe des Tages
vergeben ist (Vorprüfung). Dazu lädt `LoadPositionsAsync` seit B-606 das Fach mit (`ThenInclude(s => s!.Subject)`) – auch
in Verlaufsschleifen (`ProgressAsync`, Zeitachse), die es nicht anzeigen.

Idee: eine schlanke Belohnungs-Prüfung nach Vorbild `DutyStateAsync` für die Truhe; das Fach nur in der Tagessicht laden.
Erst messen, ob es spürbar ist – bei 4–15 Positionen je Plan vermutlich nicht.

Bewusst nicht in Sprint 83: 🟢-Hinweis des Reviewers, das Sprint-Ziel gilt ohne ihn.

## Verlauf

- **2026-11-17** — angelegt aus dem Backend-Review von B-606 (🟢, ohne Zählung).
