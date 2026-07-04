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

In [Controllers/Learn/ExerciseControllers.cs](../backend/Pugling.Api/Controllers/Learn/ExerciseControllers.cs)
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
3. **Inhalts-Bezug:** Reicht der Vokabel-Store (wie Matching) oder braucht es einen neuen Store (wie
   `ClozeText`)? Store = eigene Entity + Store-Controller + `StudyPlanItem`-FK.
4. **Test-Controller** `…/study-plans/{planId}/<verfahren>-tests` (Start ohne Lösung, Submit bewertet
   serverseitig, Punkte über den geteilten `StudyProgressService`/`TestAttemptService`).
5. **`StudyProgressService.IsTyped(...)`** um die neuen gewerteten Stufen erweitern.
6. **`PracticeSessionsController.Grade(...)`** + **`ToCard(...)`** um den neuen `LearningMethod`-Zweig
   ergänzen (server-autoritative Bewertung + lösungsfreie Karte).
7. **`ScheduleService`** nutzt du unverändert; **`ScoringService`** ist bereits verfahrensneutral.
8. **`CreatePlanDto.DefaultStage`**-Fallback im `StudyPlansController` um den neuen Method-Zweig ergänzen.
9. **Migration**, falls neue Entities/Spalten (Store, FK) hinzukamen.

Der Beleg, dass der Rahmen trägt: Matching kam **ohne** Änderung an `TestAttempt`/`StudyProgressService`
aus (nur Auswahl-Logik + Test-Mechanik). Halte dich an dieses additive, opt-in-Muster.

---

## C. Add-Ons ohne Codeänderung (nur Daten)

Vieles ist reine **Datenpflege über die API** — kein Code nötig. Ideal für einen LLM-Agenten:

- **Neue Fächer/Kapitel/Übungen** im Katalog anlegen ([03](03-uebungstypen.md)).
- **Neue Study-Pläne** für Kinder bauen ([04](04-lernplan-bauen.md), [09 · Kochbuch](09-llm-kochbuch.md)).
- **Missionen & Auszeichnungen** je Kind definieren ([05 §5](05-punkte-und-bonus.md#5-missionen--auszeichnungen)).
- **Zeitfenster-Multiplikatoren** anpassen (`TimeSlotRule`).
- **Bonus je Plan** feintunen (`PATCH /study-plans/{id}`).

---

## D. Wo was liegt (Ankerpunkte)

| Thema | Datei |
| --- | --- |
| Übungs-Configs | `Models/ExerciseConfigs.cs` |
| Übungstyp-Enum & Katalog-Entities | `Models/LearnEntities.cs` |
| Übungs-Controller (alle Typen) | `Controllers/Learn/ExerciseControllers.cs` + `ExerciseControllerBase.cs` |
| Katalog-Auswertung | `Services/ExerciseAnswerChecker.cs`, `ArithmeticProblemGenerator.cs` |
| Study-Plan-Modell & Stufen | `Models/StudyPlanEntities.cs` |
| Punkte/Bonus | `Services/ScoringService.cs`, `Models/AdminEntities.cs` (`PointKind`) |
| Tages-Auswertung | `Services/StudyProgressService.cs` |
| Auswahl/Leitner | `Services/ScheduleService.cs` |
| Gamification | `Services/GamificationService.cs`, `Models/GamificationEntities.cs` |
| Auth/Ownership | `Auth/` (`TokenService`, `AuthAccess`, `*OwnershipFilter`) |
| Seed-Beispiele | `Data/Seed.cs` |
| Migrationen | `Data/Migrations/` (`dotnet ef migrations add <Name> --project backend/Pugling.Api --output-dir Data/Migrations`) |
