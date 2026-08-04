---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/training, rolle/supervisor]
aliases: [Positions-Edit-Formular]
status: geschaetzt
prio: P3
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: memory/lehrplan-umbau.md
---

# B-16 · Prüfauftrag: deckt das Positions-Edit-Formular alle Felder ab?

Aus dem Lehrplan-Umbau blieb die Frage offen, ob das Bearbeiten-Formular einer `PlanPosition` alle Felder
abdeckt (Ziel, Punkte, Stufe, Leitner, Malus). Stand der Notiz: 2026-07-05, seither nicht nachgeprüft — der
Frontend-Ausbau hat es vermutlich längst erledigt.

**Dies ist ein Prüfauftrag, keine Änderung.** Erstes Ergebnis kann „gegenstandslos" sein — dann wird die
Story `verworfen`, und das ist der Erfolg. Nach der Recherche gegen den Code: **nicht gegenstandslos** —
vier Felder des Vertrags fehlen im Formular, darunter die namentlich genannte „Stufe". Die Story läuft
darum doch die volle Pipeline.

## User Story

Als Vater möchte ich beim Anlegen und Bearbeiten einer Plan-Position auch Stufe, Auswahl-Scope und den
Tempo-Bonus einstellen können, damit ich nicht auf den Server-Standard angewiesen bin, wenn ich eine
Übung feiner abstimmen will.

## Ist-Stand am Code

- **Vertrag** — `UpdatePositionDto` trägt 19 Felder (`backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs:136-140`):
  `Order, Stage, ItemCount, Scope, Cadence, OrderStrategy, GoalThreshold, RequireTypedTest, UseLeitner,
  MaxBox, BoxIntervalDays, StageSchedule, PointsGoalMet, PenaltyCoins, NewContentPoints, ComboThreshold,
  ComboBonusPoints, SpeedThresholdSeconds, SpeedBonusPoints`. `CreatePositionDto` (:100-104) spiegelt
  dieselben Felder plus `ExerciseId`.
- **Formular** — `PositionSettings`/`PositionFields` (`frontend/src/vater/PlanPositions.tsx:78-219`) und
  `settingsToDto` (:130-144) decken **11 von 19** Feldern ab: `cadence` (→`Cadence`), `goalThreshold`
  (→`GoalThreshold`), `itemCount` (→`ItemCount`), `orderStrategy` (→`OrderStrategy`), `pointsGoalMet`,
  `penaltyCoins`, `newContentPoints`, `comboThreshold`, `comboBonusPoints`, `useLeitner`, `requireTypedTest`.
  Dieselbe Komponente bedient Anlegen (`AddPosition`, :222-303) **und** Bearbeiten (`PositionRow`, :306-382).
- **Fehlend im Formular (8 von 19):**
  - `Order` — keine Umsortier-UI; Zeilen zeigen `pos.order + 1` nur lesend (:335).
  - `Stage` — kein Eingabefeld. Deckt sich mit dem schon offenen [B-79](B-79-position-stufe-unvalidiert.md),
    das zeigt, dass die API `Stage` roh und ungeprüft als `int` entgegennimmt.
  - `Scope` — kein Auswahl-Feld für All/New/Old (`ItemScope`,
    `backend/Pugling.Contracts/Common/PlanPositionBaseTypes.cs:15-23`).
  - `MaxBox`, `BoxIntervalDays`, `StageSchedule` — keine Leitner-Feinjustage jenseits des An/Aus-Schalters
    `useLeitner`.
  - `SpeedThresholdSeconds`, `SpeedBonusPoints` — der dritte Bonus-Kanal fehlt komplett; asymmetrisch zu
    `comboThreshold`/`comboBonusPoints`, die beide im Formular stehen (`PlanPositions.tsx:196-204`).

## Die echte Lücke

Die ursprüngliche Sorge der Notiz („Ziel, Punkte, Stufe, Leitner, Malus") ist bei vier von fünf Themen
gedeckt — **außer „Stufe"** (`Stage`), die im Formular vollständig unerreichbar ist. „Leitner" hat nur den
Schalter, nicht die Feinjustage (`MaxBox`/`BoxIntervalDays`/`StageSchedule`). Dazu kommen zwei Felder, die
die Notiz nicht kannte: `Scope` und der komplette Tempo-Bonus-Kanal. `Order` ist keine Formularfeld-Lücke
im engen Sinn — es gibt schlicht **keine** Umsortier-Funktion in der UI, unabhängig vom Bearbeiten-Formular
der übrigen Felder.

## Entscheidungen

1. **Reordering (`Order`) bleibt außerhalb dieser Story.** Es gibt aktuell keine Umsortier-UI (nicht
   einmal Pfeile) — das ist ein eigenständiges UX-Feature ohne Bezug zu den übrigen, längst vorhandenen
   Formularfeldern. Kosten: `Order` bleibt nur über die API erreichbar; eine eigene Idee, falls je gebraucht.
2. **Die Leitner-Feinjustage (`MaxBox`/`BoxIntervalDays`/`StageSchedule`) bleibt zurückgestellt.** Alle drei
   haben dokumentierte Methoden-Defaults, sind Experten-Einstellungen (Kastenzahl, Intervalle, Stufenplan
   je Box), und `BoxIntervalDays`/`StageSchedule` bräuchten eigene Listen-Editoren — deutlich mehr Aufwand
   als die übrigen Lücken, ohne erkennbaren akuten Bedarf. Kosten: bleiben Server-Default, bis eine eigene
   Story sie verlangt.
3. **Die vier übrigen fehlenden Skalarfelder werden ergänzt:** `Stage`, `Scope`, `SpeedThresholdSeconds`,
   `SpeedBonusPoints`. Alle vier sind einfache Zahlen-/Auswahlfelder nach dem Muster, das schon da ist
   (Tempo-Bonus spiegelt exakt `comboThreshold`/`comboBonusPoints`; `Scope` ist ein Drei-Werte-Select wie
   `orderStrategy`). Kosten: vier neue Felder in `PositionSettings`, `PositionFields`, `settingsToDto`,
   `defaultSettings`, `settingsFrom` — einmal für Anlegen **und** Bearbeiten, da beide dieselbe Komponente
   nutzen.
4. **`Stage` bekommt ein einfaches Zahlenfeld, keine Validierung gegen `StageOptions`.** Die Absicherung
   ist die eigenständige, schon offene Frage aus B-79 — diese Story liefert nur das fehlende Eingabefeld,
   nicht die Prüfung gegen gültige Stufen. Kosten: ein rohes `Stage`-Feld im UI trägt dasselbe Risiko wie
   die API selbst; B-79 bleibt dafür zuständig.

## Akzeptanzkriterien

1. `PositionFields` (Anlegen **und** Bearbeiten, dieselbe Komponente) zeigt vier zusätzliche Felder:
   Stufe (`stage`, Zahl), Auswahl (`scope`: Alle/Neu/Alt), Tempo-Grenze (`speedThresholdSeconds`, Sekunden)
   und Tempo-Bonus (`speedBonusPoints`, Punkte).
2. Die beiden Tempo-Felder folgen dem „leer = erbt den Bonus-Vorschlag der Übung"-Muster von
   `newContentPoints`/`comboThreshold` (String-State, `numOrNull`); `scope` folgt dem Select-Muster von
   `orderStrategy`; `stage` ist ein einfaches optionales Zahlenfeld ohne Validierung (siehe Entscheidung 4).
3. `settingsToDto` sendet alle vier neuen Felder; sowohl `addPosition` (`CreatePositionDto`) als auch
   `updatePosition` (`UpdatePositionDto`) bekommen sie, weil beide dieselbe Funktion durchlaufen.
4. Ein neuer Frontend-Komponententest für `PlanPositions`/`PositionFields` belegt, dass die vier neuen
   Felder gerendert werden und beim Speichern im DTO ankommen.
5. `Order`, `MaxBox`, `BoxIntervalDays`, `StageSchedule` bleiben bewusst außerhalb dieser Story (siehe
   Entscheidungen 1+2) — kein Akzeptanzkriterium verlangt sie.

## Schätzung

**Größe: S** — vier zusätzliche einfache Felder (zwei Zahlen mit „erbt"-Semantik, ein Select, ein rohes
Zahlenfeld; keine Listen-Editoren) nach exakt dem Muster, das für `comboThreshold`/`comboBonusPoints`/
`orderStrategy` schon existiert. Kein Backend-Schritt: der Vertrag führt alle vier Felder bereits.

`wo: frontend` · `migration: nein` · `vertragsbruch: nein`

**Risiken:** `Stage` ohne Validierung ist ein bekanntes, separates Risiko (B-79) — diese Story vergrößert
die Angriffsfläche nicht, macht sie aber sichtbarer (der Vater kann jetzt selbst einen unpassenden Wert
eintippen, wo vorher nur ein API-Client konnte). Sollte B-79 vor dieser Story greifen, entfällt das Risiko.

**Angriffsplan:** rein additiv im Frontend (`PositionSettings`, `PositionFields`, `settingsToDto`,
`defaultSettings`, `settingsFrom` in `frontend/src/vater/PlanPositions.tsx`) — kein Backend-Schritt nötig,
da `Pugling.Contracts` die Felder längst führt.

**Testweg:** neuer Vitest-Komponententest (`PlanPositions.test.tsx`, es gibt noch keinen) rendert
`PositionFields`/eine Zeile im Edit-Modus und prüft, dass die vier neuen Felder im abgeschickten DTO
ankommen. Kein `/smoke-test` und keine E2E-Erweiterung nötig — rein additive Formularfelder ohne neue
Route und ohne Änderung am Happy-Path, den `e2e/vater-von-null.spec.ts` schon abdeckt.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert: Ist-Stand gegen `UpdatePositionDto`/`CreatePositionDto`
  (`backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs:100-140`) und `PlanPositions.tsx:78-219`
  belegt — 8 von 19 Vertragsfeldern fehlen im Formular, darunter die namentlich gesuchte „Stufe" (`Stage`);
  keine gegenstandslose Frage (autonom geprüft, Nutzerauftrag 2026-08-04).
- **2026-08-03** — gegrillt: vier Entscheidungen getroffen — Reordering und Leitner-Feinjustage
  (`MaxBox`/`BoxIntervalDays`/`StageSchedule`) zurückgestellt (eigene Aufwände, kein akuter Bedarf), die
  vier übrigen Skalarfelder (`Stage`, `Scope`, `SpeedThresholdSeconds`, `SpeedBonusPoints`) werden ergänzt,
  `Stage` ohne eigene Validierung (bleibt bei B-79) (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-03** — geschätzt: Größe S, `wo: frontend`, kein Vertragsbruch, kein Backend-Schritt; Testweg
  neuer Vitest-Test für `PlanPositions` (autonom getroffen, Nutzerauftrag 2026-08-04).
