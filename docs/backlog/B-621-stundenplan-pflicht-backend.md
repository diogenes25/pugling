---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/training, rolle/supervisor, rolle/student]
aliases: [Stundenplan als Pflicht, Taktung Stundenplan, Obergrenze Pflicht-Positionen]
status: geschaetzt
wartet_auf: "B-644 (Tagesplaner) – nicht bauen, bis B-644 gegrillt ist"
prio: P1
art: Wunsch
groesse: L
wo: backend
migration: ja
vertragsbruch: ja
quelle: B-615
grund: ""
ersetzt_durch: []
---

# B-621 · Der Stundenplan erzeugt eine Pflicht: Taktung „Stundenplan“ und Obergrenze je Tag (Backend)

Teil (c) aus [B-615](B-615-tagesmission-faechermix-nach-staerke-und-stundenplan.md), Backend-Hälfte (dort gegrillt, beim Schätzen geteilt). Eine Position kann die
Taktung „Stundenplan“ tragen: Pflicht mit Malus am Wochentag des Fachs und am Tag davor. Dazu kommt eine
Obergrenze der Pflicht-Positionen pro Tag je Kind. Die Oberfläche folgt in [B-622](B-622-stundenplan-pflicht-vater-web-und-sohn-app.md).

## User Story

Als *Vater* möchte ich, dass mein Kind am Tag einer Schulstunde und am Tag davor das passende Fach üben muss,
und eine Obergrenze dafür setzen, wie viele Pflichten an einem Tag zusammenkommen, damit die Vorbereitung auf
die Stunde erzwungen wird, ohne dass der Tag überläuft.

## Ist-Stand am Code

Gemessen am 2026-11-17 auf `main` (Stand `74d2f9f5`), Pfade relativ zu `backend/Pugling.Api/`.

- **Der Stundenplan ist gespeichert, aber ohne Wirkung.** `TimetableEntry` (`Models/TimetableEntities.cs:7`:
  Kind, Fach, Wochentag, `CreatedAt`), Vater-CRUD in `Controllers/Supervisor/TimetableController.cs:28`
  (Liste), `:40` (Anlegen, prüft nur Fach und Doppelung), `:59` (Löschen, ohne weitere Prüfung). Kein Service
  liest ihn. **Gelöschte Einträge hinterlassen keine Spur**; es gibt keine Gültigkeit „von/bis“.
- **Die Taktung kennt drei Werte:** `GoalCadence` `None`/`Daily`/`Weekly`
  (`../Pugling.Contracts/Common/PlanPositionBaseTypes.cs:4`). Gespeichert als Text (Enum-Konvention,
  `Data/PuglingDbContext.cs:1093`), ohne Check-Constraint auf die Werte.
- **Die Periode ist (Taktung, Periodenstart).** `Services/Shared/PositionProgressService.cs:37` und `:47`
  schneiden Tag bzw. Woche; Belohnung und Malus sind je `(PlanPositionId, Cadence, PeriodStart)` eindeutig
  (`Data/PuglingDbContext.cs:707`, `:716`; `Models/PlanPositionEntities.cs:149-189`).
- **Der Malus wird nachträglich abgerechnet** („lazy settlement“): `ClosedPeriods`
  (`Services/Shared/PositionProgressService.cs:490`) zählt alle geschlossenen Perioden seit dem letzten Stand
  auf, die Abrechnung beginnt bei `:565`. Fairness-Regeln hängen daran: `PlanDueForPeriod` (`:482`),
  Einführungstag-Schonung (`:532`, nur `Daily`, `:535`).
- **Die Taktung verzweigt an 16 Stellen** in `PositionProgressService.cs`, darunter die Belohnungs-Sperre
  (`RewardObligations` `:269-272`: Tagesziele sperren Truhe und Bonus anders als Wochenziele) und drei
  Ledger-Texte mit „Tagesziel“/„Wochenziel“ (`:278`, `:452`, `:617`). Weitere Stellen:
  `Controllers/Supervisor/PlanPositionsController.cs:198` und `:317` (Anlegen/Ändern, ohne Querprüfung),
  `Controllers/Student/PositionPracticeController.cs`, `Services/Supervisor/WeakWordReviewService.cs:141`,
  im Agenten `PlanPlanner.cs` und `PlanAssigner.cs`. 14 Testdateien nennen `GoalCadence`.
- **Das Fach einer Position** kommt über den Plan: `StudyPlan.SubjectId`, im Vertrag Pflicht, in der Spalte
  nullable (`Models/StudyPlanEntities.cs:83`).
- **Eine Obergrenze der Pflichten gibt es nicht.** Das Vorbild für ein Feld am Kind, das nur der Vater setzt,
  ist `BonusDailyCoinCap` (`Controllers/Supervisor/ChildrenController.cs:25-28`).
- **Die Kind-Tagessicht** rechnet `ComputeChildDayAsync` (`Services/Shared/PositionProgressService.cs:295`);
  einen Grund je Pflicht („morgen Englisch“) trägt sie nicht.

## Die echte Lücke

1. Eine vierte Taktung, deren Pflichttage aus dem Stundenplan kommen, mit Tagesperioden wie `Daily`.
2. Eine Obergrenze der Pflicht-Positionen je Wochentag, geprüft beim Anlegen einer Position **und** beim Anlegen
   eines Stundenplan-Eintrags.
3. **Der Stundenplan hat keine Geschichte, die Abrechnung läuft aber nachträglich.** Ohne Vorkehrung würde ein
   heute angelegter Eintrag Pflichttage in der Vergangenheit erzeugen, und ein heute gelöschter (Rückfall auf
   `Daily`) vergangene freie Tage zu Pflichttagen machen: Malus für Tage, an denen nichts verlangt war.

## Offene Punkte

Keine Produktfragen offen; entschieden in [B-615](B-615-tagesmission-faechermix-nach-staerke-und-stundenplan.md). Die rückwirkende Wirkung einer
Stundenplan-Änderung hat der Vater am 2026-11-17 entschieden (Entscheidung 8). Die Teilnahme an der
Budget-Verteilung aus B-618 ebenso (Entscheidung 9). Damit ist keine Annahme dieser Schätzung mehr unbestätigt.

## Entscheidungen

Übernommen aus [B-615](B-615-tagesmission-faechermix-nach-staerke-und-stundenplan.md) (gegrillt und nachgegrillt am 2026-11-17 mit dem Vater), wörtlich. Wo eine
Entscheidung das Vater-Web nennt, liefert diese Story die Angabe per API; die Anzeige baut [B-622](B-622-stundenplan-pflicht-vater-web-und-sohn-app.md).
Entscheidung 4 hier (B-615 Entscheidung 9) gilt für die Taktung; ihr Teil zur Stärke ist durch B-616
Entscheidung 12 abgelöst.

1. (B-615 Entscheidung 5) **Der Stundenplan erzeugt eine Pflicht mit Malus.** Das **kehrt die Entscheidung G-7 aus B-250 um**
   (2026-10-23: nur Reihenfolge und Hervorhebung). B-250 wird `verworfen` mit `ersetzt_durch: [B-615]`.
   *Begründung:* Die Kernidee der App ist, dass der Vater Lernen erzwingt; die Vorbereitung auf die Schulstunde
   ist genau so eine Pflicht. *Kosten:* eine weitere Pflichtquelle, die den Tag verlängert (siehe Entscheidung 7)
   und Malus kosten kann.
2. (B-615 Entscheidung 6) **Eigene Taktung „Stundenplan“** an der Position, neben `None`/`Daily`/`Weekly`: Pflicht am Wochentag des
   Fachs laut Stundenplan und am Tag davor, an den übrigen Tagen bleibt die Position spielbar ohne Pflicht.
   *Begründung:* eigene Periode, ehrlich abrechenbar, `Daily` bleibt unverändert. *Kosten:* neuer Enum-Wert im
   Vertrag (Vertragsbruch für Clients, die die Liste fest kennen), eigene Perioden in `ClosedPeriods`, und die
   Malus-Abrechnung muss den Stundenplan lesen.
   **Nachfrage:** Steht das Fach nicht im Stundenplan des Kindes, **weist der Server die Taktung ab**. Wird der
   Eintrag später gelöscht, gilt die Position wie `Daily`, damit sie nie lautlos pflichtfrei wird. *Kosten:*
   eine Querprüfung beim Anlegen und eine Rückfallregel bei der Abrechnung. Den Schnitt der Perioden
   regelt Entscheidung 11.
3. (B-615 Entscheidung 7) **Obergrenze: höchstens N Pflicht-Positionen pro Tag**, je Kind vom Vater gesetzt (Vorschlag 5). Der Server
   weist beim Anlegen oder Ändern ab, was die Grenze an **irgendeinem** Wochentag überschreiten würde (wegen der
   Stundenplan-Taktung ist die Zahl je Wochentag verschieden). *Begründung:* ohne Schätzmodell prüfbar.
   *Kosten:* Prüfung über alle aktiven Pläne des Kindes und alle sieben Wochentage, und ein Stundenplan-Eintrag,
   der die Grenze nachträglich reißt, braucht eine eigene Regel (Entscheidung 12).
4. (B-615 Entscheidung 9) **Ein Plan ohne Fach rechnet mit Stärke `normal`, und die Taktung „Stundenplan“ wird dort abgewiesen.**
   *Begründung:* kein Umbau am Bestand, und die Abweisung ist dieselbe wie beim Fach ohne Stundenplan-Eintrag.
   *Kosten:* Ein Plan ohne Fach nimmt an Stärke und Stundenplan nicht teil; der Vater muss dem Plan ein Fach
   geben, um beides zu nutzen. Eine zweite Fachquelle (Fach der Übung) wird bewusst nicht eingeführt.
5. (B-615 Entscheidung 11) **Je Pflichttag eine eigene Tagesperiode wie bei `Daily`.** Vortag verpasst kostet Malus, Fachtag verpasst
   kostet noch einmal. Ein Tag, der zugleich Fachtag und Vortag ist, zählt einmal. Ein Sonntag als Vortag gilt;
   ein Vortag vor Planbeginn oder nach Planende ist keine Periode. *Begründung:* dieselbe Abrechnung wie
   `Daily`, nur an weniger Tagen; das Kind kann die Vorbereitung nicht auf den Fachtag schieben. *Kosten:* bis
   zu zwei Malus je Schulstunde, und ein Fach mit vielen Wochenstunden wird fast zu `Daily`.
6. (B-615 Entscheidung 12) **Ein Stundenplan-Eintrag, der die Obergrenze reißt, wird abgewiesen; das Löschen wird geduldet.** Hebt das
   Löschen (Rückfall auf `Daily`) einen Wochentag über die Grenze, zeigt das Vater-Web die Überschreitung an.
   *Begründung:* Der Vater muss einen falschen Stundenplan-Eintrag immer entfernen können. *Kosten:* Die Grenze
   ist nach einem Löschen nicht mehr hart, und der Stundenplan-Controller braucht dieselbe Prüfung wie die
   Position.
7. (B-615 Entscheidung 13) **Keine Sortierung nach Stundenplan.** Die Pflicht aus Entscheidung 5 mit dem Hinweis aus AK 8 genügt;
   B-220 sortiert nicht nach Stundenplan. *Begründung:* eine Wirkung des Stundenplans statt zwei. *Kosten:*
   Positionen des morgigen Fachs ohne Taktung „Stundenplan“ rücken nicht nach vorn.
8. **Eine Stundenplan-Änderung wirkt nur ab heute, nie rückwirkend.** Entschieden vom Vater am 2026-11-17 beim
   Schätzen. Vor jedem Anlegen oder Löschen eines Stundenplan-Eintrags rechnet der Server die offenen Perioden
   des Kindes mit dem alten Stand ab. *Begründung:* Der Stundenplan hat keine Geschichte, der Malus wird aber
   nachträglich abgerechnet; ohne diese Regel kostete ein Tag Malus, an dem nichts verlangt war. Es ist dieselbe
   Fairness wie bei einem abgeschalteten Plan. *Kosten:* Jede Stundenplan-Änderung löst eine Abrechnung aus, und
   ein Vater kann eine verpasste Pflicht nicht mehr nachträglich durch einen Eintrag „scharf stellen“; ein
   versehentlich gelöschter und neu angelegter Eintrag gilt für den Tag dazwischen als gelöscht.
9. **Eine Stundenplan-Position nimmt an jedem Tag an der Verteilung neuer Wörter teil**, auch an ihren
   pflichtfreien Tagen ([B-618](B-618-faecher-verteilung-backend.md)). Entschieden vom Vater am 2026-11-17 beim
   Schätzen. *Begründung:* Sie bleibt an diesen Tagen spielbar (Entscheidung 2), und die Dosis regelt das
   Spielen, nicht die Pflicht; so ändert sich die Verteilung nicht von Wochentag zu Wochentag. *Kosten:* An
   pflichtfreien Tagen hält die Position ihren Teil am Anteil des Fachs, auch wenn das Kind sie nicht spielt;
   diese neuen Wörter werden an dem Tag nicht eingeführt und nicht auf andere Positionen umgelegt.

## Akzeptanzkriterien

Final, übernommen aus B-615. Je Fall die Änderung, die seinen Test rot macht.

- **AK 1.** (B-615 AK 5) Fach am Dienstag ⇒ eine Position mit Taktung „Stundenplan“ ist Montag und Dienstag Pflicht; ein am Montag
  verpasstes Ziel kostet Malus, Mittwoch kostet nichts. *Rot ohne eigene Perioden.*
- **AK 2.** (B-615 AK 6) Taktung „Stundenplan“ für ein Fach ohne Stundenplan-Eintrag ⇒ `400 validation_error`. Wird der Eintrag danach
  gelöscht, rechnet die Position wie `Daily`. *Rot, wenn sie dann pflichtfrei wird.*
- **AK 3.** (B-615 AK 7) Überschreitet ein neuer Pflicht-Eintrag die Obergrenze an einem Wochentag ⇒ `400` mit eigenem Code
  (Vorschlag `daily_duty_limit_exceeded`, beim Schätzen festzulegen). Der Testfall reißt die Grenze nur an
  **einem** Wochentag, der nicht heute ist. *Rot ohne Prüfung über alle Wochentage.*
- **AK 4.** (B-615 AK 8) Die Kind-Tagessicht zeigt Stundenplan-Pflichten mit Hinweis auf die Stunde („morgen Englisch“). *Rot, wenn die
  Pflicht ohne Grund erscheint.*
- **AK 5.** (B-615 AK 12) Fach am Dienstag, Montag **und** Dienstag verpasst ⇒ zwei Malus-Buchungen. Fach am Montag **und** Dienstag,
  alles verpasst ⇒ drei Buchungen (Sonntag, Montag, Dienstag), nicht vier. *Rot bei einer gemeinsamen Periode
  bzw. wenn der Montag doppelt zählt.*
- **AK 6.** (B-615 AK 13) Taktung „Stundenplan“ an einer Position in einem Plan ohne Fach ⇒ `400 validation_error`. *Rot ohne die
  Prüfung auf das Plan-Fach.*
- **AK 7.** (B-615 AK 14) Ein neuer Stundenplan-Eintrag, der einen Wochentag über die Obergrenze hebt ⇒ `400` mit demselben Code wie
  AK 7. Das Löschen eines Eintrags mit derselben Folge geht durch, und die Vater-Sicht des Kindes meldet die
  Überschreitung. *Rot, wenn der Eintrag durchgeht bzw. das Löschen abgewiesen wird.*
- **AK 8.** (Entscheidung 8) Ein heute angelegter Stundenplan-Eintrag erzeugt keine
  Malus-Buchung für Tage vor heute; ein heute gelöschter Eintrag macht keinen vergangenen Tag nachträglich zum
  Pflichttag. *Rot, wenn die Abrechnung vergangene Tage mit dem heutigen Stundenplan rechnet.*

## Schätzung

**Größe: L** — wie eine DB-Umbau-Etappe: Die Taktung ist Teil des Periodenschlüssels und verzweigt an 16
Stellen der Pflicht-, Belohnungs- und Malus-Rechnung; dazu kommen zwei Querprüfungen (Position, Stundenplan)
und ein Feld am Kind. Als Ganzes mit der Oberfläche wäre Teil (c) XL, darum der Schnitt.

- **`migration: ja`** — neues Feld für die Obergrenze am Kind, angehängt an `20261117061523_StudyPlanSeriesUnit`
  (bzw. an die Migration aus B-618, wenn die zuerst gebaut wird). Die Taktung selbst braucht keine
  Schemaänderung: Sie liegt als Text ohne Check-Constraint. Bestandszeilen bleiben unberührt, Obergrenze leer.
- **`vertragsbruch: ja`** — `GoalCadence` bekommt einen vierten Wert. Für einen Client mit fester Liste ist das
  ein Bruch (B-615 Entscheidung 6): Das Frontend führt die Liste an mehreren Stellen von Hand, der Agent
  (`PlanPlanner.cs`, `PlanAssigner.cs`) kennt nur drei Werte.

### Risiken

1. **Rückwirkende Malus durch Stundenplan-Änderungen** (siehe „Die echte Lücke“ 3). Entschieden (Entscheidung 8): Vor
   jeder Änderung am Stundenplan rechnet der Server die offenen Perioden des Kindes mit dem alten Stand ab,
   sodass die Änderung erst ab heute wirkt. Das Risiko bleibt die Umsetzung: Die Abrechnung muss **vor** dem
   Speichern der Änderung und in derselben Transaktion laufen (`PlanDueForPeriod` ist das Vorbild der Regel). Verwandt: [B-613](B-613-rueckdatierter-plan-bricht-die-serie.md).
2. **Belohnungs-Sperre und Serie.** `RewardObligations` behandelt Tages- und Wochenziele verschieden
   ([B-611](B-611-wochenplan-sperrt-die-truhe-aller-plaene.md) ist dort ein offener Defekt). Die neue Taktung
   zählt an ihren Pflichttagen wie ein Tagesziel, an den übrigen Tagen gar nicht; ein Plan nur mit
   Stundenplan-Positionen hat dann Tage ohne Pflicht. Was das für Truhe und Serie heißt, muss ein Test festhalten.
3. **Einführungstag-Schonung gilt nur für `Daily`** (`:535`). Fällt der erste Pflichttag einer
   Stundenplan-Position auf den Tag, an dem alle Wörter neu sind, braucht sie dieselbe Schonung.
4. **Die echten Kinder.** Bestehende Positionen tragen `Daily`/`Weekly`/`None` und dürfen sich nicht anders
   verhalten; `PflichtMalusTests` und `VierWochenTests` müssen ohne Anpassung grün bleiben.
5. **Obergrenze über alle aktiven Pläne und sieben Wochentage.** Die Prüfung muss Positionen mit Taktung
   „Stundenplan“ je Wochentag zählen und `Daily` an allen; `Weekly` zählt nicht als Tagespflicht (Annahme,
   aus „höchstens N Pflicht-Positionen pro Tag“ gelesen).
6. **Wechselwirkung mit B-618.** Entschieden (Entscheidung 9): Eine Stundenplan-Position nimmt an
   jedem Tag an der Budget-Verteilung neuer Wörter teil. Das Risiko bleibt die Reihenfolge des Baus: Wird
   B-618 zuerst gebaut, muss seine Verteilungsrechnung die neue Taktung wie jede andere Position behandeln;
   wird diese Story zuerst gebaut, gibt es noch nichts zu verteilen.
7. **Zeit/UTC.** „Wochentag des Fachs“ rechnet in UTC wie die übrige Tageslogik; nahe Mitternacht weicht der
   lokale Kalendertag ab.

### Angriffsplan

1. Vertrag: vierter Wert in `GoalCadence`; Obergrenze am Kind (Feld, Migration, Lese- und Schreib-DTO).
2. Pflichttage als eigene, rein rechnende Funktion: Stundenplan des Kindes, Fach des Plans, Zeitfenster hinein,
   Pflichttage heraus (Fachtag und Vortag, Überlappung zählt einmal, Rückfall auf `Daily` ohne Eintrag,
   Tage außerhalb der Plan-Laufzeit entfallen). Unit-Tests.
3. Periodenschnitt und `ClosedPeriods` um die neue Taktung erweitern; Malus, Belohnung, Truhe, Serie und
   Einführungstag-Schonung nachziehen; Ledger-Texte ergänzen.
4. Querprüfung beim Anlegen und Ändern einer Position: Fach am Plan vorhanden, Fach im Stundenplan, Obergrenze
   je Wochentag.
5. Stundenplan-Controller: Obergrenze beim Anlegen prüfen, vor jeder Änderung abrechnen (Risiko 1), Löschen
   dulden und die Überschreitung in der Vater-Sicht melden.
6. Kind-Tagessicht: Grund der Pflicht je Position („morgen Englisch“ als strukturierte Angabe, nicht als Text).
7. `Pugling.Client` und Agent (`PlanPlanner.cs`, `PlanAssigner.cs`) nachziehen; Doku-Beispiele ergänzen.

### Testweg

- Neue Integrationstestklasse `StundenplanPflichtTests` in `Pugling.Api.Tests`: AK 1 bis 8, je AK mindestens
  ein Fall mit der genannten roten Probe; die Fälle rechnen über mehrere Tage mit gesetzter Uhr.
- Unit-Tests für die Pflichttage-Funktion (Überlappung, Sonntag als Vortag, Laufzeitgrenzen, Rückfall).
- Unverändert grün: `PflichtMalusTests`, `VierWochenTests`, `DailyBoxTests`, `TagsRatingsTimetableTests`.
- Migration: ein Test nach dem Muster von `DailyBoxOpenedAtMigrationTests`.
- `SchemaGuardTests` und `ConventionGuardTests`; `dotnet build Pugling.sln` wegen des Vertragsbruchs.
- `/smoke-test` gegen eine Wegwerf-DB; vor der Abnahme der Agent `pugling-reviewer`.
- Rollengang per API; der Gang an der Oberfläche ist erst mit B-622 möglich und steht dann im `## Verlauf`.

## Verlauf

- **2026-11-17** — angelegt als Backend-Hälfte von Teil (c) der Teilung von B-615. Entscheidungen und AKs von
  dort übernommen; AK 8 neu aus Risiko 1 (rückwirkende Malus), vom Vater nicht bestätigt. Geschätzt: L,
  `backend`, Migration ja, Vertragsbruch ja.
- **2026-11-17** — **Entscheidung des Vaters:** Eine Stundenplan-Änderung wirkt nur ab heute, nie rückwirkend
  (Entscheidung 8); AK 8 ist damit bestätigt. Weiter nicht bestätigt: die Annahme zur Budget-Verteilung (Risiko 6).
- **2026-11-17** — **Entscheidung des Vaters:** Eine Stundenplan-Position nimmt an jedem Tag an der Verteilung
  neuer Wörter teil (Entscheidung 9). Damit ist keine Annahme der Schätzung mehr unbestätigt.
- **2026-11-17** — **Entscheidung des Vaters (Grill-Runde B-644, Entscheidung 6): angehalten.** Die Story wird nicht gebaut, bis [B-644](B-644-tagesplaner-statt-fester-lauf.md) gegrillt ist: der Tagesplaner verlegt die Pflicht von der Position an die Tagesaufgabe. Stufe und Entscheidungen bleiben bestehen; danach wird sie als Regel des Planers überführt oder verworfen.
