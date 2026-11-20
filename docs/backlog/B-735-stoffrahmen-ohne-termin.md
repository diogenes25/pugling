---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, rolle/supervisor, rolle/student]
aliases: [Stoffrahmen ohne Termin, EndDate optional, Plan ohne Enddatum, Etappe E3 Vorbereitung]
status: geschaetzt
prio: P1
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: ja
quelle: B-644
grund: ""
ersetzt_durch: []
---

# B-735 · Ein Stoffrahmen ohne Enddatum: `StudyPlan.EndDate` wird optional (B-644, Entscheidung 17)

Sechste Teilstory aus der Teilung von [B-644](B-644-tagesplaner-statt-fester-lauf.md). Der Klausurtermin steht anfangs
meist nicht fest und kommt später. Der `StudyPlan` bleibt der Stoffrahmen (Fach, Unit, Start), trägt aber kein
Pflicht-Enddatum mehr. Das ist eine Schemaänderung mit **29 Fundstellen** in Api und Contracts und ein brechender
Vertrag; sie ist Voraussetzung dafür, dass der Vater nach dem Stichtag
([B-737](B-737-reset-zum-stichtag.md)) einen Stoffrahmen ohne erfundenes Datum anlegen kann.

**Stand:** Eintrittsbedingung `gegrillt` aus B-644 und B-680 geerbt. Entscheidung 1 stammt vom Vater, 17 ist stellvertretend und
unbestätigt.

## User Story

Als *Vater* möchte ich je Kind und Fach einen Stoff anlegen, ohne ein Enddatum erfinden zu müssen, und den Termin
nachtragen, sobald ich ihn kenne, damit der Plan nicht gegen ein falsches Datum rechnet. Als *Sohn* möchte ich, dass mein Plan
auch ohne Termin funktioniert.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`. Zählung ohne Migrationen, Tests und `bin`/`obj`: **29 Treffer** in Api und Contracts
(Entscheidung 17 stimmt), dazu eine im Agenten.

- **Entität:** `StudyPlan.EndDate` ohne `?` (`Pugling.Api/Models/StudyPlanEntities.cs:89`).
- **Lesestellen** (`EndDate` in einem Vergleich oder einer Schleife): `PositionProgressService.cs:410`, `:493`, `:522`, `:679`,
  `:873`, `:906`, `:907`; `PositionPlayService.cs:67`; `MediaDemandService.cs:48`; `ChildrenDashboardService.cs:68`, `:75`;
  `StudentPlansController.cs:42`; `PlanOverviewController.cs:43`, `:76`; `StudyPlansController.cs:28`, `:31`, `:38`, `:41`, `:60`.
- **Schreibstellen:** `StudyPlansController.cs:107` und `:116` (aus `DurationDays`, `<= 0` heißt 10), `:217` (PATCH), `Data/Seed.cs:499`.
- **Vertrag:** `PlanResponse.EndDate` (`Pugling.Contracts/Supervisor/StudyPlanDtos.cs:22`), `CreatePlanDto.DurationDays`
  (`:52`), `UpdatePlanDto.EndDate` (`:72`), `OverviewResponse.EndDate` (`Student/ProgressDtos.cs:9`), `ProgressResponse.EndDate` (`:37`).
- **Schleifen und Prozent hängen am Ende:** `ProgressAsync(plan, plan.EndDate)` und `totalDays` (`PositionProgressService.cs:906`)
  rechnen über die ganze Laufzeit; `DutyState`-Schleifen laufen `d <= plan.EndDate` (`:522`, `:873`).
- **Agent:** `PlanAssigner.cs:118` formatiert `Plan.EndDate` in eine Meldung.
- **Frontend:** `frontend/src/lib/contract.ts`, `vater/VaterPlaene.tsx`, `vater/VaterPlanDetail.tsx` lesen `endDate` (neun Fundstellen
  außerhalb von Tests, zwei Testdateien).
- **Tests:** 37 Stellen in `Pugling.Api.Tests` nennen `EndDate`.
- `Klassenarbeit.ScheduledDate` bleibt Pflicht (`Models/KlassenarbeitEntities.cs:79`, Entscheidung 17).

## Entscheidungen (geerbt)

1. **`StudyPlan.EndDate` wird optional, `Klassenarbeit.ScheduledDate` bleibt Pflicht; ein noch nicht feststehender Termin heißt:
   noch keine Klassenarbeit angelegt** (B-644 Entscheidung 17, B-680 Entscheidung 8; stellvertretend, unbestätigt). Verworfen: eine
   eigene Stoffrahmen-Entität (zweite Aktivierungsregel je Fach, B-539). Schließt aus: ein Stoffrahmen, der ein Enddatum vortäuscht.
2. **Der Termin ist optional und kommt oft später** (B-644 Entscheidung 1, Vater).
3. **Die Umstellung bricht den Vertrag** (Entscheidung 17, Kosten): `EndDate` wird in den Antworten nullable.

### Schätz-Annahmen (stellvertretend, unbestätigt)

1. **Anlegen ohne Ende läuft über ein zusätzliches Feld `WithoutEnd` an `CreatePlanDto`** (additiv), nicht über eine Umdeutung von
   `DurationDays`. Begründung: `DurationDays <= 0` heißt heute „10 Tage"; das umzudeuten würde alte Aufrufer still ändern.
   Verworfen: `DurationDays` nullable machen.
2. **Das Enddatum entfernen heißt `ClearEndDate` an `UpdatePlanDto`** (PATCH-Regel des Repos: ein `null` heißt „nicht ändern", ein
   eigener Schalter löscht, siehe `PatchClearFieldTests`).
3. **Ohne Ende rechnet das Modell mit „heute":** Schleifen laufen bis `min(EndDate ?? heute, heute)`, `TotalDays` zählt die bisher
   gelaufenen Tage (`StartDate` bis heute), `IsPlayable` verlangt nur den Start. Nichts bekommt ein erfundenes Enddatum.
4. **`ProgressResponse.TotalDays` bleibt `int`;** nur `EndDate` wird nullable.

## Akzeptanzkriterien

1. Ein `StudyPlan` ohne `EndDate` wird von Tagessicht, Fortschritt, Übersicht, Medienbedarf, Dashboard, Planlisten und Positions-Spielen
   ohne Fehler gelesen (je Lesestelle ein Fall, siehe Liste). *Rot, wenn* eine dieser Stellen bei `NULL` wirft oder ein Enddatum
   erfindet.
2. `POST study-plans` mit `WithoutEnd` legt einen Plan ohne Ende an; ohne den Schalter bleibt das Verhalten (`DurationDays <= 0`
   ergibt 10 Tage). *Rot, wenn* sich ein alter Aufruf ändert.
3. `PATCH study-plans/{id}` mit `EndDate` setzt es, mit `ClearEndDate` löscht es, ohne beides lässt es unverändert. *Rot, wenn* ein
   `null` das Datum löscht.
4. Die Serie und die Malus-Abrechnung eines Plans ohne Ende laufen bis heute und buchen nicht über „heute" hinaus. *Rot, wenn*
   eine Schleife unbegrenzt läuft oder für künftige Tage bucht.
5. Die Antworten tragen `EndDate: null` für einen offenen Plan; `TotalDays` zählt die gelaufenen Tage. *Rot, wenn* `EndDate` ein
   Platzhalterdatum trägt.
6. Die Migration macht die Spalte nullable und lässt alle Bestandspläne mit ihrem Datum unverändert, einschließlich der echten
   Pläne der Söhne (Probe an einer Kopie der echten DB). *Rot, wenn* die Migration fehlt oder ein Bestandsdatum verändert.
7. Das Vater-Web legt einen Stoffrahmen ohne Enddatum an, zeigt „kein Termin" und trägt einen Termin nach (Lesen zurück). *Rot, wenn*
   die Maske ein Datum verlangt oder `null` als „1970" zeigt.
8. `Pugling.Client`, die Agent-Meldung (`PlanAssigner.cs:118`) und `unknown_field`-Guards tragen das nullable Feld (`dotnet build
   Pugling.sln` grün). *Rot, wenn* ein Nachbar nicht mitzieht.

## Schätzung

**Größe: L** — Anker: eine DB-Umbau-Etappe wie E6: eine Spaltenänderung, die in SQLite als Tabellenneubau läuft, 29 Lesestellen mit
je eigenem „ohne Ende"-Fall, ein brechender Vertrag mit Client, Frontend und Agent.

- **`migration: ja`** — `StudyPlan.EndDate` von `NOT NULL` auf nullable. SQLite baut die Tabelle dafür neu auf; die
  Bestandszeilen (die echten Pläne) müssen mit; Probe an einer Kopie der echten DB, nie auf `pugling.db`. An die Kette hängen.
- **`vertragsbruch: ja`** — `PlanResponse.EndDate`, `OverviewResponse.EndDate`, `ProgressResponse.EndDate` werden nullable; das
  zieht `Pugling.Client`, `frontend/src/lib/contract.ts` und die `unknown_field`-Guards nach. `CreatePlanDto.WithoutEnd` und
  `UpdatePlanDto.ClearEndDate` sind additiv.
- **`wo: beides`** — Backend zuerst, dann Vater-Web.

### Risiken

- **Heimserver-Daten:** Die Migration läuft gegen die echte `pugling.db` der Söhne; die Start-Sperre gegen ungecommittete
  Migrationen (`scripts/heimnetz.ps1`) und die Sicherung beim Start gelten.
- **Schleifen ohne Ende** (`:522`, `:873`): eine vergessene Stelle läuft in die Zukunft oder unendlich.
- **EF-Übersetzung:** `p.EndDate >= day` wird zu `p.EndDate == null || p.EndDate >= day`; bei `DateOnly` und SQLite ist die
  Übersetzung zu messen, nicht zu vermuten.
- **37 Teststellen** nennen `EndDate`; sie bleiben, weil bestehende Pläne ein Datum behalten.
- **Der Kreis der Fundstellen wächst mit dem Planer:** Neuer Code aus [B-734](B-734-planer-kern-und-schattenmodus.md) und
  [B-680](B-680-taeglicher-lehrplan-je-fach-just-in-time.md) muss den offenen Plan von Anfang an können.

### Angriffsplan

1. Migration und Modell (nullable), Migrationstest mit Bestandsplan.
2. Die 29 Lesestellen einzeln, jeweils mit einem Testfall „ohne Ende".
3. Anlegen und PATCH (`WithoutEnd`, `ClearEndDate`), Vertrag und `Pugling.Client`.
4. Agent-Meldung und `dotnet build Pugling.sln`.
5. Vater-Web: Anlegemaske und Planansicht.

### Testweg

- Neue Klasse `OffenerPlanTests` (Integration, `TestClock`): je Lesestelle ein Fall (Tagessicht `KindTagessichtTests`-Muster,
  Fortschritt, Übersicht, Medienbedarf `MediaDemandTests`-Muster, Dashboard `ChildrenDashboardTests`-Muster, Plan-Spielen
  `PositionPracticeFlowTests`-Muster). Rote Probe je Stelle: den `null`-Fall dort entfernen lässt ihren Fall fallen.
- `PatchClearFieldTests`: `ClearEndDate`. `UnknownFieldTests` und `ContractDocumentTests` für die neuen Felder.
- `OffenerPlanMigrationTests` (Muster `StudyPlanSubjectMigrationTests`, das ist die Migrationsklasse einer vergleichbaren
  Plan-Spaltenänderung).
- Frontend: Vitest für die Maske (`planSubject.test.tsx`-Muster); E2E neu `frontend/e2e/stoffrahmen-ohne-termin.spec.ts`,
  Regression `frontend/e2e/zwei-faecher.spec.ts` und `frontend/e2e/ziel-vier-wochen.spec.ts`.
- Vor der Abnahme: `pugling-reviewer`, `frontend-reviewer`, Rollengang als Vater und Sohn.

## Reihenfolge und Abhängigkeiten

Nach [B-734](B-734-planer-kern-und-schattenmodus.md), vor [B-680](B-680-taeglicher-lehrplan-je-fach-just-in-time.md) (dessen
Akzeptanzkriterium 3 braucht den offenen Plan) und vor [B-737](B-737-reset-zum-stichtag.md). Technisch unabhängig von B-734 und
darum früher baubar; die Reihenfolge folgt dem Bedarf, nicht einer Sperre.

## Verlauf

- **2026-11-20** — angelegt durch Teilung von B-644 (Entscheidung 17, Vorbereitung von E3 und E4) und `gegrillt` → `geschaetzt`
  im Vorbereitungslauf (Sonnet): Eintrittsbedingung `gegrillt` aus B-644 und B-680 geerbt. Nachgesehen: 29 Fundstellen bestätigt
  (Api 23, Contracts 6), dazu Schleifen und Prozentrechnung am Ende, `DurationDays` als Anlegeweg. L, beides, Migration, brechender
  Vertrag.
