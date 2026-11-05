---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Id 0 an Inline-Items und Item-PATCH ungetestet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-v
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-329]
---

# B-388 · Zwei Fälle „Id 0 bleibt `validation_error`“ aus B-329 haben keinen Test

B-329 AK 1 sagt, eine Id `0` bleibe Formfehler (`validation_error`). Belegt ist das nur für `refs` und den Item-`POST`. Für
Inline-`items` (`ExerciseControllers.cs:59-60`) und den Item-`PATCH` (`:262`) fehlt der Fall: würde dort `0` zu
`invalid_reference` umgestellt, bliebe jede Suite grün. Je eine Zusicherung in `UnknownReferenceCodeTests` schließt das.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf U, Sprint 4 (Lauf V, Retro Sprint 5; frischer `pugling-reviewer`,
  `git show f4cf656`, 🟢). Entgangen bei B-329: das AK behauptete die Unterscheidung, der Test trug sie nur an zwei von vier Stellen.
