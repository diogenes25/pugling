---
tags: [typ/story, status/abgenommen, bereich/lehrplan, bereich/training, rolle/student, rolle/supervisor]
aliases: [Tagesbegriff, Ortszeit, Lerntag Kalendertag, Etappe E0a]
status: abgenommen
prio: P1
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-644
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-22"
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

## Entscheidungen beim Bau (Nachtlauf 2026-11-23-a)

*Stellvertretend in der übertragenen PO-Rolle getroffen, unbestätigt – zum Umstoßen. Keine davon ändert eine geerbte
Entscheidung; sie füllen, was die Story offenließ.*

4. **Die Testsuite zählt weiter im UTC-Kalender** (`LearningDay:TimeZone = UTC` in der Testfabrik), eine eigene
   `BerlinWebAppFactory` prüft die Zonen-Regel. Grund: 39 Stellen in 20 Testdateien bilden „heute" selbst aus
   `DateTime.UtcNow`; auf Berlin liefen sie jede Nacht zwischen 0 und 2 Uhr gegen den Server. Kosten: die Suite beweist
   die Geschäftsregeln im UTC-Kalender; dass jede Stelle den Dienst benutzt, hält das Quelltext-Tor (AK 2), dass der
   Dienst richtig rechnet, `TagesbegriffTests`/`LerntagUmstellungTests`.
5. **Der Shop-Auffüllkalender folgt dem Lerntag** (`ShopRefillSchedule` bekommt die Zone, rechnet auf der Wanduhr, gibt
   UTC-Augenblicke zurück). Der Ist-Stand nannte ihn; „täglich" beginnt damit um Mitternacht deutscher Zeit wie der Tag.
   Kosten: ein Auffüllen, das bisher um 1 bzw. 2 Uhr kam, kommt eine bis zwei Stunden früher.
6. **Die Fristen der Sohn-Arcade** (`stundenBisServerTagesende`, `istServerSonntag` in `frontend/src/lib/datum.ts`, B-525)
   rechnen mit derselben Zone – sie zeigten bisher Mitternacht UTC und hätten nach dem Umbau der Buchung widersprochen.
   Die Zone steht im Frontend als Konstante `LERNTAG_ZONE`, gespiegelt aus `appsettings.json` (der Browser kennt die des
   Servers nicht). Kosten: zwei Orte für eine Einstellung; ändert sie sich, ändern sich beide.
7. **Die Tageszeit-Multiplikatoren** lesen die Wanduhr des Lerntags (`LearningDay.LocalNow`) statt der Server-Zone
   (`GetLocalNow`). Auf dem Heimserver (Windows in deutscher Zeit) ändert das nichts; auf einem Host in UTC hätten sie
   bisher falsch gewichtet.
8. **Der Seed bleibt außerhalb** (Demo-Daten einer frischen DB, `Data/Seed.cs:370`, `:888`); das Tor scannt
   `Controllers/` und `Services/`.

**Wirkung auf die echte DB beim Einspielen:** keine Migration. Gespeicherte `DateOnly`-Werte behalten ihren Tag; ab dem
Start rechnet „heute" in deutscher Zeit. In der ersten Nacht endet der Tag um 0 statt um 1 Uhr – die Abrechnung eines
Tages kommt damit früher, nicht doppelt (sie ist je Tag idempotent).

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
- **2026-11-22** — gebaut im Nachtlauf 2026-11-23-a, Sprint 116 (Freigabe 8: `geschaetzt` + `Wunsch` ist baubar; die
  Entscheidungen 4–8 oben füllen offene Handwerksfragen, keine ändert eine geerbte).
  - AK 1 → `TagesbegriffTests.Antwort_Um_2330_…` (Winter/Sommer), D1 (`DayOf` = UTC-Datum) → rot; D8 (eine Stelle bildet
    den Tag wieder selbst) → rot über das Tor.
  - AK 2 → `ConventionGuardTests.Tage_Entstehen_Nur_Im_Lerntag`, D8 → rot. Ausnahmen mit Grund: die Wanduhr-Rechnung des
    Shop-Kalenders, die Multiplikator-Stunde aus `LocalNow`.
  - AK 3 → `TagesbegriffTests.Antwort_Um_2330_Zaehlt_Fuer_Den_Alten_Um_0030_Fuer_Den_Neuen_Tag` (2 Fälle), D1 → rot.
  - AK 4 → `LerntagUmstellungTests.Umstellungstag_…` (23 h / 25 h, jeder Tag genau einmal) und `Tag_Beginnt_Um_Mitternacht_Ortszeit`,
    D1 → rot, D2 (`StartUtc` = UTC-Mitternacht) → rot; Frontend `datum.test.ts` „…über die Zeitumstellung", F2 → rot.
  - AK 5 → `LastReviewedAt`: `Zweite_Richtige_Antwort_Nach_Mitternacht_…`, D3 → rot; `CreatedAt`/`DutySince`:
    `Position_Kurz_Nach_Mitternacht_…`, D9 → rot; Woche: `Muenze_Nach_Mitternacht_Am_Montag_…`, D4 (Montag aus UTC) → rot,
    D5 (Wochenbeginn UTC-Mitternacht) → rot; Shop: `Taeglicher_Shop_Bestand_…`, D6 (Zone nicht durchgereicht) → rot, D7
    (Kalender ignoriert die Zone) → rot.
  - AK 6 → `planSubject.test.tsx` „belegt den Start mit dem Lerntag vor", F1 → rot; `datum.test.ts` `heuteImLerntag`, F4 → rot;
    Arcade-Fristen `pfad.test.ts`/`datum.test.ts`, F2 → rot, F3 (Sonntag nach UTC) → rot; Shop-Termin
    `VaterShopEinmalTermin.test.tsx`, S1 (Mitternacht UTC senden) → rot, S2 (Tag per `slice` zurücklesen) → rot.
    Nicht je Aufrufer gemessen: `VaterWizard.tsx:63` und `ChildrenTimeline.tsx:48` rufen denselben Helfer, ein Rückbau dort
    fiele keinem Test auf (frontend-reviewer 🟢, bewusst offen).
  - AK 7 → umgeschrieben mit Grund im Kommentar: `pfad.test.ts`, `SohnHome.test.tsx` (behaupteten Mitternacht UTC),
    `VaterShopEinmalTermin.test.tsx` (Mitternacht des gewählten Tags), `TagesportionTests`/`ShopFlowTests`/`ShopStockPerChildTests`
    (Dienst von Hand gebaut, UTC-Kalender); die übrige Suite blieb unverändert grün auf UTC (Entscheidung 4).
  - Messung `.claude/scripts/mutant-probe.py` nach der letzten Änderung: 16 von 16 rot (D1–D9, F1–F4, S1–S2; S1 nach
    korrigierter Ortsangabe neu gemessen), Grundlinien grün. Nicht gemessen: `lastWeekNow` über eine Umstellungswoche
    (Review-🟢, behoben ohne eigenen Test).
- **2026-11-22** — Reviews: `pugling-reviewer` kein 🔴; 🟡 Swagger-Texte nannten „today, UTC" (`ChildrenDashboardController`,
  `ChildrenOverviewController`) → behoben (nur Doku, ohne Zählung); 🟢 veraltete UTC-Kommentare (5) → behoben; 🟢
  `lastWeekNow` 168 h statt Wanduhr → behoben; 🟢 Zone mit Umstellung um Mitternacht → als „nicht unterstützt" dokumentiert;
  🟢 Tor-Muster → um `TimeOnly.FromDateTime`/`GetUtcNow().Date` erweitert; 🟢 `DutyBegins` ohne Verhaltenstest → Test + D9.
  `frontend-reviewer` kein 🔴; 🟡 einmaliges Auffüllen im Shop kam weiter um 00:00 UTC (außerhalb des Diffs, ohne Zählung)
  → behoben (`beginnDesLerntags`, Rücklesen über `tagDes`); 🟢 Zonen-Spiegel ohne Prüfung → Test gegen `appsettings.json`;
  🟢 Formatierer je Aufruf → Cache; 🟢 Import-Reihenfolge → behoben; 🟢 Aufrufer-Tests Wizard/Zeitachse → offen (s. o.).
  **Zähler 0.**
- **2026-11-22** — abgenommen (`3c0122fa`, Sprint 116). Backend 2039/2039 (Release, nach der letzten Änderung), Vitest
  1338/1338, `dotnet format Pugling.sln` sauber. Rollengang: Sohn per API an der eingestellten Uhr (23:30/00:30 im Winter und
  Sommer, Antwort nach Mitternacht, Truhe/Woche – `TagesbegriffTests` gegen einen Server in Europe/Berlin) und per E2E gegen
  frisch gestarteten Server in Europe/Berlin (5211/5184): `pfad`, `truhe`, `erfolge-woche-frist`, `full-flow`; Vater
  `vater-zeitachse`, `shop-einmal-termin-bearbeiten`, `vater-felder-leeren` – 8/8 grün. Ein Gang um echte Mitternacht ist
  maschinell nicht möglich (die E2E-Uhr ist die Wanduhr); das Verhalten an der Grenze belegen die API-Fälle.
- **2026-11-22** — Nachgeschaut (Nachtlauf 2026-11-22-c, `pugling-reviewer`, nur lesend gegen `3c0122fa` und HEAD). Geprüft: Restliche Tagesbildungen außerhalb von `LearningDay` per grep (`DateOnly.FromDateTime`, `GetLocalNow`, `.Date`, `DateTime.Today`). Treffer nur im Dienst, in `ScoringService.cs:137` (Wanduhr), `ShopRefillSchedule.cs:60/86/134` und im Seed (E8). Den Controller-Diff Zeile für Zeile gegen den Elternstand verglichen. Die ~35 übrigen `GetUtcNow`-Stellen geprüft: alle sind Zeitstempel. DB-Vergleiche über `StartUtc` geprüft (`PositionPracticeController.cs:360-368`, `WalletService.cs:59-72`, `ShopService.cs:552`). Keine 24-h-Annahme gefunden. `datum.ts` `mitternacht()` von Hand gegen `StartUtc` gerechnet (28.3./29.3./31.10.2027). Für jeden AK-Test geprüft, dass er deutschen und UTC-Tag trennt (`TagesbegriffTests.cs:64-217`). **Drei Entgleitungen (🟢):** B-842 (Wochenvergleich in der Umstellungswoche), B-843 (Tor fängt weniger Formen als AK 2 verspricht), B-844 (Titel der Schwachwort-Wiederholung im UTC-Datum).
