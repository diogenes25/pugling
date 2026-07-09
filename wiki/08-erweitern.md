---
tags: [typ/tutorial, bereich/katalog, rolle/creator]
aliases: [Erweitern, Neue Lerntechnik, Neuer Übungstyp]
---

# 08 · Erweitern: neue Übung / neues Verfahren / Add-Ons

← [Zurück zum Wiki-Index](../README.md)

Diese Seite ist für **Entwickler und LLM-Agenten**, die Pugling erweitern. Sie zeigt die zwei
Erweiterungs-Achsen und den etablierten Prozess. Zum interaktiven Durchlauf gibt es den Skill
`/neuer-uebungstyp`.

> **Konventionen zwingend beachten:** C# 14/.NET 10, dünne Controller, Logik in Services,
> `record`-DTOs, deutsche `/// <summary>`-Docs, Guard Clauses zuerst, `ProblemDetails` für Fehler,
> EF-Migration bei Schemaänderung. Am bestehenden Code orientieren! Siehe [CLAUDE.md](../CLAUDE.md).

---

## A. Neuen Katalog-Übungstyp anlegen (der häufige Fall)

Ein neuer Übungstyp im Katalog (`Subject → Chapter → Exercise`) ist **additiv** und billig, weil die
Config als JSON gespeichert wird und das gesamte CRUD + Metadaten + `SuggestedBonus` aus
`ExerciseControllerBase<TConfig>` geerbt werden. Beispiel: ein Typ **`TrueFalse`**.

### Schritt 1 — Config-Schema

In [Models/ExerciseConfigs.cs](../backend/Pugling.Api/Models/ExerciseConfigs.cs) eine Config-Klasse +
Items als `record` ergänzen (mit deutschen `/// <summary>`):

```csharp
/// <summary>Wahr/Falsch-Aussagen mit korrekter Antwort.</summary>
public class TrueFalseConfig
{
    public string? Instruction { get; set; }
    public List<TrueFalseItem> Items { get; set; } = new();
}
public record TrueFalseItem(string Statement, bool IsTrue);
```

### Schritt 2 — Typ registrieren

In [Models/LearnEntities.cs](../backend/Pugling.Api/Models/LearnEntities.cs) einen Wert an das Enum
`ExerciseType` **hinten anhängen** (Reihenfolge/Werte bestehender Einträge nicht ändern):

```csharp
public enum ExerciseType { …, Birkenbihl = 11, TrueFalse = 12 }
```

> Das ändert **kein** DB-Schema (der Typ wird als int gespeichert, die Config als JSON in
> `Exercise.ConfigJson`). Eine EF-Migration ist hier **nicht** nötig.

### Schritt 3 — Controller

In [Controllers/Creator/ExerciseControllers.cs](../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs)
einen dünnen Controller ergänzen — er erbt das komplette CRUD:

```csharp
/// <summary>Wahr/Falsch-Übungen.</summary>
[Route(ExerciseRoutes.Base + "/true-false")]
[Tags("Learn – TrueFalse")]
public class TrueFalseController(PuglingDbContext db) : ExerciseControllerBase<TrueFalseConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.TrueFalse;
}
```

Damit existieren sofort `GET/POST/PUT/DELETE …/true-false[/{id}]` mit eigenem Swagger-Schema,
Metadaten (`gradeMin/gradeMax/schoolTypes/source/categoryId`), `suggestedBonus` und Übungssuche.

### Schritt 4 (optional) — Auswertung `/check`

Nur nötig, wenn die Übung serverseitig ausgewertet werden soll. Dann in
[Services/ExerciseAnswerChecker.cs](../backend/Pugling.Api/Services/ExerciseAnswerChecker.cs) eine
`CheckTrueFalse(...)`-Methode ergänzen (gibt `CheckResult` zurück) und im Controller einen Endpunkt:

```csharp
[HttpPost("{exerciseId:int}/check")]
public async Task<ActionResult<CheckResult>> Check(int subjectId, int chapterId, int exerciseId, CheckAnswersDto body)
{
    var exercise = await FindAsync(subjectId, chapterId, exerciseId);
    return exercise is null ? NotFound() : checker.CheckTrueFalse(ConfigOf(exercise), body.Answers);
}
```

`FindAsync`/`ConfigOf` kommen aus der Basis; `checker` per Primary-Constructor injizieren (wie
`ArithmeticController`/`MatchingController`).

### Schritt 5 — Verifizieren

Nach `.cs`-Edits laufen `dotnet format` + `dotnet build` automatisch (Hook). Dann:

- `/smoke-test` (isolierte DB) oder gezieltes `curl` gegen `localhost:5200`,
- optional einen Integrationstest in `Pugling.Api.Tests` ergänzen.

### Checkliste „neuer Katalog-Typ"

- [ ] Config-Klasse + Item-`record` mit deutschen `/// <summary>` in `ExerciseConfigs.cs`.
- [ ] `ExerciseType`-Wert **hinten** angehängt.
- [ ] Controller mit `Route` + `Tags` + `Type` (erbt CRUD).
- [ ] (falls prüfbar) `/check` + `ExerciseAnswerChecker`-Methode.
- [ ] **Keine** Migration nötig (JSON-Config), außer du hast echte neue Spalten hinzugefügt.
- [ ] `dotnet build` grün, smoke-getestet.

---

## B. Neues Study-Plan-Lernverfahren (größer)

Ein neues *Trainings-Verfahren* (wie Vocabulary/Cloze/Matching) berührt mehr, weil es Test-Mechanik,
Stufen und Inhalts-Bezug mitbringt. Der verfahrensneutrale Rahmen (Zeit, Punkte, Fortschritt,
Fahrplan, Leitner, Auth) bleibt aber unverändert — er trägt seit Iteration 4.

Grobe Landkarte (am Muster von Cloze/Matching orientieren):

1. **`LearningMethod`-Wert** in [Models/StudyPlanEntities.cs](../backend/Pugling.Api/Models/StudyPlanEntities.cs) ergänzen.
2. **Stufen-Enum** des Verfahrens (analog `TestStage`/`ClozeStage`/`MatchStage`) — welche Stufen sind
   „getippt/gewertet"?
3. **Inhalts-Bezug:** Kann die Übungs-Config direkt über `ExerciseContentProvider` in `ContentItem`s
   projiziert werden? Nur wenn du eine globale Bibliothek brauchst, ergänze einen Store-Controller.
4. **`ExerciseContentProvider`/Resolver** um den neuen Typ erweitern: Prompt, Antwort,
   akzeptierte Antworten, optional Hint/Audio/Choices-Grundlage.
5. **`PositionPlayService.IsTypedStage(...)`** um die neuen gewerteten Stufen erweitern.
6. **`PositionPracticeController`/`PositionTestsController`** möglichst unverändert nutzen; sie spielen
   typ-neutral gegen `ContentItem`. Nur bei echter Spezialmechanik ergänzen.
7. **`PositionProgressService`** prüfen: Welche Zielregel gilt für `ExerciseCheckMode` und
   `PlanPosition.GoalThreshold`?
8. **Manifest** (`ExerciseTypeManifest`) aktualisieren: Renderer, `checkMode`, optional `method`/
   `playRoute` für Study-Plan-fähige Typen.
9. **Migration**, falls neue Entities/Spalten hinzukamen.

Der Beleg, dass der Rahmen trägt: Das aktuelle Positionsmodell spielt Vokabeln, Cloze, Matching und
weitere checkbare Typen über denselben positionsbezogenen Practice-/Test-Pfad. Halte dich an dieses
additive Muster und vermeide neue plan-weite Sonderpfade.

---

## C. Add-Ons ohne Codeänderung (nur Daten)

Vieles ist reine **Datenpflege über die API** — kein Code nötig. Ideal für einen LLM-Agenten:

- **Neue Fächer/Kapitel/Übungen** im Katalog anlegen ([03](03-uebungstypen.md)).
- **Neue Study-Pläne** für Kinder bauen ([04](04-lernplan-bauen.md), [09 · Kochbuch](09-llm-kochbuch.md)).
- **Missionen & Auszeichnungen** je Kind definieren ([05 §5](05-punkte-und-bonus.md#5-missionen--auszeichnungen)).
- **Zeitfenster-Multiplikatoren** anpassen (`TimeSlotRule`).
- **Bonus je Position** feintunen (`PATCH /study-plans/{planId}/positions/{positionId}`).

---

## D. Wo was liegt (Ankerpunkte)

| Thema | Datei |
| --- | --- |
| Übungs-Configs | `Models/ExerciseConfigs.cs` |
| Übungstyp-Enum & Katalog-Entities | `Models/LearnEntities.cs` |
| Übungs-Controller (alle Typen) | `Controllers/Creator/ExerciseControllers.cs` + `ExerciseControllerBase.cs` |
| Katalog-Auswertung | `Services/ExerciseAnswerChecker.cs`, `ArithmeticProblemGenerator.cs` |
| Study-Plan-Container & Stufen | `Models/StudyPlanEntities.cs` |
| Positionsmodell | `Models/PlanPositionEntities.cs` |
| Punkte/Bonus | `Services/ScoringService.cs`, `Models/AdminEntities.cs` (`PointKind`) |
| Tages-/Ziel-Auswertung | `Services/PositionProgressService.cs` |
| Auswahl/Leitner | `Services/PositionPlayService.cs` |
| Gamification | `Services/GamificationService.cs`, `Models/GamificationEntities.cs` |
| Auth/Ownership | `Auth/` (`TokenService`, `AuthAccess`, `*OwnershipFilter`) |
| Seed-Beispiele | `Data/Seed.cs` |
| Migrationen | `Data/Migrations/` (`dotnet ef migrations add <Name> --project backend/Pugling.Api --output-dir Data/Migrations`) |
