---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/training, rolle/student, rolle/supervisor]
aliases: [Tagesbegriff, Ortszeit, Lerntag Kalendertag, Etappe E0a]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-644
grund: ""
ersetzt_durch: []
---

# B-730 · „Heute" entsteht an einer Stelle in deutscher Zeit (B-644, Etappe E0a)

Erste Teilstory aus der Teilung von [B-644](B-644-tagesplaner-statt-fester-lauf.md). Der Lerntag ist der
Kalendertag in deutscher Zeit und endet um Mitternacht. Heute bildet das Backend „heute" an 36 Stellen in UTC;
„bis heute Abend" endet für die Söhne damit erst um 1 bzw. 2 Uhr nachts. Das ist der einzige Teil des Umbaus,
der schon vor dem Planer das Verhalten ändert, und er ist auch ohne Planer richtig.

**Stand:** Eintrittsbedingung `gegrillt` aus B-644 geerbt (kein offener Punkt). Entscheidung 9 stammt vom Vater
(2026-11-17), der Zuschnitt (Entscheidung 26) ist stellvertretend und unbestätigt.

## User Story

Als *Sohn* möchte ich, dass „bis heute Abend" um Mitternacht meiner Zeit endet, damit ich nicht nachts um 1 Uhr
noch eine Pflicht des „alten" Tages erfüllen muss und der neue Tag nicht erst um 2 Uhr beginnt. Als *Vater* möchte
ich, dass jede Tageszahl (Serie, Malus, Zeitachse) denselben Tagesbegriff benutzt, damit sie sich nicht
widersprechen.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`. Pfade relativ zu `backend/`.

- **Direkte UTC-Tagesbildung** `DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime)`: **36 Treffer in 16 Dateien**
  (B-644 nannte 35 in 15, die Zahl hat sich verschoben): `AuthController.cs`, `ChildTodayController.cs`,
  `ExerciseFeedbackController.cs`, `MeController.cs` (5), `PlanOverviewController.cs` (2), `PositionPracticeController.cs`
  (8), `PositionTestsController.cs` (4), `StudentPlansController.cs`, `ChildrenDashboardController.cs`,
  `ChildrenOverviewController.cs`, `KlassenarbeitenController.cs`, `PlanPositionsController.cs`,
  `StudyPlansController.cs` (6), `MediaDemandService.cs`, `ExtraTrainingService.cs`, `ObjectiveService.cs`.
- **Zweite Form, die B-644 nicht zählt:** gespeicherte UTC-Zeitstempel werden in einen Tag umgerechnet. Sie müssen
  mit umgestellt werden, sonst liegt ein Ereignis um 00:30 Ortszeit auf einem anderen Tag als „heute":
  `PositionPracticeController.cs:675` (`LastReviewedAt` gegen `session.Day`), `PositionPlayService.cs:400`,
  `PositionProgressService.cs:65` (`CreatedAt` → `FirstDueDay`), `PlanPositionsController.cs:219` (`DutySince`),
  `WeakWordReviewService.cs:136`, `WalletService.cs:58` (`WeekMonday`), `ShopRefillSchedule.cs:101`. Dazu
  `Data/Seed.cs:370` und `:888` (`DateTime.UtcNow`).
- `TimeProvider` ist überall injiziert; der Test-Host trägt eine einfrierbare `TestClock` (`Pugling.Api.Tests/TestClock.cs`,
  `FreezeAt`/`Advance`), 25 Testdateien benutzen sie. `TimeZoneInfo` kommt im Backend **nicht** vor (Suche ohne Treffer).
- **Frontend:** drei Stellen bilden „heute" im Browser als UTC-Datum: `frontend/src/vater/VaterPlanCreate.tsx:13`,
  `VaterWizard.tsx:62`, `ChildrenTimeline.tsx:13` (`toISOString().slice(0, 10)`).
- Nicht gefunden: eine Quelltext-Sperre gegen neue direkte UTC-Tagesbildung in `ConventionGuardTests`. Als Bauform
  gibt es dort Quelltext-Scans über `Controllers/` und `Services/` (`Freitextsuchen_Falten_Die_Schreibweise`, Zeile 614).
- Die Tageszeit-Multiplikatoren (`Scoring:TimeSlots`, `appsettings.json:17`) rechnen mit Uhrzeiten; in welcher Zone sie
  gelesen werden, ist beim Bau nachzusehen (sie sollten dieselbe Ortszeit benutzen).

## Entscheidungen (geerbt)

1. **Lerntag = Kalendertag in deutscher Zeit, Ende um Mitternacht** (B-644 Entscheidung 9, Vater). Kosten laut
   Entscheidung: Die Umstellung trifft bestehende Pläne, Serien und die Malus-Abrechnung; Tests für den Tag der
   Umstellung und die Zeitumstellung im März und Oktober sind Pflicht.
2. **Ein Codepfad** (B-644 Grundsatz): „heute" und „Tag eines Zeitstempels" entstehen in einem Dienst, nicht an
   36 Stellen.
3. **Zuschnitt als eigene Story** (B-644 Entscheidung 26, stellvertretend, unbestätigt): E0 ist von Planer und
   Auswertung unabhängig und gehört deshalb vor alles andere.

## Akzeptanzkriterien

1. Ein Dienst liefert „heute" und „Tag eines UTC-Zeitstempels" in der konfigurierten Zone (Voreinstellung
   `Europe/Berlin`). *Rot, wenn* ein Controller oder Dienst weiter selbst `DateOnly.FromDateTime(…UtcNow…)` bildet.
2. Ein Quelltext-Scan in `ConventionGuardTests` lehnt jede neue direkte UTC-Tagesbildung außerhalb des Dienstes ab und
   nennt die Fundstelle. *Rot, wenn* das Tor fehlt oder eine der 36 Stellen stehen bleibt.
3. Eine Antwort um 23:30 Ortszeit zählt für den alten, um 00:30 für den neuen Tag, mit eingefrorener Uhr getestet, im
   Winter und im Sommer. *Rot, wenn* der Tag weiter am UTC-Datum hängt.
4. Am Tag der Umstellung im März (Tag mit 23 Stunden) und im Oktober (Tag mit 25 Stunden) entsteht je Kalendertag
   genau ein Tag, auch über Mitternacht hinweg. *Rot, wenn* ein Tag doppelt oder gar nicht vorkommt.
5. Ein gespeicherter Zeitstempel zwischen 22:00 und 24:00 UTC wird dem Ortstag zugeordnet (`LastReviewedAt`,
   `CreatedAt`, `DutySince`). *Rot, wenn* `PositionPracticeController.cs:675` weiter gegen den UTC-Tag vergleicht.
6. Die drei Frontend-Stellen bilden „heute" in deutscher Zeit (ein Helfer in `frontend/src/lib/`). *Rot, wenn* ein
   Formular um 00:30 Ortszeit das Datum von gestern vorbelegt (Vitest mit gesetzter Systemzeit).
7. Die bestehende Testsuite bleibt grün, soweit sie keine UTC-Tagesgrenze behauptet; wo sie es tut, wird der Test auf
   den Ortstag umgeschrieben und sagt im Kommentar, warum. *Rot, wenn* ein Test seine Zusicherung stillschweigend
   verliert.

## Schätzung

**Größe: M** — Anker: wie der Batch-Pfad im `MediaSelector`, aber mit einer breiten, mechanischen Umstellung
(16 Dateien) statt einer Stelle. Ein neuer Dienst, ein Quelltext-Tor, drei Frontend-Zeilen. Wächst auf L, wenn die
Probe zeigt, dass viele der 25 Testdateien an der UTC-Grenze hängen.

- **`migration: nein`** — `DateOnly`-Spalten bleiben, nur ihre Berechnung ändert sich. Bestehende Zeilen behalten ihren
  Tag; nur Ereignisse zwischen 0 und 2 Uhr Ortszeit liegen künftig auf dem Folgetag (kein Datenumbau nötig, im
  Rückblick von 14 Tagen höchstens ein verschobener Rand).
- **`vertragsbruch: nein`** — kein Feld im Vertrag ändert sich; die Tageswerte bleiben `DateOnly`.
- **`wo: beides`** — Backend zuerst, danach die drei Zeilen im Vater-Web.

### Risiken

- **Zeitzonen-Daten:** `TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")` braucht ICU/tzdata. Windows (Heimserver,
  `scripts/heimnetz.ps1`) und Ubuntu-CI haben beides; ein Container ohne tzdata wirft. Beim Start gegen die
  konfigurierte Zone prüfen und mit klarer Meldung scheitern (Muster `ProductionStartupTests`).
- **Test-Uhr:** Tests, die um 23:xx UTC einfrieren, verschieben sich. Vorab eine Suche nach `FreezeAt` über die 25
  Dateien (Serie, Malus, Tagessicht, Wochenziel).
- **Wochenrechnung:** `WeekMonday` (`WalletService.cs:58`) und die Wochen-Perioden in `PositionProgressService.ClosedPeriods`
  müssen denselben Ortstag bekommen, sonst endet die Woche an einer anderen Stelle als der Tag.

### Angriffsplan

1. Dienst `LocalClock` (Heute, Tag eines Instants, Beginn des Tages in UTC) mit Zone aus der Konfiguration; Unit-Test mit
   den vier Umstellungsfällen.
2. Die 36 direkten Stellen und die sieben Zeitstempel-Umrechnungen auf den Dienst umstellen, Dienst für Dienst, mit
   `dotnet build` je Projekt.
3. Quelltext-Tor in `ConventionGuardTests`.
4. Testsuite laufen lassen, UTC-abhängige Tests einzeln bewerten.
5. Frontend-Helfer und drei Aufrufer.

### Testweg

- Neue Klasse `TagesbegriffTests` (Integration, `TestClock.FreezeAt`): 23:30/00:30 im Winter und Sommer, die
  Umstellungstage, Zuordnung von `LastReviewedAt`. Rote Probe je AK: Dienst auf UTC zurückstellen lässt AK 3 und 4
  fallen; die Fundstelle `:675` zurückdrehen lässt AK 5 fallen.
- Bestehende Klassen als Regressionsnetz: `PflichtMalusTests`, `RueckwirkendePflichtTests`, `KindTagessichtTests`,
  `VierWochenTests`, `DailyBoxTests`, `ChildrenDashboardTests`.
- `ConventionGuardTests`: neues Tor (Rot-Probe: eine direkte `DateOnly.FromDateTime(DateTime.UtcNow)` einfügen).
- Frontend: Vitest für den Datums-Helfer. E2E `frontend/e2e/vater-zeitachse.spec.ts` als Regression der Zeitachse.
- Vor der Abnahme: `pugling-reviewer`, `frontend-reviewer` und der Rollengang als Sohn um 23:50 und 00:10 (Uhr im Test
  gesetzt).

## Reihenfolge und Abhängigkeiten

Steht an erster Stelle der Kette: **B-730**, dann [B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md). Keine
Abhängigkeit nach vorn. Voraussetzung für [B-733](B-733-tagesauswertung-und-leistungsprofil.md) und alles danach.

## Verlauf

- **2026-11-20** — angelegt durch Teilung von B-644 (Etappe E0a) und `gegrillt` → `geschaetzt` im Vorbereitungslauf (Sonnet):
  Eintrittsbedingung `gegrillt` aus B-644 geerbt. Nachgesehen: 36 statt 35 Stellen, dazu sieben Zeitstempel-Umrechnungen und
  drei Frontend-Stellen, die B-644 nicht nennt. M, beides, keine Migration, kein Vertragsbruch.
