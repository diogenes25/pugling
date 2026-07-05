# Übergabe: Vokabel-Komplextyp & Verknüpfung über Übungen

**Zweck:** Eine neue Session soll das Vokabel-Thema ohne Wiederaufrollen fortführen. Dieses Doc ist die
eine Wahrheit zum aktuellen Stand; ergänzend gelten `CLAUDE.md`, die Memory `lehrplan-umbau.md` und der
Gesamtplan [docs/lehrplan-umbau-plan.md](lehrplan-umbau-plan.md).

- **Branch:** `lehrplan-umbau` (alles committet; offene Dateien im Working Tree sind vorbestehend/unrelated).
- **Tests:** 148 grün (`dotnet test backend/Pugling.Api.Tests`).
- **Läuft:** Backend `:5200` (`/swagger`), Frontend `:5173` (`/vater`, Papa / PIN `0000`).

## Mentales Modell (wichtig!)

Vokabeln existieren im System **zweifach getrennt** — das ist der Kern des Themas:

1. **Vokabel-Store `Vocabulary`** = der **Komplextyp** (Single Source of Truth). Felder: `Key` (eindeutig),
   `Word`, `Translation`, `SourceLanguage`, `TargetLanguage`, `PartOfSpeech`, optional `Noun`/`Verb`
   (JSON), `BaseFormId` (Selbstreferenz: flektierte Form → Grundform), `PronunciationAudioUrl`.
   Datei: [Models/VocabEntities.cs](../backend/Pugling.Api/Models/VocabEntities.cs).
2. **Vokabel-Übung** (`ExerciseType.Vocabulary`, Config-Klasse `VocabularyConfig`): historisch **inline**
   (`VocabItem{Front,Back,Hint}`) und vom Store entkoppelt.

**Entscheidung dieser Session:** Vokabel-Übungen **referenzieren jetzt den Store** über `VocabularyConfig.Refs`
(Liste von Store-Keys). Damit ist dieselbe Vokabel über mehrere Übungen hinweg verknüpft und zentral pflegbar.
Inline `Items` bleibt für Abwärtskompatibilität (Seed/Alt).

## Was in dieser Session gebaut wurde (vokabel-relevant)

- **Zweistufige Store-Eingabe** ([VocabularyStoreController.cs](../backend/Pugling.Api/Controllers/Learn/VocabularyStoreController.cs)):
  `Create` nimmt `Key` **optional** (fehlt er → Auto-Slug via [VocabKey.Generate](../backend/Pugling.Api/Data/VocabKey.cs),
  Kollision → `_2`, `_3`…) und `PartOfSpeech` optional (Default `Other`). „Einfach" = nur Word/Translation
  (+Sprachen); „komplex" = volle Felder; **später nachliefern = bestehendes `PATCH`** (Merge, `null` = unverändert).
- **Übung → Store-Referenz:** `VocabularyConfig.Refs` ([ExerciseConfigs.cs](../backend/Pugling.Api/Models/ExerciseConfigs.cs)),
  aufgelöst durch **`ExerciseContentResolver`** ([Services/ExerciseContentResolver.cs](../backend/Pugling.Api/Services/ExerciseContentResolver.cs),
  scoped, DB-gestützt): lädt Store-Vokabeln per Key → `ContentItem`s; für Nicht-Vokabeln + Legacy-inline
  delegiert er an den zustandslosen `ExerciseContentProvider`. `PositionPlayService.ItemsOfAsync` + die
  Positions-Controller nutzen den Resolver (async).
- **Übungs-Verwaltung** ([ExerciseCatalogController.cs](../backend/Pugling.Api/Controllers/Learn/ExerciseCatalogController.cs)):
  `GET learn/exercises/{id}` (Detail inkl. Config + Metadaten), `GET learn/exercises/{id}/usage`
  (Lehrpläne via `PlanPosition` + Klassenarbeiten, auf eigene Kinder gefiltert), Lösch-Schutz 409
  ([ExerciseControllerBase.Delete](../backend/Pugling.Api/Controllers/Learn/ExerciseControllerBase.cs)).
- **Web-UI:** [VaterVocab.tsx](../frontend/src/vater/VaterVocab.tsx) (Einfach/Komplex-Umschalter, Suche,
  Inline-Bearbeiten/Löschen); [VaterExercises.tsx](../frontend/src/vater/VaterExercises.tsx)
  (Vokabel-Editor `VocabRefPicker` wählt Store-Einträge → `config.refs`, „+ anlegen & wählen";
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

## Store-verknüpftes Übungs-Authoring + Positions-CRUD (neu)

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
- **Refs aus Tags (P2):** `POST learn/subjects/{s}/chapters/{c}/vocabulary/{id}/refs-from-tags
  { tags:[…], matchAll?, baseFormsOnly? }` schreibt die aktuellen Keys als **Snapshot** in `config.Refs`
  (Leitner-stabil). `baseFormsOnly` (auch als `List`-Filter) schließt flektierte Formen aus.
- **Validierung + Usage (P3):** Anlegen/Ändern einer Vokabel-/Cloze-Übung prüft die referenzierten Keys
  (400 bei unbekannt, `ValidateConfigAsync`-Hook). `GET learn/vocabulary/{id}/usage` listet referenzierende
  Übungen; Löschen einer referenzierten Vokabel → 409.

Umgesetzt in [PlanPositionsController.cs](../backend/Pugling.Api/Controllers/Learn/PlanPositionsController.cs),
[ExerciseControllers.cs](../backend/Pugling.Api/Controllers/Learn/ExerciseControllers.cs) (Vocabulary/Cloze),
[ExerciseControllerBase.cs](../backend/Pugling.Api/Controllers/Learn/ExerciseControllerBase.cs) (Hook),
[ExerciseContentResolver.cs](../backend/Pugling.Api/Services/ExerciseContentResolver.cs) (Cloze-Auflösung).
Tests: `PlanPositionCrudTests`, `VocabExerciseAuthoringTests` (165 grün). Kein DB-Migration nötig.

## Offene Punkte (Kandidaten für die neue Session)

1. **In-Web-Editieren von Vokabel-Übungen** (Refs ändern/erweitern): API kann es (per-Typ `PUT`), UI fehlt.
2. **`Direction` (back-to-front / both)** wird vom Resolver noch **nicht** umgesetzt — Items sind kanonisch
   `Word→Translation` (wie der alte Provider). Für „hinten→vorne" müsste der Motor/Resolver drehen.
   Betrifft `ExerciseContentResolver.ResolveVocabRefsAsync` + Stufen-Interpretation in `PositionPlayService`.
3. **Grundform-Verknüpfung (`BaseForm`/`BaseFormRelation`)** ist im Store-Modell + API vorhanden, aber die
   zweistufige UI bietet dafür noch kein Feld (flektierte Formen → Grundform verknüpfen).
4. **Vokabel-Tags im Web-UI** ([VaterVocab.tsx](../frontend/src/vater/VaterVocab.tsx)): Filter nach Tag +
   Tags setzen fehlt noch (API steht).
5. **Text→Vokabel-Extraktion als Wort-für-Wort-Bindung *in* Übungstexten** (Cloze-Gaps/Birkenbihl-WordPairs
   an Store-Keys binden) ist bewusst **nicht** gebaut — der Agent nutzt heute `lookup`+`batch`+`tags`. Falls
   gewünscht, eigene größere Etappe (ContentItem/Config/Resolver umbauen).
6. **Seed auf `Refs` migrieren** (optional): die geseedeten Vokabel-Übungen sind noch inline; Resolver liest
   beide, daher kein Zwang. Für Konsistenz könnte man den Seed umstellen.
7. **`Hint`** kommt beim Store-Weg aus `Noun.Article`; ob das gewünscht ist / andere Quelle → offen.
8. **PATCH-Clear** von `Noun`/`Verb`/`Audio` (auf null setzen) ist bewusst nicht umgesetzt (nur Nachtragen).
9. **Größerer Kontext:** Etappe 4–7 des Lehrplan-Umbaus (Ziel-/Punkte-Rollup, Positions-CRUD, Frontend-Play,
   Autorschaft) laufen unabhängig weiter — siehe Gesamtplan.

## So testen / API-Beispiele (Pfad, den auch ein Agent nutzt)

```
# Login (Vater) → Token
POST /api/v1/auth/father        { "fatherId": 1, "pin": "0000" }

# Vokabel einfach anlegen (Key + Wortart macht der Server)
POST /api/v1/learn/vocabulary   { "sourceLanguage":"en","targetLanguage":"de","word":"cat","translation":"Katze" }
# → 201, key z.B. "en_cat_de_katze"

# Später komplex nachliefern
PATCH /api/v1/learn/vocabulary/{id}  { "partOfSpeech":"Noun","noun":{"article":"die"} }

# Vokabel-Übung, die den Store referenziert
POST /api/v1/learn/subjects/{s}/chapters/{c}/vocabulary
     { "title":"Unit 1","orderIndex":1,"rewardPoints":10,
       "config": { "direction":"front-to-back","refs":["en_cat_de_katze", ...] } }

# Verwaltung
GET  /api/v1/learn/exercises/{id}          # Detail + Config
GET  /api/v1/learn/exercises/{id}/usage    # in welchen Lehrplänen/Klassenarbeiten
PUT/DELETE .../chapters/{c}/vocabulary/{id}
```

Integrationstest-Muster: `backend/Pugling.Api.Tests/TestApi.cs` (Helfer `CreateStoreVocabAsync`,
`CreateVocabRefExerciseAsync`, `SeedLeitnerPosition`).

## Bau-/Umgebungs-Fallstricke

- **Datei-Lock:** Läuft das Backend (`dotnet run`), scheitert `dotnet build` am gesperrten `.exe`
  (nur Copy-Fehler, kein Code-Fehler). Vor Builds/Migrations das Backend stoppen.
- **EF-Migrationen** bei Schemaänderung: `dotnet ef migrations add <Name> --project backend/Pugling.Api
  --output-dir Data/Migrations`; `Program.cs` migriert beim Start. Dev-DB `pugling.db` ist gitignored und
  darf gelöscht/neu geseedet werden.
- Nach `.cs`-Edits laufen Hooks (`dotnet format` + `dotnet build`) automatisch.
