---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/test]
aliases: [stale Kommentar, 113 Aufrufstellen]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf R, Teil 1 in Lauf R, Teil 2 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-164]
wartet_auf: ""
---

# B-368 · Nach B-164 behauptet ein Kommentar den entfernten Effekt, und die Story zählt falsch

Die Nachschau über `66ff94d` (B-164) fand: `frontend/src/vater/PlanPositions.tsx:529-534` begründet die dauerhafte Sperre im
Fehlerzweig mit „der Effekt hängt an `exercises.data`" – diesen Effekt hat B-164 entfernt; und B-164 Entscheidung 1 nennt
„111 Aufrufstellen", nachgezählt sind es **113** (`useAsync(`/`useAsync<…>(` unter `frontend/src`, ohne Hook und Test). Dazu ein
🟢: kein Fall für A→B→A – ein Merker „beim Wechsel gesetzt, erst beim Erfolg gelöscht" bliebe in allen Fällen grün.

## User Story

Als **Entwickler** möchte ich, dass Kommentar und Story sagen, was der Code tut, und dass jede Form von „veraltet" eine Probe
hat.

## Ist-Stand am Code (Stand `66ff94d`)

- wie oben; selbst nachgezählt: 113.

## Die echte Lücke

Zwei Texte, die nicht mehr stimmen, und ein ungeprüfter Übergang.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Kommentar auf `exercises.stale` umschreiben**, die Zahl in B-164 berichtigen (mit Datum der alten).
2. **Ein siebter Hook-Fall „Hin und Zurück"** ohne Antwort dazwischen.

## Akzeptanzkriterien

1. Die Mutante „Merker statt Vergleich" macht einen Fall rot.
2. Kein Kommentar nennt den entfernten Effekt.

## Schätzung

`XS`, frontend. Testweg: `useAsync.test.tsx`.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf R, Teil 1 (frischer `frontend-reviewer` über `66ff94d`, 🟡 ×2, 🟢 ×1). Im
  selben Sprint gebaut (Freigabe 3). Neuer Fall grün; Mutante „Merker statt Vergleich" **1 failed** – nur am neuen Fall (er
  trägt genau diese Lücke). Kommentar in `PlanPositions.tsx` und Zahl in B-164 berichtigt.
- **2026-11-03** — Abnahme Lauf R, Teil 2: Vitest **638/638**, `tsc -b` sauber, E2E-Gesamtlauf **109 passed** (`exit=0`, nach dem letzten Edit). Rollengang: der Entwickler ist die Rolle (Kommentar, Zahl, Hook-Fall); Vater über die Ladefenster-Specs im Gesamtlauf. **Abgenommen**, Commit siehe Protokoll ([Lauf R](../pm-sitzung-2026-11-03-nacht.md)).
