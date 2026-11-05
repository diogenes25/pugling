---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [null-Element in Config-Liste 500]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-x
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-391]
---

# B-397 · Ein `null`-Element in einer Config-Liste endet in einer 500

`[Required]` (implizit für nicht-nullbare Member) prüft keine **Elemente** einer Liste, und System.Text.Json nimmt `[null]` an.
Gemessen am 2026-11-05 (temporäre Fälle im Theory-Test von B-391): `POST …/translation` mit `"items":[null]`, `…/birkenbihl` mit
`"sentences":[null]` und `…/cloze` mit `"gaps":[null]` → **500** (`NullReferenceException` in den Validatoren, etwa
`ExerciseControllers.cs:350`, `:420`, `:473`). Erwartet: `400 validation_error` mit dem Pfad.

Zu entscheiden beim Ausformulieren: ein Wächter im Basis-Controller vor `ValidateConfigAsync` (eine Regel für alle Configs) oder
`RespectNullableAnnotations` global (breiter, erst messen). Die Summary des Regressionstests aus B-391 (`UnknownReferenceCodeTests.cs`
um `:283`) wird mit berichtigt; die `[null]`-Fälle kommen in denselben Theory.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf X, Sprint 10 (Retro Sprint 11; frischer `pugling-reviewer`, `git show 2312aaf`, 🟡)
  und **gemessen** (drei 500er). Entgangen bei B-391: dessen Verwerfung im Increment **dieses** Laufs galt nur für `null`-Member –
  **Limit 2**, Lauf X endet nach Sprint 11.
