---
description: Führt den etablierten Prozess zum Hinzufügen eines neuen Übungstyps bzw. Lernverfahrens als Checkliste
allowed-tools: Read, Edit, Write, Grep, Glob, Bash
---

Hilf beim Hinzufügen eines neuen Übungstyps / Lernverfahrens: `$ARGUMENTS`.

Zuerst klären, welcher der **zwei** Erweiterungswege gemeint ist – bei Unklarheit kurz nachfragen:

## Weg A – Neuer Katalog-Übungstyp (`Exercise` + `ExerciseControllerBase`)

Für Inhalte, die als Übung im `Subject→Chapter→Exercise`-Katalog gepflegt werden (wie Vocabulary,
Reading, Grammar, Arithmetic, List …). Der günstigste Weg – meist nur eine Config + ein dünner Controller.

1. **Config-Record** in [Models/ExerciseConfigs.cs](backend/Pugling.Api/Models/ExerciseConfigs.cs) anlegen
   (`record` mit sinnvollen Defaults, deutsche `<summary>`). Wird als JSON in `Exercise.ConfigJson` gespeichert.
2. **`ExerciseType`**-Enum-Wert ergänzen (im selben Bereich der Models).
3. **Controller** in [Controllers/Learn/ExerciseControllers.cs](backend/Pugling.Api/Controllers/Learn/ExerciseControllers.cs):
   `class XController(PuglingDbContext db) : ExerciseControllerBase<XConfig>(db)` mit `[Route(ExerciseRoutes.Base + "/x")]`,
   `[Tags("Learn – X")]` und `protected override ExerciseType Type => ExerciseType.X;`. CRUD kommt aus der Basis.
4. **Optional Auswertung**: braucht der Typ ein `/check`, eine Methode in
   [Services/ExerciseAnswerChecker.cs](backend/Pugling.Api/Services/ExerciseAnswerChecker.cs) ergänzen und
   im Controller eine `Check`-Action (Muster: `MatchingController.Check`).
5. **Seed** (optional): ein Beispiel in [Data/Seed.cs](backend/Pugling.Api/Data/Seed.cs) `SeedCatalog`.

## Weg B – Neues Lehrplan-Lernverfahren (`StudyPlan` + Test-Controller)

Für ein eigenes mehrstufiges Trainings-/Test-Verfahren über einen `StudyPlan` (wie Vocabulary/Cloze/Matching).

1. **`LearningMethod`**-Enum-Wert + eine **`XStage`**-Stufen-Enum (mit deutscher `<summary>` je Stufe) anlegen.
2. Falls der Inhalt neu ist: einen **Content-Store** analog `Vocabulary`/`ClozeText` (+ FK-Verdrahtung in
   `StudyPlanItem` und `OnModelCreating`) – sonst den vorhandenen Vokabel-Store nutzen.
3. **`X-TestsController`** unter `api/study-plans/{planId}/x-tests` nach dem etablierten Muster:
   `[Authorize]` + `[ServiceFilter(typeof(PlanOwnershipFilter))]`, Konstruktor
   `(PuglingDbContext db, ScheduleService schedule, TestAttemptService attempts)`, Actions `Start`/`Get`/`Submit`
   (ggf. `Hint`). **Nicht** den Ownership-Filter inline neu schreiben; **nicht** das Lade-/Scoring-Gerüst
   duplizieren – `attempts.GetPlanAsync`/`LoadAttemptAsync`/`ScoreAndAwardAsync` nutzen. Vorlage:
   [TestsController.cs](backend/Pugling.Api/Controllers/Learn/TestsController.cs).
4. **Default-Stufe** im `switch` von [StudyPlansController.Create](backend/Pugling.Api/Controllers/Learn/StudyPlansController.cs) ergänzen.
5. `IsTyped`/Stufen-Logik in [Services/StudyProgressService.cs](backend/Pugling.Api/Services/StudyProgressService.cs)
   ergänzen, falls das Verfahren getippte (gewertete) Stufen hat.

## Für beide Wege – Qualitätsschritte (nicht überspringen)

- Konventionen aus [CLAUDE.md](CLAUDE.md) einhalten (deutsche XML-Docs, `record`-DTOs, Guard Clauses,
  Rollen-/Ownership-Sauberkeit, `AsNoTracking`, kein Selbstbetrug für den Sohn).
- `dotnet build` grün halten (Hook meldet Fehler automatisch).
- **Integrationstest** in `backend/Pugling.Api.Tests` ergänzen (mind. ein Happy-Path + ein Ownership/Role-Fall).
- Bei echter HTTP-Wirkung: `/smoke-test` bzw. gezieltes `curl` gegen `localhost:5200`.
- Kurze Doku/Changelog-Zeile, wo passend (z. B. README „Was schon funktioniert" oder docs/).
