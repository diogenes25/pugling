# Architektur-Resümee: Erweiterbarkeit, Konsistenz, Dokumentation

Stand nach drei implementierten Lernverfahren (Vokabel, Lückentext, Matching) + Auth/Rollen.

## 1. Ist der Code erweiterbar?

**Ja – der Rahmen ist bewusst verfahrensneutral.** Der Beweis kam mit dem dritten Verfahren:

| Verfahren | Neuer Code | Content-Store nötig? |
|-----------|-----------|----------------------|
| Vokabel | Basis-Rahmen + `TestsController` | Vokabel-Store (bestand) |
| Lückentext | `ClozeStage`, `ClozeTextsController`, `ClozeTestsController` | **ja** (`ClozeText`) |
| Matching | `MatchStage` + `MatchingTestsController` (~1 Datei) | **nein** (nutzt Vokabel-Store) |

**Was ein viertes Verfahren kostet:**

1. `LearningMethod`-Enum-Wert + eine `XStage`-Enum ergänzen.
2. Einen `X-TestsController` nach dem etablierten Muster: `[Authorize]` + `IAsyncActionFilter` (Ownership) + `Start`/`Submit`/`Get`, Punktevergabe über den geteilten `StudyProgressService`.
3. Default-Stufe im `StudyPlansController.Create`-`switch` ergänzen.
4. Nur falls der Inhalt neu ist (z. B. Sätze, Audio): einen Content-Store analog `VocabularyStore`/`ClozeTexts` + FK-Verdrahtung in `StudyPlanItem`.

**Warum das funktioniert:** Der Rahmen hängt an neutralen Konzepten:

- `StudyPlan.Method` + `DefaultStage` (int) + `StageSchedule` (Tag→Stufe, verfahrensunabhängig).
- `TestAttempt` speichert `StageValue` (int) + `Graded` (bool) statt eines verfahrensspezifischen Enums.
- `StudyProgressService` liest **nur** „Minuten geübt" und „bestandener/gewerteter Test an Tag X" – kennt keine Stufen-Semantik.
- `StudyPlanItem.ContentId` abstrahiert Vokabel- vs. Lückentext-Bezug.

**Grenzen der Erweiterbarkeit (ehrlich):**

- `StudyPlanItem` nutzt zwei nullbare FKs (`VocabularyId`, `ClozeTextId`). Ein drittes *neues* Inhaltsmodell bräuchte eine weitere Spalte – ab ~4 Content-Typen wäre eine echte Polymorphie (eigene Item-Tabelle je Typ oder ein generischer Content-Store) sauberer.
- `MapItem` im `StudyPlansController` unterscheidet Content-Arten per `if/else` – wächst linear mit neuen Content-Stores.
- Neue Test-Mechaniken duplizieren das `Start/Submit/Get`-Gerüst (~ bewusst, da die Mechanik je Verfahren wirklich anders ist). Ein gemeinsames abstraktes Basis-Gerüst wäre möglich, würde aber die Lesbarkeit der sehr unterschiedlichen Verfahren senken.

## 2. Ist der Prozess erweiterbar?

**Ja.** Der etablierte Ablauf ist reproduzierbar und in [vokabeltraining-prozess.md](vokabeltraining-prozess.md) dokumentiert:

1. Rollen-Design-Runde (PM/Senior-Dev/Vater/Sohn) → 2. Datenmodell → 3. Controller nach Muster → 4. Build → 5. Live-Durchlauf (Vater legt an, Sohn nutzt) → 6. Rollen-Feedback → 7. Iteration → 8. Doku.
Jede Iteration wurde live gegen die echte API verifiziert, nicht nur kompiliert.

## 3. Sind Logik und Ziel konsistent?

**Ja.** Das durchgängige Ziel — *„Vater hat die Kontrolle und erzwingt Lernerfolg, Sohn hat Spaß"* — spiegelt sich konsistent in der Logik:

- **Kontrolle/Zwang:** zwei harte Tagespflichten (Zeit **und** Test), vom Vater konfigurierbare Bestehensgrenze, `RequireTypedTest`/objektive Verfahren gegen Selbstbetrug, `day`-Backfill nur Vater, Ownership (Sohn kann nichts Fremdes, nichts anlegen).
- **Transparenz:** `progress`, `report` (Mastery pro Inhalt), `today`, Punkte-Ledger — alles aus Sicht des Vaters einsehbar.
- **Spaß/Motivation:** gestufte Schwierigkeit + `StageSchedule`, Buchstaben-Tipps, Punkte je Teilziel + Tagesbonus, Streak, Schwachstellen-Liste.

Konsistenz im Code: alle drei Test-Controller folgen demselben Muster (Auth → Ownership-Filter → `Start` ohne Lösungsverrat → `Submit` bewertet + vergibt Punkte über den geteilten Service → `Get`). Antwort-DTOs haben dieselbe Form (`SubmitResponse` mit `DayProgress`).

## 4. Ist der Code dokumentiert?

**Weitgehend ja.**

- `<GenerateDocumentationFile>` ist aktiv; öffentliche Typen/Methoden tragen `<summary>` (Entitäten, Services, Controller-Actions, Enums mit Stufen-Bedeutung).
- DTO-Records sind meist selbsterklärend; wo Semantik nicht offensichtlich ist (z. B. `Graded`, `ContentId`, `StageValue`, Backfill-`day`), gibt es erklärende Kommentare.
- Zusätzlich: OpenAPI/Swagger als lebende API-Doku, plus der Prozess-Log und diese Tutorials.
- **Lücke:** Es gibt **keine automatisierten Tests** – die Verifikation erfolgte manuell per API-Durchläufen. Für langfristige Wartbarkeit wären Integrationstests (WebApplicationFactory) der nächste Schritt.

## 5. Offene Punkte / Empfehlungen (priorisiert)

1. **PINs im Klartext** (`Father.Pin`, `Child.Pin`) – für Produktion hashen (z. B. ASP.NET Identity `PasswordHasher`).
2. **Legacy-Controller** (`Points/Sessions/Settings/Vocab` aus dem Ursprungs-Template) sind **ungesichert** und arbeiten auf dem alten `User`/`Topic`-Modell – entfernen oder absichern.
3. **EF-Migrationen** statt `EnsureCreated()` – aktuell muss bei Schemaänderungen die DB neu erzeugt werden.
4. **Automatisierte Tests** ergänzen.
5. **Punkte-Idempotenz unter Nebenläufigkeit**: `StudyDayReward`-Unique-Index schützt, aber ein paralleler Doppel-Submit kann eine Unique-Constraint-Exception werfen (nicht abgefangen). Für Einzelnutzer unkritisch.
6. **JWT**: nur Signatur-Key aus Config, keine Refresh-Token/Revocation – für den Familien-Scope okay, für mehr Nutzer ausbauen.
