---
tags: [bereich/api, bereich/architektur]
---

# API-Design-Bewertung (REST)

> Stand 2026-08-04. Bewertet wurde der Vertrag, wie er im Code steht: alle Controller unter
> `backend/Pugling.Api/Controllers/**`, alle Records unter `backend/Pugling.Contracts/**` und das
> eingecheckte OpenAPI-Dokument [docs/openapi/v1.json](openapi/v1.json). Bewusst **nicht** bewertet
> (gesetzte Entscheidungen, kein Mangel): das Ebenen-Präfix als Taxonomie, die präfixfreien Ausnahmen
> `auth/…` und `remarks/…`, PIN-Login statt OAuth, SQLite, kein GraphQL, Kettenlänge 1 bei den
> Migrationen, keine echte Schema-Versionierung. Abwärtskompatibilität ist **kein** Kriterium – v1 ist
> unveröffentlicht und darf brechen.

> **Nachtrag 2026-08-04 — dieser Bericht ist nicht das Ergebnis, sondern seine Vorlage.** In einer
> Arbeitsrunde PM / API-Designer / Entwickler wurde jeder Vorschlag gegen den Code und gegen die Kosten
> dieses Repos verhandelt. Ergebnis: **B1** (Idempotenz-Schlüssel) und **B2** (ETag/`If-Match`) sind
> verworfen, **A4** ist eine Dublette zu [B-59](backlog/B-59-status-strings-ohne-werteliste.md), **A5**s
> Umbenennung und **A6** sind zurückgezogen, **B7** ist ein Punkt in
> [B-20](backlog/B-20-ki-supervisor-agent.md), von fünf vorgeschlagenen Wächtern sind drei reif, und **B3**
> schrumpft von 35 Listen auf eine (die aber ein echter Datenverlust ist). Wo unten „★★★" steht und der
> Nachtrag widerspricht, gilt der Nachtrag. Die Stories:
> [B-97](backlog/B-97-unique-index-ohne-vorpruefung.md) · [B-98](backlog/B-98-idempotenter-link-post-luegt.md) ·
> [B-99](backlog/B-99-kaufhistorie-endet-lautlos.md) ·
> [B-100](backlog/B-100-vertragsdokument-unterdeklariert.md) ·
> [B-101](backlog/B-101-fehlercodes-und-drei-waechter.md) ·
> [B-102](backlog/B-102-token-vorgabewert-regel-schaerfen.md) ·
> [B-103](backlog/B-103-idempotenzschluessel-und-etag.md).
> Sechs Stellen, an denen dieser Bericht **überzieht**, stehen in B-99, B-100 und B-103 mit Belegen.

## Kurzfazit

Die API ist für ihre Größe ungewöhnlich diszipliniert: 323 Operationen, ein zentraler Fehlerkatalog mit
57 Codes und **null toten** Codes, DELETE ausnahmslos 204, PATCH ausnahmslos 200, jede 4xx-Antwort mit
`ProblemDetails`-Schema, 221 Vertrags-Records ohne eine einzige Namensdopplung, Ownership-Filter
lückenlos auf allen `{childId}`/`{planId}`-Routen. Vier reflexive Wächter halten das mechanisch.
Die Befunde liegen darum fast alle eine Ebene tiefer als „falsches Verb":
**(1)** ein Unique-Index ohne Controller-Vorprüfung macht aus einem 409 einen 500 – belegt bei Kapitel-Rename
und Auszeichnungen; **(2)** idempotente Schreibpfade antworten mit *synthetisierten* statt gelesenen Werten
(ein Zähler ist immer 0, zwei Zeitstempel sind erfunden) und mit 201 obwohl nichts entstand;
**(3)** 35 Array-Endpunkte haben kein Paging, darunter Listen, die über Schuljahre wachsen;
**(4)** derselbe Sachverhalt trägt zwei Codes (`duplicate_email` vs. generisches `conflict`) und drei
Filternamen (`mine`/`mineOnly`/`isOwn`); **(5)** vollständig fehlen Caching/Conditional Requests (0 Treffer
für ETag/If-Match) und Idempotenz-Schlüssel auf den geldbewegenden POSTs.
Nichts davon ist strukturell – alles sind punktuelle Lücken in einem tragfähigen Entwurf.

## Bestandsaufnahme

Alle Zahlen aus dem eingecheckten Dokument [docs/openapi/v1.json](openapi/v1.json) (197 Pfade,
323 Operationen, 295 Schemas, 53 Tag-Gruppen) und aus den Controllern nachgezählt; beide Wege kommen
unabhängig auf dieselben Tier-Summen.

| Ebene | Präfix | Controller-Klassen | Pfade | GET | POST | PUT | PATCH | DELETE | **Operationen** |
|---|---|---|---|---|---|---|---|---|---|
| Creator | `api/v1/creator` | 21 + 12 geerbt | 101 | 72 | 47 | 13 | 17 | 35 | **184** |
| Supervisor | `api/v1/supervisor` | 15 | 44 | 31 | 20 | 2 | 12 | 17 | **82** |
| Student | `api/v1/student` | 9 | 43 | 30 | 13 | 0 | 0 | 0 | **43** |
| sonstige | `api/v1/auth`, `api/v1/remarks` | 2 | 9 | 5 | 5 | 0 | 2 | 2 | **14** |
| **Summe** | | **58** | **197** | **138** | **85** | **15** | **31** | **54** | **323** |

**Zur Controller-Zahl:** 47 `[ApiController]`-Deklarationen (Creator 21, Supervisor 15, Student 9,
Wurzel 2). Eine davon ist die abstrakte Basis
[ExerciseControllerBase.cs:20](../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs);
ihre fünf CRUD-Actions (`List`, `Get`, `Create`, `Update`, `Delete`, Zeilen 154/216/225/298/345) erben
**12** konkrete Übungs-Controller in
[ExerciseControllers.cs](../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs)
(Routen ab Zeile 26, 294, 303, 323, 332, 341, 350, 369, 420, 639, 659, 703) ⇒ 58 routbare Klassen und
60 Routen aus fünf Methoden.

**Zur Endpunkt-Zahl:** 268 Action-Methoden im Quelltext; der Abdeckungs-Wächter inventarisiert 263
(`TestResults/endpoint-coverage.txt` Zeile 1, Schwelle in
[EndpointCoverageGuard.cs:30](../backend/Pugling.Api.Tests/EndpointCoverageGuard.cs)) – die Differenz von
5 sind genau die Actions der abstrakten Basis, die
[EndpointCoverage.cs:50](../backend/Pugling.Api.Tests/EndpointCoverage.cs) per `!t.IsAbstract` ausschließt.
Das ist dokumentiert und kein Fehler, aber es heißt: **wer die 263 als „Endpunkte" liest, unterschätzt die
Oberfläche um 60 Routen.**

### Auth-Attribute je Ebene

| Ebene | Muster | Abweichungen (jede mit Grund im Code) |
|---|---|---|
| Creator | 18 × `[Authorize(Roles = Roles.Creator)]` am Controller | `ExerciseTypesController.cs:16` nur `[Authorize]` (Typ-Manifest, jede Ebene liest); `TagsController.cs:20` nur `[Authorize]` (Kind **und** Erwachsener taggen), dort eine Action mit `Roles.AnyAdult` (Zeile 344); `TeacherAccountsController.cs:41` POST `[AllowAnonymous]` (Selbstregistrierung) |
| Supervisor | 11 × `[Authorize(Roles = Roles.Supervisor)]` am Controller | 4 Controller `[Authorize]` + Schreib-Gate je Action: `KeyResults` (:19 / :25,:41,:54), `Klassenarbeiten` (:21 / :92,:130,:172,:188,:205,:221,:240), `Objectives` (:22 / :46,:60,:73), `StudyPlans` (:22 / :76,:118,:162) – die dokumentierte „dual gelesene Ressource"; `AdultsController.cs:51` POST `[AllowAnonymous]` |
| Student | `MeController.cs:21` und `StudentPlansController.cs:20` mit `Roles.Student` | 7 Controller `[Authorize]` + `[ServiceFilter]` – Absicht: der Supervisor darf für Vorschau/Nachtrag mitlesen |
| sonstige | `AuthController` 3 × `[AllowAnonymous]` (:49,:65,:91), 2 × `[Authorize]`; `RemarksController.cs:28` `[Authorize]` | `RemarksController.cs:453` Export `Roles.Supervisor` |

**Ownership:** alle 6 `{planId}`-Controller tragen `PlanOwnershipFilter`, alle 11 `{childId}`-Controller
(bzw. bei `ShopController` die 7 Actions einzeln, Zeilen 310/334/355/376/397/411) den
`ChildOwnershipFilter`. Beide Filter sind No-ops ohne den Routen-Parameter
([ChildOwnershipFilter.cs:18](../backend/Pugling.Api/Auth/ChildOwnershipFilter.cs),
[PlanOwnershipFilter.cs:20](../backend/Pugling.Api/Auth/PlanOwnershipFilter.cs)), die Regel hält also
mechanisch (`ConventionGuardTests.cs:163`, Ausnahmeliste **leer**, Zeile 203). Die drei Endpunkte, die
`childId` im **Body** statt in der Route tragen, prüfen inline: `StudyPlansController.cs:85`,
`TagsController.cs:65`, `KlassenarbeitenController.cs:99`. Der Kontext-Mitschnitt der Anmerkungen prüft
sogar jede einzelne Referenz (`RemarksController.cs:187–197`). **Hier ist keine Lücke.**

## Bewertung je Dimension

### 1. Ressourcenschnitt und URI-Konsistenz — gut, mit vier messbaren Kanten

Pluralisierung ist durchgehend (`subjects`, `chapters`, `study-plans`, `class-tests`,
`practice-sessions`), Bindestriche statt camelCase, keine Datei-Endungen, Aktions-Segmente nur da, wo es
wirklich keine Ressource gibt (`/purchase`, `/equip`, `/approve`, `/submit`, `/heartbeat`) – RPC-Verben in
URIs sind hier bewusste Ausnahmen für Zustandsübergänge und in der Minderheit.

**Verschachtelungstiefe** ist der Preis: zwei Pfade liegen bei 11 Segmenten hinter `api/v1`
(`…/practice-sessions/{sessionId}/cards/{itemIndex}/image/reshuffle`,
`…/learn/subjects/{}/chapters/{}/vocabulary/{}/items`), einer bei 10; das Histogramm hat seinen Median
bei 4–5. Die tiefen Pfade sind fachlich begründet (die Karte gehört zur Sitzung), aber ein Client baut
sie nicht mehr fehlerfrei aus dem Kopf.

**Pfad-Parameter-Namen sind für dieselbe Sammlung nicht eindeutig** (aus dem Dokument ausgezählt):

| Sammlung | Parameter | Woher |
|---|---|---|
| `exercises` | `{id}` **und** `{exerciseId}` | `ExerciseCatalogController.cs:19` + `ExercisePreviewController.cs:23` vs. `ExerciseGrantsController.cs:18` + `ExerciseMediaController.cs:25` |
| `media` | `{id}`, `{assetId}`, `{linkId}` | `MediaAssetsController.cs:23` vs. `MediaVariantsController.cs:20` vs. `ExerciseMediaController.cs:62` |
| `vocabulary` | `{id}`, `{vocabularyId}`, `{exerciseId}` | `VocabularyStoreController.cs:21` vs. `VocabularyTagsController.cs:90` vs. `ExerciseControllers.cs:26` |
| `tags` | `{id}` **und** `{tagId}` | `VocabularyTagsController.cs:53` vs. `TagsController.cs:84` |

Acht Sammlungen benutzen das nackte `{id}`, über dreißig einen sprechenden Namen. Das ist nicht kosmetisch:
die Namen landen in `CreatedAtAction`-Routenwerten und in jedem generierten Client.

**Zwei Ressourcen heißen „tags" und beide verlinken Vokabeln.** `creator/tags` ist kind-skopiert
(`TagsController.cs:11–14`, Entity `Tag`, Klassenarbeits-Relevanz) mit
`creator/tags/{tagId}/vocabulary` (:343); `creator/vocabulary/tags` ist global
(`VocabularyTagsController.cs:11–15`, Entity `VocabTag`) mit `creator/vocabulary/{id}/tags` (:90).
Dazu kommt noch `creator/interest-tags` als dritte Taxonomie. Beide Vokabel-Tag-Wege unterscheiden sich
in der URI nur durch die Verschachtelung – aus dem Pfad allein ist nicht ablesbar, welche Taxonomie man
anspricht. Die Trennung ist im Code begründet, die **Benennung** trägt sie nicht.

**Literale neben `{id}` unter derselben Sammlung**: `children/daily-overview` neben `children/{childId}`,
`class-tests/repeat` neben `class-tests/{id}`, `vocabulary/batch|lookup|tags` neben `vocabulary/{id}`,
`media/upload`, `profiles/match`, `remarks/export`. Alle sind durch `:int`-Constraints kollisionsfrei –
funktional in Ordnung, aber jeder weitere Fall lebt von diesem Constraint.

### 2. Verb- und Statuscode-Semantik — sehr konsistent

- **DELETE**: 54 Operationen, **alle** mit 204 als Erfolg (aus dem Dokument geprüft) – kein einziges 200-mit-Body.
- **PATCH**: 31 Operationen, **alle** 200 mit der aktualisierten Repräsentation.
- **PUT**: 15 Operationen, alle 200, keine erzeugt (kein Upsert-201). Bei den Übungen ist PUT echtes
  Voll-Ersetzen (`ExerciseControllerBase.cs:298`), bei `children/{}/interests` Kollektions-Ersetzen
  (`ChildInterestsController.cs:47`) – beides korrekte PUT-Semantik.
- **201 mit `Location`**: 42 `CreatedAtAction`-Aufrufe, kein handgeschriebenes `Created(...)`. Alle 52
  201-Antworten tragen ein Content-Schema (der ApiExplorer leitet es aus `ActionResult<T>` ab) – die bare
  `[ProducesResponseType(Status201Created)]` schadet hier also nicht.
- **Drei idempotente Upserts** antworten korrekt 200-oder-201: `creator/interest-tags`,
  `creator/textbook-series`, `creator/vocabulary/tags`.
- **`POST …/review` antwortet 200 **oder** 204** – und das ist begründet: im `Info`-Modus gibt es kein
  Feedback (`PositionPracticeController.cs:276,295`). Sauber dokumentiert.

Die Kanten: sechs 201-Antworten zeigen mit `Location` auf die **Kollektion**, nicht auf die neue Ressource
(`ExerciseGrantsController.cs:78`, `MediaVariantsController.cs:72`, `VocabularyMediaController.cs:48`,
`MissionsController.cs:56,135`, `TimetableController.cs:55`, `ChildrenController.cs:172`). Das ist
zulässig, weil es dort kein Einzel-GET gibt – aber es heißt auch: **diese Ressourcen sind nach dem
Anlegen nicht adressierbar.** Und `TagsController.cs:80` zeigt auf `GetExercises` – die Liste der Übungen
eines Tags, nicht auf den Tag.

Weiterhin: `RemarksController.cs:222,416` gibt `version = "1.0"` explizit in die Routenwerte, alle 40
anderen `CreatedAtAction`-Aufrufe verlassen sich auf den Umgebungswert. Eine der beiden Varianten ist
überflüssig; welche, ist aus dem Code nicht belegbar (**nicht verifiziert**, warum es dort nötig war).

### 3. Idempotenz — bei Links vorbildlich, bei Geld gar nicht

Die Link-Endpunkte prüfen vor dem Einfügen und legen nicht doppelt an:
`ExerciseGrantsController.cs:66`, `ChildrenController.cs:167`, `VocabularyTagsController.cs:106`,
`ChildInterestsController.cs:82` (als PUT). Der Vokabel-Batch ist ausdrücklich wiederholbar
(`VocabularyStoreController.cs:456–458`), und die Punkte-/Malus-Verrechnung ist über eigene
Idempotenz-Tabellen abgesichert (`PositionGoalReward`/`PositionGoalPenalty`, Unique-Indizes
`PuglingDbContext.cs:586,595`).

**Was fehlt:** kein einziger Endpunkt akzeptiert einen `Idempotency-Key`
(Volltextsuche über `Pugling.Api`: nur Kommentare, kein Header-Handling). Betroffen sind genau die
Aufrufe, bei denen eine Wiederholung nach Netzwerk-Timeout Geld kostet:
`POST student/me/shop/listings/{listingId}/purchase` (`MeController.cs:261`),
`POST student/me/skins/{skinId}/purchase` (:165),
`POST supervisor/children/{childId}/points` (`ChildrenController.cs:225`),
`POST student/me/shop/inventory/{articleId}/activate` (:325).
Der `ConcurrencyStamp`-Bump (`MeController.cs:197,222`) schützt gegen **parallele** Käufe, nicht gegen den
**wiederholten** – zwei aufeinanderfolgende Requests sind für den Server zwei Käufe.

### 4. Paging, Sortierung, Filter — gute Bausteine, uneinheitlich angewandt

Die Bausteine stimmen: `PagingExtensions.ToPagedListAsync` setzt `X-Total-Count` **vor** dem Body,
klemmt `take` auf 0..500 und `skip` auf ≥0 ([PagingExtensions.cs:21–26](../backend/Pugling.Api/Controllers/PagingExtensions.cs));
`SortingExtensions.ParseSort` beherrscht beide Notationen (`?sort=-title` und `?sort=title&dir=desc`) und
verzichtet bewusst auf dynamischen Property-Zugriff ([SortingExtensions.cs:12](../backend/Pugling.Api/Controllers/SortingExtensions.cs));
CORS gibt den Header frei (`Program.cs:244`).

Die Anwendung ist es nicht:

- **35 Array-liefernde GETs tragen kein `take`** (aus dem Dokument ausgezählt). Ein Teil ist von Natur
  begrenzt (`exercise-types`, `grants`, `supervisors`, `variants`, `media`, `cards` einer Sitzung) – aber
  darunter sind Listen, die über Jahre wachsen: `supervisor/study-plans` (`StudyPlansController.cs:43`),
  `supervisor/study-plans/{planId}/positions` (`PlanPositionsController.cs:46`), `creator/subjects`,
  `creator/subjects/{subjectId}/chapters`, `creator/tags`, `creator/vocabulary/tags`, `creator/profiles`,
  `student/study-plans`, `supervisor/children/{childId}/missions|achievements|timetable` und die beiden
  Verwendungs-Listen `creator/vocabulary/{id}/usage` / `creator/media/{id}/usage`, die über den **gesamten**
  Katalog laufen.
- **Eine stille Abschneidung ohne Vertrag**: `GET student/me/shop` liefert die letzten Käufe mit
  hartem `.Take(50)` (`MeController.cs:414`) – kein `X-Total-Count`, kein `skip`, kein Weg an die
  älteren Zeilen. Der Client kann nicht unterscheiden, ob es 50 oder 500 Käufe gab.
- **Asymmetrisches Paging**: `GET remarks/export` nimmt `take`, aber kein `skip`.
- **Drei Namen für „nur meine"**: `mine` (`remarks`), `mineOnly` (`creator/profiles`,
  `creator/exercises`, `creator/textbook-series`), `isOwn`/`isOwner` (die 12 Übungs-Listen,
  `ExerciseControllerBase.cs:158–159`). Fachlich sind `isOwn` (Schreibrecht) und `isOwner`
  (Verwaltungsrecht) berechtigt getrennt – `mine` vs. `mineOnly` ist reine Uneinheitlichkeit.
- Sortierung gibt es an **9** von 138 GETs. Wo sie fehlt, ist die Ordnung serverseitig festgelegt
  (deterministisch, also fensterfähig) – aber die Oberfläche kann sie nicht ändern.
- Nur Offset-Paging, kein Cursor. Für den Datenumfang dieser App vollkommen ausreichend; der Hinweis
  gehört nur der Vollständigkeit halber hierher.

### 5. Fehlerformat und Code-Katalog — die stärkste Dimension, mit zwei Rissen

`ApiErrors` hält **57** Codes, jeder mit kanonischem Status und `/// <summary>`
([ApiErrors.cs](../backend/Pugling.Api/Errors/ApiErrors.cs)). **Kein toter Code**: die vier, die kein
Controller nennt (`bad_request`, `rate_limited`, `internal_error`, `http_error`), gehen über
`ForStatus(int)` (:183) als Netz für Framework-Antworten raus und stehen darum zu Recht in `AllCodes`.
Der Katalog fließt als `enum` ins `ProblemDetails`-Schema (`Program.cs:389–408`) und ein Drift-Test hält
beide Seiten deckungsgleich (`ErrorCodeTests.cs:151`). **Alle 485 4xx-Antworten im Dokument tragen ein
Schema.** Ein Quelltext-Wächter verbietet rohes `BadRequest(`/`Problem(`
(`ConventionGuardTests.cs:31`, Untergrenze 100 `ProblemWithCode`-Treffer als Selbstschutz).

**Riss 1 – derselbe Sachverhalt, zwei Codes.** `AuthController.cs:174` meldet eine schon belegte
E-Mail mit dem **generischen** `ApiErrors.Conflict`, während `ApiErrors.DuplicateEmail`
(`ApiErrors.cs:103`) existiert und in `AdultsController.cs:57,78` für genau denselben Fall benutzt wird.
Ein Client, der auf `duplicate_email` verzweigt, verpasst den `PATCH auth/me`-Pfad. Dasselbe Muster in
`ExerciseCategoriesController.cs:66,92` („Kategorie existiert schon im Fach" → generisches `conflict`),
obwohl der analoge Kapitel-Fall einen eigenen Code hat (`DuplicateChapterName`, `ApiErrors.cs:73`).

**Riss 2 – ein freier String, wo ein Code hingehört.** `BatchItemResult.Status`
([VocabularyStoreDtos.cs:58](../backend/Pugling.Contracts/Creator/VocabularyStoreDtos.cs)) ist ein
`string`, gefüllt mit `"created"`, `"existing"`, `"error"` (`VocabularyStoreController.cs:475,480,483`)
bzw. `"updated"`, `"not-found"` (:507–509). Das ist der einzige Ort in der API, wo ein
maschinenlesbarer Zustand **nicht** als Enum im Schema steht – ausgerechnet in der Antwort, die ein
Agent auswerten soll. Zusätzlich: das Batch antwortet immer 200, auch wenn jedes Element fehlschlug, und
liefert keine Zusammenfassung.

### 6. PATCH/PUT-Semantik und `Clear`-Schalter — Regel sitzt, eine Lücke pro Ressource

15 `Clear…`-Schalter über 10 DTOs, alle im Vertragsprojekt; `PatchSemanticsTests` prüft reflexiv, dass
**jeder** Schalter einen Fall in der Tabelle hat. Das „erst Wert, dann Schalter"-Muster ist eingehalten
(vorbildlich kommentiert in `AuthController.cs:169–177`). `null` heißt überall „unverändert".

Ein realer Defekt in dieser Dimension: **PATCH prüft die Eindeutigkeit nicht, die POST prüft.**
`ChaptersController.Create` fängt den Namenskonflikt ab (:61–62, Code `DuplicateChapterName`),
`ChaptersController.Update` (:73–85) schreibt `chapter.Name` ohne jede Prüfung – der Unique-Index
`(SubjectId, Name)` (`PuglingDbContext.cs:412`) schlägt dann in der DB zu. Da es **keinen** globalen
`DbUpdateException`→409-Handler gibt (Volltextsuche: `DbUpdateException` nur in fünf Services, nie
zentral), wird daraus ein **500 `internal_error`**. `duplicate_chapter_name` ist mithin über PATCH
unerreichbar – genau der Fall, den [backend/Pugling.Api/CLAUDE.md](../backend/Pugling.Api/CLAUDE.md)
(„Neue Eindeutigkeit braucht immer eine Vorprüfung im Controller **plus** einen `ApiErrors`-Code – ohne
sie wird aus dem 409 ein 500") ausdrücklich verbietet.

Ich habe acht weitere PATCH-Pfade auf dasselbe Muster geprüft. Sauber sind
`MediaVariantsController` (prüft `(AssetId, Purpose, Format)` vor, :93–97, mit genau dieser Begründung im
Kommentar: „the unique index would be a 500"), `AdultsController` (E-Mail),
`VocabularyStoreController` (Key geprüft), `MediaAssetsController` (`UpdateMediaAssetDto`
enthält den Key nicht, `MediaDtos.cs:28–30`),
`InterestTagsController` (Slug ausdrücklich unveränderlich, :106–107),
`TextbookSeriesController` (Name änderbar, Slug bleibt), `VocabularyTagsController` (:65–66),
`ExerciseCategoriesController` (:92). **Ein zweiter Treffer:** Auszeichnungen. `Achievement` trägt einen
Unique-Index `(ChildId, Metric, Threshold)` (`PuglingDbContext.cs:716`), aber weder
`MissionsController.cs:115–136` (POST) noch :139–154 (PATCH) prüfen vor – und es gibt für diesen Fall
**gar keinen** `ApiErrors`-Code. Zweimal dieselbe Auszeichnung anlegen ⇒ 500.

### 7. DTO-Namensraum — makellos

221 Records in `Pugling.Contracts`, **null** doppelte einfache Typnamen (nachgezählt), und genau das
hält `ConventionGuardTests.cs:72` mechanisch mit Untergrenze 200 als Selbstschutz. Ein zweiter Wächter
(:92) verbietet, dass eine Action einen Typ aus `Pugling.Api` zurückgibt. Die einzige Ausnahme ist
begründet und dokumentiert: `MediaUploadForm` bleibt in der API, weil `IFormFile` ein
ASP.NET-Core-Typ ist und `Pugling.Contracts` ein referenzfreies Blatt bleiben soll
([MediaUploadForm.cs:5–9](../backend/Pugling.Api/Controllers/Creator/MediaUploadForm.cs)).
Die generischen `ExerciseResponse<TConfig>`/`ExercisePayload<TConfig>` erzeugen pro Übungstyp ein eigenes
Schema – deshalb 295 Schemas bei 221 Records.

Eine inhaltliche Falle im Namensraum: `VocabularyResponse.Version` /
`UpdateVocabularyDto.Version` (`VocabularyStoreDtos.cs:11,40`) sieht aus wie ein Concurrency-Token, ist
aber ein **client-gesetzter Freitext** mit Vorgabe `"1.0"` (`VocabularyStoreController.cs:240,297`), den
der Server nie prüft. Wer eine Client-Bibliothek generiert, hält das für ETag-Ersatz.

### 8. Auth-/Ownership-Konsistenz — lückenlos

Siehe die Tabelle in der Bestandsaufnahme. Bemerkenswert ist der zusätzliche Wächter
`Actions_Mit_Loesungsfeld_Sind_Vor_Dem_Studenten_Gegated` (`ConventionGuardTests.cs:208`): er urteilt
über den **Typgraphen** der Antwort, nicht über den Ordner, umfasst bewusst auch die geerbten
Übungs-Actions (:235) und hält seine fünf Ausnahmen gegen die Realität (:269 – eine Ausnahme, die
nichts mehr trifft, ist selbst rot). Das ist ein Auth-Muster, das über Rollenprüfung hinausgeht.

Eine Namensfrage, kein Loch: `creator/tags` ist eine **kind-skopierte** Ressource
(`?childId=`, Ownership über `AuthAccess`, Schreiben auch durch den Studenten) und liegt trotzdem auf
dem Creator-Präfix. Nach der eigenen Regel in `ApiRoutes.cs:15–18` ist „dual" nur dann kein Vorwand,
solange wirklich beide Ebenen rufen – das ist hier gegeben, die Einordnung bleibt aber die unnatürlichste
im ganzen Vertrag.

### 9. OpenAPI-Qualität — hoch, drei konkrete Lücken

Stark: Bearer-Security-Scheme (`Program.cs:344`), Enums als String **mit** Wertelisten in der
Beschreibung (:291–306), `required` aus der Nullability **und** aus Vorgabewerten neu berechnet
(:307–336, inklusive notierter Grenze bei `ArithmeticProblem.Tolerance`), Fehlercode-`enum` im
`ProblemDetails` (:389), deterministische Tag-Reihenfolge nach Ebene (:364–386), abschaltbare Beispiele
für ein byte-stabiles Dokument (:280–286), `[Produces("application/json")]` an **jedem** der 47 Controller,
dazu `text/markdown` am Remarks-Export (`RemarksController.cs:454`) – 48 Treffer, keine Lücke. Das
eingecheckte Dokument ist damit ein diffbares Artefakt.

Die Lücken:

1. **24 Operationen haben keine `summary`** – und zwar genau die 12 `POST` und 12 `PUT` der
   Übungs-Controller. Die Doku-Kommentare **existieren**
   (`ExerciseControllerBase.cs:224,297`), werden für die geerbten Methoden aber nicht aufgelöst; bei
   `List`/`Get`/`Delete` derselben Basis kommen sie durch. Ergebnis: die 24 Operationen, mit denen man
   Übungen anlegt und ersetzt, sind im Swagger/Scalar unbeschriftet.
2. **`X-Total-Count` ist nirgends deklariert** – **0** Antworten im Dokument tragen `headers`, obwohl
   31 Endpunkte den Header senden. Er steht nur in Prosa in den `<param name="take">`-Texten. Ein
   generierter Client kennt ihn nicht.
3. **401 ist an 5 von 323 Operationen deklariert** (nur die fünf `auth/…`-Actions), obwohl 318
   Operationen ein Token brauchen. 403 steht an 64, 404 an 263. Der `ProducesResponseType`-Bestand ist
   handgepflegt und driftet: heuristisch gemessen können **24 von 268** Action-Blöcken einen
   Fehlerstatus liefern, den sie nicht deklarieren. Vier davon habe ich einzeln gelesen und bestätigt:
   `ChaptersController.cs:51–54` (deklariert 201/400/404, liefert 409 in :62),
   `PositionPracticeController.cs:279–281` (deklariert 200/204/404, liefert 403 in :290),
   `AuthController.cs:146–149` (deklariert 400/401/403/409, liefert 404 in :160),
   `ShopController` Aktivierungs-Genehmigung (deklariert 200/404/409, liefert 400).
   `ProducesResponseType<T>` wird **nirgends** benutzt (0 Treffer) – für die Fehlerfälle ist das
   folgenlos (die Factory setzt `ProblemDetails`), für die 200er kommt der Typ aus `ActionResult<T>`.
4. `ChildrenDashboardController` trägt **kein** `ProducesResponseType` (die Datei hat 26 Zeilen); der
   Antworttyp heißt schlicht `Dashboard` – der unspezifischste Schema-Name im ganzen Vertrag.

### 10. Caching / ETag / Conditional Requests — vollständig abwesend

Volltextsuche über `Pugling.Api` **und** `Pugling.Contracts` nach `ETag`, `If-Match`, `If-None-Match`,
`Cache-Control`, `Last-Modified`, `ResponseCache`: **null Treffer.** Konsequenzen:

- Es gibt keine bedingten Schreibzugriffe. Optimistische Nebenläufigkeit existiert intern
  (`Child.ConcurrencyStamp`, Bump in `MeController.cs:197,222`, `ShopController.cs:280`) und der Code
  `concurrency_conflict` ist registriert (`ApiErrors.cs:42`) – aber der Client hat **keinen** Weg, an
  seinem Stand teilzunehmen: kein ETag im GET, kein `If-Match` im PATCH. „Wer zuletzt schreibt, gewinnt"
  ist damit die Vertragslage für jede Vater-Bearbeitung.
- Das große Typ-Manifest (`creator/exercise-types`) und die Katalog-Listen sind nicht revalidierbar.
- Der Token-Endpunkt setzt kein `Cache-Control: no-store` (`AuthController.cs:48–61`).
- Die 429-Antwort trägt kein `Retry-After` (`Program.cs:250–258` setzt nur `RejectionStatusCode`, kein
  `OnRejected`) – der Client kann nur blind erneut versuchen.

### 11. Bulk-Operationen — genau eine Ressource, ansonsten Fehlanzeige

Vorhanden: `POST creator/vocabulary/batch` und `PATCH creator/vocabulary/batch`
(`VocabularyStoreController.cs:462,491`) mit Ergebnis je Element, `POST creator/vocabulary/lookup` (:419)
als Vorab-Dedup, `POST creator/…/vocabulary/{exerciseId}/refs-from-tags` (:94 in `ExerciseControllers.cs`),
und mehrwertige Verlinkungen (`tags/{tagId}/exercises`, `vocabulary/{id}/tags`, `media/{id}/tags`).
Für den KI-Creator ist das die richtige Auswahl.

Nicht vorhanden, wo es sich anbietet: kein Bulk-Anlegen von `PlanPositions` (ein Vater, der 20 Übungen
in einen Plan hängt, macht 20 POSTs), kein Bulk für `ExerciseItem`s über den POST der Übung hinaus, keine
Bulk-Umsortierung (`OrderIndex`/`Order` wird pro Zeile gepatcht). Das ist eine bewertende Beobachtung,
kein belegter Schmerz – ob es weh tut, sagt erst eine Messung am Frontend.

### 12. Versionierung und Developer Experience — solide

`ApiRoutes.V1` hält das Versionssegment an einer Stelle
([ApiRoutes.cs:10](../backend/Pugling.Api/Controllers/ApiRoutes.cs)), jeder Controller trägt
`[ApiVersion("1.0")]` (47 Treffer), das Dokument liegt eingecheckt und diffbar unter
`docs/openapi/v1.json`, `UnmappedMemberHandling.Disallow` verhindert stillen Datenverlust, und die
Beispiele unter `docs/api-examples/` sind test-verifiziert. Die drei Login-Türen
(`auth/adult`, `auth/child`, `auth/login`) sind laut Doku Absicht, bleiben für einen neuen Konsumenten
aber die erste Frage („welche nehme ich?").

## Priorisierte Verbesserungsvorschläge

### (a) Echte Inkonsistenzen im Vertrag

**A1 — Unique-Index ohne Vorprüfung: 500 statt 409.** ★★★
*Befund:* `ChaptersController.Update` (:73–85) schreibt den Namen ohne Prüfung gegen den Unique-Index
`(SubjectId, Name)` (`PuglingDbContext.cs:412`); `duplicate_chapter_name` ist über PATCH unerreichbar.
Zweiter Treffer: `Achievement (ChildId, Metric, Threshold)` (:716) ist in `MissionsController.cs:115`
(POST) **und** :139 (PATCH) ungeprüft und hat gar keinen Code. Kein globaler
`DbUpdateException`→409-Handler existiert.
*Vorschlag:* die Vorprüfung in `ChaptersController.Update` nachziehen (Muster:
`MediaVariantsController.cs:94`), einen Code `duplicate_achievement` additiv in `ApiErrors` ergänzen und
in beiden Achievement-Pfaden prüfen. Zusätzlich ein Tor: für jeden Unique-Index einen bekannten
Schreibpfad **mit** Vorprüfung fordern – die Liste steht schon in `PuglingDbContext` (47 `IsUnique`, davon
18 mit `HasFilter`; die ursprünglich hier stehende „41" war falsch, siehe Nachtrag), die
Zuordnung wird also messbar, nicht geraten. *Aufwand:* klein (2 Prüfungen + 1 Code), Tor mittel.
*Risiko:* das Tor wird zuerst rot und deckt vermutlich weitere Indizes auf – das ist der Zweck, kostet
aber eine Sitzung Nacharbeit.

**A2 — Idempotente Schreibpfade antworten mit erfundenen Werten.** ★★★
*Befund:* drei Stellen synthetisieren die Antwort statt zurückzulesen.
(i) `VocabularyTagsController.cs:44` liefert auf dem „existiert schon"-Pfad
`existing.Links.Count` – `Links` ist nie geladen und initialisiert leer
(`VocabEntities.cs:76`), Lazy Loading ist nirgends aktiv (0 Treffer für `UseLazyLoadingProxies`), also
steht dort **immer 0**, während `List` (:29) denselben Zähler korrekt per SQL berechnet.
(ii) `ExerciseGrantsController.cs:78–79` antwortet mit `User.AdultId()` und `DateTime.UtcNow`, auch wenn
der Grant längst von jemand anderem angelegt wurde.
(iii) `ChildrenController.cs:172–173` antwortet mit `dto.Relation` und `DateTime.UtcNow`, auch wenn die
bestehende Betreuung eine andere Verwandtschaft trägt.
Zusätzlich melden (ii) und (iii) **201 Created**, obwohl nichts entstand.
*Vorschlag:* auf allen drei Pfaden die gespeicherte Zeile lesen und projizieren; bei (ii)/(iii) auf
**200** umstellen, wenn nichts angelegt wurde (dasselbe Muster, das `creator/interest-tags` und
`creator/vocabulary/tags` schon fahren), oder – konsequenter – die Verlinkung auf `PUT
…/grants/{creatorId}/{permission}` bzw. `PUT …/supervisors/{supervisorId}` umbauen, denn genau das ist
PUT-Semantik. *Aufwand:* klein bis mittel. *Risiko:* die Statusänderung bricht Tests und Frontend-Pfade,
die auf 201 prüfen – in v1 zulässig und billig.

**A3 — Derselbe Sachverhalt, zwei Fehlercodes.** ★★
*Befund:* `AuthController.cs:174` benutzt `ApiErrors.Conflict`, wo `ApiErrors.DuplicateEmail`
(`ApiErrors.cs:103`, in `AdultsController.cs:57,78` benutzt) hingehört.
`ExerciseCategoriesController.cs:66,92` benutzt `Conflict`, wo der analoge Kapitel-Fall einen eigenen
Code hat.
*Vorschlag:* `DuplicateEmail` in `AuthController` einsetzen; einen Code `duplicate_category_name`
additiv ergänzen und in `ExerciseCategoriesController` einsetzen. Danach die generischen `Conflict`/
`BadRequest`/`NotFound` als **nur für Framework-Pfade** kennzeichnen und ein Tor ziehen, das ihre
Nutzung in Controllern verbietet (aktuell 3 bzw. 4 Stellen – die Untergrenze ist also greifbar).
*Aufwand:* klein. *Risiko:* keiner; rein additiv am Katalog.

**A4 — `BatchItemResult.Status` ist ein freier String.** ★★
*Befund:* `VocabularyStoreDtos.cs:58`; Werte in `VocabularyStoreController.cs:475,480,483,507–509`.
Der einzige maschinenlesbare Zustand der API ohne Enum im Schema – ausgerechnet in der Antwort für den
KI-Creator.
*Vorschlag:* Enum `BatchItemStatus { Created, Existing, Updated, NotFound, Error }` in `Contracts`; der
Enum-Transformer in `Program.cs:291` schreibt die Werte dann automatisch ins Schema. Zusätzlich einen
Zähl-Kopf oder ein Summen-Objekt, damit ein Aufrufer nicht 500 Elemente durchzählen muss.
*Aufwand:* klein. *Risiko:* Breaking Change am Batch-Vertrag (Client + Agent + Tests).

**A5 — Pfad-Parameter-Namen pro Sammlung vereinheitlichen.** ★★
*Befund:* `exercises` → `{id}`+`{exerciseId}`, `media` → `{id}`+`{assetId}`+`{linkId}`,
`vocabulary` → `{id}`+`{vocabularyId}`+`{exerciseId}`, `tags` → `{id}`+`{tagId}` (Stellen in Dimension 1).
*Vorschlag:* eine Regel („die Sammlung im Singular + `Id`", also `exerciseId`, `mediaAssetId`,
`vocabularyId`, `tagId`), das nackte `{id}` überall ersetzen, und ein Tor: über alle Routen darf pro
Sammlungssegment nur **ein** Platzhaltername folgen. Das Tor ist zehn Zeilen über `ApiSurface.RouteOf`
und fängt jede künftige Abweichung.
*Aufwand:* mittel (8 Sammlungen × Routen + `CreatedAtAction`-Routenwerte + Frontend/Client).
*Risiko:* mechanisch, aber breit; jede vergessene `CreatedAtAction`-Routenwert-Zeile macht den
`Location`-Header leer statt falsch – am besten mit dem Tor zusammen in einem Zug.

**A6 — Zwei Ressourcen namens „tags" mit derselben Verlinkung.** ★
*Befund:* `creator/tags/{tagId}/vocabulary` (kind-skopiert, `TagsController.cs:343`) vs.
`creator/vocabulary/{id}/tags` (global, `VocabularyTagsController.cs:90`), dazu `creator/interest-tags`.
*Vorschlag:* die globale Vokabel-Taxonomie im Pfad benennen, was sie ist – z. B.
`creator/vocabulary-tags` (+ `creator/vocabulary/{id}/vocabulary-tags`) – und die kind-skopierte
`creator/child-tags` oder `supervisor/children/{childId}/tags`. Dann sagt jede URI, welche Taxonomie
gemeint ist. *Aufwand:* mittel. *Risiko:* Frontend-Routen und Tutorials ziehen mit; rein
Umbenennung, keine Logik.

### (b) Fehlende Bausteine

**B1 — Idempotenz-Schlüssel für geldbewegende POSTs.** ★★★
*Befund:* kein `Idempotency-Key` in der ganzen API; betroffen `MeController.cs:165,261,325` und
`ChildrenController.cs:225`. Der `ConcurrencyStamp` deckt Parallelität, nicht Wiederholung.
*Vorschlag:* Header `Idempotency-Key` auf diesen vier Endpunkten, eine Tabelle
`(AccountId, Key)` → gespeicherte Antwort, Wiederholung liefert die erste Antwort erneut. Ein neuer Code
`idempotency_key_reused` für „gleicher Schlüssel, anderer Body".
*Aufwand:* mittel (eine Tabelle, ein Filter, vier Endpunkte). *Risiko:* eine neue Tabelle bedeutet
Migration neu falten und die Schema-Tore G1–G9 bewusst bedienen.

**B2 — ETag / `If-Match` für die Bearbeitungspfade.** ★★
*Befund:* 0 Treffer für ETag/If-Match; `concurrency_conflict` existiert, ist für einen Client aber nicht
erreichbar.
*Vorschlag:* dort, wo schon ein `ConcurrencyStamp` liegt, ihn als `ETag` im GET ausgeben und `If-Match`
im PATCH auswerten → 412 bzw. der vorhandene 409-Code. Ausdrücklich **nicht** flächendeckend: nur die
Ressourcen, die zwei Erwachsene gleichzeitig bearbeiten (Kind, Plan, Position, Shop-Angebot).
*Aufwand:* mittel. *Risiko:* halbe Einführung ist schlimmer als keine – ein Client, der ETags mal
bekommt und mal nicht, baut die Prüfung nicht ein. Also mit einer festen Liste und einem Tor.

**B3 — Paging auf die wachsenden Listen ziehen.** ★★
*Befund:* 35 Array-GETs ohne `take`, davon rund ein Dutzend unbegrenzt wachsend (Liste in Dimension 4),
plus die stille `.Take(50)`-Abschneidung in `MeController.cs:414`.
*Vorschlag:* `ToPagedListAsync` an den wachsenden Endpunkten nachziehen; für
`GET student/me/shop` die Käufe aus der Sammel-Antwort **herausnehmen** und auf den bereits paginierten
`student/me/shop/purchases`-Weg legen, statt sie abzuschneiden. Danach ein Tor: jeder Array-liefernde GET
hat entweder `take` oder steht mit Begründung auf einer Ausnahmeliste (Manifest, Session-Karten,
`{id}/forms` …). *Aufwand:* mittel. *Risiko:* Frontend-Listen, die heute alles auf einmal bekommen,
brauchen den Pager – `ListControls` existiert dafür schon.

**B4 — `X-Total-Count` und 401 im OpenAPI deklarieren.** ★★
*Befund:* 0 Antworten mit `headers`, 401 an 5 von 323 Operationen.
*Vorschlag:* ein Operation-Transformer (neben den vier vorhandenen in `Program.cs`), der bei jeder
Operation mit `skip`/`take`-Parametern den Antwort-Header `X-Total-Count` einträgt, und einer, der jeder
nicht-`[AllowAnonymous]`-Operation 401 (und bei rollen-gegateten 403) mit `ProblemDetails` hinzufügt.
Das ist genau der Weg, den die Datei schon für den Fehlercode-`enum` geht – und es nimmt der
handgepflegten `ProducesResponseType`-Liste die langweilige Hälfte ab.
*Aufwand:* klein. *Risiko:* das eingecheckte `v1.json` wächst und der Contract-Test muss einmal neu
abgenommen werden.

**B5 — Die 24 unbeschrifteten Übungs-Operationen beschriften.** ★★
*Befund:* genau die 12 POST und 12 PUT der Übungs-Controller ohne `summary`, obwohl die Doku-Kommentare
in `ExerciseControllerBase.cs:224,297` existieren.
*Vorschlag:* zuerst herausfinden, warum die Auflösung bei `Create`/`Update` scheitert und bei
`List`/`Get`/`Delete` gelingt (Verdacht: der generische Parametertyp `ExercisePayload<TConfig>` im
Doc-Schlüssel – **nicht verifiziert**). Fällt das flach, greift der Notausgang: `[EndpointSummary]` in
den konkreten Controllern, was die Beschreibung ohnehin typ-spezifisch macht („Legt eine
Vokabelübung an"). Danach ein Tor: keine Operation ohne `summary`.
*Aufwand:* klein bis mittel. *Risiko:* keins.

**B6 — `Retry-After` und `Cache-Control: no-store`.** ★
*Befund:* `Program.cs:250–258` ohne `OnRejected`; `AuthController.cs:48–61` ohne `no-store`.
*Vorschlag:* `OnRejected` setzt `Retry-After` aus dem Fenster; die drei Login-Actions und
`GET auth/me` setzen `Cache-Control: no-store`.
*Aufwand:* winzig. *Risiko:* keins.

**B7 — Bulk für Plan-Positionen.** ★
*Befund:* kein Bulk-POST auf `supervisor/study-plans/{planId}/positions`, keine Bulk-Umsortierung.
*Vorschlag:* erst am Frontend messen, wie viele Positionen ein Vater typischerweise in einem Rutsch
setzt. Erst wenn die Zahl > 5 ist, ein `POST …/positions/batch` im Muster des Vokabel-Batches (mit dem
Enum aus A4). *Aufwand:* mittel. *Risiko:* verfrüht gebaut ist es eine zweite Schreibsemantik ohne
Nutzer – deshalb ausdrücklich hinter einer Messung.

### (c) Geschmacksfragen (bewusst so bewertet, nicht als Mangel)

**C1 — `{id}`-Nachbarn wie `children/daily-overview`, `class-tests/repeat`, `vocabulary/batch`.**
Funktioniert wegen der `:int`-Constraints. Wer es aufräumen will, hängt Aggregate an ein eigenes Segment
(`supervisor/dashboards/daily`, `supervisor/class-tests/suggestions`). Kein Handlungsdruck.

**C2 — Drei Login-Türen.** `auth/adult`, `auth/child`, `auth/login` – laut Doku Absicht, und
`AuthController.cs:42–46` begründet den `adult`-Namen sorgfältig. Ein einziges `auth/login` mit
explizitem Subjekt-Typ wäre der schlankere Vertrag; die Redundanz kostet aber nichts außer der ersten
Frage eines neuen Konsumenten.

**C3 — `POST creator/vocabulary/lookup` als Lesevorgang.** Ein POST, der nur liest (:419). Für eine
Wortliste im Body ist das die pragmatisch richtige Wahl (GET-Query-Länge), verhindert aber jedes Caching.
Nur relevant, wenn B2 kommt.

**C4 — 55 Actions nehmen `CancellationToken ct` ohne `= default`.** Aus 268 Action-Blöcken gemessen;
funktional folgenlos (MVC bindet den Token ohnehin), aber die Konvention in `CLAUDE.md` verlangt den
Vorgabewert, und der Wächter `ConventionGuardTests.cs:123` prüft nur die **Anwesenheit** des Parameters
(:148). Drei Actions haben gar keinen Token: `AuthController.Me` und die beiden
`ExerciseTypesController`-GETs – alle drei synchron, also korrekt. Wenn man die Regel wörtlich haben
will, ist es eine mechanische Sitzung plus eine Zeile im Wächter.

**C5 — `Dashboard` als Schema-Name** (`ChildrenDashboardController.cs:21`). Global eindeutig, also vom
Wächter gedeckt – in einem generierten Client aber der nichtssagendste Typ. `DailyOverviewResponse` wäre
sprechend.

**C6 — Doppelte Plan-Ladung.** `PlanOwnershipFilter.cs:23` lädt den Plan, `StudyPlansController.cs:69`
lädt ihn erneut. Kein Vertragsthema, nur eine Query pro Request.

### Zwei Nebenbefunde außerhalb des API-Designs

- **Deutscher Block-Kommentar im Code:** `ExerciseControllerBase.cs:354–359` ist deutsch, obwohl
  „Code-Doku auf Englisch – ausnahmslos" auch für `//` gilt.
- **Stale Doku-Zahl:** `ApiSurface.cs:28` spricht von „den dreizehn Übungs-Controllern"; es sind **12**
  (`ExerciseControllerBase<`-Ableitungen ausgezählt). Die Zahl steht nur im Kommentar, nicht in einer
  Zusicherung – aber sie ist falsch.
