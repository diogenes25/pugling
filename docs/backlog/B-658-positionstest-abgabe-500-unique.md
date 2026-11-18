---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/student]
aliases: [Test-Abgabe 500, UNIQUE PositionItemProgress]
status: abgenommen
prio: P1
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: "E2E-Lauf 2026-11-18 (pfad.spec.ts, zwei-faecher.spec.ts) auf main 23d19204"
grund: ""
ersetzt_durch: []
---

# B-658 · Positions-Test abgeben scheitert mit 500 (UNIQUE PositionItemProgress)

`POST student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/submit` endet in `pfad.spec.ts`
und `zwei-faecher.spec.ts` mit `500 internal_error`; der Server meldet
`UNIQUE constraint failed: PositionItemProgress.PlanPositionId, PositionItemProgress.ItemIndex`. Reproduziert
auf `main` 23d19204 **ohne** Frontend-Änderung (Gegenprobe per `git stash`, Seitenports 5211/5184, Release).
Beide Specs bestehen den Test über die API, ohne vorher geübt zu haben – vermutlich legt der Submit eine
Fortschrittszeile doppelt an. P1, weil ein Kind, das direkt in den Test geht, seinen bestandenen Test nicht
gebucht bekäme (Friedrichs Generalprobe am 24.11. ist ein Wochentest).

## Ursache und Fix (2026-11-18, `39efb32b`)

Direkt nach dem Fund behoben, auf Wunsch des Vaters ohne den Weg über die Zwischenstufen.

**Ursache:** `PositionPlayService.ProgressForAsync` fragte nur die Datenbank. Ein Test, der ein kaltes Wort im freien
Feld richtig bekommt, führt es ein (`IntroduceAnsweredAsync` → neue, noch ungespeicherte Zeile) und prägt es im selben
Request ein (`ImprintAnsweredAsync`, B-643) – der zweite Aufruf fand die Zeile nicht und legte sie ein zweites Mal an.
Betroffen war nicht nur die Sammelabgabe der Specs, sondern auch der **schrittweise `/answer`-Weg der App**, auf
Positionen ohne Karteikasten (dort fragt der Test jedes Wort auf der Stufe der Position, auch ein kaltes frei).

**Fix:** `ProgressForAsync` fragt zuerst `db.PositionItemProgress.Local`.

**Nachweis:** `LernkarteTests.Kalter_Test_Frei_Getippt_Fuehrt_Ein_Und_Praegt_Ein` (schrittweise und Sammelabgabe);
rote Probe ohne Fix: 500 an Antwort bzw. Abgabe – am richtigen Ort, nicht an der Einpräge-Zusicherung. Eine erste
Fassung mit Karteikasten fiel ohne *und* mit Fix an derselben Stelle (der Test fragte kalte Wörter leichter) und wurde
verworfen. `pfad.spec.ts` und `zwei-faecher.spec.ts` wieder grün.
