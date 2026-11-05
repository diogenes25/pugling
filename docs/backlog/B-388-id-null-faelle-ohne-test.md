---
tags: [typ/story, status/geschaetzt, bereich/backend, rolle/creator]
aliases: [Id 0 an Inline-Items und Item-PATCH ungetestet]
status: geschaetzt
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-v
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-329]
---

# B-388 · Zwei Fälle „Id 0 bleibt `validation_error`“ aus B-329 haben keinen Test

B-329 AK 1 sagt, eine Id `0` bleibe Formfehler (`validation_error`). Belegt ist das nur für `refs` und den Item-`POST`. Für
Inline-`items` (`ExerciseControllers.cs:59-60`) und den Item-`PATCH` (`:262`) fehlt der Fall: würde dort `0` zu
`invalid_reference` umgestellt, bliebe jede Suite grün. Je eine Zusicherung in `UnknownReferenceCodeTests` schließt das.

## User Story

Als **Entwickler** möchte ich, dass jede Stelle, an der B-329 „Id 0 bleibt Formfehler" behauptet, einen Test hat, der fällt, wenn
die Stelle `0` als unbekannte Referenz meldet.

## Ist-Stand am Code (Stand `f78ff5d`)

- `backend/Pugling.Api.Tests/UnknownReferenceCodeTests.cs`: `0` ist belegt für `refs` (`:49-50`) und den Item-`POST` (`:88`), nicht für
  Inline-`items` (Validator `ExerciseControllers.cs:59-60`) und den Item-`PATCH` (Route `:262`).

## Entscheidungen

1. **(Bau)** Je eine Zusicherung in den bestehenden Fällen, kein neuer Fall: die Unterscheidung gehört neben ihr Gegenstück.

## Akzeptanzkriterien

1. Inline-Item mit `vocabularyId = 0` → `validation_error`, getestet.
2. Item-`PATCH` mit `vocabularyId = 0` → `validation_error`, getestet.

## Schätzung

`XS`, `wo: backend`, nur Tests. Rote Probe je Stelle: `0` auf `invalid_reference` umstellen.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf U, Sprint 4 (Lauf V, Retro Sprint 5; frischer `pugling-reviewer`,
  `git show f4cf656`, 🟢). Entgangen bei B-329: das AK behauptete die Unterscheidung, der Test trug sie nur an zwei von vier Stellen.
- **2026-11-05** — ausformuliert und geschätzt in Lauf V.
