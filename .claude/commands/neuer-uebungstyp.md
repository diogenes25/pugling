---
description: Führt den etablierten Prozess zum Hinzufügen eines neuen Übungstyps bzw. Lernverfahrens als Checkliste
allowed-tools: Read, Edit, Write, Grep, Glob, Bash
---

Hilf beim Hinzufügen eines neuen Übungstyps / Lernverfahrens: `$ARGUMENTS`.

Seit dem Registry-Umbau gilt: **ein Typ = eine Klasse** ([`IExerciseType`](backend/Pugling.Api/Exercises/IExerciseType.cs),
i. d. R. über [`ExerciseTypeBase`](backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)), aufgelöst über die
[`ExerciseTypeRegistry`](backend/Pugling.Api/Exercises/ExerciseTypeRegistry.cs) per String-`Key`. **Kein
`ExerciseType`-Enum, kein `switch`, kein `ExerciseAnswerChecker` mehr** — alle typ-spezifischen Regeln
leben an der Typklasse. Die generischen Play-/Test-Pfade (`PositionPracticeController`/
`PositionTestsController`) und der Testmodus (`ExercisePreviewService`) spielen den neuen Typ automatisch.

Ein einfacher Katalog-Typ und ein mehrstufiges Verfahren sind **derselbe** Weg — Letzteres überschreibt
nur mehr Facetten. Ausführlich: [wiki/08-erweitern.md](wiki/08-erweitern.md).

## Prozess

1. **Config-Record** in [Models/ExerciseConfigs.cs](backend/Pugling.Api/Models/ExerciseConfigs.cs) anlegen
   (`class`/`record` mit sinnvollen Defaults, deutsche `<summary>`). Wird als JSON in `Exercise.ConfigJson`
   gespeichert — kein DB-Schema, keine Migration.

2. **Schlüssel** in `ExerciseTypeKeys` ([Exercises/IExerciseType.cs](backend/Pugling.Api/Exercises/IExerciseType.cs))
   ergänzen (der eine Ort für Magic Strings).

3. **Typklasse** in [Exercises/BuiltInExerciseTypes.cs](backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)
   (oder eigene Datei bei Umfang wie `VocabularyExerciseType`). Pflicht: `Key`, `Manifest`
   (Selbstbeschreibung fürs Frontend — Label/Renderer/SchemaVersion/AuthoringRoute/CheckMode/PlayRoute/
   Method/Capabilities), `ItemsOf` (Config → `ContentItem`s). `ExerciseTypeBase` liefert alle übrigen
   Defaults — **nur überschreiben, was der Typ braucht**:
   - `Check` — nur bei `CheckMode CatalogCheck`/`CatalogGenerateCheck` (Katalog-Direktcheck); geteilte
     Prüf-Primitive in `AnswerChecking` nutzen (`ByIndex`/`Value`/`TextMatch`/`NumericMatch`/`Aggregate`).
   - `DefaultStage`/`PreviewStage`/`IsTypedStage`/`StageOptions` — mehrstufige/getippte Verfahren
     (Stufen-Enum analog `TestStage`/`ClozeStage`/`MatchStage`, „getippt" via `StageMechanics.IsTyped`).
   - `Choices`/`StageFacets` — Multiple-Choice-Ablenker, Buchstabenkästchen-Länge, Audioquelle je Stufe.
   - `SupportsItemProgress`/`SupportsLearnGoals`/`SupportsObjectives` — plan-übergreifender Item-Lernstand
     bzw. erlaubte Lernziele/Objectives.
   - `StoreResolution` (`ItemTable`/`VocabRefs`) — nur wenn Inhalte DB-gestützt aus dem Vokabel-Store
     kommen; dann die Auflösung in [ExerciseContentResolver.cs](backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs)
     ergänzen. Vorlage für den Vollumfang: `VocabularyExerciseType`.

4. **Registrieren:** eine Zeile in `AddExerciseTypes`
   ([Exercises/ExerciseTypeRegistry.cs](backend/Pugling.Api/Exercises/ExerciseTypeRegistry.cs)):
   `services.AddSingleton<IExerciseType, XExerciseType>();` — Manifest & `exercise-types`-Endpunkt kommen automatisch.

5. **Controller** in [Controllers/Creator/ExerciseControllers.cs](backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs):
   `class XController(PuglingDbContext db, ExerciseTypeRegistry registry) : ExerciseControllerBase<XConfig>(db, registry)`
   mit `[Route(ExerciseRoutes.Base + "/x")]`, `[Tags("Creator – X")]` und
   `protected override string TypeKey => ExerciseTypeKeys.X;`. CRUD + Metadaten erbt die Basis. Ist der
   Typ prüfbar, eine dünne `/check`-Action, die an `RunCheckAsync(...)` delegiert (Muster:
   `MatchingController`/`ArithmeticController`/`ListController`).

6. **Zielregel prüfen** (nur `StudyPlanTest`-Typen): liest [PositionProgressService.cs](backend/Pugling.Api/Services/Shared/PositionProgressService.cs)
   den `CheckMode` aus dem Manifest — passt die „erledigt"-Regel für den neuen Modus?

7. **Seed** (optional): ein Beispiel in [Data/Seed.cs](backend/Pugling.Api/Data/Seed.cs).

## Qualitätsschritte (nicht überspringen)

- Konventionen aus [CLAUDE.md](CLAUDE.md) einhalten (deutsche XML-Docs, `record`-DTOs, dünne Controller,
  Guard Clauses, Rollen-/Ownership-Sauberkeit, `AsNoTracking`, kein Selbstbetrug für den Sohn).
- `dotnet build` grün halten (Hook meldet Fehler automatisch); `dotnet format` läuft automatisch.
- **Integrationstest** in `backend/Pugling.Api.Tests` ergänzen — mind. ein Happy-Path. Typklassen lassen
  sich direkt instanziieren (Vorlage: `CatalogExerciseTests`, `ExerciseContentProviderTests`,
  `PositionPlayChoicesTests`); der Manifest-Konsistenztest (`ExerciseTypeManifestTests`) deckt den neuen
  Typ automatisch mit ab.
- Bei echter HTTP-Wirkung: `/smoke-test` bzw. gezieltes `curl` gegen `localhost:5200`.
- Kurze Doku-Zeile, wo passend (z. B. [wiki/03-uebungstypen.md](wiki/03-uebungstypen.md) oder README).

## Checkliste

- [ ] Config-Klasse + Item-`record` mit deutschen `/// <summary>`.
- [ ] Schlüssel in `ExerciseTypeKeys` + Typklasse (`Key`/`Manifest`/`ItemsOf` + nötige Überschreibungen).
- [ ] **Eine Zeile** in `AddExerciseTypes`.
- [ ] Controller mit `Route`/`Tags`/`TypeKey` (erbt CRUD), bei Prüfbarkeit `/check` → `RunCheckAsync`.
- [ ] Store-Auflösung ergänzt, falls `StoreResolution` gesetzt.
- [ ] Keine Migration nötig (JSON-Config), außer echte neue Spalten/Entities.
- [ ] `dotnet build` grün, Integrationstest, smoke-getestet.
