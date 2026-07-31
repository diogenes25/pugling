# Pugling.Api – das Domänenmodell

Diese Datei lädt nur, wenn unter `backend/Pugling.Api/` gearbeitet wird – die Landkarte des Fachmodells
gehört nicht in jede Frontend- oder Doku-Sitzung. Der Rahmen (API-First, die drei Ebenen, Konventionen,
Fallstricke) steht in der [CLAUDE.md im Repo-Root](../../CLAUDE.md).

Zusammenhänge über die Kette **Übung → Lehrplan → Kind → Auswertung** stehen in
[docs/endpunkt-beziehungen.md](../../docs/endpunkt-beziehungen.md) – dort einsteigen, statt breit zu suchen.

## Schema & Migrationen

Die Regel „**die Kette ist genau EINE Migration**" und ihre Begründung stehen im Root; hier das Handwerk
dahinter. `SchemaGuardTests` hält beides mechanisch: kein Modell-Drift (`HasPendingModelChanges`) und
Kettenlänge 1.

- Nach dem Falten muss der `git diff` am `PuglingDbContextModelSnapshot.cs` **genau die beabsichtigte
  Änderung** zeigen – das ist die Abnahme.
- Die EF-Tools laufen über die Design-Time-Factory
  ([Data/PuglingDbContextFactory.cs](Data/PuglingDbContextFactory.cs)), nicht über den Web-Host.
  `database update` braucht **kein** `--no-build`, sonst läuft es gegen die Assembly von *vor* dem
  `migrations add`.
- `*.db` ist gitignored; eine DB aus einer *alten* Kette weist der Start mit einer handlungsfähigen Meldung
  ab – ein Upgrade-Pfad existiert bewusst nicht.

**Schema-Konventionen** (ebenfalls von `SchemaGuardTests` gehalten):

- Jedes persistierte **Enum liegt als String** in der DB. Ausnahmen nur `[Flags]` und die ordnend
  verglichenen aus `PuglingDbContext.IntEnumsByDesign`, jeweils mit Grund im Code.
- **DB-Defaults** gibt es genau einen (`Exercises.ExecutePublic`, ein Fail-Safe); ein
  `AddColumn(defaultValue:…)` darf keine weiteren nachwachsen lassen.
- Neue **Eindeutigkeit** braucht immer eine Vorprüfung im Controller **plus** einen `ApiErrors`-Code – ohne
  sie wird aus dem 409 ein 500 mit halb gespeichertem Zustand.
- Ein Index auf einer Spalte, die die Query in einen Ausdruck wickelt (`LOWER(Word)`), wird **nie** benutzt:
  dafür trägt `Vocabulary.Word`/`.Translation` die Collation `NOCASE`.
- **String-Längen** kommen aus einer Konventionsschleife (200 / Freitext 2000 / `…Key`,`…Slug` 128), nicht
  von Hand; unbegrenzt nur über `PuglingDbContext.UnlimitedByDesign` mit Grund. Eine unique-indizierte
  String-Spalte **muss** begrenzt sein – auf `NVARCHAR(MAX)` ist kein Unique-Index anlegbar.
- **Kein Zeitpunkt und keine Periode als Text.** Eine Periode ist `(Taktung, Perioden-Anfang)` mit echten
  Typen, und die Taktung gehört als **Momentaufnahme** auf die Log-Zeile – sonst deutet ein Wechsel Tag→Woche
  rückwirkend gebuchte Perioden um.
- **NULL ist in SQLite nicht gleich NULL:** ein Unique-Index über eine nullable Spalte hält die Invariante
  *nicht*. „Genau eine Zeile ohne X" braucht einen **gefilterten** Index, „genau eine je X" einen zweiten mit
  `IS NOT NULL` (Vorbilder `MissionAward`, `ObjectiveReward`, `MediaLink`).
- Eine Invariante „genau eines von N" gehört als **Check-Constraint** in die DB, nicht in einen Kommentar.
- **`db.Model` ist nicht das ganze Modell:** das laufzeit-optimierte wirft Check-Constraints (es *wirft* dann)
  und **Annotationen** weg. Schema-Fragen über `db.GetService<IDesignTimeModel>().Model`, DDL-Details über
  `GetRelationalModel()`. Und `GetValueComparer()` liefert immer etwas – „gesetzt" verrät nur die Annotation.

Der Umbau, der diese Regeln eingezogen hat, samt Begründungen, bewussten Verzichten und Fallstricken:
[docs/db-struktur-umbau-plan.md](../../docs/db-struktur-umbau-plan.md) (abgeschlossen).

## Lern-Katalog

`Subject → Chapter → Exercise` (typisiert, Config als JSON), ein Controller je `ExerciseType`, CRUD geerbt
aus `ExerciseControllerBase<TConfig>` ([Controllers/Creator/ExerciseControllers.cs](Controllers/Creator/ExerciseControllers.cs)).
Route: `api/v1/creator/subjects/{}/chapters/{}/<typ>`.

**Vokabelübungen** halten ihre Paare als **eigene Ebene**: stabil identifizierte `ExerciseItem`s in einer
Tabelle (nicht in der `ConfigJson`), CRUD unter `…/vocabulary/{exerciseId}/items/{itemId}`. Ein Item ist eine
positionierte Referenz auf eine Store-`Vocabulary` (Front/Back/Audio kommen live von dort) plus optionaler
lokaler Hinweis. POST akzeptiert weiterhin inline `items`/`refs` (materialisiert ID-erhaltend per
`ExerciseItemService`); die Config trägt danach nur noch Einstellungen (Direction/Sprachen). Der
Engine-Index ist die Listenposition – zum Legacy-`ItemIndex` kompatibel.

## Unterrichtsmaterial & Creator-Profile

[Models/CurriculumEntities.cs](Models/CurriculumEntities.cs). Die **Lehrwerk-Reihe** ist eine *geteilte*
Katalog-Größe: `TextbookSeries` (slug-idempotent wie `InterestTag`) → `SeriesUnit`; Band und Unit liegen
bewusst in **einer** Ebene (`Grade` = Band). Der fachliche Wert steckt in `Topics`/`Grammar`/
`VocabularyNotes` – das macht einen KI-Creator materialkundig, statt ihn den Stoff erfinden zu lassen.
Route `api/v1/creator/textbook-series` (+ `…/{id}/units`): lesen darf jeder Creator, ändern nur der Owner
(`OwnerAdultId`, Muster `Exercise.AuthorAdultId`, FK `SetNull`).

Das **Kind** zeigt über `Textbook.SeriesId`/`CurrentUnitId` darauf (Titel/`CurrentChapter` bleiben
Rückfallebene für unkatalogisierte Werke). Eine Unit **muss** zur Reihe des Buchs gehören, sonst
`validation_error` – sonst bekäme der Creator den Stoff eines fremden Werks.

Ein **`CreatorProfile`** (`api/v1/creator/profiles`) ist der *Fachlehrer*: Fach, Schulart,
Klassenstufen-Bereich, optional die Reihe, dazu `Persona`/`Didactics` (gehen dem festen Regelblock des
Agenten **voran**, weichen ihn nie auf) und `DefaultTypes` (JSON-Liste → **neu zuweisen**, nicht in place
mutieren). Angelpunkt `…/profiles/match?childId=` (`CreatorProfileService`): harte Ausschlüsse (inaktiv,
Klassenstufe außerhalb, Schulart disjunkt), dann Punkte **Reihe 8 > Fach 4 > Klassenstufe 2 > Schulart 1**,
Gleichstand über die `Id` – deterministisch wie der `MediaSelector`, damit derselbe Datenstand denselben
Lehrer liefert. Der Endpunkt liest Kind-Daten und trägt darum **trotz Creator-Route** eine
Betreuungs-Prüfung (`AuthAccess`, sonst 403). Die `Reasons` sind stabile Codes (`series_match` …), die
Formulierung macht die Oberfläche.

## Lehrplan/Training

`StudyPlan` ist ein **reiner Container** (`ChildId, Title, Start/End, Active`);
[Controllers/Supervisor/StudyPlansController.cs](Controllers/Supervisor/StudyPlansController.cs). Inhalt sind
`PlanPosition`s ([PlanPositionsController.cs](Controllers/Supervisor/PlanPositionsController.cs)), die je auf
eine Katalog-`Exercise` verweisen und **eigenes** Ziel, Punkte, Stufe und Leitner tragen. Route:
`api/v1/supervisor/study-plans/{planId}/…`.

- **`GoalThreshold`** = Bestehensgrenze in **Prozent** (`null` = 80 %), typ-unabhängig auch bei
  Katalog-Checks. Die API weist Werte außerhalb 1–100 ab, weil eine mit Trefferzahlen verwechselte Schwelle
  die Pflicht lautlos *entschärft* statt sie zu verschärfen. Ein `TestAttempt` entsteht nur im
  Positions-Test – ein zweiter Auswertungspfad existiert nicht.
- **Punkte**: Ziel-Belohnung + optionaler **Münz-Malus** `PenaltyCoins` bei gerissener Pflicht (der „Stick").
- **Gespielt wird pro Position**: `PositionPracticeController` (Üben/Leitner) + `PositionTestsController`
  (Abschlusstest). Inhalt kommt aus der Übungs-Config (`ExerciseContentProvider`), Leitner-Fortschritt
  materialisiert je Inhalts-Atom in `PositionItemProgress`. Tagesmission/Verlauf über
  `PlanOverviewController` (`…/overview`, `…/overview/progress`).

**Plan-übergreifender Item-Lernstand** (nur Vokabel): `PositionPracticeController.Review` und
`PositionTestsController.Submit` schreiben je Antwort über `ItemProgressService` einen Stand pro
`(Kind, ItemId)` (`ItemProgress`: Box/Beherrschung/Zähler) plus eine Historie (`ItemReviewEvent`), beide mit
denormalisierter `VocabularyId`. Kind-zentrische Auswertung: `ChildVocabularyProgressController` unter
`api/v1/student/children/{childId}/vocabulary-progress` (Liste mit `?exerciseId/?maxBox/?onlyWeak`,
`/{itemId}`, `/{itemId}/history`, `/by-word`-Rollup für „schlecht gelernte Wörter"). Ergänzt den
positionsgebundenen `PositionReportService` um die plan-übergreifende Sicht.

Ein plan-weites `StudyPlanItem`/`Method`-Modell existiert **nicht** (vollständig entfernt).

## Services

[Services/](Services/) – Logik gehört hierher, nicht in die Controller.

- **`PositionPlayService`** – Fälligkeit/Scope/Stufen + Leitner-Terminierung je Position.
- **`PositionProgressService`** – Ziel-„erledigt"-Regel je `ExerciseCheckMode`, idempotente Ziel-Punkte über
  `PositionGoalReward`, Tages-/Verlaufs-Rollup über Positionen. **Malus fürs Nicht-Lernen** via
  `SettleClosedPeriodsAsync`: gerissene Pflicht-Periode → negative `PointKind.GoalPenalty`, idempotent über
  `PositionGoalPenalty`, **Schuld erlaubt**. Es gibt **keinen Scheduler** → lazy an Kind-Login und Shop-Kauf
  abgerechnet, Fairness bei inaktivem Plan, `ConcurrencyStamp`-Bump.
- **`ScoringService`** – die *eine* Stelle für Review-Punkte: Basis × Zeitfenster plus Ereignis-Boni
  (Combo, schnelle Antwort). Jede Buchung trägt einen `PointKind`; `StageMechanics` hält die geteilten
  Stufen-/Vergleichs-Statics.
- **`MetricsService`** – Fortschritts-Metriken aus den Tabellen.
- **`GamificationService`** – Missionen & Auszeichnungen, idempotent belohnt. Vater-CRUD unter
  `api/v1/supervisor/children/{}/missions|achievements`, Sohn-Sicht `api/v1/student/me/missions|achievements`.

## Reward-Ökonomie (zwei Währungen)

🪙 **Münzen** fürs Lernen → reale Vater-Belohnungen aus dem **Familien-Shop**; 💎 **Gems** aus Boni →
**Skins** (und optionaler Gem-Anteil bei Shop-Artikeln). Währung = reine Funktion des `PointKind`
(`PointKindCurrency`, **keine** Ledger-Spalte); Salden über `WalletService`.
Details: [wiki/05-punkte-und-bonus.md](../../wiki/05-punkte-und-bonus.md).

Der **Familien-Shop** ist der **einzige Münz-Ausgabeweg** (`ShopArticle` → `ShopListing`): Vater pflegt
Artikel mit `UnitType`/`ActionType` und Angebote mit Coin+Gem-Preis sowie Bestand (inkl. `ShopRefillKind`
fürs Auffüllen). Kauf bucht `PointKind.ShopCoins`/`ShopGems` ab und erhöht das aggregierte
`ChildInventory`. Der Sohn stellt eine **Aktivierungsanfrage** (`ActivationRequest`), der Vater
genehmigt/lehnt ab (`ShopService`, `children/{}/shop/activations/{}/approve|reject`). Käufe und
Aktivierungen sind **ausstellergebunden** (`SupervisorId`-Snapshot).

Daneben zwei vater-getriebene Münz-Bewegungen: der **Malus** (`GoalPenalty`, s. o.) zieht ab, das
**Verschenken** gibt dazu. `POST children/{}/points` nimmt `currency` (Coins → `PointKind.Manual`,
Gems → `PointKind.ManualGems`) – Belohnung außerhalb der App und Druckventil gegen Malus-Schulden.
`PointKind.Reward` ist nur noch Ledger-Tombstone historischer Buchungen (das alte „Angebots"-System
`Reward`/`RewardRedemption`/`OfferService` ist entfernt).

> **Invariante – Wallet & Nebenläufigkeit:** Jeder Pfad, der das Wallet **abbucht**, MUSS
> `child.ConcurrencyStamp` bumpen. Der Stamp ist der geteilte Serialisierungspunkt des Saldos; ohne Bump
> führen parallele Käufe zu Doppelspend bzw. negativem Saldo.

## Medien & Interessen

Modell (`MediaAsset`/`MediaVariant`/`MediaLink`) in [Models/MediaEntities.cs](Models/MediaEntities.cs), die
**geteilte Interessen-Taxonomie** (`InterestTag`, von Bildern *und* Kindern referenziert – nur darum ist die
Auswahl berechenbar) in [Models/InterestEntities.cs](Models/InterestEntities.cs). **Ein Motiv, viele
Bilder.** Bewertungs-/Einfrier-Verfahren des `MediaSelector` (`ChildMediaPick`; Bildkonstanz *ist* der
Merkeffekt) und der Upload stehen in [docs/medien-bilder.md](../../docs/medien-bilder.md) – **dort
nachsehen, bevor du an Medien arbeitest.**

Beim Bauen einer Ausspielung muss man diese Regeln vorher kennen:

- **Anti-Cheat:** Bild nur auf **nicht-getippten** Stufen (`ShowBoth`/`SelfAssess`) – schärfer als beim
  Audio, weil ein Motiv die Bedeutung in beide Richtungen zeigt. Der Alt-Text folgt dem Bild.
- Das gilt **auch für „anderes Bild"** am Karten-Endpunkt: er gibt ein Bild *heraus* und trägt darum
  dieselben Schranken wie die Ausspielung (spielbarer Plan, nur Karten der Sitzung, nur wo die Karte ein
  Bild zeigt → `409 media_not_on_card`) – sonst wäre er die Hintertür um die Regel herum.
- Kein Treffer = **kein Bild**, nie ein Notnagel.
- `childId` wird auf dem Weg zur Karte **explizit** durchgereicht (nicht aus einer geladenen Navigation),
  sonst entschiede ein vergessenes `Include` über die Bebilderung.

## Tags & Klassenarbeiten

`api/v1/supervisor/class-tests`
([KlassenarbeitenController.cs](Controllers/Supervisor/KlassenarbeitenController.cs)); die Typnamen heißen
intern weiterhin `Klassenarbeit`. Tagging: [docs/klassenarbeiten-tagging.md](../../docs/klassenarbeiten-tagging.md).

## Anmerkungen beim Testen (`api/v1/remarks`)

Dev-Werkzeug, **kein Produktfeature**: Beobachtung im Widget erfassen (Alt+A) → **Log-Id** → in Claude Code
per Skill `anmerkungen` belegt beantworten. Der Wert steckt im automatisch mitgeschnittenen Kontext (Route,
Kind/Übung, letzte Fehler), nicht im Text. Verlauf, Sichtbarkeits-Schalter (`Remarks:GlobalRead`) und die
Rechte-Regeln stehen in [docs/anmerkungen-plan.md](../../docs/anmerkungen-plan.md) – dort nachsehen, bevor
du an `remarks/…` arbeitest.
