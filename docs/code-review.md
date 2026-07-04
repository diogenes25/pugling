# Code-Review (Abschluss)

Zwei unabhängige Reviews zusammengeführt: eigenes Review + ein unabhängiger Reviewer-Agent
(gegen Autoren-Bias). Das Projekt ist kein Git-Repo → kein Diff-Baseline; Review über die Quelldateien.

Bewertet nach: Korrektheit, Sicherheit, Konsistenz, Erweiterbarkeit, Dokumentation.

## Update 2026-07-04 – Legacy-Abbau & Duplikations-Refactor

Folge-Review nach weiterem Wachstum (StudyPlan, Klassenarbeiten, Exercise-Katalog). Befund:
Der *neue* Code war sauber (0 Compiler-Warnungen, moderne C#-Idiome, durchgängige XML-Docs),
aber es hatte sich strukturelles Legacy gebildet – ein komplettes totes Template-Modell, an dem
zudem noch das Frontend hing. Richtungsentscheidung **API-First** getroffen und umgesetzt
(Details: [architektur-entscheidung.md](architektur-entscheidung.md)):

- **Legacy entfernt** (erledigt L3/L-Empfehlungen aus B): 4 Legacy-Controller, alle Legacy-Entitäten
  (nur `TimeSlotRule` bleibt), 9 tote `DbSet`s, 2 Legacy-`PointsService`-Methoden, Seed bereinigt.
- **M8 (Controller-Duplikation) behoben**: `PlanOwnershipFilter` (statt 5× Inline-Filter) +
  `TestAttemptService` (gemeinsamer Attempt-Lebenszyklus der drei Test-Controller).
- **Zusatz-Fix**: `AuthController` – `[AllowAnonymous]` auf Klassenebene machte `GET /api/v1/auth/me`
  ungewollt anonym; korrigiert (Login-Actions anonym, `me` erfordert Token).
- Verifiziert: Build sauber + Live-Smoke-Test (Auth, Ownership-404, Plan→Test→Submit inkl. Punkte).

Weiterhin offen (vor Prod): **PIN-Hashing (H3)** samt Rate-Limit/Lockout. Erledigt seit diesem Review:
**EF-Migrationen (L4)**, **automatisierte Integrationstests** (`backend/Pugling.Api.Tests`) und der
**Frontend-Neubau** (`frontend/`, gegen `api/v1`, funktionsfähig – Sohn-App/Vater-Web inkl.
Lehrplan-Assistent, Playwright-E2E). Siehe Migrationsplan in [architektur-entscheidung.md](architektur-entscheidung.md).

## A) Behoben & live verifiziert

| # | Sev | Befund | Fix | Verifikation |
|---|-----|--------|-----|--------------|
| H1 | HIGH | 4 Legacy-Controller (`Points/Sessions/Settings/Vocab`) komplett **anonym** erreichbar (u. a. `POST /points/adjust` = beliebige Punkte ohne Token) | `[Authorize(Roles=Vater)]` ergänzt | `POST /points/adjust` ohne Token → **401** |
| H2 | HIGH | **IDOR** in `FathersController`: jeder Vater konnte fremde Väter lesen/ändern/**löschen** (Kaskade!) | `IActionFilter` (Route-`fatherId` == Token-`fid`) + `List` nur eigener Datensatz | Vater2 GET/DELETE `/fathers/1` → **403**; `List` → nur `[1]` |
| H4 | HIGH | Hartkodierter JWT-Dev-Fallback-Key → in Prod Token fälschbar | Fail-fast: außerhalb Dev ohne `Jwt:Key` Startabbruch | Build ok (Dev unverändert) |
| M1 | MED | Kind konnte **eigene Teststufe wählen** → `stage=ShowBoth` = 100 % Gratis-Punkte | Für Sohn serverseitig **Fahrplan-Stufe erzwungen** (nur Vater darf wählen) | Sohn `stage=ShowBoth` → erzwungen **SelfAssess** |
| M2 | MED | Heartbeat akzeptierte unbegrenzte Sekunden → Zeit-Cheat (1 Call = 20 min) | Pro Heartbeat auf 120 s geclampt | `seconds:1200` → nur **2 min** angerechnet |
| M3 | MED | Race in idempotenter Punktevergabe → `DbUpdateException`/500 bei Doppel-Submit | `DbUpdateException` gefangen (gilt als bereits vergeben) | Build ok |
| M4 | MED | `ClozeTests.Submit` `.First()`/Indexer → 500 bei mittendrin geänderten Lücken | `TryGetValue`/`FirstOrDefault`, fehlende Lücke = falsch statt Crash | Build ok |
| L1 | LOW | `AddItem` nutzte `Items.Count` als Order → Kollision nach `RemoveItem` | `Max(Order)+1` | Build ok |
| L5 | LOW | `Create` verriet fremde Kind-Existenz (404 vor Ownership) | Ownership zuerst → einheitlich 404 | Build ok |
| L6 | LOW | `ClozeTexts.Update` erlaubte leere Lücken-Liste → nie bestehbarer Test | leere `Gaps` → 400 | Build ok |

## B) Bewusst offen (Empfehlungen, nicht angewendet)

Diese sind Design-/Prod-Entscheidungen mit Seiteneffekten – bewusst gemeldet statt still geändert:

- **H3 (HIGH) – PINs im Klartext + kein Rate-Limiting.** `Father.Pin`/`Child.Pin` als Klartext gespeichert und verglichen; numerische IDs im Login-DTO. **Empfehlung:** `PasswordHasher` (PBKDF2), konstantzeitiger Vergleich, Lockout/Rate-Limit. Bewusst nicht angewendet, weil es Seed + Login + Tutorial berührt – vor Prod aber zwingend.
- **M5 (MED) – N+1 in `progress`/`today`:** `ComputeDayAsync` je Tag = 3 Queries → 30–90 Roundtrips/Request. Für Familien-Scale unkritisch; bei Bedarf die drei Aggregate einmal gruppiert laden.
- **M6 (MED) – Vokabel-/Cloze-Store ohne Mandanten-Isolation:** jeder Vater kann jeden Eintrag ändern (verfälscht ggf. fremde Bewertungen). Entweder bewusst als globaler Katalog dokumentieren oder `OwnerFatherId` einführen.
- **M7 (MED) – `Graded`-Semantik:** Matching setzt immer `Graded=true` (objektiv, kann nicht selbst-eingeschätzt werden) – bewusst, aber verfahrensübergreifend zu dokumentieren; unter `RequireTypedTest` zählt Matching-Auswahl damit als „gewertet".
- **M8 (MED) – Controller-Duplikation:** die drei Test-Controller teilen ~150 Zeilen Gerüst. Ein gemeinsamer Attempt-Lifecycle-Service würde ein viertes Verfahren weiter verschlanken (siehe [architektur-resumee.md](architektur-resumee.md)).
- **M9 (LOW) – JSON-Collections ohne `ValueComparer`** (`Gaps`, `WordBank`, `StageSchedule`): funktioniert nur, weil Controller die Listen neu zuweisen; `SetValueComparer` ergänzen für saubere Change-Detection.
- **L2 – Datumsgrenzen in UTC:** Tages-/Backfill-Logik nutzt `DateTime.UtcNow`; nahe Mitternacht lokal ggf. falscher Kalendertag. Zeitzone des Kindes berücksichtigen.
- **L3 – Doppelte Identitätswelt:** Seed pflegt Legacy-`Users` **und** `Fathers`/`Children` + Default-PINs `0000`/`1111`. Für Prod bereinigen.
- **L4 – `EnsureCreated()` statt EF-Migrationen** (kein Upgrade-Pfad).

## C) Bewertung nach Achse

- **Korrektheit:** Kern solide; die gefundenen Crash-/Race-Pfade sind behoben. Keine automatisierten Tests (Verifikation manuell/API) – nächster Schritt: Integrationstests.
- **Sicherheit:** Nach den Fixes ist die neue Fläche rollen- und eigentumssauber (401/403 verifiziert). Vor Prod bleiben **H3** (PIN-Hashing) und die Doku-Entscheidung zu **M6**.
- **Konsistenz:** Die drei Verfahren folgen demselben Muster (Auth → Ownership-Filter → `Start` ohne Lösungsverrat → `Submit` bewertet + Punkte über den geteilten Service). Der generische Kern (`StudyPlan`/`TestAttempt`/`StudyProgressService`) ist echt verfahrensneutral.
- **Erweiterbarkeit:** Ein viertes Verfahren = Enum + Stufen-Enum + ein Controller (+ Store nur bei neuem Inhalt). Belegt durch Matching (~1 Datei) und die parallel begonnenen Arithmetic-Typen. Grenze: `StudyPlanItem` mit zwei Nullable-FKs skaliert nicht über wenige Content-Typen hinaus (→ Polymorphie).
- **Dokumentation:** Durchgängige, sinnvolle deutsche XML-`<summary>` + `ProducesResponseType`; die Legacy-Controller hoben sich negativ ab (jetzt zumindest abgesichert + kurz dokumentiert). Plus Prozess-Log, Résumé, Tutorial.

## Fazit

Der neue Study-Kern ist sauber, modern und tragfähig erweiterbar. Die kritischen Sicherheits-/Cheat-Lücken
(H1, H2, H4, M1, M2) sind behoben und verifiziert. Vor einem echten Einsatz verbleiben v. a. **PIN-Hashing (H3)**,
die **Katalog-Mandantenfrage (M6)** sowie **EF-Migrationen** und **automatisierte Tests**.
