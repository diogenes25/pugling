# Pugling – Claude-Leitfaden

Lern-App mit Punktesystem (Leitner-Prinzip). **Vater** steuert und **erzwingt** Lernerfolg – Kernidee:
wiederkehrende **Pflichtziele** (täglich/wöchentlich) + Klausuren, und **verpasste Pflicht kostet Münzen**
(der „Stick"; siehe Positions-`PenaltyCoins` / Reward-Ökonomie). **Sohn** lernt mit Spaß. Drei Ebenen
(Creator/Supervisor/Student), Punkte, Zeitfenster, Klassenarbeiten.

## Grundprinzip: API-First

Die REST-API (**OpenAPI/Swagger**) ist das Produkt und die einzige Quelle der Wahrheit – direkt bedient
oder über die Rollen-Skills `creator`/`supervisor`/`student`, die die API je Ebene treiben und die
verifizierten [Rollen-Tutorials](docs/tutorial.md) schreiben. (Das dateibasierte `lehrplan-autor`/
`lehrplan-lerner`-Kursformat ist eine **separate** Spur und berührt die App-API nicht.) Das React-Frontend
unter [frontend/](frontend/) wurde **neu gegen die `api/v1` gebaut und ist funktionsfähig**; Playwright-E2E
unter [frontend/e2e/](frontend/e2e/) tragen den Vater→Sohn-Durchstich (Landkarte: [frontend/CLAUDE.md](frontend/CLAUDE.md)).
→ **Neue Features beginnen weiterhin im Backend** (API-First); das Frontend hängt an der API.
Details: [docs/architektur-entscheidung.md](docs/architektur-entscheidung.md), [frontend starten](#frontend).

## Befehle

`dotnet build` / `run` / `test` / `format` laufen aus `backend/Pugling.Api` bzw. dem Repo-Root;
Format und Build übernimmt nach `.cs`-Edits ohnehin der Hook. Nicht erratbar ist nur das:

```bash
dotnet tool restore                          # einmalig nach dem Clone (installiert dotnet-ef aus dem Manifest)
dotnet ef migrations add <Name> --project backend/Pugling.Api --output-dir Data/Migrations   # bei Schemaänderung
```

Dazu die Kommandos `/smoke-test` (lässt die echte `pugling.db` unangetastet) und `/neuer-uebungstyp`.

### Frontend

Startbefehle stehen in `frontend/package.json`. Routen-Landkarte, UI-Konventionen und die Regel
„Übungstypen kommen aus dem Server-Manifest" liegen in [frontend/CLAUDE.md](frontend/CLAUDE.md) –
die Datei lädt automatisch, sobald du unter `frontend/` arbeitest.

### KI-Creator (Konsolen-Agent)

Der Konsolen-Agent, der die Creator-Rolle übernimmt (Briefing/Entwurf/Klausur gegen die laufende API):
Aufrufe und Betriebsarten stehen im Skill `ki-creator`, die Architektur unter „Konventionen".
Details: [backend/Pugling.Agent.Creator/README.md](backend/Pugling.Agent.Creator/README.md).

## Architektur (das produktive Modell)

- **Drei Ebenen** (siehe [docs/grundprinzip.md](docs/grundprinzip.md)): **Creator** (Inhalte), **Supervisor**
  (Steuerung), **Student** (Lernen). Sie sind **Rollen**, entkoppelt vom Login, und schneiden API **und** Code:
  Routen `api/v1/{creator|supervisor|student}/…`, Ordner `Controllers/{Tier}` + `Services/{Creator,Supervisor,Student,Shared}`
  (Sub-Namespaces projektweit via csproj `<Using>`). Das Präfix ist die **Taxonomie**; die Auth-Wand deckt
  sich damit, wo gegated wird: schreibende Creator-/Supervisor-Endpunkte tragen `[Authorize(Roles = Roles.Creator)]`
  bzw. `Roles.Supervisor`. Die spielenden/lesenden **Student-Endpunkte** sind bewusst nur `[Authorize]` und
  trennen die Rollen inline (`IsSupervisor`/`IsStudent`), damit der Supervisor für Vorschau/Nachtrag mitlesen darf.
  **Bewusste Ausnahmen ohne Ebenen-Präfix** (kein Versehen): `auth/…` und `remarks/…` – Ressourcen, die
  keiner Ebene gehören, weil derselbe Mensch sie aus mehreren Rollen bedient.
- **Anmerkungen beim Testen** (`api/v1/remarks`, Dev-Werkzeug – kein Produktfeature): Beobachtung im
  Widget erfassen (Alt+A) → **Log-Id** → in Claude Code per Skill `anmerkungen` belegt beantworten. Der
  Wert steckt im automatisch mitgeschnittenen Kontext (Route, Kind/Übung, letzte Fehler), nicht im Text.
  Verlauf, Sichtbarkeits-Schalter (`Remarks:GlobalRead`) und die Rechte-Regeln stehen in
  [docs/anmerkungen-plan.md](docs/anmerkungen-plan.md) – dort nachsehen, bevor du an `remarks/…` arbeitest.
- **`Adult` statt `Father`** ([docs/lehrer-konto-plan.md](docs/lehrer-konto-plan.md)): die fachliche Zeile
  hinter jeder Nicht-Kind-Rolle heißt **`Adult`** – an ihr hängen Autorschaft (`Exercise.AuthorAdultId`)
  und die RWX-Rechte (`ExerciseGrant.CreatorId`). Sie hieß `Father`, trägt aber auch ein **Lehrer-Konto**
  ohne Betreuungsauftrag. „Vater" bleibt richtig, wo ein Vater gemeint ist – etwa in
  `SupervisorRelation.Father` als Verwandtschaftsangabe und in der Oberfläche. Der Token-Claim heißt
  weiter `fid` (er steckt in ausgestellten Tokens), der Zugriff `User.AdultId()`. Der **Vertrag** ist
  nachgezogen ([Etappe 2](docs/father-zu-adult-etappe2-plan.md)): `AdultResponse`/`CreateAdultDto`,
  `supervisor/adults` und `auth/adult` (letzterer meldet auch das Lehrer-Konto an, darum der Name),
  dazu die Felder `AuthorAdultId`/`OwnerAdultId`/`GrantedByAdultId`/`MeResponse.AdultId`. Rein interne
  Namen (`EnsureForFatherAsync`, `FatherOwnsChildAsync`, lokale `fatherId`) tragen den alten Namen noch.
- **Identität/Auth** ([Auth/](backend/Pugling.Api/Auth/)): Ein `Account` (Login/PIN-Hash) trägt über
  `AccountProfile` **mehrere Rollen** (`ProfileRole` Creator/Supervisor/Student → `Adult`/`Child`-Profil);
  ein Vater ist zugleich Creator+Supervisor. PIN-Login (`auth/{father|child}` oder konto-zentrisch `auth/login`)
  → JWT mit `aid` + je Rolle einem Ebenen-Claim (`Creator`/`Supervisor`/`Student`) + `fid`/`cid`. Das frühere
  Vater/Sohn-Alias wurde **entfernt**; gegated wird direkt auf die Ebenen-Rollen, `LoginResponse.role` ist
  `Supervisor` bzw. `Student` (fürs UI-Routing). `AuthAccess` prüft Eigentum OR-verknüpft je Rolle
  (`IsSupervisor`/`IsStudent`); Bestandsnutzer bekommen Konten per idempotentem `AccountBackfill`.
- **Multi-Supervisor** ([AdminEntities.cs](backend/Pugling.Api/Models/AdminEntities.cs)): `SupervisorLink`
  (Supervisor ⇢ Student) ersetzt die frühere 1:1-Bindung Kind→Erwachsener. Ein Student hat mehrere Supervisor
  (Vater/Mutter/Oma), je mit eigenem Familien-Shop; **Wallet gemeinsam**, Einlösung **ausstellergebunden**
  (Momentaufnahme `SupervisorId` auf `ShopPurchase`/`ActivationRequest`).
  Betreuung: `…/supervisor/children/{id}/supervisors`.
- **Lern-Katalog** ([Controllers/Creator/ExerciseControllers.cs](backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs)):
  `Subject → Chapter → Exercise` (typisiert, Config als JSON). Ein Controller je `ExerciseType`,
  erben CRUD aus `ExerciseControllerBase<TConfig>`. Route: `api/v1/creator/subjects/{}/chapters/{}/<typ>`.
  **Vokabelübungen** halten ihre Vokabelpaare als **eigene Ebene**: stabil identifizierte `ExerciseItem`s
  (Tabelle, nicht mehr in der ConfigJson) mit CRUD unter `…/vocabulary/{exerciseId}/items/{itemId}`. Ein Item
  ist eine positionierte Referenz auf eine Store-`Vocabulary` (Front/Back/Audio kommen live von dort) + optionaler
  lokaler Hinweis. POST akzeptiert weiterhin inline `items`/`refs` im Payload (materialisiert per `ExerciseItemService`,
  ID-erhaltend); die Config trägt danach nur noch Einstellungen (Direction/Sprachen). Der Resolver liest Vokabel-Items
  aus der Tabelle; der Engine-Index ist die Listenposition (bleibt zum Legacy-`ItemIndex` kompatibel).
- **Unterrichtsmaterial & Creator-Profile** ([CurriculumEntities.cs](backend/Pugling.Api/Models/CurriculumEntities.cs)):
  Die **Lehrwerk-Reihe** ist eine *geteilte* Katalog-Größe: `TextbookSeries` („Access", Slug-idempotent wie
  `InterestTag`) → `SeriesUnit`. Band und Unit liegen bewusst in **einer** Ebene (`Grade` = Band); der
  fachliche Wert steckt in `Topics`/`Grammar`/`VocabularyNotes` – **das** macht einen KI-Creator
  materialkundig, statt ihn den Stoff der Unit erfinden zu lassen. Route `api/v1/creator/textbook-series`
  (+ `…/{id}/units`), lesen darf jeder Creator, ändern nur der Owner (`OwnerAdultId`, Muster
  `Exercise.AuthorAdultId`, FK `SetNull`). Das **Kind** zeigt über `Textbook.SeriesId`/`CurrentUnitId`
  darauf (Titel/`CurrentChapter` bleiben Rückfallebene für unkatalogisierte Werke); eine Unit muss zur
  Reihe des Buchs gehören, sonst `validation_error` – sonst bekäme der Creator den Stoff eines fremden Werks.
  Ein **`CreatorProfile`** (`api/v1/creator/profiles`) ist der *Fachlehrer*: Fach, Schulart,
  Klassenstufen-Bereich, optional die Reihe, dazu `Persona`/`Didactics` (gehen dem festen Regelblock des
  Agenten **voran**, weichen ihn nie auf) und `DefaultTypes` (JSON-Liste → Neuzuweisung, kein
  In-Place-Mutieren). Der Angelpunkt ist `…/profiles/match?childId=` (`CreatorProfileService`):
  harte Ausschlüsse (inaktiv, Klassenstufe außerhalb, Schulart disjunkt), dann Punkte
  **Reihe 8 > Fach 4 > Klassenstufe 2 > Schulart 1**, Gleichstand über die `Id` – deterministisch wie der
  `MediaSelector`, damit derselbe Datenstand denselben Lehrer liefert. Der Endpunkt liest Kind-Daten und
  trägt darum trotz Creator-Route eine **Betreuungs-Prüfung** (`AuthAccess`, sonst 403); die `Reasons` sind
  stabile Codes (`series_match` …), die Formulierung macht die Oberfläche.
- **Lehrplan/Training** ([StudyPlansController](backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs)):
  `StudyPlan` ist ein **reiner Container** (`ChildId, Title, Start/End, Active`). Inhalt sind
  `PlanPosition`s ([PlanPositionsController](backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs)),
  die je auf eine Katalog-`Exercise` verweisen und **eigenes** Ziel (Rhythmus Tag/Woche + `GoalThreshold`
  = Bestehensgrenze in **Prozent**, `null` = 80 %, typ-unabhängig auch bei Katalog-Checks – ein
  `TestAttempt` entsteht nur im Positions-Test, ein zweiter Auswertungspfad existiert nicht; die API weist
  Werte außerhalb 1–100 ab, weil eine mit Trefferzahlen verwechselte Schwelle die Pflicht lautlos
  entschärft statt sie zu verschärfen),
  Punkte (Ziel-Belohnung + optionaler **Münz-Malus** `PenaltyCoins` bei gerissener Pflicht = der „Stick"),
  Stufe und Leitner tragen. Gespielt wird pro Position: `PositionPracticeController` (Üben/Leitner)
  + `PositionTestsController` (Abschlusstest); Inhalt kommt aus der Übungs-Config (`ExerciseContentProvider`),
  Leitner-Fortschritt materialisiert je Inhalts-Atom in `PositionItemProgress`. Tagesmission/Verlauf über
  `PlanOverviewController` (`…/overview` + `…/overview/progress`). Route: `api/v1/supervisor/study-plans/{planId}/…`.
  Das alte plan-weite `StudyPlanItem`/`Method`-Modell wurde vollständig entfernt (kein Legacy mehr).
  **Plan-übergreifender Item-Lernstand** (nur Vokabel): `PositionPracticeController.Review`/`PositionTestsController.Submit`
  schreiben je Antwort über `ItemProgressService` einen Stand pro `(Kind, ItemId)` (`ItemProgress`: Box/Beherrschung/Zähler)
  + eine Antwort-Historie (`ItemReviewEvent`), beide mit denormalisierter `VocabularyId`. Kind-zentrische Auswertung:
  `ChildVocabularyProgressController` unter `api/v1/student/children/{childId}/vocabulary-progress` (Liste mit `?exerciseId/?maxBox/?onlyWeak`,
  `/{itemId}`, `/{itemId}/history`, `/by-word`-Rollup für „schlecht gelernte Wörter"). Ergänzt den positionsgebundenen
  `PositionReportService` um die plan-übergreifende Sicht.
- **Tags & Klassenarbeiten** ([KlassenarbeitenController](backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs)):
  Route: `api/v1/supervisor/class-tests` (Typnamen intern weiterhin `Klassenarbeit`).
- **Medien & Interessen** ([MediaEntities.cs](backend/Pugling.Api/Models/MediaEntities.cs),
  [InterestEntities.cs](backend/Pugling.Api/Models/InterestEntities.cs), Plan:
  [docs/medien-bilder.md](docs/medien-bilder.md)): **Ein Motiv, viele Bilder.** Modell (`MediaAsset` /
  `MediaVariant` / `MediaLink`), die **geteilte Interessen-Taxonomie** (`InterestTag` von Bildern *und*
  Kindern referenziert – nur darum ist die Auswahl berechenbar), das Bewertungs-/Einfrier-Verfahren des
  `MediaSelector` (`ChildMediaPick`; Bildkonstanz *ist* der Merkeffekt) und der Upload stehen in
  [docs/medien-bilder.md](docs/medien-bilder.md) – **dort nachsehen, bevor du an Medien arbeitest.**
  Resident bleiben nur die Regeln, die man beim Bauen einer Ausspielung schon kennen muss:
  **Anti-Cheat:** Bild nur auf nicht-getippten Stufen (`ShowBoth`/`SelfAssess`) – schärfer als beim Audio,
  weil ein Motiv die Bedeutung in beide Richtungen zeigt; der Alt-Text folgt dem Bild. Das gilt **auch für
  „anderes Bild"** am Karten-Endpunkt: er gibt ein Bild *heraus* und trägt darum dieselben Schranken wie die
  Ausspielung (spielbarer Plan, nur Karten der Sitzung, nur wo die Karte ein Bild zeigt →
  `409 media_not_on_card`) – sonst wäre er die Hintertür um die Regel herum.
  Kein Treffer = **kein Bild**, nie ein Notnagel. `childId` wird auf dem Weg zur Karte **explizit**
  durchgereicht (nicht aus einer geladenen Navigation), sonst entschiede ein vergessenes `Include` über die
  Bebilderung.
- **Services** ([Services/](backend/Pugling.Api/Services/)): `PositionPlayService` (Fälligkeit/Scope/Stufen +
  Leitner-Terminierung je Position), `PositionProgressService` (Ziel-„erledigt"-Regel je `ExerciseCheckMode`,
  idempotente Ziel-Punkte via `PositionGoalReward`, Tages-/Verlaufs-Rollup über Positionen; **Malus fürs
  Nicht-Lernen** via `SettleClosedPeriodsAsync`: gerissene Pflicht-Periode → negative `PointKind.GoalPenalty`,
  idempotent über `PositionGoalPenalty`, **Schuld erlaubt**; es gibt **keinen Scheduler** → lazy an Kind-Login
  und Shop-Kauf abgerechnet, Fairness bei inaktivem Plan, `ConcurrencyStamp`-Bump), `ScoringService`
  (die eine Stelle für Review-Punkte: Basis × Zeitfenster plus Ereignis-Boni wie Combo/Schnelle Antwort;
  jede Buchung trägt einen `PointKind`; `StageMechanics` hält die geteilten Stufen-/Vergleichs-Statics),
  `MetricsService` (Fortschritts-Metriken aus den Tabellen) + `GamificationService` (Missionen &
  Auszeichnungen, idempotent belohnt; Vater-CRUD unter `api/v1/supervisor/children/{}/missions|achievements`,
  Sohn-Sicht `api/v1/student/me/missions|achievements`).
- **Reward-Ökonomie** (zwei Währungen): 🪙 **Münzen** fürs Lernen → reale Vater-Belohnungen aus dem
  **Familien-Shop**, 💎 **Gems** aus Boni → **Skins** (und optionaler Gem-Anteil bei Shop-Artikeln). Währung =
  reine Funktion des `PointKind` (`PointKindCurrency`, keine Ledger-Spalte); Salden über `WalletService`.
  Der **Familien-Shop** ist der **einzige Münz-Ausgabeweg** (`ShopArticle` → `ShopListing`): Vater pflegt
  Artikel-Katalog mit `UnitType`/`ActionType` und Angebote mit Coin+Gem-Preis sowie Bestand (inkl.
  `ShopRefillKind` für automatisches Auffüllen). Kauf bucht `PointKind.ShopCoins`/`ShopGems` ab, erhöht das
  aggregierte Inventar (`ChildInventory`) des Sohns. Sohn stellt **Aktivierungsanfrage** (`ActivationRequest`),
  Vater genehmigt/lehnt ab (`ShopService`; `children/{}/shop/activations/{}/approve|reject`). Käufe/Aktivierungen
  sind **ausstellergebunden** (`SupervisorId`-Snapshot). Der Shop ist der einzige Weg, auf dem der **Sohn**
  Münzen *ausgibt*; daneben stehen zwei vater-getriebene Münz-Bewegungen: der **Malus** (`GoalPenalty`, s. o.)
  zieht ab, das **Verschenken** gibt dazu. **Verschenken/Manuell:** `POST children/{}/points` nimmt `currency`
  (Coins → `PointKind.Manual`, Gems → `PointKind.ManualGems`) – der Vater kann Münzen **und Gems** verschenken
  (Belohnung außerhalb der App + Druckventil gegen Malus-Schulden). Das frühere separate „Angebots"-System
  (`Reward`/`RewardRedemption`/`OfferService`) wurde **entfernt**; `PointKind.Reward` bleibt nur als Ledger-Tombstone
  für historische Buchungen. Details: [wiki/05-punkte-und-bonus.md](wiki/05-punkte-und-bonus.md).

## Konventionen (an bestehendem Code orientieren!)

- **Doku auf Deutsch.** Öffentliche Typen/Members tragen `/// <summary>` (fließt in Swagger).
  Kommentare erklären das *Warum* (Geschäftsregel, Anti-Cheat), nicht das Was.
- **Controller dünn**, Logik in Services. DTOs als `record` projizieren – nie EF-Entities zurückgeben.
- **Vertrag im eigenen Projekt** ([backend/Pugling.Contracts/](backend/Pugling.Contracts/)): *alle* Request-/
  Response-`record`s und die geteilten Basistypen (Enums, `StageStep`, die Übungs-Configs) liegen dort –
  **nicht** als verschachtelte Typen im Controller. Neues DTO? Ins Vertrags-Projekt, mit `/// <summary>`.
  Namen sind **global eindeutig** zu halten (der OpenAPI-Generator schlüsselt Schemas über den einfachen
  Typnamen; gleichnamige Records verschmelzen sonst still zu einem Schema). Namespace-Aufteilung und die
  Blatt-Regel stehen in [backend/Pugling.Contracts/CLAUDE.md](backend/Pugling.Contracts/CLAUDE.md).
- **Client-Bibliothek** ([backend/Pugling.Client/](backend/Pugling.Client/)): die *eine* HTTP-Schicht für
  Nicht-Browser-Konsumenten (die KI-Agenten). Neuer Endpunkt? Erst Backend, dann dort eine einzeilige
  Methode ergänzen – nie HTTP-Plumbing duplizieren. Aufbau (AuthHandler/TokenStore/Fassaden):
  [backend/Pugling.Client/CLAUDE.md](backend/Pugling.Client/CLAUDE.md).
- **KI-Creator** ([backend/Pugling.Agent.Creator/](backend/Pugling.Agent.Creator/README.md)): Konsolen-App,
  die die Creator-Rolle übernimmt – lokal gegen Ollama, **deterministische Pipeline** (C# besitzt den Ablauf,
  das Modell liefert nur strukturierten Inhalt – kein Tool-Calling). Fachliche Kernregel: **Interessen
  kleiden den Stoff ein, sie ersetzen ihn nie**. Pipeline, Briefing-Quellen und `ExamPlanner`:
  [backend/Pugling.Agent.Creator/CLAUDE.md](backend/Pugling.Agent.Creator/CLAUDE.md).
- **Guard Clauses zuerst** (früh `return NotFound()/Forbid()` bzw. `Problem(statusCode:…, detail:…)`),
  Happy Path un-eingerückt.
- **API-Versionierung**: Alle Routen unter `api/v1/…` – das Versionssegment steckt zentral in
  `ApiRoutes.V1` ([Controllers/ApiRoutes.cs](backend/Pugling.Api/Controllers/ApiRoutes.cs)), Controller
  tragen `[ApiVersion("1.0")]`. Bis zur Publikation bleiben wir bei 1.0 und ändern frei; ein Bruch danach
  läuft über eine parallele `v2` (neue Controller/DTOs neben v1), nicht über Abwärtskompatibilität.
- **Mechanische Tore statt Disziplin** ([Directory.Build.props](Directory.Build.props), [.editorconfig](.editorconfig),
  `ConventionGuardTests`): Warnungen **sind Fehler** (`TreatWarningsAsErrors`; NuGet-Audit NU1901–1904 bewusst
  nicht, die ändern sich ohne Codeänderung), fehlende `/// <summary>` (CS1591) brechen den Build in
  `Contracts`/`Client`/`Agent.Creator` – in `Pugling.Api` noch nicht (428 der Treffer sind EF-Entities,
  siehe csproj-Kommentar). Vier reflexive Wächter halten fest, was vorher nur Prosa war: Fehler nur über
  `ProblemWithCode`, Antworttypen nur aus `Pugling.Contracts`, dortige Typnamen global eindeutig,
  Ownership über die geteilten Filter. `CancellationToken` an Actions gilt seit 2026-07-30 ebenfalls **hart**
  (vorher Zuwachs-Sperre mit Baseline; die 188 Altlasten sind abgearbeitet): jede async Action nimmt
  `CancellationToken ct = default` als **letzten** Parameter – der Vorgabewert ist nötig, weil C# keinen
  erforderlichen Parameter nach den optionalen `[FromQuery]`-Werten erlaubt – und reicht ihn in jeden
  EF-/Service-Aufruf durch. Der Wächter prüft die **Signatur**, nicht die Kette dahinter: ein Helfer ohne
  Token-Parameter verbirgt jeden Aufruf in seinem Rumpf vor CA2016, und in Lambdas schweigt der Analyzer
  ohnehin – ein neuer Helfer nimmt den Token also mit, sonst versickert er lautlos hinter grünem Build.
  Und zwar **ohne `= default`**: das Weglassen eines optionalen Arguments rügt CA2016 nicht, ein Helfer
  ohne Vorgabewert lässt den Compiler das Durchreichen erzwingen. Der Vorgabewert bleibt den Actions
  vorbehalten (dort erzwingt ihn die Parameter-Reihenfolge). Umgekehrt gilt für **kompensierende Schritte
  nach dem Commit** (Dateien wegräumen o. Ä.) bewusst `CancellationToken.None` – ein Client-Abbruch darf
  nicht entscheiden, ob aufgeräumt wird, und ein Abbruch *des Clients* endet über den
  `ClientAbortExceptionHandler` als 499 ohne Fehler-Log, nicht als 500.
  Dazu drei Wächter aus [Etappe C](docs/codequalitaet-gates-plan.md): die **Ownership-Matrix**
  (jede Action unter `{childId}`/`{planId}` wird mit fremdem Zugang aufgerufen und muss abweisen), der
  **PATCH-Semantik-Guard** (`null` ändert nichts, `Clear…` leert und gewinnt – reflexiv gegen *alle*
  `Update…Dto`/`Update…Request` geprüft) und der **Endpunkt-Abdeckungs-Wächter**: keine Controller-Action
  ohne einen Test, der sie mit Status < 400 aufruft. Letzterer urteilt im Assembly-Fixture, weil erst dort
  alle Tests durch sind – die Konsole verschluckt seine Meldung, sie steht in
  `TestResults/endpoint-coverage.txt` (Stop-Hook und CI geben sie aus). Neue Regel scharf stellen? Erst
  messen – Begründung in [docs/codequalitaet-gates-plan.md](docs/codequalitaet-gates-plan.md).
- **Unbekannte Felder werden abgelehnt** (`UnmappedMemberHandling.Disallow`): ein Feld, das der Vertrag
  nicht kennt, liefert `400` mit `code: unknown_field` – nicht mehr `201` mit stillem Datenverlust. Wer
  einen Payload schreibt (Test, Client, Frontend), muss die Feldnamen des DTOs treffen; ein vertippter
  Name ist ab jetzt ein Fehler und kein Nichts.
- **Fehler** einheitlich als `ProblemDetails` (RFC 7807) mit **maschinenlesbarem `code`**: statt
  `Problem(statusCode:, detail:)` immer `return this.ProblemWithCode(ApiErrors.<Code>, "…")` nutzen
  (Registry: [Errors/ApiErrors.cs](backend/Pugling.Api/Errors/ApiErrors.cs); Status/Titel/`type`-URI
  kommen aus dem `ApiError`). Neuen fachlichen Fehler? Erst einen Code additiv in `ApiErrors` ergänzen.
  `AddProblemDetails(CustomizeProblemDetails)` + die `CodeStampingProblemDetailsFactory` stempeln leere
  Fehler (404/403/401/429) und unbehandelte 500 mit einem status-basierten Default-Code. Meldungstexte
  (`detail`) sind **englisch** (i18n); der `code` ist stabiler Vertragsbestandteil. Beispiele:
  [docs/api-examples/](docs/api-examples/index.md) (verifiziert von `DocsCaptureTests`).
- **PATCH-Semantik**: `null` heißt „nicht angegeben" (der Wert bleibt), **nicht** „leeren". Ein Feld
  löschbar zu machen braucht darum einen ausdrücklichen `bool Clear<Feld>`-Schalter im Update-DTO – so wie
  `UpdateKlassenarbeitDto.ClearGrade`, `UpdateChildDto.ClearBirthYear/ClearGrade`,
  `UpdateTextbookDto.ClearSeries/ClearUnit/…`, `UpdateCreatorProfileDto.ClearSubject/ClearSeries/…`.
  Im Controller **erst den Wert, dann den Schalter** anwenden, damit „leeren" gewinnt, wenn ein Formular
  beides schickt. Ohne den Schalter meldet eine Oberfläche mit „– keine Angabe –" fröhlich „Gespeichert."
  und der alte Wert steht weiter da (verifiziert von `PatchClearFieldTests`).
- **Eigentum**: Für Endpunkte unter `{planId}` den `[ServiceFilter(typeof(PlanOwnershipFilter))]`,
  für Endpunkte unter `{childId}` den `[ServiceFilter(typeof(ChildOwnershipFilter))]` nutzen
  (nicht inline wiederholen). Sonst `AuthAccess` explizit. Kindbezogene Ressourcen leben unter
  `api/v1/supervisor/children/{childId}/…`; top-level Aggregate, die nur nach Kind filtern, nehmen `?childId=`.
- **EF**: `AsNoTracking()` für Lesequeries, in DB filtern (`Where` vor `ToListAsync`), N+1 via `Include`/
  Projektion vermeiden, `async`/`Async`-Suffix, `CancellationToken` durchreichen.
- **Rolle & Selbstbetrug**: Für den Sohn serverseitig erzwingen (Stufe aus dem Fahrplan, Heartbeat clampen,
  fremde Tage nur der Vater). Neue Endpunkte immer role-/ownership-sauber.

## Fallstricke

- **EF-Migrationen** ([Program.cs](backend/Pugling.Api/Program.cs) ruft beim Start `db.Database.Migrate()`):
  Bei jeder Schemaänderung eine Migration erzeugen (`dotnet ef migrations add …`, siehe Befehle) – **nicht**
  auf `EnsureCreated` zurückfallen. Die EF-Tools laufen über die Design-Time-Factory
  ([Data/PuglingDbContextFactory.cs](backend/Pugling.Api/Data/PuglingDbContextFactory.cs)), nicht über den Web-Host.
  `*.db` ist gitignored; eine alte, per `EnsureCreated` erzeugte DB einmalig löschen (wird neu migriert + geseedet).
- **PINs sind gehasht** (`Auth/PinHasher`): `Adult.Pin`/`Child.Pin` und `Account.PinHash` halten den Hash,
  nie den Klartext. Wer eine PIN setzt, muss durch `PinHasher.Hash` und den Hash **auf das Konto spiegeln**
  (sonst läuft der konto-zentrische `/auth/login` aus dem Takt) – siehe `ChildrenController`/`AdultsController`.
  Der PIN-Login ist zusätzlich per `AddRateLimiter` gebremst (Policy `login`, über `RateLimiting:LoginEnabled`
  abschaltbar – der In-Process-TestServer teilt sonst eine IP-Partition und bekäme 429).
- **`TimeSlotRule`** ist das *einzige* bewusst erhaltene Legacy-Entity (Leitner-Multiplikator). Alles
  andere aus dem Ursprungs-Template wurde entfernt – **kein** `User`/`Topic`/`VocabCard`/`Points…` mehr anlegen.
- **Zeit/UTC**: Tageslogik nutzt `DateTime.UtcNow`/`DateOnly` – nahe Mitternacht lokal ggf. anderer Kalendertag.
- **JSON-Spalten** (`Gaps`, `WordBank`, `BoxIntervalDays`, `StageSchedule`, `Noun`/`Verb`, `Interests`,
  `OwnedSkins`, `SuggestedBonus`): tragen alle einen `ValueComparer` aus
  [Data/JsonValueComparer.cs](backend/Pugling.Api/Data/JsonValueComparer.cs) – EF erkennt Änderungen also
  auch bei In-Place-Mutation. **Neue JSON-Spalte? Comparer nicht vergessen**, sonst gehen Änderungen still
  verloren, solange niemand die Liste neu zuweist.

## Arbeitsweise

- Nach `.cs`-Edits laufen automatisch `dotnet format whitespace` (nur die geänderte Datei) und
  `dotnet build` – jeweils nur für das **besitzende Projekt**, nicht die Solution; Build-Fehler kommen als
  Feedback zurück. Bei einer Reihe zusammengehöriger Edits ruhig weiterarbeiten – der Hook meldet sich.
  Zwei Dinge deckt er bewusst **nicht** ab, dafür vor dem Commit je einen Lauf: einen Umbau in
  `Contracts`/`Client`, der einen abhängigen Nachbarn bricht (`dotnet build Pugling.sln`), und alles
  jenseits von Einrückung/Umbrüchen (`dotnet format Pugling.sln` – kostet mit Analyzern ~23 s, darum
  nicht im Hook).
- **Test-Tor am Ende der Antwort** (Stop-Hook [.claude/hooks/test-gate.sh](.claude/hooks/test-gate.sh)):
  Weichen `.cs`-Dateien von `HEAD` ab, läuft `dotnet test Pugling.sln -c Release` (~63 s) – rot blockt und
  meldet die gefallenen Tests zurück. Zweimal derselbe Stand wird nicht zweimal getestet (Fingerprint), und
  `PUGLING_SKIP_TEST_GATE=1` schaltet ab, wenn rot der beabsichtigte Zwischenstand eines Umbaus ist.
  `Release` ist Absicht: ein parallel laufender Dev-Server sperrt sonst die Debug-Ausgabe.
  Dasselbe Tor steht in CI ([.github/workflows/ci.yml](.github/workflows/ci.yml)) vor jedem Push und
  Pull Request; das Azure-Deploy hängt per `workflow_run` daran und läuft **nur bei grün** an.
  Hintergrund und die offenen Etappen: [docs/codequalitaet-gates-plan.md](docs/codequalitaet-gates-plan.md).
- Änderungen mit echtem Laufzeit-Effekt per `/smoke-test` oder gezieltem `curl` gegen `localhost:5200` prüfen,
  nicht nur kompilieren. Für nichttriviale Änderungen einen Integrationstest in `Pugling.Api.Tests` ergänzen.
- Weitere Doku unter [docs/](docs/): Architektur-Resümee, Code-Review, Tutorials, Klassenarbeiten/Tagging.
- **Erst die Wissenskarte, dann breit suchen:** Zusammenhänge/Einstieg über [docs/endpunkt-beziehungen.md](docs/endpunkt-beziehungen.md)
  (Übung→Lehrplan→Kind→Auswertung) und die MOC in [docs/obsidian.md](docs/obsidian.md) – spart Tokens ggü. Voll-Scans.
  Neue Doku nach den dortigen Konventionen taggen (`bereich/…`, `lerntechnik/…`); neue Lerntechnik = neuer `ExerciseType`
  im bestehenden Muster (kein Parallel-Stack), siehe [wiki/08-erweitern.md](wiki/08-erweitern.md).
