# Pugling – Claude-Leitfaden

Lern-App mit Punktesystem (Leitner-Prinzip). **Vater** steuert und erzwingt Lernerfolg,
**Sohn** lernt mit Spaß. Zwei Rollen, Punkte, Zeitfenster, Klassenarbeiten.

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

- **Identität/Auth** ([Auth/](backend/Pugling.Api/Auth/)): `Father`→`Child`, PIN-Login → JWT mit
  Rollen-Claim (`Roles.Vater`/`Roles.Sohn`) + `fid`/`cid`. Eigentum prüft `AuthAccess`
  (`OwnsPlanAsync`/`OwnsChildAsync`/`FatherOwnsChildAsync`).
- **Lern-Katalog** ([Controllers/Learn/ExerciseControllers.cs](backend/Pugling.Api/Controllers/Learn/ExerciseControllers.cs)):
  `Subject → Chapter → Exercise` (typisiert, Config als JSON). Ein Controller je `ExerciseType`,
  erben CRUD aus `ExerciseControllerBase<TConfig>`. Route: `api/v1/learn/subjects/{}/chapters/{}/<typ>`.
- **Lehrplan/Training** ([StudyPlansController](backend/Pugling.Api/Controllers/Learn/StudyPlansController.cs)):
  `StudyPlan` ist ein **reiner Container** (`ChildId, Title, Start/End, Active`). Inhalt sind
  `PlanPosition`s ([PlanPositionsController](backend/Pugling.Api/Controllers/Learn/PlanPositionsController.cs)),
  die je auf eine Katalog-`Exercise` verweisen und **eigenes** Ziel (Rhythmus Tag/Woche + Schwelle),
  Punkte, Stufe und Leitner tragen. Gespielt wird pro Position: `PositionPracticeController` (Üben/Leitner)
  + `PositionTestsController` (Abschlusstest); Inhalt kommt aus der Übungs-Config (`ExerciseContentProvider`),
  Leitner-Fortschritt materialisiert je Inhalts-Atom in `PositionItemProgress`. Tagesmission/Verlauf über
  `PlanOverviewController` (`…/overview` + `…/overview/progress`). Route: `api/v1/study-plans/{planId}/…`.
  Das alte plan-weite `StudyPlanItem`/`Method`-Modell wurde vollständig entfernt (kein Legacy mehr).
- **Tags & Klassenarbeiten** ([KlassenarbeitenController](backend/Pugling.Api/Controllers/Learn/KlassenarbeitenController.cs)):
  Übungen taggen, Arbeiten planen/benoten, gezielt üben/wiederholen. Route: `api/v1/class-tests`
  (Typnamen intern weiterhin `Klassenarbeit`).
- **Services** ([Services/](backend/Pugling.Api/Services/)): `PositionPlayService` (Fälligkeit/Scope/Stufen +
  Leitner-Terminierung je Position), `PositionProgressService` (Ziel-„erledigt"-Regel je `ExerciseCheckMode`,
  idempotente Ziel-Punkte via `PositionGoalReward`, Tages-/Verlaufs-Rollup über Positionen), `ScoringService`
  (die eine Stelle für Review-Punkte: Basis × Zeitfenster plus Ereignis-Boni wie Combo/Schnelle Antwort;
  jede Buchung trägt einen `PointKind`; `StageMechanics` hält die geteilten Stufen-/Vergleichs-Statics),
  `MetricsService` (Fortschritts-Metriken aus den Tabellen) + `GamificationService` (Missionen &
  Auszeichnungen, idempotent belohnt; Vater-CRUD unter `api/v1/children/{}/missions|achievements`,
  Sohn-Sicht `api/v1/me/missions|achievements`).
- **Reward-Ökonomie** (zwei Währungen): 🪙 **Münzen** fürs Lernen → reale Vater-**Angebote**,
  💎 **Gems** aus Boni → **Skins**. Währung = reine Funktion des `PointKind` (`PointKindCurrency`,
  keine Ledger-Spalte); Salden über `WalletService`. Angebote (`Reward` mit `Period`/`Quantity` =
  Kontingent pro Periode) kauft der Sohn **direkt** (`api/v1/me/rewards/{}/purchase`), der Vater
  **erfüllt/storniert** (`OfferService`; `children/{}/rewards…/fulfill|cancel`). Details:
  [wiki/05-punkte-und-bonus.md](wiki/05-punkte-und-bonus.md).

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
- **Fehler** einheitlich als `ProblemDetails` (RFC 7807): `return Problem(statusCode: 400, detail: "…")`
  statt nackter Strings; `AddProblemDetails` + `UseExceptionHandler`/`UseStatusCodePages` formen auch
  leere Fehler (404/403/401) und unbehandelte Exceptions dazu.
- **Eigentum**: Für Endpunkte unter `{planId}` den `[ServiceFilter(typeof(PlanOwnershipFilter))]`,
  für Endpunkte unter `{childId}` den `[ServiceFilter(typeof(ChildOwnershipFilter))]` nutzen
  (nicht inline wiederholen). Sonst `AuthAccess` explizit. Kindbezogene Ressourcen leben unter
  `api/v1/children/{childId}/…`; top-level Aggregate, die nur nach Kind filtern, nehmen `?childId=`.
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
