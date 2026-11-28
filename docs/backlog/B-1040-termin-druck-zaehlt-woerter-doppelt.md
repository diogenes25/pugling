---
tags: [typ/story, status/idee, bereich/lehrplan, rolle/supervisor]
aliases: [Termin-Druck doppelt, Planer zählt Wörter doppelt]
status: idee
prio: P2
art: Defekt
wo: backend
quelle: docs/pm-sitzung-2026-11-27-p.md
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-680]
---

# B-1040 · Zwei Positionen derselben Übung zählen ihre Wörter im Termin-Druck doppelt

Gefunden in der Nachschau von Sprint 197 (Nachtlauf P, 2026-11-27) beim Rollengang an einer Kopie der echten DB.

## Befund (an der Kopie gemessen)

Friedrichs Englisch-Plan 16 führt dieselbe Übung zweimal: Übung 78 als Tagesposition 133 (33 Karten eingeführt) und als Wochenposition 218
(0 eingeführt), Übung 79 ebenso (134/219). Die Klassenarbeit am 04.12. nennt beide Übungen. Der Planer rechnet je Position
`NewWordsNeededPerDay` aus deren eigenem Fortschritt (`DailyPlanner.CandidatesAsync`, `newAvailable = pool - introduced`) und summiert sie zum
Budget-Bedarf (`DailyPlanRules.Compose`, `examNeed`). Die Wochenposition zählt damit jedes Wort der Übung als neu, auch die, die das Kind über
die Tagesposition längst kennt – der Plan meldet „99 neue Wörter pro Lerntag … fehlen etwa 558“ und hebt das Budget an den Deckel.

## Erwartung

Ein Wort, das das Kind schon kennt, ist nicht neu, gleich über welche Position es eingeführt wurde; der Termin-Druck zählt jedes Wort einer
Übung einmal. *Rot, wenn* eine zweite Position derselben Übung den Bedarf erhöht.

## Offen fürs Grillen (Defekt, autonom grillbar)

- Zählt „neu“ je Übung (höchster Fortschritt der Positionen) oder kind-weit über `ItemProgress`?
- Der Bedarf zählt Karten (Richtung „beide“ = zwei Karten je Wort), die Meldung sagt „Wörter“ – gehört das hierher oder zu
  [B-1038](B-1038-dosis-aus-der-tagesaufgabe.md)?

## Verlauf

- **2026-11-27** — angelegt aus der Nachschau von Sprint 197 (B-680) im Nachtlauf P.
