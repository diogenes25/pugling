---
tags: [typ/adr, bereich/doku, rolle/creator, rolle/supervisor, rolle/student]
---

# Architektur-Entscheidung: API-First + Legacy-Abbau

Stand: 2026-07-04. Kurzformat (ADR). Ersetzt die im [architektur-resumee.md](architektur-resumee.md)
und [code-review.md](code-review.md) offen gelassene Richtungsfrage.

> **Nachtrag (2026-11-12):** Die Entscheidung gilt unverändert; einzelne Namen im Text sind historisch.
> Die Vertrags-Entität heißt heute **`Adult`** statt `Father` ([lehrer-konto-plan.md](lehrer-konto-plan.md)),
> `TimeSlotRule` gibt es nicht mehr (Zeitfenster sind Konfiguration, siehe Punkt 4), und die API wird
> direkt bzw. über die Rollen-Skills `creator`/`supervisor`/`student` bedient – `lehrplan-autor`/
> `lehrplan-lerner` sind eine separate, dateibasierte Spur ohne App-API.

## Kontext

Das Backend ist schnell gewachsen und trug drei parallele Modell-Welten:

1. **Legacy-Template** (`User`, `Topic`, `VocabCard`, `LearningPlan`, `LearningSession`,
   `ReviewLog`, `PointsTransaction`, `RewardOffer`, `TestResult`) mit den Controllern
   `Vocab`, `Sessions`, `Points`, `Settings`.
2. **Produktives Modell**: `Father`/`Child` (heute `Adult`/`Child`) + JWT-Auth, `StudyPlan`/`TestAttempt`,
   Vokabel-/Lückentext-Store, `Subject→Chapter→Exercise`-Katalog (seit B-106 `TextbookSeries→SeriesUnit→Exercise`,
   ohne `Chapter`), Tags, Klassenarbeiten.

Befund des Reviews: Das **Frontend** sprach ausschließlich das Legacy-Modell an, während der
gesamte neue Aufwand im produktiven Modell steckt. `TestResult` war komplett tot, sieben weitere
Legacy-Entitäten lebten nur noch in `Seed` + ihren eigenen deprecated Controllern.

## Entscheidung

**Pugling ist API-First.** Die REST-API (OpenAPI/Swagger) ist das Produkt und die einzige
Quelle der Wahrheit; sie wird direkt bzw. über die Skills `lehrplan-autor`/`lehrplan-lerner` bedient. Das produktive
Modell (Father/Child – heute `Adult`/`Child` –, StudyPlan, Learn-Katalog) ist verbindlich.

Konsequenzen:

- Der **Legacy-Stack wurde entfernt** (siehe unten). Das mitgelieferte Legacy-Frontend wurde
  verworfen und das Frontend unter `frontend/` **neu gegen die `api/v1` gebaut** – seit 2026-07-04
  **funktionsfähig** (Vite+React+TS+PWA: Produktseite `/`, Sohn-App `/sohn`, Vater-Web `/vater` inkl.
  Lehrplan-Assistent). Feature-Arbeit beginnt weiterhin im Backend (API-First); das UI hängt an der API.
- Einziges bewusst erhaltenes Legacy-Element: **`TimeSlotRule`** (Zeitfenster-Multiplikator),
  weil der aktuelle positionsbezogene `ScoringService` es für Leitner-Basispunkte nutzt.
  *Nachtrag:* Seit dem DB-Struktur-Umbau (E12) ist auch diese Tabelle weg – die Zeitfenster sind
  Konfiguration `Scoring:TimeSlots` ([ScoringOptions.cs](../backend/Pugling.Api/Services/Shared/ScoringOptions.cs)),
  eine Position kann eigene ergänzen.

## In diesem Schritt umgesetzt

### (b) Toter/Legacy-Code entfernt

- Gelöscht: `Controllers/VocabController`, `SessionsController`, `PointsController`, `SettingsController`.
- Gelöscht: `Models/Entities.cs` (alle Legacy-Entitäten) – ersetzt durch `Models/TimeSlotRule.cs`
  (das einzig noch genutzte Entity).
- `PuglingDbContext`: 9 tote `DbSet`s entfernt (`TimeSlots` bleibt).
- `PointsService`: Legacy-Überladung `PointsForReviewAsync(VocabCard,…)` und `BalanceAsync` entfernt.
- `Seed`: `SeedCore` → `SeedTimeSlots` (nur noch Zeitfenster; keine `User`/`Topic`/`LearningPlan`-Seeds).
- Bonus-Fix: `AuthController` – `[AllowAnonymous]` lag auf Klassenebene und hebelte das `[Authorize]`
  auf `GET /api/v1/auth/me` aus (Endpunkt war ungewollt anonym). Jetzt liegt `[AllowAnonymous]` gezielt
  auf den beiden Login-Actions; `me` erfordert wieder ein Token.

### (c) Duplikation refactort

- Neuer `PlanOwnershipFilter` (`IAsyncActionFilter`, DI): prüft `planId`-Ownership zentral.
  Ersetzt den byte-gleich in **5** Controllern duplizierten Inline-Filter via
  `[ServiceFilter(typeof(PlanOwnershipFilter))]` (Tests, ClozeTests, MatchingTests, PracticeSessions, Ratings).
- Neuer `TestAttemptService`: gemeinsamer Attempt-Lebenszyklus (`GetPlanAsync`, `LoadAttemptAsync`,
  `ScoreAndAwardAsync`) für die drei Test-Controller. Entfernt das dreifach duplizierte
  Laden/Scoring/Punkte-Gerüst; die verfahrensspezifische Bewertung bleibt im Controller.

Verifiziert: Build 0 Warnungen/0 Fehler; Live-Smoke-Test (401 ohne Token, 404 des Ownership-Filters,
`auth/me`-Fix, vollständiger Plan→Test→Submit-Flow mit korrekter Punktevergabe).

## Offene Migrationsschritte (priorisiert)

1. ~~**PIN-Hashing** (`Father.Pin`, `Child.Pin`; heute `Adult`) – vor Produktion zwingend (PBKDF2, konstantzeitiger
   Vergleich, Rate-Limit/Lockout). Aktuell Klartext.~~ – **erledigt (2026-07-05)**: `Auth/PinHasher`
   plus Login-Rate-Limit (Policy `login`). Die Doppelhaltung des Hashs auf Profil **und** Konto ist seit
   2026-10-26 aufgelöst: Die PIN liegt nur noch am Konto ([B-230](backlog/B-230-pin-hash-doppelt-gehalten.md)).
2. ~~**EF-Migrationen** statt `EnsureCreated()`~~ – **erledigt (2026-07-04)**: `Program` ruft `Migrate()`,
   Initial-Migration `InitialCreate` liegt unter `backend/Pugling.Api/Data/Migrations`, `dotnet-ef` ist als
   lokales Tool im Manifest. Schemaänderungen ab jetzt per `dotnet ef migrations add …`.
   *Nachtrag:* Seit 2026-11-11 wird die Kette nur noch **verlängert** (Basis `20261110142003_InitialCreate`,
   erzwungen von `SchemaGuardTests`), weil die echte `pugling.db` jede Schemaänderung überleben muss.
3. ~~**Frontend neu gegen die neue API**~~ – **erledigt (2026-07-04)**: `frontend/` neu aufgebaut
   (Vite+React+TS+PWA), Sohn-Arcade-PWA `/sohn` + Vater-Web `/vater` (mit Lehrplan-Assistent `/vater/wizard`)
   + Produktseite `/`; Playwright-E2E deckt den Vater→Sohn-Loop ab. Weitere Ausbaustufen laufend.
4. ~~**Zeitfenster-Verwaltung** – Editieren der `TimeSlotRule`-Multiplikatoren hatte nur der gelöschte
   `SettingsController`. Bei Bedarf einen schlanken, abgesicherten `TimeSlotsController` ergänzen
   (aktuell nur über Seed-Defaults).~~ – **gegenstandslos**: Die Tabelle `TimeSlotRule` ist entfernt; die
   globalen Zeitfenster stehen in `Scoring:TimeSlots` (`appsettings.json`, `ScoringOptions`), je Position
   kommen eigene über `PlanPosition.TimeSlots` hinzu. Ein Verwaltungs-Controller ist damit nicht nötig.
5. ~~**`StudyPlanItem`-Polymorphie**~~ – **erledigt (2026-07-05)**: Das alte Item-/Method-Modell wurde
  durch `PlanPosition` ersetzt. Positionen referenzieren Katalog-Übungen; Fortschritt je Inhalts-Atom
  lebt in `PositionItemProgress`.
6. **N+1 in `progress`/`today`** (`ComputeDayAsync` je Tag) – bei Bedarf gruppiert laden.
7. ~~**Automatisierte Tests** (WebApplicationFactory-Integrationstests) – bislang nur manuelle API-Läufe.~~
   – **erledigt**: `backend/Pugling.Api.Tests` (überwiegend Integrationstests gegen den In-Process-TestServer,
   über 1100 Testmethoden, dazu Konventions- und Schema-Tore) läuft als Test-Tor lokal und in CI.
