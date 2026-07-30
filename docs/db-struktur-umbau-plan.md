---
tags: [bereich/architektur, bereich/datenmodell, status/laufend]
---

# DB-/EF-Struktur-Umbau

> **Übergabe-Dokument.** E0–E9 sind umgesetzt und verifiziert; offen sind E10–E14. Beide
> echten Defekte sind damit behoben – der Rest ist Struktur. Dieses Dokument ist so
> geschrieben, dass jemand ohne Vorwissen die restlichen Etappen zu Ende führen kann: es nennt die
> getroffenen Entscheidungen, die Arbeitsregeln, die Belege, die bewussten Abweichungen und die
> Fallstricke, die beim Umsetzen Zeit gekostet haben.

## Warum

Das Datenmodell ist in 25 Tagen über **48 Migrationen** auf **62 Tabellen** gewachsen. Die Kette war
driftfrei – das Problem war nicht Drift, sondern **fehlende Regel**: 12 Enums lagen als String in der DB
und ~20 als int (in `Remarks` sogar beides in derselben Tabelle), kein einziges `HasMaxLength` existierte,
8 Fremdschlüssel verließen sich auf Konventions-Cascade, 14 Spalten sahen wie Fremdschlüssel aus und
waren keine, und `Subjects`/`Adults` hatten außer dem Primärschlüssel überhaupt keinen Index.

Dazu zwei echte Defekte, **beide behoben**:
1. **Löschen eines Supervisors vernichtete bezahltes Kind-Inventar** (`Adult→ShopArticle` Cascade →
   `ShopArticle→ChildInventory` Cascade, während die Kaufbelege per SetNull stehenblieben) → **E6**.
2. **PATCH auf Name/E-Mail eines Erwachsenen zog das Konto nicht nach**, obwohl der gefilterte
   Unique-Index dort sitzt und die Kollisionsprüfung gegen den veralteten Wert lief – aus dem fälligen 409
   wurde ein 500 mit halb gespeichertem Zustand → **E8**.

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
3. **Die Reihenfolge in `Seed.RunAsync` ist eingefroren.** Neue Seed-Routinen kommen ans Ende. Die Seed-IDs
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
7. **Wer eine Beziehung anfasst, zieht die G2-Tabelle mit** (`SchemaGuardTests`, literal gepinnte Liste
   aller 95 FKs). Das betrifft E12 (`TimeSlotRule` fällt weg) und
   E13 (`LearnGoal.ChildId` fällt weg, drei `KeyResult`-Scope-FKs kommen). Das Tor **soll** dabei rot sein –
   die bewusste Zeile ist der Zweck. Bei mehreren Änderungen lohnt der Wegwerf-Dump aus E6 wieder.

## Umgesetzt: E0–E9

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

### E6 · Löschverhalten explizit + bezahltes Inventar retten

**6a:** Es waren **neun** Konventions-Cascades, nicht acht – `ChildPointsEntry→Child` fehlte im Plan. Alle
stehen jetzt in `PuglingDbContext.ApplyExplicitCascades`. Verhalten unverändert, Suite unverändert grün.

Zwei Dinge, die dabei zu lernen waren:

1. **Die Gegen-Navigation MUSS benannt werden, wo sie existiert.** `HasOne(p => p.Child).WithMany()` ohne
   `c => c.PointsEntries` lässt EF die per Konvention gefundene Beziehung nicht wiedererkennen und legt
   eine **zweite** an – im Modell wuchs eine Spalte `ChildId1` nach. Gefangen hat das die
   `ClientSetNull`-Zusicherung von G2, noch vor dem ersten Testlauf.
2. **Vor dem Ausschreiben einen Ist-Abzug machen.** Ein Wegwerf-Test, der alle FKs mit ihrem
   `DeleteBehavior` in eine Datei schreibt, liefert die G2-Tabelle und gleichzeitig die Baseline: der Diff
   nach der Etappe muss **genau** die beabsichtigte Zeile zeigen (hier: `ChildInventory.ShopArticleId`
   `Cascade` → `SetNull`). Ohne den Abzug hätte niemand die zusätzliche `ChildId1`-Zeile gesehen.

Die beiden handkopierten Guards (`SubjectsController`, `ChaptersController`) laufen jetzt über
`ExerciseUsageQueries.AnyBlockingAsync(db, scope, ct)`. Die Klasse existierte schon und war reicher als die
Kopien – neu ist nur die Scope-Frage („blockiert *irgendeine* Übung aus diesem Fach/Kapitel"). Die
**Meldungstexte bleiben bei den Aufrufern**: sie benennen die Ebene und sind nicht dieselbe Aussage.

**6b, der Datenverlust-Defekt – behoben.** `ChildInventory.ShopArticleId` ist nullable mit **SetNull**, und
die Momentaufnahme daneben ist **größer als geplant**: nicht nur `ArticleTitle`/`UnitType`/`ActionType`,
sondern auch `ArticleNumber` und `SupervisorId`. Beide fielen erst beim Umstellen der Lesepfade auf:

- `ArticleNumber` ist der **Sortierschlüssel** beider Inventar-Sichten. Über die Navigation wäre er nach dem
  Löschen NULL – der Posten wäre stillschweigend nach vorne gerutscht und namenlos angezeigt worden.
- `SupervisorId` trägt den **Vater-Filter**. Der lief über `i.ShopArticle!.AdultId == fid`; ohne Snapshot
  wäre der verwaiste Posten aus der Vater-Liste gefallen – und unsichtbar ist so gut wie gelöscht.

Der Unique ist **gefiltert** (`WHERE ShopArticleId IS NOT NULL`), und das ist keine Kosmetik: SQLite
behandelt NULLs als verschieden. Genau so ist es gewollt – je lebendem Artikel höchstens eine Zeile, aber
zwei verschiedene gelöschte Artikel dürfen zwei verwaiste Bestände hinterlassen, die nicht kollidieren.

**Zwei Tests, beide vorher rot** (der erste sogar als Compile-Fehler: der xUnit-Analyzer verbot
`Assert.Null` auf dem damals nicht-nullbaren `int` – der Defekt war vor dem Testlauf bewiesen):
`ShopFlowTests.ArtikelLoeschen_LaesstBezahltesInventarStehen` (der direkte Weg) und
`AdultLifecycleTests.Loeschen_Vernichtet_Kein_Bezahltes_Kind_Inventar` (der transitive über zwei Kaskaden).
`Adult→ShopArticle` bleibt bewusst `Cascade`: ein Vater mit Artikeln muss sich selbst löschen können.

**Vertragswirkung, klein aber vorhanden:** `InventoryItemDto.ShopArticleId` und
`MyInventoryItemResponse.ShopArticleId` sind `int?`. Das folgt dem Schwester-DTO `ActivationRequestDto`,
das für dieselbe Lage schon `int?` trug. Frontend nachgezogen (`InventoryItem.shopArticleId: number | null`,
Schlüssel-Rückfall auf den Titel, Einlöse-Button deaktiviert, Hinweis „gibt's bei Papa nicht mehr").
`docs/api-examples` blieb byte-stabil.

**Bewusst offen geblieben – ein verwaister Posten ist nicht mehr einlösbar.** Die Aktivierung wird über die
**Artikel-Id** adressiert (`POST me/shop/inventory/{articleId}/activate`); ohne Artikel gibt es keinen
Schlüssel. Der Bestand bleibt sichtbar und prüfbar, und der Ausgleich läuft über das vorhandene Druckventil
`POST children/{id}/points`. Ihn wieder einlösbar zu machen heißt: eine Route auf der **Inventar-Id** –
neuer Endpunkt, eigene Entscheidung, nicht Teil einer Struktur-Etappe. Ebenso bewusst: der Restbestand ist
für das **Kind** sichtbar, nicht für eine zweite Betreuerin – die Ökonomie ist ausstellergebunden
(`SupervisorId`-Momentaufnahme), das ist die bestehende Regel und keine neue Lücke.

**Tor G2 steht** (`SchemaGuardTests.Jeder_Fremdschluessel_Hat_Ein_Abgenommenes_Loeschverhalten`): eine
literal gepinnte Tabelle über alle 95 FKs, verglichen als sortierte Zeilen (die Meldung zeigt dann direkt,
welche Zeile fehlt oder anders ist), plus das Verbot von `ClientSetNull`. Falsch-Grün-Probe:
`TestItemResult→TestAttempt` auf `Restrict` gedreht → rot mit lesbarer Meldung; zurückgenommen.

**Stand:** 604 Tests grün, Kette bei 1, `docs/api-examples` unverändert.

### E8 · Konto-Drift + Profil-Invariante

Der zweite echte Defekt, und der billigste Wert im Umbau: **das Konto ist die Spiegelung der fachlichen
Zeile, nicht ein zweiter Datenstand.** Drei Schreibpfade behaupteten das, zwei hielten es nicht.

**Der Befund war größer als „ein Name driftet".** Die Kollisionsprüfung in `AdultsController` liest
`Account.Email` (dort sitzt der gefilterte Unique-Index), seit E5 trägt aber **auch** `Adult.Email` einen.
Blieb das Konto stehen, ging die Drift in **beide** Richtungen falsch:
- eine aufgegebene Adresse hielt den Adressraum weiter besetzt – niemand konnte sie je wieder bekommen;
- eine belegte Adresse sah **frei** aus: die Vorprüfung ließ sie durch, der Index am `Adult` schlug zu, und
  aus dem fälligen 409 wurde ein **500 mit halb gespeichertem Zustand**. Genau das zeigte der Test
  `AdultLifecycleTests.Adresswechsel_Macht_Die_Neue_Adresse_Fuer_Andere_Belegt` als erstes Rot
  (`InternalServerError`).

**Umgesetzt:**
- **Eine** Stelle trägt die Invariante: `AccountService.MirrorAsync(Adult, ct)` und
  `MirrorAsync(Child, ct)` – Anzeigename, E-Mail und PIN-Hash von der fachlichen Zeile aufs Konto,
  **unbedingt**, nicht nur das gerade geänderte Feld. „Das Konto trägt, was die fachliche Zeile trägt" ist
  als Invariante prüfbar, „das Konto trägt, was der letzte PATCH mitschickte" nicht – und bestehende Drift
  heilt so beim nächsten Schreibzugriff. Das `SaveChanges` bleibt beim Aufrufer, damit fachliche Änderung
  und Spiegelung in **einem** Commit landen.
- Aufrufer: `AdultsController.Update`, `ChildrenController.Update` und `AuthController.UpdateMe`. Letzterer
  spiegelte schon vorher korrekt – seine drei Zuweisungen sind auf denselben Aufruf zusammengelaufen, damit
  die Regel nicht an drei Orten gepflegt wird. Sein XML-Doc nannte die Lücke im `AdultsController`
  ausdrücklich; der Satz ist entfernt.
- Check-Constraint `CK_AccountProfile_SingleProfile` (genau eines von `AdultId`/`ChildId`) in der DB,
  gleiche Bauart wie `MediaLink`/`ChildMediaPick`.

**Neue Tests, alle vorher rot gesehen** (5 an der Zahl): Name-Spiegelung über den konto-zentrischen Login
(`Umbenennen_Zieht_Den_Namen_Des_Kontos_Nach`), Adresse wird wieder frei
(`Adresswechsel_Gibt_Die_Alte_Adresse_Wieder_Frei`), der 500→409-Fall (oben), der Kindname
(`IdentityAccountTests.UmbenanntesKind_MeldetSichMitDemNeuenNamenAn`) und die XOR-Regel
(`ProfilOhneGenauEinZiel_WeistDieDatenbankAb`, beide Verstöße: beides gesetzt und keines gesetzt).

**Tor G8 steht** (`SchemaGuardTests.Erwartete_Check_Constraints_Stehen_Im_Modell`): die **Menge** der
Check-Constraints, nicht „mindestens diese drei" – eine verschwundene Invariante ist genauso ein Fund wie
eine neue ohne Eintrag. Falsch-Grün-Probe: Constraint umbenannt → rot mit lesbarer Meldung; zurückgenommen.
**Fallstrick dabei:** `db.Model.GetCheckConstraints()` *wirft* („not stored in the read-optimized model") –
EF wirft Check-Constraints aus dem Laufzeitmodell, weil sie dort niemand liest. Gefragt ist
`db.GetService<IDesignTimeModel>().Model` (Namespace `Microsoft.EntityFrameworkCore.Metadata`), dasselbe
Modell, aus dem die Migration entsteht. Der Schlüssel ist bewusst der **Entity**-Name, nicht der
Tabellenname – E11 zieht Tabellennamen um, die Invariante bleibt.

**Stand:** 610 Tests grün, Kette bei 1 (`20260730215446_InitialCreate`), Snapshot-Diff genau 5 Zeilen (der
Constraint), `docs/api-examples` unverändert, Abdeckung weiter 268/268. Live gegengeprüft: PATCH auf
Name+E-Mail → `auth/login` liefert `"name":"Papa E8"`, zweite Registrierung auf dieselbe Adresse → **409
`duplicate_email`** (vorher 500).

### E7 · `PeriodKey` aufspalten

Ein Spaltenname, **drei** Formate (`2026-07-04`, `2026-W27`, `once`, dazu `kr:42`/`done`), **vier**
Tabellen – und alle vier Vorkommen idempotenz-tragend, also Teil eines Unique-Index. Ein Tippfehler im
Format hätte doppelt gezahlt, ohne dass irgendetwas auffällt. **Vertragsneutral:** `periodKey` steht in
keinem DTO, die Spalte war rein intern (vorab geprüft).

**Eine Korrektur am Plan:** dort stand, bei `PositionGoalReward`/`PositionGoalPenalty` „entfällt die
Spalte, das `DateOnly Day` daneben trägt dieselbe Periode doppelt". **Das ist falsch.** `Day` ist der
Buchungs- bzw. Auswertungstag, `PeriodStart` der Perioden-Anfang: bei einem Wochenziel, das am Mittwoch
erreicht wird, steht der Montag im einen und der Mittwoch im anderen Feld. Beide sind nötig – der Tag für
`PointsAwardedAsync` und die Tages-/Serien-Metriken, die Periode für die Idempotenz. Die Spalte wurde also
**typisiert**, nicht gestrichen. (Genau davor warnt der Kommentar in `PositionProgressService`: nach der
Periode statt nach dem Tag zu filtern überhöht Wochenziele um bis zu 7×.)

**Umgesetzt:**
- `PositionGoalReward`/`PositionGoalPenalty`: `string PeriodKey` → `GoalCadence Cadence` +
  `DateOnly PeriodStart`, Unique auf `(PlanPositionId, Cadence, PeriodStart)`.
- `MissionAward`: → `MissionPeriod Period` + `DateOnly? PeriodStart`, **zwei gefilterte** Uniques
  (`PeriodStart IS NOT NULL` / `IS NULL` für `OneOff`). Nebengewinn: die ISO-Wochen-Rechnung
  (`ISOWeek.GetYear/GetWeekOfYear`) fällt weg – der Montag stand direkt daneben schon da und bestimmt die
  ISO-Woche eindeutig. Zwei Darstellungen desselben Zeitraums, eine davon zu parsen, sind jetzt eine.
- `ObjectiveReward`: → `PaidKeyResultId int?`, NULL = Voll-Abschluss, zwei gefilterte Uniques.

**Warum die Taktung mit auf die Log-Zeile** (`Cadence` bzw. `Period` als Momentaufnahme): ohne sie deutet
ein Wechsel Tag→Woche rückwirkend gebuchte Perioden um. Die Belohnung für Montag als *Tages*ziel würde die
Woche, die an diesem Montag beginnt, stillschweigend als „schon bezahlt" abweisen.

**Warum `PaidKeyResultId` bewusst KEIN Fremdschlüssel ist** (drei Gründe, alle in dieselbe Richtung):
`SetNull` würde eine Etappen-Buchung beim Löschen der Etappe lautlos in die *Abschluss*-Buchung verwandeln –
ein Diskriminator darf nicht durch ein Löschen kippen; `Cascade` ergäbe einen zweiten Kaskadenpfad vom
Objective her (Objective → KeyResult → Reward **neben** Objective → Reward), also genau den SQLite-Diamanten,
den dieses Modell sonst vermeidet; und die Buchung soll die Etappe ohnehin überleben – bezahlt ist bezahlt.
Damit ist die Spalte eine Audit-Momentaufnahme wie `ShopPurchase.SupervisorId` (G5-Liste).

**Fallstrick, der Zeit gekostet hat – ein gefilterter Index verdrängt den Fremdschlüssel-Index.** EF legt
den FK-Index nur an, solange die Spalte *keinen* Index hat; ein gefilterter zählt für die Konvention mit,
taugt aber nicht: ein partieller Index bedient ein blankes `WHERE ObjectiveId IN (…)` nicht. Nach dem ersten
Falten war der Index also weg – und genau diese Abfrage ist der heiße Lesepfad
(`ObjectiveRewardService` lädt bei jedem Kind-Login die gebuchten Anlässe). Darum steht dort jetzt ein
**namentlich** deklarierter FK-Index. `MissionAwards` braucht ihn nicht: jede Abfrage nennt
`(MissionId, Period, PeriodStart)` vollständig, nur die Kaskade sucht auf `MissionId` allein.

**Neuer Test:** `GamificationTests.Einmal_Mission_WirdNichtDoppeltBelohnt_UndDieDatenbankHaeltDagegen` –
der `OneOff`/NULL-Pfad war bisher ungetestet, und er ist der einzige, an dem der NULL-Diskriminator hängt.
Der Test prüft **beides**: dass die Auswertung nicht doppelt bucht (Existenz-Check im Code) *und* dass die
DB eine zweite Buchung ablehnt (die harte Garantie). Nur die erste Hälfte zu prüfen wäre die Fehlerklasse
„Regel getestet, Grenzfall offen" – der Test bliebe grün, wenn genau die Absicherung fehlte, die er belegen
soll. Die Idempotenz der drei anderen Logs deckten `PositionGoalOverviewTests`, `PflichtMalusTests` und
`ObjectiveTests` schon ab.

**Tor G6 steht** (`SchemaGuardTests.Keine_Zeitangabe_Als_Text`): keine `string`-Spalte, deren Name auf
`Key`/`Period`/`Day`/`Date`/`On`/`At`/`Time`/`Week`/`Month`/`Year` endet – case-sensitiv, damit er
`PeriodKey` trifft und nicht jedes Wort, das auf „on" endet. Ausnahmen namentlich mit Grund: die drei
fachlichen Naturschlüssel (`Vocabulary.Key`, `MediaAsset.Key`, `ClozeText.Key`) und **ein echter Fund beim
ersten Lauf**: `TimetableEntry.TimeOfDay` ist Freitext („Nachmittag", „1./2. Stunde"), keine Uhrzeit – bleibt
also, mit Begründung. Falsch-Grün-Probe: einen Naturschlüssel aus der Liste genommen → rot mit Namen;
zurückgenommen.

**Stand:** 612 Tests grün, Kette bei 1 (`20260730222450_InitialCreate`), `docs/api-examples` unverändert,
Abdeckung weiter 268/268.

### E9 · Backfills ins Seed + `ExerciseGrants`-Lücke — keine Migration

Die drei „Backfills" waren **kein Altdaten-Pfad, sondern Seed-Nachlauf**: ohne sie hat eine *frische* DB
Adults/Children ohne Login, Vokabelübungen ohne Items und Kinder ohne referenzierte Interessen. Der Name
war das Missverständnis. Sie sind jetzt vier private Routinen am Ende von `Seed.RunAsync`; die drei Dateien
(`AccountBackfill.cs`, `ExerciseItemBackfill.cs`, `InterestTagBackfill.cs`) sind gelöscht.

**Umgesetzt:**
- `Seed.Run(db)` → `Seed.RunAsync(db, ExerciseItemService, AccountService, InterestTagService, ct)` mit
  **expliziten** Parametern (kein `IServiceProvider`). Die 11 bestehenden Routinen unverändert, dann
  `SeedExerciseGrantsAsync`, `SeedExerciseItemsAsync`, `SeedAccountsAsync`, `SeedChildInterestsAsync`.
- **Die Lücke:** `ExerciseGrants` seedete niemand – die Vergabe steckte als Raw-SQL in einer Migration, die
  auf leerer DB ein No-op ist. Rechte laufen aber **ausschließlich** über Grants
  (`ExercisePermissionService`), **der geseedete Lehrer konnte seine eigenen drei Übungen also nicht
  bearbeiten.** `SeedExerciseGrantsAsync` gibt jeder Übung mit Autor einen Owner-Grant – genau was
  `ExerciseControllerBase` beim Anlegen tut. Idempotent über „diese Übung hat *überhaupt keinen* Grant",
  bewusst nicht über „hat keinen Owner-Grant für ihren Autor": nach einer Eigentumsübertragung gäbe der
  Start dem alten Autor sein Recht sonst bei jedem Hochfahren zurück.
- **Der Zusatzbefund ist behoben:** `AccountBackfill` rief für *jeden* Adult `EnsureForFatherAsync`, der
  geseedete Lehrer bekam also Creator **und** Supervisor, obwohl `EnsureForTeacherAsync` (Creator-only)
  genau für ihn existiert und nie erreicht wurde. `SeedAccountsAsync` unterscheidet jetzt am
  **Betreuungsauftrag** (`SupervisedLinks.Count > 0`) – exakt die fachliche Definition aus
  docs/lehrer-konto-plan.md. Dass die Routine am *Ende* läuft, ist Teil der Regel: erst dann stehen die
  Betreuungen. `SeedContractTests` sichert die Rolle jetzt zu (`role == "Creator"`), wo vorher bewusst
  keine Zusage stand.
- **Umgebungs-Gate `Seed:Enabled`**, Vorgabe `IsDevelopment()`. Live geprüft: mit `Seed__Enabled=false`
  bootet die App gegen eine frische DB und der Login von Adult 1 endet mit **401** (nichts geseedet);
  ohne das Flag läuft alles wie vorher.

**Zwei neue Tests, beide vorher rot:**
- `Seed_Zweimal_Ausgefuehrt_Dupliziert_Nichts` – vergleicht die Zeilenzahlen **aller** Tabellen (über die
  rohe Verbindung, damit Tabellen ohne DbSet mitzählen). Das prüfte bisher **nichts**, obwohl der Start den
  Seed bei jedem Hochfahren ruft. *Fallstrick beim Schreiben:* die Test-Factory löscht nach dem Start die
  Zeitfenster (Wanduhr-Neutralisierung) – ein „Startzustand vs. nach einem Lauf"-Vergleich wäre an ihrem
  Aufräumen gescheitert, nicht an einem Seed-Fehler. Darum: einmal säen, messen, erneut säen, vergleichen –
  und die Zeitfenster danach wieder abräumen.
- `Lehrer_Darf_Seine_Geseedete_Uebung_Bearbeiten` – über einen **additiven** Schreibzugriff (ein Item
  anlegen), nicht über das vollständige `PUT`: derselbe Rechte-Pfad, aber ohne die geseedete Übung zu
  ersetzen. Genau dieser Aufruf liefert einem fremden Creator 403.

**Erwartung, die nicht eintrat:** der Plan sagte, `docs/api-examples/catalog.md` bekomme andere
`grantCount`/`canWrite`-Werte und müsse neu erzeugt werden. Der Diff ist **leer** – `DocsCaptureTests` legt
seine Übungen selbst an und berührt die Lehrer-Bibliothek nicht.

**Stand:** 614 Tests grün, `docs/api-examples` unverändert, Abdeckung weiter 268/268, keine
Schemaänderung (Kette unverändert bei `20260730222450_InitialCreate`).

## Offen: E10–E14

Jede Etappe endet grün, mit neu gefalteter Migration (siehe Arbeitsregeln).

### E10 · Legacy-Lesepfad entfernen — **zwingend nach E9** (E9 ist durch)
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
8. **`db.Model` ist nicht das ganze Modell.** Das laufzeit-optimierte Modell wirft für Check-Constraints
   ausdrücklich („not stored in the read-optimized model") – EF wirft weg, was zur Laufzeit niemand liest.
   Wer Schema-*Form* prüft, fragt `db.GetService<IDesignTimeModel>().Model` (Namespace
   `Microsoft.EntityFrameworkCore.Metadata`) bzw. `GetRelationalModel()` für DDL-Details wie DEFAULTs. Kostete
   in E8 und E1 je einen Anlauf.
9. **Der Login-Rate-Limiter bremst auch die Handprobe.** Ein Dutzend `curl` gegen `auth/*` in Folge endet in
   `429 rate_limited` – das ist die Policy `login`, kein Fehler. In der Testsuite ist sie über
   `RateLimiting:LoginEnabled` aus (der In-Process-TestServer teilte sonst eine IP-Partition).

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

**Pro Etappe:** `dotnet test Pugling.sln -c Release` (~55 s, aktuell **614** Tests) und
`git diff -- docs/api-examples` prüfen (leer, oder im gleichen Commit neu erzeugt).

**Laufzeit statt nur Kompilieren:** `/smoke-test` (Wegwerf-DB, lässt `pugling.db` unangetastet) nach E6,
E9 und E13. Der Vater→Sohn-Durchstich unter `frontend/e2e/` nach E9 und E13 (dort ändern sich
Grant-Rechte bzw. verschwindet eine UI-Sektion).

**Nach jeder Schemaänderung:** Migration neu falten, dann muss der `git diff` am
`PuglingDbContextModelSnapshot.cs` genau die beabsichtigte Änderung zeigen – und G1/G1b grün sein.
