---
tags: [typ/story, status/idee, bereich/beides, rolle/creator]
aliases: [Unit-Löschsperre zeigt auf „Verwendung"]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-07.md#runde-2--review
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-413 · Unit/Reihe löschen meldet `exercise_in_use` mit Verweis auf „Verwendung" – Ziel-Etappen auf der Unit stehen dort nie

Gefunden im Review zu B-409 (`frontend-reviewer`, 🟡), bewusst nicht in den Sprint genommen (Regel: ein Fund während der Verifikation
wird eine eigene Story). Der deutsche Text zu `exercise_in_use` (`frontend/src/lib/api.ts`, Eintrag `exercise_in_use`) sagt, nur fremde
Verwendungen stünden nicht unter „Verwendung". Derselbe Code kommt aber auch beim Löschen einer **Unit** bzw. **Reihe**
(`SeriesUnitsController`, `TextbookSeriesController` über `ExerciseUsageQueries.AnyBlockingAsync`), und dort sperrt zusätzlich eine
Ziel-Etappe auf der **ganzen Unit** (`KeyResult.SeriesUnitId` ohne `ExerciseId`). Die zeigt keine „Verwendung" einer Übung – zu Recht,
sie sperrt das Löschen der Übung nicht (B-409, Fall 3b). Der Vater bekommt 409 und findet an der genannten Stelle nichts.

Nach der Meldungsregel vom 2026-11-07 (`pm-loop` Step 5) muss eine Fläche, auf die eine Meldung zeigt, die Sperre zeigen. Zu entscheiden
beim Ausformulieren: eigener Code bzw. eigener Text für das Löschen einer Unit/Reihe (nennt Ziel-Etappen auf der Unit, ohne „Verwendung"),
oder eine Verwendungs-Sicht auf Unit-Ebene. Hängt mit [B-414](B-414-meldungstexte-nennen-auswege.md) zusammen (dieselben Sätze).

## Verlauf

- **2026-11-07** — angelegt aus dem Review zu B-409 (Lauf AC, Sprint 20).
