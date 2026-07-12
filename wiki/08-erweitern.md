---
tags: [typ/tutorial, bereich/katalog, rolle/creator]
aliases: [Erweitern, Neue Lerntechnik, Neuer Übungstyp]
---

# 08 · Erweitern: neue Übung / neues Verfahren / Add-Ons

← [Zurück zum Wiki-Index](../README.md)

Diese Seite ist für **Entwickler und LLM-Agenten**, die Pugling erweitern. Zum interaktiven Durchlauf
gibt es den Skill `/neuer-uebungstyp`.

> **Konventionen zwingend beachten:** C# 14/.NET 10, dünne Controller, Logik in Services,
> `record`-DTOs, deutsche `/// <summary>`-Docs, Guard Clauses zuerst, `ProblemDetails` für Fehler,
> EF-Migration bei Schemaänderung. Am bestehenden Code orientieren! Siehe [CLAUDE.md](../CLAUDE.md).

---

## Das Prinzip: ein Typ = eine Klasse (Plugin-Contract)

Seit dem Registry-Umbau ist ein Übungstyp eine **selbstbeschreibende Einheit**: eine Klasse, die
[`IExerciseType`](../backend/Pugling.Api/Exercises/IExerciseType.cs) implementiert (i. d. R. über die
Basis [`ExerciseTypeBase`](../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)). Es gibt **kein
`ExerciseType`-Enum und keine verstreuten `switch`-/`ExerciseAnswerChecker`-Stellen mehr** — alle
typ-spezifischen Regeln (Content-Projektion, Antwort-Prüfung, Play-/Preview-Facetten, Fähigkeiten,
Store-Auflösung) leben an der Typklasse. Aufgelöst wird über die
[`ExerciseTypeRegistry`](../backend/Pugling.Api/Exercises/ExerciseTypeRegistry.cs) per stabilem
String-`Key` (= Wire-/DB-Wert von `Exercise.Type`).

**Ein neuer Typ = eine Klasse + eine Zeile in `AddExerciseTypes`.** Die generischen Play-/Test-Pfade
(`PositionPracticeController`/`PositionTestsController`) und der Testmodus (`ExercisePreviewService`)
spielen den neuen Typ automatisch — sie fragen nur die Facetten der Typklasse ab.

---

## A. Neuen Übungstyp anlegen — Beispiel `TrueFalse`

### Schritt 1 — Config-Schema

In [Models/ExerciseConfigs.cs](../backend/Pugling.Api/Models/ExerciseConfigs.cs) eine Config +
Items ergänzen (mit deutschen `/// <summary>`). Wird als JSON in `Exercise.ConfigJson` gespeichert:

```csharp
/// <summary>Wahr/Falsch-Aussagen mit korrekter Antwort.</summary>
public class TrueFalseConfig
{
    public string? Instruction { get; set; }
    public List<TrueFalseItem> Items { get; set; } = new();
}
public record TrueFalseItem(string Statement, bool IsTrue);
```

### Schritt 2 — Typ-Schlüssel + Typklasse

Den stabilen Schlüssel in [Exercises/IExerciseType.cs](../backend/Pugling.Api/Exercises/IExerciseType.cs)
(`ExerciseTypeKeys`) ergänzen — der eine Ort für Magic Strings:

```csharp
public const string TrueFalse = "TrueFalse";
```

Dann die Typklasse in [Exercises/BuiltInExerciseTypes.cs](../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)
(oder eine eigene Datei bei größerem Umfang, wie `VocabularyExerciseType`). `ExerciseTypeBase` liefert
sinnvolle Defaults (kein Check, immer getippt, keine Auswahl/Facetten/Stufen, keine Capabilities, keine
Store-Auflösung) — **überschreibe nur, was der Typ wirklich braucht**. Pflicht sind `Key`, `Manifest`,
`ItemsOf`:

```csharp
/// <summary>Wahr/Falsch: je Aussage ein Item; Katalog-Direktcheck.</summary>
public sealed class TrueFalseExerciseType : ExerciseTypeBase
{
    public override string Key => ExerciseTypeKeys.TrueFalse;

    // Manifest = Selbstbeschreibung fürs Frontend-Routing (früher ExerciseManifests, jetzt hier).
    // Reihenfolge: Type, Label, Renderer, SchemaVersion, AuthoringRoute, CheckMode, PlayRoute, Method, Capabilities.
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.TrueFalse, "Wahr/Falsch", "true-false", 1, "true-false",
        ExerciseCheckMode.CatalogCheck, null, null, []);

    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<TrueFalseConfig>(configJson);
        return [.. c.Items.Select((it, i) =>
            new ContentItem(i, it.Statement, it.IsTrue ? "wahr" : "falsch", [it.IsTrue ? "wahr" : "falsch"]))];
    }

    // Nur überschreiben, weil der Typ serverseitig geprüft wird (CatalogCheck). Geteilte Prüf-Primitive
    // liegen in AnswerChecking (BuiltInExerciseTypes.cs), damit sich Textvergleiche wie im Test verhalten.
    public override CheckResult Check(string configJson, IReadOnlyList<GivenAnswer> answers, int? seed)
    {
        var c = Deserialize<TrueFalseConfig>(configJson);
        var given = AnswerChecking.ByIndex(answers);
        var items = c.Items.Select((it, i) =>
        {
            var expected = it.IsTrue ? "wahr" : "falsch";
            var value = AnswerChecking.Value(given, i);
            return new ItemCheck(i, it.Statement, value, expected, AnswerChecking.TextMatch(value, expected));
        });
        return AnswerChecking.Aggregate(items);
    }
}
```

> Welche Facetten überschreibst du sonst noch? `DefaultStage`/`PreviewStage`/`IsTypedStage`/`StageOptions`
> (mehrstufige Verfahren), `Choices`/`StageFacets` (Multiple-Choice, Buchstabenkästchen, Audio),
> `SupportsItemProgress`/`SupportsLearnGoals`/`SupportsObjectives` (Item-Lernstand/Ziele) und
> `StoreResolution` (DB-gestützte Inhalte, siehe Abschnitt B). Vorlage: `VocabularyExerciseType`.

### Schritt 3 — Registrieren (die eine Zeile)

In [Exercises/ExerciseTypeRegistry.cs](../backend/Pugling.Api/Exercises/ExerciseTypeRegistry.cs)
(`AddExerciseTypes`) eine Zeile anhängen — mehr braucht die Auflösung nicht:

```csharp
services.AddSingleton<IExerciseType, TrueFalseExerciseType>();
```

### Schritt 4 — Controller (erbt CRUD)

In [Controllers/Creator/ExerciseControllers.cs](../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs)
einen dünnen Controller ergänzen. `TypeKey` statt Enum; die Registry wird an die Basis durchgereicht:

```csharp
/// <summary>Wahr/Falsch-Übungen. <see cref="Check"/> bewertet die Antworten.</summary>
[Route(ExerciseRoutes.Base + "/true-false")]
[Tags("Creator – TrueFalse")]
public class TrueFalseController(PuglingDbContext db, ExerciseTypeRegistry registry)
    : ExerciseControllerBase<TrueFalseConfig>(db, registry)
{
    protected override string TypeKey => ExerciseTypeKeys.TrueFalse;

    // Prüfbar? Dann eine dünne /check-Action, die an IExerciseType.Check delegiert (eine Quelle der Wahrheit).
    [HttpPost("{exerciseId:int}/check")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<CheckResult>> Check(int subjectId, int chapterId, int exerciseId, CheckDto body) =>
        RunCheckAsync(subjectId, chapterId, exerciseId, body);
}
```

Damit existieren sofort `GET/POST/PUT/DELETE …/true-false[/{id}]` mit eigenem Swagger-Schema,
Metadaten (`gradeMin/gradeMax/schoolTypes/source/categoryId`), `suggestedBonus` und Übungssuche.
Der `exercise-types`-Endpunkt und das Manifest kommen **automatisch** aus der Registry.

### Schritt 5 — Verifizieren

Nach `.cs`-Edits laufen `dotnet format` + `dotnet build` automatisch (Hook). Dann:

- `/smoke-test` (isolierte DB) oder gezieltes `curl` gegen `localhost:5200`,
- **Integrationstest** in `Pugling.Api.Tests` ergänzen (Vorlage: `CatalogExerciseTests`,
  `ExerciseContentProviderTests`, `PositionPlayChoicesTests` — sie instanziieren Typklassen direkt).

### Checkliste „neuer Übungstyp"

- [ ] Config-Klasse + Item-`record` mit deutschen `/// <summary>` in `ExerciseConfigs.cs`.
- [ ] Schlüssel in `ExerciseTypeKeys` + Typklasse (`IExerciseType`/`ExerciseTypeBase`) mit `Key`,
      `Manifest`, `ItemsOf` und den nötigen Überschreibungen.
- [ ] **Eine Zeile** in `AddExerciseTypes`.
- [ ] Controller mit `Route` + `Tags` + `TypeKey` (erbt CRUD), bei Prüfbarkeit `/check` → `RunCheckAsync`.
- [ ] **Keine** Migration nötig (JSON-Config), außer du hast echte neue Spalten/Entities hinzugefügt.
- [ ] `dotnet build` grün, smoke-getestet, Integrationstest.

> **DB-Bestand:** `Exercise.Type` ist ein String-Schlüssel. Neue Typen brauchen **keine** Migration.
> Ein *umbenannter* Schlüssel bräuchte eine (String-Remap wie
> [`ExerciseTypeToStringKey`](../backend/Pugling.Api/Data/Migrations)) — Schlüssel gelten aber als
> stabiler Vertrag, also nicht umbenennen.

---

## B. Mehrstufige/getippte Verfahren & Store-gestützte Inhalte

Ein „Lernverfahren" mit Test-Mechanik (wie Vocabulary/Cloze/Matching) ist **kein Sonderweg** mehr —
es ist dieselbe Typklasse, nur mit mehr überschriebenen Facetten. Der verfahrensneutrale Rahmen
(Zeit, Punkte, Fortschritt, Fahrplan, Leitner, Auth, Practice-/Test-Controller) bleibt unverändert.

Am Muster von `VocabularyExerciseType`/`ClozeExerciseType`:

1. **Manifest:** `CheckMode = StudyPlanTest`, dazu `PlayRoute` (z. B. `"tests"`) und `Method`
   (Lernfamilie aus `LearningMethod`, [Models/StudyPlanEntities.cs](../backend/Pugling.Api/Models/StudyPlanEntities.cs)).
2. **Stufen:** eine Stufen-Enum (analog `TestStage`/`ClozeStage`/`MatchStage`), dann `DefaultStage`,
   `PreviewStage`, `IsTypedStage` (welche Stufen sind objektiv/getippt — via `StageMechanics.IsTyped`)
   und `StageOptions` (die im Testmodus umschaltbaren Abfrageformen) überschreiben.
3. **Facetten:** `Choices` (Multiple-Choice-Ablenker) und `StageFacets` (Buchstabenkästchen-Länge,
   Audioquelle) je Stufe, falls das Verfahren sie kennt.
4. **Inhalts-Bezug:** Kommt der Inhalt allein aus der Config? Dann reicht `ItemsOf`. Braucht der Typ den
   **Vokabel-Store** (globale Bibliothek), setze `StoreResolution` (`ItemTable`/`VocabRefs`) und ergänze
   die DB-Auflösung in [ExerciseContentResolver.cs](../backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs).
   Der `ExerciseContentProvider` bleibt die Store-freie Projektion (dünne Fassade über die Registry).
5. **Item-Lernstand/Ziele:** `SupportsItemProgress` (plan-übergreifender `ItemProgress`),
   `SupportsLearnGoals`/`SupportsObjectives` (Lernziele/Objectives dürfen gesetzt werden).
6. **Zielregel:** [PositionProgressService.cs](../backend/Pugling.Api/Services/Shared/PositionProgressService.cs)
   liest den `CheckMode` aus dem Manifest — prüfen, ob die „erledigt"-Regel für den neuen Modus passt.
7. **Migration** nur, falls neue Entities/Spalten hinzukamen (z. B. ein neuer Store).

Der Beleg, dass der Rahmen trägt: Vokabeln, Cloze und Matching laufen über **denselben**
positionsbezogenen Practice-/Test-Pfad — allein über ihre Typklassen-Facetten unterschieden.

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
| Übungstyp-Contract & Registry | `Exercises/IExerciseType.cs`, `Exercises/ExerciseTypeBase.cs`, `Exercises/ExerciseTypeRegistry.cs` |
| Eingebaute Typklassen | `Exercises/BuiltInExerciseTypes.cs`, `Exercises/VocabularyExerciseType.cs` |
| Prüf-Primitive + Ergebnis-DTOs | `AnswerChecking` (in `BuiltInExerciseTypes.cs`), `Services/Shared/CheckResult.cs` |
| Übungs-Configs | `Models/ExerciseConfigs.cs` |
| Manifest-Record | `Models/ExerciseTypeManifest.cs` |
| Übungs-Controller (alle Typen) | `Controllers/Creator/ExerciseControllers.cs` + `ExerciseControllerBase.cs` |
| Content-Projektion / Store-Auflösung | `Services/Shared/ExerciseContentProvider.cs`, `ExerciseContentResolver.cs` |
| Study-Plan-Container & Stufen | `Models/StudyPlanEntities.cs` |
| Positionsmodell | `Models/PlanPositionEntities.cs` |
| Punkte/Bonus | `Services/ScoringService.cs`, `Models/AdminEntities.cs` (`PointKind`) |
| Tages-/Ziel-Auswertung | `Services/PositionProgressService.cs` |
| Auswahl/Leitner/Stufe | `Services/PositionPlayService.cs`, `Services/Shared/StageMechanics.cs` |
| Gamification | `Services/GamificationService.cs`, `Models/GamificationEntities.cs` |
| Auth/Ownership | `Auth/` (`TokenService`, `AuthAccess`, `*OwnershipFilter`) |
| Seed-Beispiele | `Data/Seed.cs` |
| Migrationen | `Data/Migrations/` (`dotnet ef migrations add <Name> --project backend/Pugling.Api --output-dir Data/Migrations`) |
