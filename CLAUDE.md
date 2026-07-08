# Pugling – Claude-Leitfaden

Lern-App mit Punktesystem (Leitner-Prinzip). **Vater** steuert und erzwingt Lernerfolg,
**Sohn** lernt mit Spaß. Drei Ebenen (Creator/Supervisor/Student), Punkte, Zeitfenster, Klassenarbeiten.

## Grundprinzip: API-First

Die REST-API (**OpenAPI/Swagger**) ist das Produkt und die einzige Quelle der Wahrheit –
bedient direkt oder über die Skills `vater`/`sohn`. Das React-Frontend unter [frontend/](frontend/)
wurde **neu gegen die `api/v1` gebaut und ist funktionsfähig** (Vite+React+TS+PWA): Produktseite `/`,
Sohn-Arcade-PWA `/sohn`, Vater-Web `/vater` inkl. Lehrplan-Assistent `/vater/wizard`. Ein Playwright-E2E
([frontend/e2e/full-flow.spec.ts](frontend/e2e/full-flow.spec.ts)) fährt den kompletten Vater→Sohn-Loop.
→ **Neue Features beginnen weiterhin im Backend** (API-First); das Frontend hängt an der API.
Details: [docs/architektur-entscheidung.md](docs/architektur-entscheidung.md), [frontend starten](#frontend).

## Befehle

Immer aus `backend/Pugling.Api` bzw. Repo-Root:

```bash
cd backend/Pugling.Api && dotnet build      # baut die API (nach .cs-Edits automatisch per Hook)
cd backend/Pugling.Api && dotnet run        # http://localhost:5200, Swagger unter /swagger
dotnet test                                 # Integrationstests (backend/Pugling.Api.Tests)
dotnet format backend/Pugling.Api           # Formatierung (läuft nach Edits automatisch)

dotnet tool restore                          # einmalig nach dem Clone (installiert dotnet-ef aus dem Manifest)
dotnet ef migrations add <Name> --project backend/Pugling.Api --output-dir Data/Migrations   # bei Schemaänderung
```

- **Smoke-Test gegen laufende API:** `/smoke-test` (startet gegen eine Temp-DB, prüft Auth +
  Ownership + einen Plan→Test→Submit-Flow, lässt die echte `pugling.db` unangetastet).
- **Neuen Übungstyp/Lernverfahren anlegen:** `/neuer-uebungstyp` (führt den etablierten Prozess).

### Frontend

```bash
cd frontend && npm install        # einmalig
cd frontend && npm run dev         # http://localhost:5173, /api-Proxy → :5200 (Backend muss laufen)
cd frontend && npm run build       # tsc -b && vite build (Typecheck + Prod-Build)
cd frontend && npm run test:e2e    # Playwright: startet Backend (Temp-DB) + Vite, fährt den Vater→Sohn-Loop
```

Rollen im SPA: `/` Produktseite, `/vater` Web-Admin (inkl. `/vater/wizard` Lehrplan-Assistent),
`/sohn` Arcade-PWA. API-Client + Types zentral unter [frontend/src/lib/](frontend/src/lib/).

## Architektur (das produktive Modell)

- **Drei Ebenen** (siehe [docs/grundprinzip.md](docs/grundprinzip.md)): **Creator** (Inhalte), **Supervisor**
  (Steuerung), **Student** (Lernen). Sie sind **Rollen**, entkoppelt vom Login, und schneiden API **und** Code:
  Routen `api/v1/{creator|supervisor|student}/…`, Ordner `Controllers/{Tier}` + `Services/{Creator,Supervisor,Student,Shared}`
  (Sub-Namespaces projektweit via csproj `<Using>`). Das Präfix ist Taxonomie, nicht die Auth-Wand.
- **Identität/Auth** ([Auth/](backend/Pugling.Api/Auth/)): Ein `Account` (Login/PIN-Hash) trägt über
  `AccountProfile` **mehrere Rollen** (`ProfileRole` Creator/Supervisor/Student → `Father`/`Child`-Profil);
  ein Vater ist zugleich Creator+Supervisor. PIN-Login (`auth/{father|child}` oder konto-zentrisch `auth/login`)
  → JWT mit `aid` + je Rolle einem Claim (plus Alias `Roles.Vater`/`Roles.Sohn`) + `fid`/`cid`. `AuthAccess`
  prüft Eigentum OR-verknüpft je Rolle; Bestandsnutzer bekommen Konten per idempotentem `AccountBackfill`.
- **Multi-Supervisor** ([AdminEntities.cs](backend/Pugling.Api/Models/AdminEntities.cs)): `SupervisorLink`
  (Supervisor ⇢ Student) ersetzt die frühere 1:1-`Child.FatherId`. Ein Student hat mehrere Supervisor
  (Vater/Mutter/Oma), je mit eigenem Shop/Angeboten; **Wallet gemeinsam**, Einlösung **ausstellergebunden**
  (Momentaufnahme `SupervisorId` auf `Reward`/`RewardRedemption`/`ShopPurchase`/`ActivationRequest`).
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
- **Lehrplan/Training** ([StudyPlansController](backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs)):
  `StudyPlan` ist ein **reiner Container** (`ChildId, Title, Start/End, Active`). Inhalt sind
  `PlanPosition`s ([PlanPositionsController](backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs)),
  die je auf eine Katalog-`Exercise` verweisen und **eigenes** Ziel (Rhythmus Tag/Woche + Schwelle),
  Punkte, Stufe und Leitner tragen. Gespielt wird pro Position: `PositionPracticeController` (Üben/Leitner)
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
  Übungen taggen, Arbeiten planen/benoten, gezielt üben/wiederholen. Route: `api/v1/supervisor/class-tests`
  (Typnamen intern weiterhin `Klassenarbeit`).
- **Services** ([Services/](backend/Pugling.Api/Services/)): `PositionPlayService` (Fälligkeit/Scope/Stufen +
  Leitner-Terminierung je Position), `PositionProgressService` (Ziel-„erledigt"-Regel je `ExerciseCheckMode`,
  idempotente Ziel-Punkte via `PositionGoalReward`, Tages-/Verlaufs-Rollup über Positionen), `ScoringService`
  (die eine Stelle für Review-Punkte: Basis × Zeitfenster plus Ereignis-Boni wie Combo/Schnelle Antwort;
  jede Buchung trägt einen `PointKind`; `StageMechanics` hält die geteilten Stufen-/Vergleichs-Statics),
  `MetricsService` (Fortschritts-Metriken aus den Tabellen) + `GamificationService` (Missionen &
  Auszeichnungen, idempotent belohnt; Vater-CRUD unter `api/v1/supervisor/children/{}/missions|achievements`,
  Sohn-Sicht `api/v1/student/me/missions|achievements`).
- **Reward-Ökonomie** (zwei Währungen): 🪙 **Münzen** fürs Lernen → reale Vater-**Angebote** oder **Shop-Artikel**,
  💎 **Gems** aus Boni → **Skins** (und optionaler Gem-Anteil bei Shop-Artikeln). Währung = reine Funktion des
  `PointKind` (`PointKindCurrency`, keine Ledger-Spalte); Salden über `WalletService`. Zwei Ausgabe-Kreisläufe:
  (1) **Angebote** (`Reward` mit `Period`/`Quantity` = Kontingent pro Periode) — der Sohn kauft direkt
  (`api/v1/student/me/rewards/{}/purchase`), der Vater erfüllt/storniert (`OfferService`; `children/{}/rewards…/fulfill|cancel`).
  (2) **Familien-Shop** (`ShopArticle` → `ShopListing`): Vater pflegt Artikel-Katalog mit `UnitType`/`ActionType`
  und Angebote mit Coin+Gem-Preis sowie Bestand (inkl. `ShopRefillKind` für automatisches Auffüllen). Kauf bucht
  `PointKind.ShopCoins`/`ShopGems` ab, erhöht das aggregierte Inventar (`ChildInventory`) des Sohns. Sohn stellt
  **Aktivierungsanfrage** (`ActivationRequest`), Vater genehmigt/lehnt ab (`ShopService`;
  `children/{}/shop/activations/{}/approve|reject`). Details: [wiki/05-punkte-und-bonus.md](wiki/05-punkte-und-bonus.md).

## Konventionen (an bestehendem Code orientieren!)

- **Modernes C# 14 / net10, `Nullable` an.** File-scoped Namespaces, Primary Constructors für DI,
  `record`s für DTOs/Requests/Responses, Expression-bodied Members, Pattern Matching, Collection Expressions.
- **Doku auf Deutsch.** Öffentliche Typen/Members tragen `/// <summary>` (fließt in Swagger).
  Kommentare erklären das *Warum* (Geschäftsregel, Anti-Cheat), nicht das Was.
- **Controller dünn**, Logik in Services. DTOs als `record` projizieren – nie EF-Entities zurückgeben.
- **Guard Clauses zuerst** (früh `return NotFound()/Forbid()` bzw. `Problem(statusCode:…, detail:…)`),
  Happy Path un-eingerückt.
- **API-Versionierung**: Alle Routen unter `api/v1/…` – das Versionssegment steckt zentral in
  `ApiRoutes.V1` ([Controllers/ApiRoutes.cs](backend/Pugling.Api/Controllers/ApiRoutes.cs)), Controller
  tragen `[ApiVersion("1.0")]`. Bis zur Publikation bleiben wir bei 1.0 und ändern frei; ein Bruch danach
  läuft über eine parallele `v2` (neue Controller/DTOs neben v1), nicht über Abwärtskompatibilität.
- **Fehler** einheitlich als `ProblemDetails` (RFC 7807) mit **maschinenlesbarem `code`**: statt
  `Problem(statusCode:, detail:)` immer `return this.ProblemWithCode(ApiErrors.<Code>, "…")` nutzen
  (Registry: [Errors/ApiErrors.cs](backend/Pugling.Api/Errors/ApiErrors.cs); Status/Titel/`type`-URI
  kommen aus dem `ApiError`). Neuen fachlichen Fehler? Erst einen Code additiv in `ApiErrors` ergänzen.
  `AddProblemDetails(CustomizeProblemDetails)` + die `CodeStampingProblemDetailsFactory` stempeln leere
  Fehler (404/403/401/429) und unbehandelte 500 mit einem status-basierten Default-Code. Meldungstexte
  (`detail`) sind **englisch** (i18n); der `code` ist stabiler Vertragsbestandteil. Beispiele:
  [docs/api-examples/](docs/api-examples/index.md) (verifiziert von `DocsCaptureTests`).
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
- **PINs im Klartext** (`Father.Pin`/`Child.Pin`) – vor Prod hashen (offen, siehe Migrationsplan).
- **`TimeSlotRule`** ist das *einzige* bewusst erhaltene Legacy-Entity (Leitner-Multiplikator). Alles
  andere aus dem Ursprungs-Template wurde entfernt – **kein** `User`/`Topic`/`VocabCard`/`Points…` mehr anlegen.
- **Zeit/UTC**: Tageslogik nutzt `DateTime.UtcNow`/`DateOnly` – nahe Mitternacht lokal ggf. anderer Kalendertag.
- **JSON-Spalten** (`Gaps`, `WordBank`, `StageSchedule`, `Noun`/`Verb`): funktionieren, weil Controller
  die Listen neu zuweisen; kein In-Place-Mutieren erwarten (fehlender `ValueComparer`).

## Arbeitsweise

- Nach `.cs`-Edits laufen automatisch `dotnet format` (nur die Datei) und `dotnet build` (API); Build-Fehler
  kommen als Feedback zurück. Bei einer Reihe zusammengehöriger Edits ruhig weiterarbeiten – der Hook meldet sich.
- Änderungen mit echtem Laufzeit-Effekt per `/smoke-test` oder gezieltem `curl` gegen `localhost:5200` prüfen,
  nicht nur kompilieren. Für nichttriviale Änderungen einen Integrationstest in `Pugling.Api.Tests` ergänzen.
- Weitere Doku unter [docs/](docs/): Architektur-Resümee, Code-Review, Tutorials, Klassenarbeiten/Tagging.
- **Erst die Wissenskarte, dann breit suchen:** Zusammenhänge/Einstieg über [docs/endpunkt-beziehungen.md](docs/endpunkt-beziehungen.md)
  (Übung→Lehrplan→Kind→Auswertung) und die MOC in [docs/obsidian.md](docs/obsidian.md) – spart Tokens ggü. Voll-Scans.
  Neue Doku nach den dortigen Konventionen taggen (`bereich/…`, `lerntechnik/…`); neue Lerntechnik = neuer `ExerciseType`
  im bestehenden Muster (kein Parallel-Stack), siehe [wiki/08-erweitern.md](wiki/08-erweitern.md).
