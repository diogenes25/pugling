---
tags: [typ/story, status/idee, bereich/backend, rolle/student]
aliases: [Test-Abgabe 500, UNIQUE PositionItemProgress]
status: idee
prio: P1
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "E2E-Lauf 2026-11-18 (pfad.spec.ts, zwei-faecher.spec.ts) auf main 23d19204"
grund: ""
ersetzt_durch: []
unverifiziert: true
---

# B-658 · Positions-Test abgeben scheitert mit 500 (UNIQUE PositionItemProgress)

`POST student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/submit` endet in `pfad.spec.ts`
und `zwei-faecher.spec.ts` mit `500 internal_error`; der Server meldet
`UNIQUE constraint failed: PositionItemProgress.PlanPositionId, PositionItemProgress.ItemIndex`. Reproduziert
auf `main` 23d19204 **ohne** Frontend-Änderung (Gegenprobe per `git stash`, Seitenports 5211/5184, Release).
Beide Specs bestehen den Test über die API, ohne vorher geübt zu haben – vermutlich legt der Submit eine
Fortschrittszeile doppelt an. P1, weil ein Kind, das direkt in den Test geht, seinen bestandenen Test nicht
gebucht bekäme (Friedrichs Generalprobe am 24.11. ist ein Wochentest).
