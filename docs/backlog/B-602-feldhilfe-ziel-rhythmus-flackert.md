---
tags: [typ/story, status/idee, bereich/qualitaet, rolle/entwickler]
aliases: [Feldhilfe-Spec flackert]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Sprint 82, Verifikation von B-597 (pm-sitzung-2026-11-16-backlog-lauf.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-602 · `feldhilfe.spec.ts` flackert beim „Ziel-Rhythmus“

Im Sprint 82 fiel `frontend/e2e/feldhilfe.spec.ts:32` („Positions-Formular: jedes erklärungsbedürftige Feld hat den
passenden Hinweis“) in einem von drei Einzelläufen rot: Nach dem Klick auf das „ⓘ“ am Feld „Ziel-Rhythmus“ erschien
kein `note` mit „Wochenziel“ einmal pro Woche“ (10 s Wartezeit). Im vollen Lauf fiel einmal stattdessen der Fall
`:89` (Münz-Malus, ebenfalls `getByRole("note")`). Der Diff des Sprints berührt das Positionsformular nicht
(`PlanPositions.tsx` nur im Bericht). Vermutung: Der erste Klick kommt vor dem Einhängen des Popovers bzw. trifft das
Formular, während es noch nachlädt. Absichtlich nicht im Sprint, weil das Sprint-Ziel ohne ihn erreicht wird.

## Verlauf

- **2026-11-16** — angelegt bei der Verifikation von B-597 (Sprint 82): 2 von 3 Einzelläufen grün.
