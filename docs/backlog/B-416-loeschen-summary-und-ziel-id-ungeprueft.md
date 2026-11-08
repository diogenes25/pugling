---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Lösch-Summary ohne Ziele]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08.md#retrospektive
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-409]
---

# B-416 · Das Swagger-Summary von „Übung löschen" nennt keine Ziel-Etappen; `GoalUsage.ObjectiveId` prüft kein Test

Nachschau zu Sprint 20 (frischer `pugling-reviewer`, `git show c910cf8`, ein 🟡, dazu 🟢). Am Code nachgeprüft (2026-11-08):

- 🟡 `ExerciseControllerBase.Delete` (`backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs:489`): „Not possible while it is
  embedded in a study plan or a class test." – das fließt in `docs/openapi/v1.json` für jedes `DELETE …/{type}/{exerciseId}`. Seit dem
  Scope-FK sperrt auch eine Ziel-Etappe, und B-409 hat genau das sichtbar gemacht, aber nur das Summary von `Usage` nachgezogen.
  Dieselbe Drift in `docs/uebergabe-vokabeln.md:52-55` (alte Route `learn/exercises/{id}/usage`, „Lehrpläne + Klassenarbeiten").
- 🟢 `ExerciseUsageScopeTests.EigeneZielEtappe_WirdUnterVerwendungGenannt` sichert `objectiveId` nicht zu: eine Projektion von `k.Id`
  statt `k.ObjectiveId` bliebe grün.
- 🟢 kein Fall mit einem Kind, das zwei Betreuer hat (beide sollen die Etappe namentlich sehen).

## Verlauf

- **2026-11-08** — angelegt aus der Nachschau zu Sprint 20 (Retro Sprint 21, Lauf AD). Increment eines **früheren** Laufs (AC) – kein Limit 2 für Lauf AD.
