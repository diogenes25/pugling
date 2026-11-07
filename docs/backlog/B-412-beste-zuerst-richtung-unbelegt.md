---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [„beste zuerst" ohne Richtungsbeleg]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-07.md#retrospektive
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-270]
---

# B-412 · „beste Bewertung zuerst" im Planbau ist nur gegen „nicht sortiert" belegt, nicht gegen „schlechteste zuerst"

Nachschau zu Sprint 19 (frischer `frontend-reviewer`, `git show ecbe5d8`, 🟡). Der Server stellt unbewertete Übungen in **beiden**
Richtungen ans Ende (`ExerciseCatalogController.ApplySort`, Fall `("rating", …)`, B-12). `e2e/planbau-bewertung.spec.ts` hat nur eine
bewertete und eine unbewertete Übung – damit liefern `dir=desc`, `dir=asc` und `sort=rating` ohne `dir` dieselbe Reihenfolge. Die
Mutante `dir: "asc"` in `PlanPositions.tsx` (Aufruf von `api.searchExercises`, um `:439`) zeigte „schlechteste zuerst" und bliebe grün;
gemessen war in B-270 nur „Sortierung nicht gesendet". Der Teil des AK, der „beste" ausmacht, ist unbelegt.

Am Code geprüft (2026-11-07): die Prämisse stimmt, beide Zweige ordnen zuerst nach `Ratings.Any() ? 0 : 1`.

Vorschlag des Reviewers: eine dritte Übung, vom zweiten Konto mit 2 Sternen bewertet und im Katalog ganz vorn; zusichern 5 ★ → 2 ★ →
unbewertet, dann die Mutante `dir: "asc"` mit `.claude/scripts/mutant-probe.py` messen.

## Verlauf

- **2026-11-07** — angelegt aus der Nachschau zu Sprint 19 (Retro Sprint 20, Lauf AC). Increment eines **früheren** Laufs (AB) – kein Limit 2 für Lauf AC.
