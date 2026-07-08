---
tags: [typ/tutorial, bereich/katalog, bereich/auswertung, lerntechnik/vokabeln]
---

# Vokabel-Funktionalitäten für Entwickler

Dieses Tutorial beschreibt, wie die aktuelle Vokabel-Funktionalität technisch gedacht ist und wie man sie
über die API erstellt, pflegt und im Training auswertet. Es ergänzt die allgemeine Anleitung
[04 · Lernplan bauen](../wiki/04-lernplan-bauen.md) und die kompakte API-Referenz
[07 · API-Referenz](../wiki/07-api-referenz.md).

## 1. Mentales Modell

Eine Vokabel ist nicht mehr einfach ein Eintrag in einer Übungs-Config. Die aktuelle Logik trennt drei
Ebenen:

1. `Vocabulary`: globaler Store-Eintrag mit Key, Wort, Übersetzung, Sprachcodes, Wortart, optionalen
   Nomen-/Verbdetails, Grundform-Verknüpfung, Audio und Tags.
2. `Exercise` mit `VocabularyConfig`: Katalog-Übung mit Richtung, Sprachcodes, Defaults und Metadaten.
   `items`/`refs` im Payload sind nur Authoring-Hilfen.
3. `ExerciseItem`: stabile, sortierte Item-Zeile unter einer Vokabelübung. Sie verweist per
   `VocabularyId` auf den Store und trägt die `ItemId`, an der Lernfortschritt hängt.

Der Grund für diese Trennung: Wörter bleiben zentral pflegbar, Übungen können dieselben Store-Vokabeln
wiederverwenden, und Fortschritt kippt beim Bearbeiten nicht versehentlich auf ein anderes Wort.

## 2. Store-Vokabeln anlegen und pflegen

Für einzelne Wörter reicht ein einfacher Create-Call. `key` und `partOfSpeech` sind optional; der Server
kann den Key aus Sprache, Wort und Übersetzung ableiten.

```http
POST /api/v1/learn/vocabulary
{ "sourceLanguage":"en", "targetLanguage":"de", "word":"cat", "translation":"Katze" }
```

Für Agenten oder Importe ist der Batch-Pfad idempotenter:

```http
POST /api/v1/learn/vocabulary/batch
[
  { "sourceLanguage":"en", "targetLanguage":"de", "word":"cat", "translation":"Katze", "tags":["Unit 1"] },
  { "sourceLanguage":"en", "targetLanguage":"de", "word":"dog", "translation":"Hund", "tags":["Unit 1"] }
]
```

Vor dem Anlegen kann `POST /api/v1/learn/vocabulary/lookup` Duplikate über Wörter oder Keys finden.
Nachträge laufen über `PATCH /api/v1/learn/vocabulary/{id}` oder `PATCH /api/v1/learn/vocabulary/batch`.

## 3. Vokabelübung erstellen

Beim Erstellen darf der Client inline `items` schicken. Der Server legt fehlende Store-Einträge an,
materialisiert die Übungsmenge in `ExerciseItems` und speichert in der Config nur noch Einstellungen.

```http
POST /api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary
{
  "title": "Unit 1 – Tiere",
  "orderIndex": 1,
  "rewardPoints": 10,
  "config": {
    "direction": "front-to-back",
    "sourceLang": "en",
    "targetLang": "de",
    "items": [
      { "front": "cat", "back": "Katze" },
      { "front": "dog", "back": "Hund", "hint": "der" }
    ]
  },
  "defaultStage": 2,
  "defaultItemCount": 20,
  "defaultUseLeitner": true,
  "defaultRequireTypedTest": true
}
```

Die Response enthält in `config.items` normalerweise keine Vokabelpaare mehr. Das ist korrekt: Die Items
liegen jetzt unter der Übung.

## 4. Items einer Übung pflegen

Die Item-Subressource ist der bevorzugte Weg für spätere Änderungen:

```http
GET /api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items

POST /api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items
{ "vocabularyId": 26, "hint": "die" }

POST /api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items
{ "front": "bird", "back": "Vogel" }

PATCH /api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items/{itemId}
{ "hint": "der", "orderIndex": 3 }
```

Wenn eine Übung schon in einem Study-Plan verwendet wird, blockiert die API Löschungen, Umsortierungen und
Einfügungen an fester Position. Das schützt `PositionItemProgress`, weil dieser Fortschritt weiterhin den
positionsbezogenen Item-Index nutzt. Anhängen ans Ende ist erlaubt.

## 5. Items aus Tags bauen

Für größere Einheiten ist der Tag-Flow am stabilsten: Store-Einträge taggen und die Übungsitems als
Snapshot aus den aktuellen Treffern setzen.

```http
POST /api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/refs-from-tags
{ "tags":["Unit 1"], "matchAll":false, "baseFormsOnly":true }
```

Der Endpunkt schreibt nicht mehr in `config.refs`, sondern gleicht `ExerciseItems` ab. Wörter, die im
Snapshot bleiben, behalten ihre `ItemId`; entfernte Wörter verschwinden aus der Übung.

## 6. Im Lernplan verwenden

Der Study-Plan bekommt keine kopierten Vokabeln. Er verweist über eine `PlanPosition` auf die Übung:

```http
POST /api/v1/study-plans/{planId}/positions
{
  "exerciseId": 13,
  "cadence": "Daily",
  "goalThreshold": 80,
  "useLeitner": true,
  "requireTypedTest": true,
  "stageSchedule": [
    { "dayNumber": 1, "stage": 2 },
    { "dayNumber": 5, "stage": 3 },
    { "dayNumber": 8, "stage": 4 }
  ]
}
```

Beim Üben und Testen löst `ExerciseContentResolver` die `ExerciseItems` zu `ContentItem`s auf. Front,
Rückseite, Audio und abgeleitete Hinweise kommen live aus dem Store; die `ItemId` und `VocabularyId` werden
mitgeführt, damit Fortschritt gespeichert werden kann.

## 7. Lernfortschritt auswerten

Es gibt zwei Fortschritts-Sichten:

- `PositionItemProgress`: Fälligkeit und Leitner-Box innerhalb einer konkreten Plan-Position.
- `ItemProgress` plus `ItemReviewEvent`: planübergreifender Lernstand eines Kindes je `ItemId` und Rollup je
  Store-Wort.

Nützliche Endpunkte:

```http
GET /api/v1/study-plans/{planId}/positions/{positionId}/report
GET /api/v1/children/{childId}/vocabulary-progress?onlyWeak=true
GET /api/v1/children/{childId}/vocabulary-progress/{itemId}
GET /api/v1/children/{childId}/vocabulary-progress/{itemId}/history
GET /api/v1/children/{childId}/vocabulary-progress/by-word?onlyWeak=true
```

`onlyWeak=true` bedeutet aktuell: Beherrschung unter 50 %. Die Item-Liste kann zusätzlich nach
`exerciseId` oder `maxBox` gefiltert werden.

## 8. Implementierungsstellen

- [Models/ExerciseConfigs.cs](../backend/Pugling.Api/Models/ExerciseConfigs.cs): `VocabularyConfig`, `VocabItem`, `VocabRef`.
- [Models/ExerciseItemEntities.cs](../backend/Pugling.Api/Models/ExerciseItemEntities.cs): stabile Item-Zeilen der Vokabelübung.
- [Controllers/Learn/ExerciseControllers.cs](../backend/Pugling.Api/Controllers/Learn/ExerciseControllers.cs): Vokabel-CRUD, `refs-from-tags`, Item-CRUD.
- [Services/ExerciseItemService.cs](../backend/Pugling.Api/Services/ExerciseItemService.cs): Materialisierung und ID-erhaltender Abgleich.
- [Services/ExerciseContentResolver.cs](../backend/Pugling.Api/Services/ExerciseContentResolver.cs): Auflösung von `ExerciseItems` zu spielbaren `ContentItem`s.
- [Services/ItemProgressService.cs](../backend/Pugling.Api/Services/ItemProgressService.cs): planübergreifender Lernstand und Antwort-Historie.
- [Controllers/Learn/ChildVocabularyProgressController.cs](../backend/Pugling.Api/Controllers/Learn/ChildVocabularyProgressController.cs): kindzentrierte Progress-API.

## 9. Tests und Checks

Für Änderungen an der Vokabel-Logik mindestens die fokussierten Integrationstests ausführen:

```bash
dotnet test backend/Pugling.Api.Tests --filter "FullyQualifiedName~ExerciseItemsAndProgressTests|FullyQualifiedName~PositionVocabRefTests|FullyQualifiedName~CatalogExerciseTests"
```

Wenn sich OpenAPI-Beispiele oder Fehlercodes ändern, zusätzlich `DocsCaptureTests` laufen lassen und die
generierten Dateien unter `docs/api-examples/` neu erzeugen lassen statt von Hand zu bearbeiten.