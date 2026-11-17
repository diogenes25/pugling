---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, rolle/student, rolle/supervisor]
aliases: [Taktung bis erledigt, UntilDone, Abschlussbonus]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-632
grund: ""
ersetzt_durch: []
---

# B-636 · Eine Pflicht „täglich, bis erledigt“ mit Abschlussbonus

Teil D aus [B-632](B-632-schultest-nacharbeiten-mit-leichter-schalten.md). Eine Nacharbeit ist eine tägliche
Pflicht, die endet, sobald alle Aufgaben erledigt sind. Am Ende gibt es einmalig einen Bonus.

## User Story

Als *Vater* möchte ich, dass die Nacharbeit jeden Tag gespielt werden muss, bis sie erledigt ist, und danach
verschwindet. Für den Abschluss bekommt mein Sohn einen Bonus.

## Ist-Stand am Code

Gelesen am 2026-11-17 (`b1bf80c8`), aus B-632:

- `GoalCadence` kennt `None`, `Daily` und `Weekly` (`backend/Pugling.Contracts/Common/PlanPositionBaseTypes.cs:7-11`).
  `PeriodRange` behandelt jeden anderen Wert als Tag
  (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:35-39`).
- Eine Pflicht ist erfüllt durch einen bestandenen Test oder, bei Typen ohne Prüfung, durch eine gespielte Lernrunde
  (`IsGoalMetAsync` `:144-153`, `PlayedRoundMeetsGoalAsync` `:165-187`). Ein Lückentext ist `StudyPlanTest`, also
  bisher an den Test gebunden.
- Der Malus wird lazy über `SettleClosedPeriodsAsync` abgerechnet (`:558`).
- Belohnungen werden idempotent über `PositionGoalReward` gebucht
  (`backend/Pugling.Api/Models/PlanPositionEntities.cs:160`). `PointKind` steht in
  `backend/Pugling.Contracts/Common/AdminBaseTypes.cs:38`.

## Die echte Lücke

Es gibt keine Taktung, die endet. Bei einer Nacharbeit gilt die Tagespflicht durch eine gespielte Runde als erfüllt,
nicht durch einen Test. Einen Abschlussbonus gibt es nicht.

## Entscheidungen

Aus B-632, Entscheidung 5: Jeden Tag eine Runde durch alle offenen Aufgaben. Eine ganz gespielte Runde erfüllt die
Tagespflicht und bringt die eingestellten Münzen. Ein Tag ohne Runde kostet den eingestellten Malus. Mit der letzten
frei richtigen Aufgabe endet die Pflicht: kein Malus mehr, die Position verschwindet aus der Tagesmission, und
einmalig gibt es den Abschlussbonus.

## Akzeptanzkriterien

1. `GoalCadence.UntilDone` lässt sich am Positionsformular wählen, wenn die Einstellung „Nacharbeit“ (B-635)
   gesetzt ist.
2. Eine ganz gespielte Nacharbeitsrunde erfüllt die Tagespflicht, auch wenn danach noch Aufgaben offen sind. Die
   Pflicht-Regel aller anderen Positionen bleibt unverändert.
3. Ein Tag ohne Runde kostet den eingestellten Malus. Ein Tag nach dem Ende kostet nichts, auch wenn die Abrechnung
   ihn erst später nachholt.
4. Wenn die letzte Aufgabe erledigt ist, wird der Abschlussbonus genau einmal gebucht, mit eigenem `PointKind`. Die
   Position erscheint danach nicht mehr in der Tagesmission.
5. Der Vater stellt die Höhe des Abschlussbonus an der Position ein.

## Schätzung

**M.** Eine neue Taktung zieht durch die Pflicht-Regel, die Tagesmission, die Malus-Abrechnung und die Buchung.

- **`migration: ja`:** Die Höhe des Bonus ist eine Spalte an `PlanPosition`. Die idempotente Bonus-Zeile kommt
  nach dem Vorbild von `PositionGoalReward`, je nach Bau als eigene Tabelle. Die neuen Enum-Werte liegen als
  String in der DB und ändern das Schema nicht.
- **`vertragsbruch: nein`:** Die Enum-Werte und das Bonus-Feld sind additiv. `frontend/src/vater/PlanPositions.tsx`
  muss den neuen Wert kennen.
- **Abhängigkeit:** Ob eine Aufgabe erledigt ist, kommt aus B-635. Ohne B-635 kann diese Story nie enden.
- **Risiken:**
  - Ein Malus, der nach dem Ende rückwirkend anfällt.
  - Ein doppelt gebuchter Bonus.
  - Ein Zweig in `IsGoalMetAsync`, der die Regel anderer Positionen aufweicht.
- **Testweg:** in `RemediationPositionTests` (aus B-635) und `PositionGoalOverviewTests.cs`:
  - Eine Runde erfüllt die Pflicht.
  - Ein Tag ohne Runde kostet Malus, ein Tag nach dem Ende nicht.
  - Der Bonus wird einmal gebucht, auch wenn die Abrechnung zweimal läuft.
  - Die Position verschwindet aus der Tagesmission.
  - Dazu die Migrationsprobe gegen eine Kopie der echten DB.

## Verlauf

- **2026-11-17** — aus B-632 geteilt (Teil D), auf `geschaetzt` übernommen: Ist-Stand, Entscheidung und Schätzung
  stammen aus B-632.
