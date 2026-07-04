# Architektur-Entscheidung: API-First + Legacy-Abbau

Stand: 2026-07-04. Kurzformat (ADR). Ersetzt die im [architektur-resumee.md](architektur-resumee.md)
und [code-review.md](code-review.md) offen gelassene Richtungsfrage.

## Kontext

Das Backend ist schnell gewachsen und trug drei parallele Modell-Welten:

1. **Legacy-Template** (`User`, `Topic`, `VocabCard`, `LearningPlan`, `LearningSession`,
   `ReviewLog`, `PointsTransaction`, `RewardOffer`, `TestResult`) mit den Controllern
   `Vocab`, `Sessions`, `Points`, `Settings`.
2. **Produktives Modell**: `Father`/`Child` + JWT-Auth, `StudyPlan`/`TestAttempt`,
   Vokabel-/Lückentext-Store, `Subject→Chapter→Exercise`-Katalog, Tags, Klassenarbeiten.

Befund des Reviews: Das **Frontend** sprach ausschließlich das Legacy-Modell an, während der
gesamte neue Aufwand im produktiven Modell steckt. `TestResult` war komplett tot, sieben weitere
Legacy-Entitäten lebten nur noch in `Seed` + ihren eigenen deprecated Controllern.

## Entscheidung

**Pugling ist API-First.** Die REST-API (OpenAPI/Swagger) ist das Produkt und die einzige
Quelle der Wahrheit; sie wird direkt bzw. über die Skills `vater`/`sohn` bedient. Das produktive
Modell (Father/Child, StudyPlan, Learn-Katalog) ist verbindlich.

Konsequenzen:

- Der **Legacy-Stack wurde entfernt** (siehe unten). Das mitgelieferte React-Frontend hängt
  an den entfernten Endpunkten und ist damit **vorübergehend funktionslos** – bewusst akzeptiert,
  weil das UI-Konzept noch erarbeitet wird und später **neu gegen die neue API** gebaut wird.
- Einziges bewusst erhaltenes Legacy-Element: **`TimeSlotRule`** (Zeitfenster-Multiplikator),
  weil der neue `PracticeSessionsController` es über `PointsService` für Leitner-Punkte nutzt.

## In diesem Schritt umgesetzt

**(b) Toter/Legacy-Code entfernt**
- Gelöscht: `Controllers/VocabController`, `SessionsController`, `PointsController`, `SettingsController`.
- Gelöscht: `Models/Entities.cs` (alle Legacy-Entitäten) – ersetzt durch `Models/TimeSlotRule.cs`
  (das einzig noch genutzte Entity).
- `PuglingDbContext`: 9 tote `DbSet`s entfernt (`TimeSlots` bleibt).
- `PointsService`: Legacy-Überladung `PointsForReviewAsync(VocabCard,…)` und `BalanceAsync` entfernt.
- `Seed`: `SeedCore` → `SeedTimeSlots` (nur noch Zeitfenster; keine `User`/`Topic`/`LearningPlan`-Seeds).
- Bonus-Fix: `AuthController` – `[AllowAnonymous]` lag auf Klassenebene und hebelte das `[Authorize]`
  auf `GET /api/auth/me` aus (Endpunkt war ungewollt anonym). Jetzt liegt `[AllowAnonymous]` gezielt
  auf den beiden Login-Actions; `me` erfordert wieder ein Token.

**(c) Duplikation refactort**
- Neuer `PlanOwnershipFilter` (`IAsyncActionFilter`, DI): prüft `planId`-Ownership zentral.
  Ersetzt den byte-gleich in **5** Controllern duplizierten Inline-Filter via
  `[ServiceFilter(typeof(PlanOwnershipFilter))]` (Tests, ClozeTests, MatchingTests, PracticeSessions, Ratings).
- Neuer `TestAttemptService`: gemeinsamer Attempt-Lebenszyklus (`GetPlanAsync`, `LoadAttemptAsync`,
  `ScoreAndAwardAsync`) für die drei Test-Controller. Entfernt das dreifach duplizierte
  Laden/Scoring/Punkte-Gerüst; die verfahrensspezifische Bewertung bleibt im Controller.

Verifiziert: Build 0 Warnungen/0 Fehler; Live-Smoke-Test (401 ohne Token, 404 des Ownership-Filters,
`auth/me`-Fix, vollständiger Plan→Test→Submit-Flow mit korrekter Punktevergabe).

## Offene Migrationsschritte (priorisiert)

1. **PIN-Hashing** (`Father.Pin`, `Child.Pin`) – vor Produktion zwingend (PBKDF2, konstantzeitiger
   Vergleich, Rate-Limit/Lockout). Aktuell Klartext.
2. **EF-Migrationen** statt `EnsureCreated()` – sobald das Schema stabil ist; sonst kein Upgrade-Pfad.
3. **Frontend neu gegen die neue API** – wenn das UI-Konzept steht. Bis dahin ist das alte PWA-UI außer Betrieb.
4. **Zeitfenster-Verwaltung** – Editieren der `TimeSlotRule`-Multiplikatoren hatte nur der gelöschte
   `SettingsController`. Bei Bedarf einen schlanken, abgesicherten `TimeSlotsController` ergänzen
   (aktuell nur über Seed-Defaults).
5. **`StudyPlanItem`-Polymorphie** – die zwei nullbaren FKs (`VocabularyId`, `ClozeTextId`) skalieren
   nicht über wenige Content-Typen; ab ~4 Typen echte Polymorphie.
6. **N+1 in `progress`/`today`** (`ComputeDayAsync` je Tag) – bei Bedarf gruppiert laden.
7. **Automatisierte Tests** (WebApplicationFactory-Integrationstests) – bislang nur manuelle API-Läufe.
