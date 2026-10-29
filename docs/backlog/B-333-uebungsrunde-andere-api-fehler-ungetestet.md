---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Übungsrunde endet bei jedem ApiError – ungetestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf J, Teil 2 in Lauf K (docs/pm-sitzung-2026-10-29-nacht.md)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-330]
wartet_auf: ""
nachgeschaut: "2026-10-29"
---

# B-333 · Dass nur `plan_inactive` die Übungsrunde beendet, hält kein Fall mit einem anderen Server-Fehler

## Ist-Stand (Lauf K, am Code belegt)

`SohnPractice.tsx:173-179` beendet die Runde nur bei `ApiError` mit Code `plan_inactive`; jeder andere Fehlschlag der
Bewertung lässt sie weiterlaufen (B-330, Entscheidung 5). Der einzige Fall dazu
(`SohnPractice.test.tsx`, „rückt auch nach einer gescheiterten Bewertung weiter") wirft ein schlichtes `Error("offline")`,
keinen `ApiError`. Die Mutanten `if (e instanceof ApiError)` und `e.status === 403` statt des Codes blieben grün – beide
beendeten die Runde bei jedem Server-Fehler, genau die Überreaktion, die Entscheidung 5 vermeidet. In der Klausur hält
`SohnTest.test.tsx:66-67` das Gegenstück.

## User Story

Wie B-330: Als Sohn, dessen Bewertung einmal an etwas anderem scheitert als an einem pausierten Plan, möchte ich
weiterspielen können – und ein Test soll das halten.

## Entscheidungen (autonom, Freigabe 1 – `Defekt`)

1. Nur ein Fall, kein Code: das Verhalten ist richtig, es fehlte der Beleg. Ein `403` mit anderem Code, damit beide
   Mutanten fallen. *Kosten:* keine.

## Akzeptanzkriterien

1. Ein Fall wirft `ApiError(403, …, "forbidden")` aus der Bewertung und erwartet die nächste Karte; beide Mutanten fallen.

## Schätzung

`XS`, Frontend, Komponententest in `SohnPractice.test.tsx`.

## Verlauf

- **2026-10-29** — angelegt aus der Nachschau (frischer `pugling-reviewer`, `git show 02ce632`) in Lauf K; außerhalb des
  Sprint-Diffs, darum ohne Zählung. Gebaut im selben Sprint: der neue Fall ist mit dem Ist-Code **grün** (erwartet – das
  Verhalten stimmt); Mutante `instanceof ApiError` **1 failed / 5 passed**, Mutante `status === 403` **1 failed / 5 passed**,
  wiederhergestellt **6/6**.
- **2026-10-29** — Abnahme Lauf K, Teil 1: volle Suite **1015/1015**, Vitest **576/576**, `tsc -b` sauber, E2E **100 passed** (`exit=0`, nach der letzten Code-Änderung gestartet; danach nur ein Testkommentar berichtigt). Rollengang als E2E-Gesamtlauf (Browser-Extension nicht genutzt): Creator/Vater über `schnellanlage-ohne-dublette` und `auch-richtig-im-uebungseditor`, Sohn als Regressionszeuge über seine Specs. Reviews: `pugling-reviewer`, `frontend-reviewer`, zwei Delta-Reviews (das letzte ohne 🔴/🟡). **Abgenommen**, Commit siehe Protokoll ([Lauf K](../pm-sitzung-2026-10-29-nacht.md)).
- **2026-10-29** — Nachschau in Lauf K, Teil 2 (frischer `pugling-reviewer`, `git show 0fcff19`): sauber. Geprüft: der neue Fall (`403 forbidden`) trennt Code von Fehlerart – beide genannten Mutanten fallen, wie die Story sagt.
