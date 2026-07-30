---
tags: [bereich/architektur, bereich/datenmodell, status/laufend]
---

# DB-/EF-Struktur-Umbau

> **Übergabe-Dokument.** E0–E5 sind umgesetzt und verifiziert, E6–E14 offen. Dieses Dokument ist so
> geschrieben, dass jemand ohne Vorwissen die restlichen Etappen zu Ende führen kann: es nennt die
> getroffenen Entscheidungen, die Arbeitsregeln, die Belege, die bewussten Abweichungen und die
> Fallstricke, die beim Umsetzen Zeit gekostet haben.

## Warum

Das Datenmodell ist in 25 Tagen über **48 Migrationen** auf **62 Tabellen** gewachsen. Die Kette war
driftfrei – das Problem war nicht Drift, sondern **fehlende Regel**: 12 Enums lagen als String in der DB
und ~20 als int (in `Remarks` sogar beides in derselben Tabelle), kein einziges `HasMaxLength` existierte,
8 Fremdschlüssel verließen sich auf Konventions-Cascade, 14 Spalten sahen wie Fremdschlüssel aus und
waren keine, und `Subjects`/`Adults` hatten außer dem Primärschlüssel überhaupt keinen Index.

Dazu zwei echte Defekte, von denen einer noch offen ist:
1. **Löschen eines Supervisors vernichtet bezahltes Kind-Inventar** (`Adult→ShopArticle` Cascade →
   `ShopArticle→ChildInventory` Cascade, während die Kaufbelege per SetNull stehenbleiben) → **E6**, offen.
2. **PATCH auf Name/E-Mail eines Erwachsenen zieht das Konto nicht nach**, obwohl der gefilterte
   Unique-Index dort sitzt und die Kollisionsprüfung gegen den veralteten Wert läuft → **E8**, offen.

Altdaten sind ausdrücklich verzichtbar: die DB darf gelöscht und über den Seed neu gebaut werden. Genau
diese Freiheit trägt den Umbau – siehe „Arbeitsregeln".

## Getroffene Entscheidungen (nicht neu verhandeln)

| Frage | Entscheidung |
|---|---|
| Umfang | **Nur Struktur, kein API-Vertragsbruch** – mit *einer* bewussten Ausnahme: `LearnGoal` wird gelöscht (E13). |
| Ziel-Systeme | **`LearnGoal` löschen**, `Objective`/`KeyResult` ist der Superset. Bedingung: `POST objectives` nimmt KeyResults inline an. |
| Migrationskette | **Dauerhaft bei 1** – vor jedem Etappenabschluss neu falten. Tor G1b nagelt es fest. |
| `TimeSlotRule` | **In Konfiguration auflösen** – Tabelle weg, `Scoring:TimeSlots` in appsettings (E12). |

**Bewusst ausgeklammert** (jeweils mit Begründung, nicht aus Vergessen): die zwei Leitner-Boxen
entwirren (`PositionItemProgress` vs. `ItemProgress` – Verhaltensumbau, eigener Plan),
`Adult.Pin`/`Child.Pin`/`Adult.Email` abschaffen (Login-Konsolidierung, eigener Plan),
`Klassenarbeit*` → `ClassTest*`, `Exercise.RewardPoints`, `Exercise.AuthorAdultId`,
Stufen typisieren, die 6 Adult-FK-Spaltennamen vereinheitlichen (4 von 6 sind Rollennamen und damit
informativer), `SchoolTypes` behält die deutschen Werte (Eigennamen ohne korrekte Übersetzung).
Die Langfassung steht in der Plandatei dieser Sitzung.

## Arbeitsregeln

1. **Jede Etappe endet grün** (`dotnet test Pugling.sln -c Release`, ~55 s). Rot ist nie der
   Übergabezustand einer Etappe.
2. **Die Migrationskette bleibt bei genau 1.** Am Ende jeder Etappe mit Schemaänderung:
   ```bash
   rm -rf backend/Pugling.Api/Data/Migrations
   dotnet dotnet-ef migrations add InitialCreate --project backend/Pugling.Api --output-dir Data/Migrations
   ```
   Das macht Spaltenumbenennungen und Typwechsel kostenlos – kein generierter SQLite-Tabellen-Neubau, den
   jemand abnehmen muss. `dotnet-ef` ist auf 10.0.9 gepinnt (`.config/dotnet-tools.json`), vorher einmal
   `dotnet tool restore`. Tor **G1b** (`SchemaGuardTests`) schlägt fehl, wenn es mehr als eine Migration
   gibt – die Regel endet bewusst mit der ersten Veröffentlichung, und dann wird das Tor *ausdrücklich*
   entfernt statt zu erodieren.
3. **Die Reihenfolge in `Seed.Run` ist eingefroren.** Neue Seed-Routinen kommen ans Ende. Die Seed-IDs
   sind außerhalb des Testlaufs hart verdrahtet: `frontend/playwright.config.ts`, `frontend/e2e/*.spec.ts`,
   `.claude/scripts/tutorial-api.sh`, `.claude/skills/{creator,supervisor,student,anmerkungen}/SKILL.md`
   und die eingecheckten `docs/api-examples/`. `SeedContractTests` fängt eine Verschiebung in 60 s ab,
   statt Playwright nachts.
4. **`docs/api-examples/` im gleichen Commit neu erzeugen**, wenn eine Etappe die Antwortform ändert –
   `DocsCaptureTests` schreibt die Dateien beim Testlauf, CI-Tor D4 prüft danach Byte-Stabilität.
5. **Numerische Ratschen mitziehen:** `EndpointCoverageGuard.FullRunTouchedActions` (aktuell **268**,
   sinkt in E13 um die entfallenden LearnGoal-Actions) und `ConventionGuardTests` (`types.Count >= 200`,
   in E13 prüfen).
6. **Jedes neue Tor braucht eine Falsch-Grün-Probe:** die Regel lokal brechen, das Tor rot sehen,
   zurücknehmen, nicht committen. Ein Wächter, der nie rot war, ist kein Wächter.

## Umgesetzt: E0–E5

### E0 · Netz spannen — keine Migration

- **neu** `backend/Pugling.Api.Tests/SchemaGuardTests.cs` — Tore **G1** (kein Modell-Drift via
  `HasPendingModelChanges`), **G1b** (Kette == 1), **G4** (Enums als String), **G9** (nur bewusste
  DB-Defaults). Braucht keinen Host und keine DB: Modell und Snapshot liegen in der Assembly.
- **neu** `backend/Pugling.Api.Tests/SeedContractTests.cs` — pinnt Login `adultId=1/0000` („Papa"),
  `adultId=2/9999` (Lehrer, E-Mail über die DB geprüft), `childId=1/1111` („Sohn"), Fach 1 = „Englisch".
- **geändert** `QueryPlanSmokeTests.cs` — Fixture von 12 Roh-`INSERT`s auf einen **EF-Graphen** umgebaut.
  Das war der Squash-Blocker: die Roh-Inserts liefen nur über DEFAULT-Klauseln, die kein Mensch beabsichtigt
  hatte. Über den Graphen hält der Compiler die Fixture am Schema fest.

### E1 · Squash: 48 Migrationen → 1

Aktuelle Migration: `20260730123746_InitialCreate`. Der Squash ist **bewiesen**, nicht bloß „läuft":

| Prüfung | Ergebnis |
|---|---|
| `git diff` am `PuglingDbContextModelSnapshot.cs` | **leer** → Modell identisch |
| Tabellen / Spalten (Typ, NotNull, PK) | 62 / 518 **identisch** |
| Indizes | 115 **identisch** |
| Fremdschlüssel **inkl. ON DELETE** | 95 **identisch** |
| Einziger Unterschied | **15** zufällige DEFAULT-Klauseln, alle entfallen |

Verglichen wurde **mengenbasiert** (PRAGMA `table_info`/`index_list`/`foreign_key_list`), nicht per
Textdiff: die alte Kette hat Spalten per `ALTER TABLE` angehängt, die Spalten*reihenfolge* unterscheidet
sich also und ein Textdiff ist unlesbar. Die Alt-DB wurde dafür in einem `git worktree` auf HEAD gebaut.

**Korrektur zur Bestandsaufnahme:** es waren **15** zufällige DEFAULTs, nicht 35. Die 35 waren die
`AddColumn(defaultValue:…)`-Aufrufe; die meisten hatten spätere SQLite-Tabellen-Neubauten schon
eingesammelt. Tor **G9** hält fest, dass genau einer übrig ist (`Exercises.ExecutePublic`, ein bewusster
Fail-Safe).

**Historien-Guard** in `Program.cs` (vor `MigrateAsync`): eine DB mit ausschließlich unbekannten
angewandten Migrationen wirft mit handlungsfähiger Meldung, statt Azure `table "Adults" already exists`
sagen zu lassen. Verifiziert gegen die Alt-Ketten-DB.

**Nebeneffekt:** der Testlauf fiel von 61 s auf 31 s – 70 Testklassen migrieren je eine eigene
SQLite-Datei, also vorher 3 360 Migrationsanwendungen, jetzt 70.

### E2 · Toter Ballast

Entfernt: `ReviewEvent.ContentId/.ItemIndex/.StageValue` (write-only), `TestItemResult.ContentId`
(write-only) und `.GapIndex` (nie geschrieben, nie gelesen), `ExerciseTag.TaggedByRole`,
`VocabularyTag.TaggedByRole`, die `DbSet<TestItemResult>`-Property, `ClaimsPrincipalExtensions.Owns(Exercise)`
(nie aufgerufen – der Kommentar dort behauptete fälschlich, hier lebe die Autorschaftsregel), veraltete
`StudyPlanItem`-Kommentare.

**Zwei Abweichungen zur Bestandsaufnahme:**
- `TestItemResult.HintsUsed` **bleibt** – es steckt in `ItemResultDto` (`Pugling.Contracts/Student/TestDtos.cs`),
  Entfernen wäre ein Vertragsbruch. Ist jetzt als „wird nie gesetzt, immer 0" kommentiert. Entweder
  befüllen (die Tipps existieren in der Ausspielung) oder mit dem DTO gemeinsam streichen.
- Der `TaggedBy`-**Enum bleibt** – `Tag.CreatedBy` wird in `TagResponse` gelesen. Nur die zwei
  `TaggedByRole`-Spalten waren tot.

### E3 · Enum-Persistenz: eine Regel statt 32 Einzelfällen

`PuglingDbContext.ApplyEnumConvention` am Ende von `OnModelCreating`: jedes persistierte Enum wird String.
Ausnahmen als Liste **mit Grund** im Code (`IntEnumsByDesign`): `Child.AllowedContentRating` und
`MediaAsset.Rating` (werden ordnend verglichen – als Text wäre die Altersfreigabe lexikografisch und damit
stillschweigend falsch), sowie `[Flags]` (`SchoolTypes` – eine Bit-Kombination hat keinen Namen).
Vertragsneutral, weil die API über `JsonStringEnumConverter` ohnehin Strings sprach.

Tor **G4** liest genau diese Ausnahmeliste (`PuglingDbContext.IntEnumErlaubt`), damit Regel und Ausnahme
nicht an zwei Orten gepflegt werden.

### E4 · Enum-Mitglieder ohne Produzenten

Entfernt: `PointKind.Minutes/Test/DayComplete/Duration/Reward` (fünf stille Tombstones, nur einer davon
war als solcher dokumentiert), `ClozeStage.WordBank`, `MatchStage.Reverse/.ReverseDistractors`.
`PointKind` ist danach lückenlos neu nummeriert – die Zahlen tragen seit E3 keine Bedeutung mehr, Lücken
sähen nach Versehen aus.

`MatchStage` ist jetzt als **halb umgesetzt** dokumentiert: `MatchingExerciseType` überschreibt weder
`StageOptions` noch `IsTypedStage` noch `Choices`, es gibt also keinen Code, der auf den Enum verzweigt –
`PlanPosition.Stage` wird für Zuordnungs-Positionen gespeichert und beim Ausspielen ignoriert.

**Nebenfund behoben:** die Frontend-`PointKind`-Union in `frontend/src/lib/types.ts` war schon vorher
unvollständig (`ShopCoins`/`ShopGems`/`ObjectiveCoins`/`ObjectiveGems` fehlten); der `?? k`-Fallback in
`pointKindLabel` verdeckte es.

### E5 · Indizes und Eindeutigkeit

**Neue Uniques**, jeder mit Vorprüfung + Fehlercode + Test (ohne Vorprüfung wird aus dem 409 ein 500 mit
halb gespeichertem Zustand – gemessen, nicht vermutet): `Chapter(SubjectId, Name)`
(`duplicate_chapter_name`), `Adult.Email` (gefiltert), `AccountProfile(AccountId, Role)`,
`ExerciseItem(ExerciseId, VocabularyId)` (`duplicate_vocabulary_in_exercise` – verhindert zwei
konkurrierende `ItemProgress`-Zeilen für dasselbe Wort), `Achievement(ChildId, Metric, Threshold)`.

**Neue Indizes:** `Subject.Name`, `Exercise.Type`, `MediaLink.MediaAssetId`, `Mission(ChildId, Active)`.

**`Vocabulary.Word`/`.Translation`: Collation `NOCASE` + Index + Wegfall des `ToLower()`** in
`VocabularyStoreController`. Ein Index allein hätte nichts gebracht – die Query verglich `LOWER(Word)`, und
über einen Ausdruck greift kein Spaltenindex. Zwei neue `EXPLAIN`-Zusicherungen in `QueryPlanSmokeTests`
beweisen, dass beides zusammen wirkt. Folge, die man wissen muss: `Word == "march"` findet ab jetzt auch
„March" (für einen Vokabelspeicher gewollt; die Eindeutigkeit hängt am `Key`).

**Entfernt:** zwei `ChildMediaPick`-Indizes. Per `EXPLAIN QUERY PLAN` gemessen wählt SQLite für
`ChildId = ? AND VocabularyId = ?` den *gefilterten* Unique-Index – eine Gleichheit auf der Trägerspalte
impliziert dessen `IS NOT NULL`-Filter, die Zusatzindizes wurden nie benutzt.

**Bewusst NICHT gemacht: `Subject.Name` eindeutig.** Der Plan sah es vor; die Umsetzung zeigte den Preis
(~20 Testklassen brechen) und damit das eigentliche Problem: `Subject` trägt **keinen Owner**. Ein globaler
Unique machte den wichtigsten Namensraum des Katalogs first-come-first-served über alle Creator, und jeder
weitere Lehrer müsste seine Kapitel an ein Fach hängen, das ihm nicht gehört. Das ist eine
Produktentscheidung über Katalog-Eigentum plus ein Vertragsbruch (`POST /subjects` → 409) und gehört nicht
in eine Etappe mit der Vorgabe „kein Bruch". **Erst entscheiden, wem ein Fach gehört, dann eindeutig
machen.** Die Begründung steht im `PuglingDbContext` an der Stelle, wo der Index sonst stünde.

**Ebenfalls nicht gemacht:** Unique auf `SeriesUnit(SeriesId, Grade, OrderIndex)` – `Grade` ist nullable,
SQLite behandelt NULLs als verschieden, ein einfacher Unique hielte die Invariante also nicht. Und
Reihenfolge ist keine Identität. Auch die im Plan genannten „unbenutzten FK-Indizes"
(`ItemReviewEvent.ItemId`, drei bei `Remark`) bleiben: das sind Konventions-Indizes von EF, sie einzeln zu
entfernen erfordert das Abschalten der `ForeignKeyIndexConvention` – global, für alle 95 FKs. Aufwand und
Risiko stehen in keinem Verhältnis zum Gewinn.

## Offen: E6–E14

Jede Etappe endet grün, mit neu gefalteter Migration (siehe Arbeitsregeln).

### E6 · Löschverhalten explizit + bezahltes Inventar retten
**6a, rein deklarativ:** die 8 Konventions-Cascades in `PuglingDbContext` ausschreiben – `Chapter→Subject`,
`Exercise→Chapter`, `StudyPlan→Child`, `PracticeSession→StudyPlan`, `TestAttempt→StudyPlan`,
`ReviewEvent→PracticeSession`, `TestItemResult→TestAttempt`, `LearnGoal→Child` (letzterer entfällt mit
E13 – Reihenfolge beachten oder E13 vorziehen). Verhalten bleibt gleich, nur die Absicht wird sichtbar:
die Suite muss **unverändert** grün sein; jede Abweichung heißt, die ausgeschriebene Absicht war nicht die
gelebte. Dazu die drei handkopierten App-Guards zum `Restrict` bei `PlanPosition→Exercise` in
`Services/Shared/ExerciseUsageQueries.cs` zusammenziehen (`SubjectsController`, `ChaptersController`,
`ExerciseControllerBase.Delete`).

**6b, der Datenverlust-Defekt:** `ChildInventory.ShopArticleId` nullable + **SetNull**, dazu die
Momentaufnahme-Felder, die `ActivationRequest` schon hat (`ArticleTitle`, `UnitType`, `ActionType`).
Bezahlte 120 Minuten Fernsehen sind Geld; Geld überlebt Katalogpflege. **Nicht** `Adult→ShopArticle` auf
Restrict setzen – dann könnte sich ein Vater mit Artikeln nicht mehr selbst löschen und
`AdultLifecycleTests` wäre rot; die Kaskade ist dort ausdrücklich gewollt.

Test **zuerst rot**: „Vater löschen lässt bezahltes Kind-Inventar stehen" in `AdultLifecycleTests` bzw.
`ShopFlowTests`. Tragende Bestandstests: `MediaLinkTeardownTests`, `AdultLifecycleTests`.
Danach Tor **G2** (Löschverhalten) einziehen: eine im Test literal gepinnte Approval-Liste
`"Entity.Fk" → DeleteBehavior`. Reflexion kann „explizit" nicht von „Konvention" unterscheiden – das ist
der ehrliche Ersatz, und er erzwingt bei jeder neuen FK eine bewusste Zeile.

### E7 · `PeriodKey` aufspalten
Ein Spaltenname, drei Formate, alle vier Vorkommen idempotenz-tragend:
- `PositionGoalReward`/`PositionGoalPenalty` (`PlanPositionEntities.cs`, `yyyy-MM-dd`): Spalte **entfällt**,
  das `DateOnly Day` daneben trägt dieselbe Periode doppelt. Unique auf
  `(PlanPositionId, Cadence, PeriodStart)`, mit `Cadence` als Snapshot auf der Log-Zeile – sonst deutet ein
  Wechsel Tag→Woche rückwirkend gebuchte Perioden um. Macht den Kommentar in
  `PositionProgressService.cs:80-83` („nach PeriodKey filtern überhöht Wochenziele um bis zu 7×") vom
  Kommentar zum Typ.
- `MissionAward` (`2026-W27`/`once`): `(MissionId, Period, PeriodStart DateOnly?)` mit **zwei gefilterten**
  Unique-Indizes (`PeriodStart IS NOT NULL` / `IS NULL` für `OneOff`). **Fallstrick:** SQLite behandelt
  NULLs als verschieden – ein einzelner Unique über eine nullable Spalte hält die Invariante *nicht*.
  Genau das machte den Text-Key ursprünglich attraktiv.
- `ObjectiveReward` (`kr:42`/`done`): keine Periode, sondern ein Anlass mit zwei Ausprägungen →
  `PaidKeyResultId int?` + NULL als Diskriminator + zwei gefilterte Uniques (Bauart wie `MediaLink`).

**Vorher prüfen**, ob `periodKey` in einem Response-DTO steht; wenn ja, dieses Feld zurückstellen.
Tests: `PositionGoalOverviewTests`, `PflichtMalusTests`, `ObjectiveTests`, `GamificationTests`, plus je
ein Doppelbuchungs-Test (zweimal dieselbe Periode → eine Buchung). Danach Tor **G6** (kein Datum als Text).

### E8 · Konto-Drift + Profil-Invariante
- `AdultsController.cs` (~:80-86): PATCH setzt `adult.Name`/`.Email`, spiegelt aber nur die PIN aufs Konto.
  `Account.DisplayName`/`Account.Email` bleiben stehen – und der gefilterte Unique-Index sitzt auf
  `Account.Email`, gegen den die Kollisionsprüfung läuft. Ein Erwachsener kann sich damit eine Adresse
  geben, die laut Index noch frei aussieht. Analog `ChildrenController.cs` (~:105) für den Kindnamen
  (wirkt auf `LoginResponse` und den `ClaimTypes.Name`-Claim).
- `AccountProfile`: Check-Constraint `CK_AccountProfile_SingleProfile` (genau eines von `AdultId`/`ChildId`)
  in die DB. `IdentityEntities.cs` behauptet die Invariante seit immer; das Muster steht daneben in
  `MediaLink` und `ChildMediaPick`. Das Unique `(AccountId, Role)` kam bereits in E5.

Tests zuerst rot in `AccountSelfServiceTests`/`AdultLifecycleTests`. Danach Tor **G8** (erwartete
Check-Constraints existieren).

### E9 · Backfills ins Seed + `ExerciseGrants`-Lücke
Die drei „Backfills" sind **kein Altdaten-Pfad, sondern Seed-Nachlauf**: ohne sie hat die frische DB
Adults/Children ohne Login (`AccountBackfill`), keine `ExerciseItems` (`ExerciseItemBackfill` – der Seed
schreibt Items inline in `ConfigJson`) und keine `InterestTags` (`InterestTagBackfill`). Sie hängen heute
in `Program.cs` hinter `Seed.Run`.

1. `Seed.Run(db)` → `Seed.RunAsync(db, ExerciseItemService, AccountService, InterestTagService, ct)` –
   explizite Parameter, kein `IServiceProvider`.
2. Die 11 bestehenden Routinen **unverändert**, dann `SeedExerciseGrants`, `SeedExerciseItems`,
   `SeedAccounts`, `SeedChildInterests`. Die drei `Data/*Backfill.cs` werden gelöscht.
3. **Die Idempotenz muss überleben** – `Program.cs` ruft das bei jedem Start. Neuer Test:
   `RunAsync` zweimal gegen dieselbe DB → alle Zeilenzahlen unverändert. Das prüft heute nichts.
4. **Die Lücke:** `ExerciseGrants` werden von niemandem geseedet – die Vergabe steckt als Raw-SQL in einer
   Migration, die auf leerer DB ein No-op war (und mit dem Squash entfiel). Rechte laufen aber
   ausschließlich über Grants (`Auth/ExercisePermissionService.cs`), **der geseedete Lehrer kann seine
   eigenen drei Übungen also nicht bearbeiten.** `SeedExerciseGrants` gibt jeder Übung mit
   `AuthorAdultId != null` einen Owner-Grant mit `GrantedByAdultId = AuthorAdultId` – wie
   `ExerciseControllerBase` beim Anlegen. Das ist eine **Verhaltensänderung**:
   `docs/api-examples/catalog.md` bekommt andere `grantCount`/`canWrite`-Werte → neu erzeugen und
   mitcommitten. Positivtest in `SeedContractTests`: Lehrer (2/9999) PATCHt seine geseedete Übung → 200.
5. **Zusatzbefund aus E0, hier mit erledigen:** `AccountBackfill.cs` ruft `EnsureForFatherAsync` für
   *jeden* Adult – der geseedete Lehrer bekommt damit Creator **und** Supervisor, obwohl
   `AccountService.EnsureForTeacherAsync` (Creator-only) genau für ihn existiert und nie erreicht wird.
   `SeedAccounts` muss unterscheiden (ein Adult ohne `SupervisorLink` ist ein Lehrer). `SeedContractTests`
   lässt die Rollen-Zusicherung deshalb bewusst offen, mit Kommentar – danach nachziehen.
6. **Umgebungs-Gate** `Seed:Enabled`, Vorgabe `IsDevelopment()`. **Falle:** Azure läuft in Production und
   seedet heute – siehe „Betriebsschritt" unten, `Seed__Enabled=true` muss dort gesetzt sein.

### E10 · Legacy-Lesepfad entfernen — **zwingend nach E9**
`Services/Shared/ExerciseContentResolver.cs` (`if (rows.Count == 0) return provider.ItemsOf(exercise);` –
Fallback auf Inline-`ConfigJson`-Items) und `Exercises/VocabularyExerciseType.ItemsOf`. In einer neuen DB
unerreichbar, weil `VocabularyController.AfterSaveAsync` die Config nach dem Materialisieren leert.
`VocabularyConfig.Items`/`.Refs` bleiben als **Eingabeform** im Vertrag. Solange die Items nur über den
Backfill entstehen, ist der Fallback für geseedete Übungen der einzige Inhaltsweg – daher die Reihenfolge.
Tests: `ExerciseContentProviderTests`, `ExerciseItemsAndProgressTests`, `VocabTwoStepTests`.

### E11 · String-Längen + Tabellennamen
**Längen** als zweite Konventionsschleife in `OnModelCreating` (Muster: `ApplyEnumConvention`):
Standard 200, Slugs/Keys 128, Freitext 2000. `UnlimitedByDesign`-Liste mit Begründung: `*.ConfigJson`,
`Remark.ContextJson/.RecentErrorsJson`, `Vocabulary.Noun/Verb`, `ClozeText.Gaps/WordBank`,
`Child.Interests/OwnedSkins`, `CreatorProfile.DefaultTypes`, `PlanPosition.BoxIntervalDays/StageSchedule`,
`Exercise.SuggestedBonus`, `PracticeSession.Order`, `TestAttempt.Order`. **Harte Zusatzregel:** eine
unique-indizierte String-Spalte MUSS begrenzt sein (`Vocabulary.Key`, `MediaAsset.Key`,
`TextbookSeries.Slug`, `ClozeText.Key` sind es nicht).

**Ehrlich dazugesagt:** SQLite setzt `HasMaxLength` nicht durch und EF validiert es nicht beim
`SaveChanges`. Der Wert liegt in der Portabilität (sonst bei einem Provider-Wechsel `NVARCHAR(MAX)` mit
nicht anlegbaren Unique-Indizes) und in Tor **G3** – deshalb steht die Etappe spät. Eingabe-Durchsetzung
ist Sache der DTO-Validierung und nicht Teil dieses Umbaus.

**Tabellennamen** auf die DbSet-Namen ziehen (mit Kette=1 gratis): `Vocabulary → Vocabularies`,
`ChildPoints → ChildPointsEntries`, `Timetable → TimetableEntries`. Dazu die internen Namensreste ohne
Vertragswirkung: `AuthAccess.FatherOwnsChildAsync → SupervisorOwnsChildAsync`,
`AccountService.EnsureForFatherAsync → EnsureForAdultAsync`, `Seed.demoFather → demoSupervisor`, lokale
`fatherId`-Variablen. Danach Tore **G3** und **G7** (JSON-Comparer).

### E12 · `TimeSlotRule` in Konfiguration auflösen
Keine API, kein Schreibpfad außer dem Seed, kein Index, kein Unique, keine Überlappungsprüfung
(`ScoringService` nimmt bei Überlappung willkürlich eine Regel) – und `PuglingWebAppFactory` **löscht die
Zeilen**, um deterministische Tests zu bekommen. Eine Tabelle, deren Zeilen die Test-Suite wegräumen muss,
um sinnvolle Ergebnisse zu erhalten, ist Konfiguration.

Entity + DbSet + `SeedTimeSlots` weg, `Scoring:TimeSlots` in `appsettings.json`, `ScoringService` liest
`IOptions`, `ScoringTimeSlotTests` überschreibt per `UseSetting`. Damit fällt auch die Legacy-Ausnahme aus
`CLAUDE.md` („das *einzige* bewusst erhaltene Legacy-Entity") weg – und `PuglingWebAppFactory` braucht das
`ExecuteDelete` nicht mehr.

### E13 · `LearnGoal` löschen — der eine bewusste Vertragsbruch
`LearnGoal` und `KeyResult` sind strukturell identisch (gleiches Scope-Tripel, gleicher Evaluator
`ChildLearnProgressService.ScopeEvaluator`; `LearnGoalService` und `ObjectiveEvaluationService` sind
derselbe Code zweimal). `KeyResult` ist der Superset (Objective-Klammer, Belohnungslog, `ClassTestGrade`).
Die eine Metrik, die `LearnGoal` mehr hat, ist **`Coverage` – und `KeyResultMetric` schließt sie
ausdrücklich aus**, mit Begründung „steigt schon durchs bloße Sehen von Vokabeln"
(`Contracts/Common/ObjectiveBaseTypes.cs`). Die Konsolidierung entfernt also eine farmbare Metrik statt
einer Funktion.

1. **Bedingung zuerst:** `CreateObjectiveRequest` nimmt eine optionale `keyResults`-Liste (additiv), damit
   „ein Ziel anlegen" **ein** Aufruf bleibt. Ohne das wird ein Ein-Satz-Ziel zu zwei Requests und
   `LearnGoal` kommt in sechs Monaten zurück.
2. Löschen (Fläche per Grep verifiziert): `Models/LearnGoalEntities.cs`,
   `Services/Supervisor/LearnGoalService.cs`, `Controllers/Supervisor/LearnGoalsController.cs`,
   `Contracts/Common/LearnGoalBaseTypes.cs`, die LearnGoal-DTOs in `Contracts/Supervisor/GoalDtos.cs`,
   `Pugling.Client/SupervisorApi.cs`, `Pugling.Api.Tests/LearnGoalTests.cs`, das `DbSet` und die
   Konfiguration im `PuglingDbContext`. Berührt außerdem: `Program.cs` (DI),
   `Pugling.Api.Tests/PatchSemanticsTests.cs`, `Pugling.Api.Tests/PuglingClientTests.cs`.
3. `KeyResult`-Scope wird echt (behebt den Pflichtfeld-ohne-FK-Zombie): `SubjectId` FK **Cascade**,
   `ChapterId`/`ExerciseId` FK **Restrict** + Aufnahme in `ExerciseUsageQueries` → Löschen einer Übung, auf
   die ein Ziel zeigt, gibt einen sauberen 409. Bewusst nicht `SetNull`: das würde ein Kapitel-Ziel
   lautlos zum Fach-Ziel aufweiten, also die Messlatte heimlich verschieben.
4. Unique auf den Scope: **drei gefilterte** Indizes (`Subject`-only / `+Chapter` / `+Exercise`) – wegen
   der NULL-Falle aus E7.
5. Frontend: `frontend/src/vater/VaterZiele.tsx` (LearnGoals-Sektion), `frontend/src/lib/types.ts`,
   `frontend/src/lib/api.ts`, `frontend/src/lib/fieldHelp.ts`. Danach `npx tsc --noEmit` in `frontend/`.
6. Doku/Agenten: `.claude/skills/supervisor/SKILL.md`, `docs/tutorial-supervisor.md`,
   `docs/endpunkt-beziehungen.md`, `docs/REST/Supervisor.http`, `docs/lernziele-objectives-plan.md`
   (als „zurückgenommen" fortschreiben, nicht löschen – die Begründung ist wertvoll).
7. **Ratschen senken:** `EndpointCoverageGuard.FullRunTouchedActions` (268 → minus die entfallenden
   Actions) und `ConventionGuardTests` (`types.Count >= 200`) prüfen. Sie schlagen bewusst in *beide*
   Richtungen an – das ist Absicht, kostet aber 10 Minuten Verwirrung, wenn man es nicht erwartet.

### E14 · Abschluss
- `CLAUDE.md` „Konventionen"/„Fallstricke": die neuen Schema-Regeln + die Tore; die `TimeSlotRule`-Ausnahme
  entfernen (nach E12); die Kette-bleibt-1-Regel aufnehmen.
- `docs/codequalitaet-gates-plan.md` um G1–G9 als neue Etappe fortschreiben.
- Dieses Dokument auf „abgeschlossen" fortschreiben.
- Die alte Azure-Datei `/home/data/pugling.db*` löschen, wenn v2 mehrere Tage steht.

## Fallstricke (haben beim Umsetzen Zeit gekostet)

1. **`dotnet ef database update --no-build` nach `migrations add`** läuft gegen die Assembly von *vor* dem
   `migrations add` (der Befehl baut zuerst, schreibt die Dateien danach) und meldet „No migrations were
   applied. The database is already up to date." Ohne `--no-build` arbeiten.
2. **`IProperty.GetDefaultValue()` überzeichnet** – es liefert auch dort einen Wert, wo EF keine
   DEFAULT-Klausel schreibt (z. B. an jedem `CreatedAt`). Die Wahrheit steht im relationalen Modell:
   `db.Model.GetRelationalModel().Tables` → `Columns` → `DefaultValue`/`DefaultValueSql`. So macht es G9.
3. **`git worktree remove` scheitert auf Windows** an der Pfadlänge, sobald `obj/bin` im Worktree liegen.
   Weg: mit `robocopy /MIR` gegen ein leeres Verzeichnis leeren, dann `rm -rf` + `git worktree prune`.
4. **Falsch-Grün-Probe für `SeedContractTests`:** einen zusätzlichen `Adult` *vor* `SeedAdmin` einzufügen
   wirkt anders als gedacht – `SeedAdmin` hat den Wächter `if (db.Adults.Any()) return;` und steigt dann
   ganz aus. Der Test wird trotzdem rot (401 beim Login), aber aus diesem Grund. Ebenso ein No-op:
   `SeedTeacherLibrary` nach vorn ziehen (es braucht das Fach Englisch und steigt sonst aus).
5. **Test-Helfer und neue Uniques:** `TestApi` legt Fächer über `UniqueName(...)` an, weil dieselbe
   Testklasse gegen dieselbe DB mehrfach durchläuft. Wer eine neue Eindeutigkeit einführt, muss mit
   genau dieser Sorte Kollision rechnen – und mit einem **500 statt 409**, solange die Vorprüfung fehlt.
6. **`DocsCaptureTests` zählt nur, was es selbst aufruft.** Ein neuer Fehlercode erscheint in
   `docs/api-examples/index.md` als „Über HTTP im In-Process-Test nicht erreichbar", auch wenn ein anderer
   Test ihn sehr wohl über HTTP prüft. 21 der 54 Codes sind dort ohnehin nicht abgedeckt – das ist der
   Normalzustand, kein Befund.
7. **Der Stop-Hook testet die ganze Solution** (~55 s) und der Edit-Hook baut nach jeder `.cs`-Änderung
   das besitzende Projekt. Bei einer Reihe zusammengehöriger Edits ruhig weiterarbeiten; der Hook nennt
   die kaputten Aufrufstellen und ist damit die schnellste Liste dessen, was noch nachzuziehen ist.

## Betriebsschritt (einmalig, außerhalb des Repos) — **vor dem nächsten Deploy**

E1 ist wirksam: die Azure-DB stammt aus der alten Kette und wird vom Historien-Guard abgewiesen.

1. Azure App Settings: `ConnectionStrings__Default` → `Data Source=/home/data/pugling-v2.db`
   **und** `Seed__Enabled=true` (Letzteres, weil E9 ein Umgebungs-Gate einführt und die neue Datei sonst
   leer bliebe).
2. Die alte Datei **nicht löschen** – sie ist die Rückfallebene, das Zurückflippen der Einstellung ist der
   komplette Rollback. Aufräumen erst in E14.
3. Lokal ist die Dev-DB bereits ersetzt. `PuglingDbContextFactory` hardcodet `Data Source=pugling.db`;
   eine DB aus der alten Kette muss weg, sonst wirft der Start (mit einer Meldung, die das sagt).

## Verifikation

**Pro Etappe:** `dotnet test Pugling.sln -c Release` (~55 s, aktuell **601** Tests) und
`git diff -- docs/api-examples` prüfen (leer, oder im gleichen Commit neu erzeugt).

**Laufzeit statt nur Kompilieren:** `/smoke-test` (Wegwerf-DB, lässt `pugling.db` unangetastet) nach E6,
E9 und E13. Der Vater→Sohn-Durchstich unter `frontend/e2e/` nach E9 und E13 (dort ändern sich
Grant-Rechte bzw. verschwindet eine UI-Sektion).

**Nach jeder Schemaänderung:** Migration neu falten, dann muss der `git diff` am
`PuglingDbContextModelSnapshot.cs` genau die beabsichtigte Änderung zeigen – und G1/G1b grün sein.
