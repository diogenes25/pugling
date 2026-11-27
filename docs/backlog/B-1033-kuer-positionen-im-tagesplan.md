---
tags: [typ/story, status/verworfen, bereich/lehrplan, bereich/training, rolle/supervisor]
aliases: [Kür im Tagesplan, freie Position als Planer-Aufgabe]
status: verworfen
prio: P2
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-26.md#sprint-195--ziel--umfang
grund: "Beantwortet am 2026-11-27: der Planer wählt nur Positionen mit Pflicht, Kür bleibt Kür (Option 1). Gebaut als Teil von B-736."
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
---

# B-1033 · Darf der Tagesplaner eine Kür-Position zur Aufgabe machen?

Gefunden im Review von B-734 (Sprint 195, 2026-11-26) und bewusst nicht dort entschieden. Das Verhalten ist für den Vater
sichtbar, darum ist es eine Frage und keine Reparatur.

## Ist-Stand (gebaut mit B-734)

Der Planer nimmt als Kandidaten jede Position der aktiven, laufenden Pläne eines Kindes, mit demselben Filter wie die
Tagesansicht des Kindes (`DailyPlanner.CandidatesAsync`, Filter wie `PositionProgressService.ComputeDayAsync`). Dazu gehören
auch **freie** Positionen (Taktung „keine“, also Kür, oder nach `DutyUntil`). Beispiel: Friedrichs Grammatik-Positionen sind
bewusst nur Kür (siehe Plan 8 und 15). Eine solche Position kann im Tagesplan als Aufgabe erscheinen: als Füllung bis zum
Boden, mit neuen Wörtern oder als Gescheitertes vom letzten Lerntag.

Im Schattenmodus wirkt das nicht. Sobald B-736 die Pflicht aus der Tagesaufgabe macht, würde aus einer Kür-Position eine
Pflicht mit Malus.

## Die Frage an den Vater

Soll der Planer
1. nur aus Positionen mit Pflicht wählen (Kür bleibt Kür),
2. Kür-Positionen weiter nehmen, sie aber als Bonus statt als Pflicht führen (Anschluss an B-738), oder
3. alle Positionen gleich behandeln, weil der Planer die Taktung der Position ablösen soll?

Bis zur Entscheidung zeigt der Schattenplan Kür-Positionen mit. Die Schattenwoche zeigt, wie oft das vorkommt.

## Antwort

**2026-11-27 (Nutzer, PM-Sitzung):** Option 1 – der Planer wählt nur aus Positionen mit Pflicht; eine Kür-Position (Taktung „keine“ oder nach
`DutyUntil`) wird nie Tagesaufgabe und kostet nie Malus. Die Frage ist damit beantwortet und `verworfen`; die Umsetzung (Kandidatenfilter
in `DailyPlanner.CandidatesAsync`) gehört zu [B-736](B-736-pflicht-aus-der-tagesaufgabe.md), wo aus der Tagesaufgabe eine Pflicht wird – im
Schattenmodus wirkt der Befund nichts, und die Schattenwoche zeigt bis dahin, wie oft Kür im Plan stand.
