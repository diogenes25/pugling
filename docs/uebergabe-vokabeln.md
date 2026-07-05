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

## Offene Punkte (Kandidaten für die neue Session)

1. **In-Web-Editieren von Vokabel-Übungen** (Refs ändern/erweitern): API kann es (per-Typ `PUT`), UI fehlt.
2. **`Direction` (back-to-front / both)** wird vom Resolver noch **nicht** umgesetzt — Items sind kanonisch
   `Word→Translation` (wie der alte Provider). Für „hinten→vorne" müsste der Motor/Resolver drehen.
   Betrifft `ExerciseContentResolver.ResolveVocabRefsAsync` + Stufen-Interpretation in `PositionPlayService`.
3. **Grundform-Verknüpfung (`BaseForm`)** ist im Store-Modell + API vorhanden (`BaseFormKey`), aber die
   zweistufige UI bietet dafür noch kein Feld (flektierte Formen → Grundform verknüpfen).
4. **Seed auf `Refs` migrieren** (optional): die geseedeten Vokabel-Übungen sind noch inline; Resolver liest
   beide, daher kein Zwang. Für Konsistenz könnte man den Seed umstellen.
5. **`Hint`** kommt beim Store-Weg aus `Noun.Article`; ob das gewünscht ist / andere Quelle → offen.
6. **Größerer Kontext:** Etappe 4–7 des Lehrplan-Umbaus (Ziel-/Punkte-Rollup, Positions-CRUD, Frontend-Play,
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
