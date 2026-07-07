# Übergabe: Vokabel-Store, Übungs-Items und Lernfortschritt

**Zweck:** Diese Seite fasst den aktuellen Stand der Vokabel-Funktionalität zusammen, damit eine
Entwicklungs- oder Agenten-Session ohne erneutes Reverse Engineering weiterarbeiten kann. Autoritativ
bleibt die laufende API bzw. Swagger; diese Seite erklärt das mentale Modell und die wichtigsten Pfade.

**Stand:** 2026-07-07, nach dem Umbau auf stabile `ExerciseItem`-Zeilen und kindzentrierten
`ItemProgress`.

## Mentales Modell (wichtig!)

Vokabeln existieren im System auf drei klar getrennten Ebenen:

1. **Vokabel-Store `Vocabulary`** = der **Komplextyp** (Single Source of Truth). Felder: `Key` (eindeutig),
   `Word`, `Translation`, `SourceLanguage`, `TargetLanguage`, `PartOfSpeech`, optional `Noun`/`Verb`
   (JSON), `BaseFormId` (Selbstreferenz: flektierte Form → Grundform), `PronunciationAudioUrl`.
   Datei: [Models/VocabEntities.cs](../backend/Pugling.Api/Models/VocabEntities.cs).
2. **Vokabel-Übung** (`ExerciseType.Vocabulary`, Config-Klasse `VocabularyConfig`) = Einstellungen der
   Übung: Richtung, Sprachen, Defaults und Metadaten. Beim `POST`/`PUT` darf der Client weiterhin `items`
   oder `refs` mitschicken; diese Payload ist aber nur Authoring-Input.
3. **Übungs-Items `ExerciseItem`** = die eigentliche Vokabelmenge einer Übung. Jede Zeile ist eine stabile,
   positionierte Referenz auf eine Store-Vokabel (`VocabularyId`) und hat eine eigene `ItemId`. Front,
   Rückseite und Audio werden nicht dupliziert, sondern live aus dem Store gelesen; nur ein optionaler
   übungslokaler `Hint` liegt am Item.

**Wichtige Konsequenz:** `VocabularyConfig.Items`/`Refs` sind nicht mehr die Quelle der Wahrheit für den
gespielten Inhalt. Nach dem Speichern materialisiert `ExerciseItemService` die Payload in die Tabelle
`ExerciseItems` und reduziert die Config wieder auf Einstellungen. Die API-Response zeigt deshalb
typischerweise `"items": []` und `"refs": null`.

## Was in dieser Session gebaut wurde (vokabel-relevant)

- **Zweistufige Store-Eingabe** ([VocabularyStoreController.cs](../backend/Pugling.Api/Controllers/Learn/VocabularyStoreController.cs)):
  `Create` nimmt `Key` **optional** (fehlt er → Auto-Slug via [VocabKey.Generate](../backend/Pugling.Api/Data/VocabKey.cs),
  Kollision → `_2`, `_3`…) und `PartOfSpeech` optional (Default `Other`). „Einfach" = nur Word/Translation
  (+Sprachen); „komplex" = volle Felder; **später nachliefern = bestehendes `PATCH`** (Merge, `null` = unverändert).
- **Übung → Store-Referenz:** `ExerciseItemService` ([Services/ExerciseItemService.cs](../backend/Pugling.Api/Services/ExerciseItemService.cs))
  übersetzt `VocabularyConfig.Items` bzw. `VocabularyConfig.Refs` in stabile `ExerciseItem`-Zeilen. Inline-
  Items ohne `vocabularyId` werden über den Store angelegt oder wiedergefunden. Überlebende Wörter behalten
  beim Abgleich ihre `ItemId`; nur entfernte Wörter verlieren ihren Item-Datensatz.
- **Inhaltsauflösung:** `ExerciseContentResolver` ([Services/ExerciseContentResolver.cs](../backend/Pugling.Api/Services/ExerciseContentResolver.cs))
  liest Vokabelübungen aus `ExerciseItems`, sortiert nach `OrderIndex`/`Id` und baut daraus `ContentItem`s
  mit `ItemId` und `VocabularyId`. Ohne Item-Zeilen fällt er auf die alte Config-Projektion zurück, damit
  Alt-/Seed-Daten weiterhin spielbar bleiben.
- **Item-CRUD:** [ExerciseControllers.cs](../backend/Pugling.Api/Controllers/Learn/ExerciseControllers.cs)
  bietet unter `.../vocabulary/{exerciseId}/items` eine eigene Subressource zum Auflisten, Anlegen,
  Ändern und Löschen einzelner Vokabelpaare. Das ist der bevorzugte Weg für nachträgliche Änderungen.
- **Übungs-Verwaltung** ([ExerciseCatalogController.cs](../backend/Pugling.Api/Controllers/Learn/ExerciseCatalogController.cs)):
  `GET learn/exercises/{id}` (Detail inkl. Config + Metadaten), `GET learn/exercises/{id}/usage`
  (Lehrpläne via `PlanPosition` + Klassenarbeiten, auf eigene Kinder gefiltert), Lösch-Schutz 409
  ([ExerciseControllerBase.Delete](../backend/Pugling.Api/Controllers/Learn/ExerciseControllerBase.cs)).
- **Web-UI:** [VaterVocab.tsx](../frontend/src/vater/VaterVocab.tsx) (Einfach/Komplex-Umschalter, Suche,
  Inline-Bearbeiten/Löschen); [VaterExercises.tsx](../frontend/src/vater/VaterExercises.tsx)
  (Vokabel-Editor `VocabRefPicker` wählt Store-Einträge bzw. Items, „+ anlegen & wählen";
  Kapitel-Liste mit Verwendung + Löschen). API/Types: [lib/api.ts](../frontend/src/lib/api.ts),
  [lib/types.ts](../frontend/src/lib/types.ts). Tests: `VocabTwoStepTests`, `PositionVocabRefTests`, `CatalogManagementTests`.

## Agenten-API (neu — Datenarbeit auslagerbar)

Der Store ist jetzt so gebaut, dass ein AI-Agent Vokabeln nachträgt/vervollständigt/verknüpft, **ohne**
dass die API Sprachlogik übernimmt (reine Datenschicht; Tokenisieren/Übersetzen/Grundform-Bestimmung macht
der Agent). Route-Präfix `api/v1/learn/vocabulary`:

- **„Einfach" = nur `word`** (Sprachen empfohlen). `translation` ist **optional** → unübersetzte Vokabeln
  können existieren und sind per Filter auffindbar. Key/Wortart wie gehabt optional.
- **Vollständigkeits-Filter am `List` (`GET`)** — 1:1 die drei Agenten-Kriterien:
  `?untranslated=true` (nicht übersetzt), `?incomplete=true` (keine Übersetzung / Wortart `Other` /
  fehlende Noun-/Verb-Details), `?linked=false` (ohne Grundform). Dazu `?sourceLanguage=`/`?targetLanguage=`,
  Paging `?skip=&take=` und Gesamtzahl im Header `X-Total-Count`.
- **`POST …/lookup`** — Dedup vor dem Anlegen: `{ sourceLanguage?, targetLanguage?, words:[…], keys?:[…] }`
  → pro Wort `{ word, exists, matches[] }` (case-insensitiv) + Menge existierender Keys.
- **`POST …/batch`** — idempotentes Massen-Anlegen (`[CreateVocabularyDto]`): pro Item Status
  `created`/`existing`/`error`; gesetzter, bereits vorhandener Key → `existing` (Batch gefahrlos wiederholbar).
- **`PATCH …/batch`** — Massen-Nachtrag (`[{ id, …Felder }]`), gleiche Merge-Semantik wie Einzel-PATCH.
- **`GET …/{id}/forms`** — komplette Grundform-Familie (go→went→gone) über eine beliebige Form; Grundform
  zuerst, jede Form mit ihrem `baseFormRelation`-Label.
- **Beziehungswert:** `baseFormRelation` (z. B. „Präteritum") an der Grundform-Kante — in Create/PATCH
  neben `baseFormKey`, in jeder `VocabularyResponse`.

### Vokabel-Tags (kindneutral, such- & gruppierbar)

Eigenes Konzept `VocabTag`/`VocabTagLink` (**bewusst getrennt** vom kind-skopierten `Tag`/`ExerciseTag`,
weil der Store kindneutral ist). Tags gruppieren Vokabeln (Kapitel/Klasse/Thema), damit Mensch **und** Agent
beim Bauen von Übungen/Übungstexten gezielt Teilmengen ziehen (z. B. „nur Kapitel ≤ 5").

- Filter am `List`: `?tag=Kapitel 5` (wiederholbar; Default **ODER**, `&matchAll=true` = UND).
- Beim Anlegen direkt taggen: `tags:["Kapitel 5"]` in Create/Batch (create-if-missing) — praktisch für die
  Text-Extraktion, um alle extrahierten Wörter sofort zu gruppieren. In jeder `VocabularyResponse` als `tags[]`.
- Tag-CRUD: `GET/POST …/tags`, `PATCH/DELETE …/tags/{id}` (POST idempotent per Name).
- Vokabel↔Tag: `POST …/{id}/tags` `{ tags:[…] }`, `DELETE …/{id}/tags/{tagId}`.

Umgesetzt in [VocabularyStoreController.cs](../backend/Pugling.Api/Controllers/Learn/VocabularyStoreController.cs)
+ [VocabularyTagsController.cs](../backend/Pugling.Api/Controllers/Learn/VocabularyTagsController.cs);
Modell in [VocabEntities.cs](../backend/Pugling.Api/Models/VocabEntities.cs); Migration
`VocabTagsAndBaseFormRelation`. Tests: `VocabAgentApiTests` (157 grün gesamt).

## Store-verknüpftes Übungs-Authoring + Positions-CRUD

Aufbauend auf dem Store: Übungen lassen sich jetzt store-verknüpft **erstellen** und als **Position** einem
Kind-Plan zuweisen (die Kette „globale Übung → individueller Lehrplan").

- **Lehrplan-Positionen** (`PlanPositionsController`, Vater-only): `GET/POST/PATCH/DELETE
  study-plans/{planId}/positions`. `POST { exerciseId, stage?, itemCount?, scope?, cadence?, goalThreshold?,
  useLeitner?, … }` weist eine globale Katalog-Übung zu; leere Overrides erben Übungs-Defaults
  (`SuggestedBonus`/`DefaultStage`). Leeren Plan-Container über `POST study-plans` **ohne** `contentKeys`
  anlegen. `DELETE` → 409, wenn schon Test-/Übungsverlauf existiert. (Lücke: **kein** DELETE für den Plan selbst.)
- **Lückentext ↔ Vokabel-Store (P1):** `Gap.VocabKey` (optional) → die Lösung der Lücke kommt aus dem
  Store-Wort (`ResolveClozeRefsAsync`), zentral pflegbar; inline `answer` bleibt Fallback. Bewertung läuft
  unverändert über `AcceptedAnswers`. Beispiel: `config.gaps = [{ index:1, answer:"", vocabKey:"en_opportunity_de_gelegenheit" }]`.
- **Refs aus Tags:** `POST learn/subjects/{s}/chapters/{c}/vocabulary/{id}/refs-from-tags
  { tags:[…], matchAll?, baseFormsOnly? }` schreibt die aktuellen Store-Vokabeln als **Snapshot** in
  `ExerciseItems`. Der Abgleich bewahrt die `ItemId` überlebender Wörter; nur nicht mehr enthaltene Wörter
  werden entfernt. `baseFormsOnly` schließt flektierte Formen aus.
- **Validierung + Usage:** Anlegen/Ändern einer Vokabelübung prüft referenzierte `vocabularyId`s (400 bei
  unbekannt). Lückentext-Lücken prüfen weiter `vocabKey`s. `GET learn/vocabulary/{id}/usage` listet
  referenzierende Vokabelübungen über `ExerciseItems`; Löschen einer referenzierten Vokabel → 409.

## Kindzentrierter Vokabel-Lernstand

Der positionsgebundene Leitner-Zustand (`PositionItemProgress`) bleibt für Fälligkeit und Boxen innerhalb
einer Plan-Position zuständig. Zusätzlich schreibt `ItemProgressService` bei bewerteten Vokabel-Antworten
einen planübergreifenden Lernstand:

- `ItemProgress`: eine Zeile je `(Kind, ItemId)` mit Box, Beherrschung, Zählern und letzter Antwort.
- `ItemReviewEvent`: jede Antwort-Historie, aus Üben und Abschlusstests.
- `VocabularyId` und `ExerciseId` sind denormalisiert, damit Auswertungen je Store-Wort auch über mehrere
  Übungen hinweg möglich bleiben.

API-Sicht für Vater und eigenes Kind: `GET /api/v1/children/{childId}/vocabulary-progress`,
`/{itemId}`, `/{itemId}/history` und `/by-word`. `onlyWeak=true` filtert auf Wörter bzw. Items mit
Beherrschung unter 50 %.

Umgesetzt in [PlanPositionsController.cs](../backend/Pugling.Api/Controllers/Learn/PlanPositionsController.cs),
[ExerciseControllers.cs](../backend/Pugling.Api/Controllers/Learn/ExerciseControllers.cs) (Vocabulary/Cloze),
[ExerciseControllerBase.cs](../backend/Pugling.Api/Controllers/Learn/ExerciseControllerBase.cs) (Hook),
[ExerciseContentResolver.cs](../backend/Pugling.Api/Services/ExerciseContentResolver.cs) (Cloze-Auflösung).
Tests: `PlanPositionCrudTests`, `VocabExerciseAuthoringTests` (165 grün). Kein DB-Migration nötig.

## Offene Punkte (Kandidaten für die neue Session)

1. **Grundform-Verknüpfung (`BaseForm`/`BaseFormRelation`)** ist im Store-Modell + API vorhanden, aber die
   zweistufige UI bietet dafür noch kein Feld (flektierte Formen → Grundform verknüpfen).
2. **Vokabel-Tags im Web-UI** ([VaterVocab.tsx](../frontend/src/vater/VaterVocab.tsx)): Filter nach Tag +
   Tags setzen fehlt noch (API steht).
3. **Text→Vokabel-Extraktion als Wort-für-Wort-Bindung *in* Übungstexten** (Cloze-Gaps/Birkenbihl-WordPairs
   an Store-Keys binden) ist bewusst **nicht** gebaut — der Agent nutzt heute `lookup`+`batch`+`tags`. Falls
   gewünscht, eigene größere Etappe (ContentItem/Config/Resolver umbauen).
4. **`Hint`** kommt ohne übungslokalen Hinweis aus `Noun.Article`; ob weitere Store-Quellen sinnvoll sind,
   ist noch offen.
5. **PATCH-Clear** von `Noun`/`Verb`/`Audio` (auf null setzen) ist bewusst nicht umgesetzt; `PATCH` trägt
  derzeit vor allem nach.

## So testen / API-Beispiele (Pfad, den auch ein Agent nutzt)

```
# Login (Vater) → Token
POST /api/v1/auth/father        { "fatherId": 1, "pin": "0000" }

# Vokabel einfach anlegen (Key + Wortart macht der Server)
POST /api/v1/learn/vocabulary   { "sourceLanguage":"en","targetLanguage":"de","word":"cat","translation":"Katze" }
# → 201, key z.B. "en_cat_de_katze"

# Später komplex nachliefern
PATCH /api/v1/learn/vocabulary/{id}  { "partOfSpeech":"Noun","noun":{"article":"die"} }

# Vokabel-Übung anlegen; die Items werden danach in ExerciseItems materialisiert
POST /api/v1/learn/subjects/{s}/chapters/{c}/vocabulary
     { "title":"Unit 1","orderIndex":1,"rewardPoints":10,
    "config": { "direction":"front-to-back","sourceLang":"en","targetLang":"de",
      "items":[{"front":"cat","back":"Katze"}] } }

# Items der Übung lesen oder später erweitern
GET  /api/v1/learn/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/items
POST /api/v1/learn/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/items
  { "vocabularyId": 26, "hint": "die" }

# Fortschritt eines Kindes über Vokabel-Items und Wörter
GET  /api/v1/children/{childId}/vocabulary-progress?onlyWeak=true
GET  /api/v1/children/{childId}/vocabulary-progress/by-word?onlyWeak=true

# Verwaltung
GET  /api/v1/learn/exercises/{id}          # Detail + Config
GET  /api/v1/learn/exercises/{id}/usage    # in welchen Lehrplänen/Klassenarbeiten
PUT/DELETE .../chapters/{c}/vocabulary/{id}
```

Integrationstest-Muster: `backend/Pugling.Api.Tests/TestApi.cs` (Helfer für Store-Vokabeln,
Vokabelübungen und Leitner-Positionen) sowie `ExerciseItemsAndProgressTests` und
`PositionVocabRefTests`.

## Bau-/Umgebungs-Fallstricke

- **Datei-Lock:** Läuft das Backend (`dotnet run`), scheitert `dotnet build` am gesperrten `.exe`
  (nur Copy-Fehler, kein Code-Fehler). Vor Builds/Migrations das Backend stoppen.
- **EF-Migrationen** bei Schemaänderung: `dotnet ef migrations add <Name> --project backend/Pugling.Api
  --output-dir Data/Migrations`; `Program.cs` migriert beim Start. Dev-DB `pugling.db` ist gitignored und
  darf gelöscht/neu geseedet werden.
- Nach `.cs`-Edits laufen Hooks (`dotnet format` + `dotnet build`) automatisch.
