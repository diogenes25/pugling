---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [„beste zuerst" ohne Richtungsbeleg]
status: abgenommen
prio: P3
art: Defekt
groesse: "XS"
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

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. Dritte Übung, vom zweiten Konto mit 2 ★ bewertet und im Katalog **ganz vorn**; zugesichert wird die volle Reihenfolge
   5 ★ → 2 ★ → unbewertet. Damit unterscheidet die Spec „beste zuerst" von „schlechteste zuerst" und von „nicht sortiert".

## Akzeptanzkriterien

1. Die Mutante `dir: "asc"` am Aufruf in `PlanPositions.tsx` macht die Spec rot.

## Verlauf

- **2026-11-07** — angelegt aus der Nachschau zu Sprint 19 (Retro Sprint 20, Lauf AC). Increment eines **früheren** Laufs (AB) – kein Limit 2 für Lauf AC.
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt XS (Tageslauf, Sprint 54).
- **2026-11-13** — gebaut (Sprint 54). Dritte Übung mit 2 ★ ganz vorn im Katalog, volle Reihenfolge 5 → 2 → unbewertet;
  nach Review zusätzlich die Sternzeile der mittleren. E1 (`dir: "asc"` in `PlanPositions.tsx`) → rot (Playwright).
  - Verifikation: Vitest 820/820, `tsc -b` sauber; Mutanten D1–D6 und E1 nach der letzten Änderung (Tabelle im Protokoll,
    Sprint 54): D1, D2, D3, D5, D6, E1 rot; **D1b und D4 nicht rot – begründet**: ein `DateOnly` wird als Mittag UTC gelesen
    *und* in der Arcade in UTC formatiert; fällt eine Hälfte weg, trägt die andere für alle Zonen bis UTC+11 (D4 wirkt nur
    jenseits davon, ohne Zonenparameter für DateOnly nicht messbar). Playwright 140/140 auf 5201/5174.
  - Rollengang Sohn/Vater: E2E-Suite gegen echten Server und UI; Creator Regressionszeuge. Abgenommen.
