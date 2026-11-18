---
tags: [typ/story, status/idee, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Malus doppelt nach Umstellen, Daily und UntilDone teilen keine Periode]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "PM-Sitzung 2026-11-18, Runde 4 – Nachschau von Sprint 86 (177027d9)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-636]
---

# B-666 · Umstellen einer Nacharbeit von „Tagesziel“ auf „bis erledigt“ bucht den Malus schon abgerechneter Tage noch einmal

## Beobachtung

Gefunden in der Nachschau von [B-636](B-636-pflicht-bis-erledigt-mit-abschlussbonus.md), am Code bestätigt (`fe13531c`):
`SettleClosedPeriodsAsync` prüft „schon belohnt/schon bestraft“ nur gegen `r.Cadence == cadence` mit dem **heutigen**
Takt (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:776`, `:778`), und der eindeutige Index liegt auf
`(PlanPositionId, Cadence, PeriodStart)` (`backend/Pugling.Api/Data/PuglingDbContext.cs:723`). Eine `Daily`-Zeile schützt
denselben Tag also nicht vor einer zweiten `UntilDone`-Zeile.

**Szenario:** Nacharbeit mit Tagesziel und Malus 5. Montag verpasst ⇒ `PositionGoalPenalty(Daily, Mo)`, −5. Dienstag stellt
der Vater auf „bis erledigt“ (der in B-636 vorgesehene Weg; in der echten DB liegen 11 Nacharbeits-Positionen). Die nächste
Abrechnung findet für Montag keine `UntilDone`-Zeile, prüft mit der neuen Regel (ganze Runde) „nicht erfüllt“ ⇒ Montag
kostet **noch einmal** −5. Betroffen ist jeder Tag im 14-Tage-Fenster vor dem Umstellen, auch belohnte Tage (Schwelle
erreicht, aber keine ganze Runde). Kleiner Doppelgänger bei der Belohnung (`:537`): Tageszielbelohnung mit `Daily` und am
selben Tag eine zweite mit `UntilDone`.

**Dringlich vor dem nächsten Umstellen einer echten Nacharbeit** – das Kind verliert Münzen ohne eigenes Zutun.

## Naheliegend (nicht entschieden)

Tagespflichten (`IsDailyDuty`) teilen eine Periode: beim Nachsehen `Daily` und `UntilDone` gleich behandeln oder
`UntilDone` unter `Daily` buchen. Test: Malus mit `Daily` buchen, auf `UntilDone` patchen, abrechnen ⇒ keine zweite Zeile.
Das ältere Muster Daily ↔ Weekly bleibt davon zu trennen.
