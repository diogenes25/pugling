---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Serie bricht nachträglich, Malus beim Wiedereinschalten, Kür wird Pflicht]
status: abgenommen
prio: P2
art: Defekt
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: "Nachschau Sprint 83 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-606]
nachgeschaut: 2026-11-18
---

# B-613 · Eine nachgetragene Pflicht oder ein wieder eingeschalteter Plan bricht die Serie und bucht Malus nachträglich

Die Kind-Serie (B-606, Entscheidung 6) wertet jede Pflicht-Position eines heute aktiven Plans an **jedem Tag seiner
Laufzeit** – auch an Tagen, bevor es die Position gab oder an denen der Plan aus war. Anders als der Malus
(`FirstDueDay`) kennt sie keine Untergrenze je Position. Am Code bestätigt und **breiter als die Idee**: nicht nur ein
rückdatierter Plan, sondern schon jede Position, die der Vater in einen laufenden Plan nachträgt, bricht die Serie
rückwirkend; und das Wiedereinschalten eines alten Plans bucht zusätzlich rückwirkend **Malus-Münzen**.

## User Story

Als Sohn möchte ich, dass meine Serie und meine Truhenstufe nur an Pflichten gemessen werden, die es an dem Tag gab und
die ich an dem Tag spielen konnte, damit eine Änderung des Vaters am Plan mir nicht nachträglich wegnimmt, was ich schon
geschafft habe.

Als Vater möchte ich einen Plan rückdatiert anlegen, eine Position nachtragen oder einen alten Plan wieder einschalten
können, ohne dass das Kind dafür rückwirkend bestraft wird, damit ich den Plan anpassen kann, ohne die Motivation zu
beschädigen.

## Ist-Stand am Code

Gemessen am 2026-11-17 auf `main` (`f9cd2a38`), am Code gelesen, nicht per Test nachgestellt.

**Die Serie und ihre Tagesregel.**

- `IsCompleteDay` (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:348`): ein Tag ist vollständig, wenn
  ein Plan mit Pflicht sie erfüllt hat und **kein jetzt aktiver** Plan eine Pflicht offen hat (`:351`). „Aktiv“ ist der
  heutige Wert `StudyPlan.Active` – das Modell hat kein Aktivierungsdatum (`backend/Pugling.Api/Models/StudyPlanEntities.cs:88`
  bis `:91`: `StartDate`, `EndDate`, `Active`, `CreatedAt`, sonst nichts).
- `DutyStateAsync` (`PositionProgressService.cs:370`) nimmt **alle** Positionen mit Kadenz (`:372`) und fragt je Tag
  `IsGoalMetAsync` (`:144`). Keine der beiden Methoden sieht `PlanPosition.CreatedAt`
  (`backend/Pugling.Api/Models/PlanPositionEntities.cs:137`) an. Eine Position zählt also ab `plan.StartDate`, egal wann
  sie angelegt wurde.
- `ChildStreakAsync` (`PositionProgressService.cs:387`) läuft von heute rückwärts über alle Pläne mit
  `StartDate <= today` (`:390`), je Tag über die Pläne, deren Laufzeit den Tag deckt (`:401`), und bricht am ersten
  unvollständigen Tag (`:410`). Leser: `student/today` (`Controllers/Student/ChildTodayController.cs:33`), die
  Plan-Übersicht (`Controllers/Student/PlanOverviewController.cs:41`), die Historie (`PositionProgressService.cs:748`) und
  die Truhe über `StreakBoundedAsync` (`:719`).
- `CompleteChildDaysAsync` (`PositionProgressService.cs:420`) iteriert je Plan ab `plan.StartDate` (`:430`) – dieselbe
  Regel für Missionen und Auszeichnungen (`Services/Shared/MetricsService.cs:44`, `:47`, `:60`).
- Die Aufsicht-Zeitachse (B-558) rechnet je Tag über `ComputeDayAsync` (`Services/Supervisor/ChildrenDashboardService.cs:67`
  bis `:85`), und `ComputeDayAsync` (`PositionProgressService.cs:236`, `:256`) kennt die Untergrenze ebenso wenig.

**Der Malus hat die Untergrenze.** `FirstDueDay` (`PositionProgressService.cs:533`) = `max(CreatedAt als UTC-Datum,
plan.StartDate)`; die Abrechnung beginnt bei `max(FirstDueDay, erster Spieltag, heute − 14)` (`:608`, `:609`,
`MaxSettleLookbackDays` `:25`). Für einen rückdatierten Plan und eine nachgetragene Position ist der Malus damit richtig –
die Idee stimmt hier.

**Wege, auf denen eine Position vor ihrem Anlegetag zählt** (Teil 1):

1. Plan mit `StartDate` in der Vergangenheit anlegen: `StudyPlansController.Create` übernimmt `dto.StartDate` ohne
   Untergrenze (`Controllers/Supervisor/StudyPlansController.cs:106`); die Positionen kommen erst danach und tragen
   `CreatedAt = jetzt` (`Controllers/Supervisor/PlanPositionsController.cs:191`). Die Oberfläche lässt jedes Datum zu
   (`frontend/src/vater/VaterPlanCreate.tsx:98`, `frontend/src/vater/VaterWizard.tsx:674`, kein `min`).
2. **In einen laufenden Plan eine Pflicht-Position nachtragen** – das ist der häufigere Fall und stand nicht in der
   Idee: `PlanPositionsController.cs:187` (Kadenz aus dem DTO, `:198`) und die Schwächen-Wiederholung (B-584), die immer
   eine **Tagespflicht** in den gewählten Plan legt (`Services/Supervisor/WeakWordReviewService.cs:133`, `:141`;
   Endpunkt `POST …/positions/from-words`, `PlanPositionsController.cs:245`).
3. Den Start eines bestehenden Plans per PATCH nach vorn verlegen (`StudyPlansController.cs:212`).

**Folgen** (je am Code nachgerechnet):

- **Serie:** In jedem der drei Fälle ist die neue Position an allen Tagen zwischen `StartDate` und gestern offen und ihr
  Plan aktiv ⇒ `IsCompleteDay` ist gestern falsch. Die Serie ist danach **1**, sobald das Kind heute alles erfüllt hat,
  vorher **0** (die Schleife beginnt bei heute, `:398`/`:410`). Die Idee sagt „fällt auf 0“ – genauer: auf 0 bzw. 1.
- **Truhenstufe:** `DailyBoxService.AwardAsync` liest die Serie beim Öffnen (`Services/Shared/DailyBoxService.cs:58`)
  und wählt die Stufe danach (`:59`–`:63`; Stufen ab 7 und 30 Tagen, `appsettings.json:30`). Eine gebrochene Serie fällt
  auf Faktor 1,0. Schon geöffnete Truhen behalten ihren `StreakAtClaim` (`:76`).
- **Missionen/Auszeichnungen:** Die Metriken `DaysComplete`/`StreakDays` verlieren die Tage rückwirkend. **Nicht**
  verloren geht schon Gebuchtes: `MissionAward` und `AchievementAward` sind gespeichert und werden nicht zurückgenommen
  (`Services/Shared/GamificationService.cs:31`, `:58`). Betroffen ist nur der Fortschritt einer noch laufenden Mission
  bzw. einer noch nicht erreichten Auszeichnung. Die Idee sagt „verlieren rückwirkend Tage“ – stimmt für die Anzeige
  und das Erreichen, nicht für Gebuchtes.
- **Heilung:** Die Serie wird bei jedem Aufruf neu gerechnet, nichts davon ist gespeichert. Eine Korrektur heilt alle
  schon gebrochenen Serien sofort; gebuchte Truhen und Missionen bleiben, wie sie sind.

**Wiedereinschalten** (Teil 2). `StudyPlansController.Update` setzt `Active` (`:214`) und schaltet danach die anderen
aktiven Pläne desselben Fachs ab (`:217`, `DeactivateSameSubjectPlansAsync` `:159`–`:165`). Ab da gilt der alte Plan für
jeden Tag seiner Laufzeit als „jetzt aktiv“:

- **Serie:** Jeder Tag, an dem er aus war und seine Pflicht deshalb offen blieb, bricht die Serie. Das Kind **konnte**
  ihn an diesen Tagen gar nicht spielen: `PlanPlayableForChild` verlangt `Active`
  (`Services/Shared/PositionPlayService.cs:66`). Das gilt auch für einen schon abgelaufenen Plan – der Filter `:401`
  fragt nur die Laufzeit, nicht ob sie vorbei ist.
- **Malus (neuer Befund, nicht in der Idee):** `SettleClosedPeriodsAsync` lädt die Positionen ohne Blick auf `Active`
  (`PositionProgressService.cs:587`) und prüft `PlanDueForPeriod` mit dem **heutigen** `Active` (`:505`, `:614`).
  Perioden, in denen der Plan aus war, wurden damals übersprungen, aber nicht als erledigt vermerkt. Nach dem
  Wiedereinschalten sind sie fällig, ungespielt und ohne Belohnung ⇒ Strafe für bis zu 14 Tage zurück, sofern die
  Positionen `PenaltyCoins > 0` tragen und das Kind den Plan schon einmal gespielt hatte (`:605`). Der Kommentar an
  `PlanDueForPeriod` (`:501`–`:503`, „no penalty … on which the supervisor had the plan turned off“) stimmt nur, solange
  der Plan aus bleibt.

**Realfall vom 2026-11-17** (Friedrich, Plan 12 Englisch neben Plan 8 Französisch, laut
[B-620](B-620-friedrich-englisch-lehrwerk-und-plan.md) Laufzeit ab 17.11.): **nicht betroffen.** Mit `StartDate` = heute
deckt Plan 12 keinen Vortag (`:401`, `:430`), seine Positionen entstanden am selben Tag; heute zählt er wie jede neue
Pflicht. Betroffen wäre derselbe Plan, sobald der Vater in Plan 8 oder Plan 12 eine Pflicht-Position nachträgt (etwa
Unit 2 oder eine Schwächen-Wiederholung) – dann bricht die Serie über alle Tage seit dem Planstart. Ob das in der echten
DB schon einmal geschehen ist (Position mit `CreatedAt` nach `StartDate` in einem aktiven Plan), ist hier nicht
nachgesehen: Messungen nur auf einer Kopie, und keine lag vor.

**Tests.** Kein Test deckt einen rückdatierten Plan, eine nachgetragene Position oder das Wiedereinschalten für Serie,
Missionen oder Malus ab. `PlanSubjectTests.cs:225` prüft beim Wiedereinschalten nur, welche Pläne aktiv sind. Im
Gegenteil **beruhen** die B-606-Tests auf dem Defekt: `TestApi.SeedLeitnerPositionCore` legt die Position mit
`CreatedAt = heute` an (`backend/Pugling.Api.Tests/TestApi.cs:327`), und `KindTagessichtTests` schiebt danach den
Planstart zwei Tage zurück (`backend/Pugling.Api.Tests/KindTagessichtTests.cs:33`); ebenso `ChildrenOverviewTests.SeedPlan`
(`backend/Pugling.Api.Tests/ChildrenOverviewTests.cs:167`). Die Fälle `KindTagessichtTests.cs:158` und `:212` messen also
offene Tage einer Position vor ihrem Anlegetag. Eine Korrektur macht sie rot, bis ihr Aufbau `CreatedAt` auf den
Planstart setzt (wie es `ChildrenOverviewTests.SeedSecondPosition` schon tut, `:188`).

## Die echte Lücke

Serie, Missionen und Zeitachse fragen „war die Pflicht an dem Tag offen?“, ohne zu prüfen, ob es die Pflicht an dem Tag
**gab** (Anlegetag der Position) und ob das Kind sie an dem Tag **spielen durfte** (Plan aktiv). Der Malus prüft das
Erste, aber nicht das Zweite. Für das Erste liegt die Antwort im Modell (`CreatedAt`), für das Zweite nicht.

## Akzeptanzkriterien

Final (Entscheidungen 1–5). Je Fall die Änderung, die ihn rot macht.

1. **Rückdatierter Plan:** Ein Kind hat Plan A seit drei Tagen täglich erfüllt; der Vater legt Plan B (anderes Fach) mit
   `StartDate` = heute − 3 an und trägt heute eine Tagespflicht ein. Nach Erfüllen beider Pläne heute zeigt
   `student/today` die Serie **4**. *Rot heute: 1.*
2. **Nachgetragene Position:** Wie 1, aber die Tagespflicht kommt heute in den laufenden Plan A. Serie bleibt 4. *Rot
   heute: 1.* Derselbe Fall über die Schwächen-Wiederholung (`POST …/positions/from-words`, `PlanPositionsController.cs:245`) ebenso.
3. **Grenze am Anlegetag:** Eine gestern angelegte, gestern offene Tagespflicht bricht die Serie gestern. *Rot, wenn die
   Untergrenze einen Tag zu spät liegt.*
4. **Missionen:** Im Fall 1 bleiben `DaysComplete` und `StreakDays` der Missions-Metrik unverändert. *Rot heute.*
5. **Truhe:** Im Fall 1 öffnet das Kind am 7. Tag der Serie die Truhe mit Faktor 1,5. *Rot heute: 1,0.*
6. **Zeitachse:** In der Aufsicht-Zeitachse (`ChildrenDashboardService`) hat ein Tag vor dem Anlegetag einer Position
   für diese Position keine offene Pflicht. *Rot heute.*
7. **Wiedereinschalten – Serie** (Entscheidung 2): Plan B war aus, Plan A desselben Fachs aktiv und
   täglich erfüllt; der Vater schaltet B wieder ein. Die Tage, an denen B aus war, bleiben vollständig. *Rot heute.*
8. **Wiedereinschalten – Malus** (Entscheidung 3): Im Fall 7 mit `PenaltyCoins > 0` bucht die nächste
   Abrechnung keine Strafe für Perioden, in denen B aus war. *Rot heute.*
9. **Kür wird Pflicht** (Entscheidung 5): Eine Kür-Position, angelegt vor drei Tagen, wird heute per PATCH auf
   `Daily` gestellt ⇒ Serie und Missionstage der drei Vortage bleiben unverändert, und die nächste Abrechnung bucht für
   sie keinen Malus; ab heute ist sie Pflicht. *Rot heute: Serie bricht, und der Malus zählt ab `CreatedAt`.* Der
   umgekehrte Weg (Pflicht → Kür) ändert nichts Rückwirkendes.
10. **Einschaltdatum im Bestand:** Nach der Migration trägt jeder aktive Plan sein `StartDate` als Einschaltdatum und
   jede Pflicht-Position ihren Anlegetag als „Pflicht seit"; an einer Kopie der echten DB ändern sich Serie und
   Malus-Stand der Kinder nicht. *Rot, wenn der Backfill fehlt oder das falsche Datum nimmt.*
11. **Unverändert:** Ohne nachgetragene Position und ohne Wiedereinschalten verhält sich alles wie bisher – die
   B-606-Tests bleiben inhaltlich gleich, nur ihr Aufbau setzt `CreatedAt` auf den Planstart.

## Offene Punkte

1. ~~**Teil 1: Gilt für Serie, Missionen und Zeitachse dieselbe Untergrenze wie für den Malus?**~~ → Entscheidung 1.
2. ~~**Teil 2: Wie zählt ein wieder eingeschalteter Plan für die Tage, an denen er aus war?**~~ → Entscheidung 2.
3. ~~**Gehört der rückwirkende Malus beim Wiedereinschalten in diese Story?**~~ → Entscheidung 3.
4. ~~**Soll der Vertrag einen `StartDate` in der Vergangenheit verbieten oder warnen?**~~ → Entscheidung 4.
5. ~~**Was gilt, wenn der Vater eine Kür-Position nachträglich zur Pflicht macht?**~~ → Entscheidung 5.

## Entscheidungen

Grill-Runde mit dem Vater, 2026-11-17. 1–4 nach Empfehlung, 5 gegen die Empfehlung.

1. **Eine Pflicht zählt für Serie, Missionstage, Zeitachse und Plan-Historie erst ab ihrem ersten fälligen Tag –
   dieselbe Untergrenze `FirstDueDay` wie der Malus,** an einer Stelle (`DutyStateAsync`/`IsGoalMetAsync`).
   *Begründung:* Eine Pflicht, die es nicht gab, kann nicht versäumt sein; die Regel ist seit B-432 bezahlt. *Kosten:*
   Tage vor dem Anlegen zählen auch nicht zu Gunsten; der Aufbau der B-606-Tests (`TestApi.cs:327`,
   `KindTagessichtTests.cs:33`, `ChildrenOverviewTests.cs:167`) muss nachziehen.
2. **Beim Einschalten speichert der Plan das Datum (`ActivatedAt`);** Tage davor zählen wie bei einem inaktiven Plan
   nur zu Gunsten des Kindes (B-606). *Begründung:* ein Feld heilt Serie und Malus. *Kosten:* Migration (Kette
   verlängern, Backfill `StartDate` für alle Pläne); nur das **letzte** Einschalten ist bekannt – die Unschärfe geht
   immer zu Gunsten des Kindes.
3. **Der rückwirkende Malus beim Wiedereinschalten gehört in diese Story** (Abrechnung `PositionProgressService.cs:587`,
   `:614` beachtet `ActivatedAt`). *Begründung:* dieselbe Ursache, dieselbe Abhilfe, und Münzen treffen härter als die
   Serie. *Kosten:* Titel und Umfang wachsen um die Malus-Abrechnung.
4. **Ein `StartDate` in der Vergangenheit bleibt erlaubt, ohne Warnung.** *Begründung:* Mit Entscheidung 1 harmlos;
   ein Verbot nähme das Nachtragen vergangener Runden (`VaterRundeZaehltFuersKindTests`). *Kosten:* keine.
5. **Wird eine Kür-Position zur Pflicht, zählt sie erst ab dem Tag der Umstellung** – für Serie, Missionen, Zeitachse
   **und** Malus. Die Position merkt sich dafür „Pflicht seit" (Vorschlag: Datumsfeld an `PlanPosition`, gesetzt beim
   Wechsel von `None` auf einen Takt; `FirstDueDay` = max(Anlegetag, Pflicht seit, `StartDate`)). *Begründung
   (Vater):* derselbe Fehler wie die nachgetragene Position, er soll nicht als eigene Idee liegen bleiben. *Kosten:*
   ein zweites neues Feld – in **derselben** Migration wie Entscheidung 2, Backfill = Anlegetag; ein weiterer Pfad in
   `PlanPositionsController.cs:317`; die Story wird größer (Schätzung vermutlich L statt M).

## Schätzung

**L.** So groß wie die Anker-Etappe E6 und wie B-643: eine Migration über die echte DB mit zwei Feldern und Backfill
samt Migrationstest, dazu ein Eingriff im Kern der Tagesregel (`PositionProgressService`), von dem Serie, Truhe,
Missionen, Zeitachse, Plan-Historie und Malus abhängen. Kleiner als B-643 an der Oberfläche: kein Frontend, kein
Vertragsfeld. Mehr als M (B-03), weil fünf Schreibpfade das Datum setzen müssen und fünf Testaufbauten nachziehen.
**Keine Teilung:** Entscheidung 5 verlangt beide Felder in **einer** Migration. Ein Schnitt „Teil 1 / Teil 2“ hieße
zwei Migrationen auf derselben echten DB und zweimal dieselbe Untergrenze anfassen. Alles hat eine Ursache und liegt an
einer Stelle.

- **`wo: backend`** – `PositionProgressService`, `ChildrenDashboardService`, zwei Supervisor-Controller,
  `WeakWordReviewService`, `Seed`, Migration. Das Frontend sendet nichts Neues und liest nichts Neues.
- **`migration: ja`** – eine Migration (Kette verlängern, nach `20261117061523_StudyPlanSeriesUnit`):
  - `StudyPlan.ActivatedAt` (`DateOnly`, nicht nullbar) neben `Active` (`Models/StudyPlanEntities.cs:90`), Backfill
    `UPDATE StudyPlans SET ActivatedAt = StartDate` für **alle** Pläne (Entscheidung 2).
  - „Pflicht seit“ an `PlanPosition` (Arbeitsname `DutySince`, `DateOnly`, nicht nullbar) neben `CreatedAt`
    (`Models/PlanPositionEntities.cs:137`), Backfill `UPDATE PlanPositions SET DutySince = date(CreatedAt)` für **alle**
    Positionen. Eine Kür-Position bekommt damit auch ihren Anlegetag. Das schadet nicht, denn der Wechsel None → Takt
    überschreibt ihn. AK 10 verlangt den Anlegetag nur für Pflicht-Positionen.
  - `AddColumn(defaultValue: …)` ist für G9 unschädlich: G9 prüft das Modell, nicht die Migration (Vorbild
    `20261116065113_ForgivingDemotion`). Kein `HasDefaultValue` im Modell.
- **`vertragsbruch: nein`** – beide Felder bleiben intern. `PlanResponse` (`Pugling.Contracts/Supervisor/StudyPlanDtos.cs:21`)
  und `PositionResponse` (`:138`) tragen schon heute kein `CreatedAt`. Der Vater stellt weder das Einschaltdatum noch
  „Pflicht seit“ von Hand ein; beides setzt der Server beim Zustandswechsel. Kein DTO-Feld, kein Client, keine OpenAPI.
- **Risiken:**
  1. **Die Untergrenze gehört in den Pflichtfilter, nicht in `IsGoalMetAsync`.** Entscheidung 1 nennt
     „`DutyStateAsync`/`IsGoalMetAsync`“. Würde `IsGoalMetAsync` (`PositionProgressService.cs:144`) vor dem ersten
     fälligen Tag `true` liefern, zählte ein solcher Tag **zu Gunsten** (Pflicht „erfüllt“). Das widerspricht den Kosten
     derselben Entscheidung („Tage vor dem Anlegen zählen auch nicht zu Gunsten“). Richtig ist: Die Position ist an dem
     Tag **keine Pflicht** (`HasDuty`). `IsGoalMetAsync` bleibt unverändert, denn es liest auch die Belohnung (`:454`)
     und die Abrechnung (`:622`). Die Regel braucht **zwei** Leser mit einem gemeinsamen Helfer (etwa
     `IsObligationOn(pos, plan, day)` = Takt ≠ None und `day >= FirstDueDay`):
     - `DutyStateAsync` (`:372`, Filter `obligations`) – Serie und Missionen;
     - `ComputeDayAsync` (`:256`, `:274`) – Zeitachse (`ChildrenDashboardService.cs:77`) und Plan-Historie
       (`ProgressAsync` `:700`).
     In `ComputeDayAsync` muss die Position an einem Tag vor ihrer Pflicht aus `obligations` fallen. Dafür
     entweder `Cadence` im `PositionStatus` maskieren oder getrennt filtern. Das ändert nur vergangene Tage der
     Historie, nicht den Vertrag.
  2. **Wer sich ändern muss und wer nicht** (grep am 2026-11-17):
     - **muss:** `FirstDueDay` (`:533`) = max(`date(CreatedAt)`, `DutySince`, `StartDate`).
     - **muss:** `DutyStateAsync` (`:370`–`:380`) – Pflichtfilter (Risiko 1) und `Active` als „aktiv **an dem Tag**“ =
       `plan.Active && day >= ActivatedAt`. Darüber erben `IsCompleteDay` (`:348`), `ChildStreakAsync` (`:387`, mit
       `StreakBoundedAsync` `:719` für die Truhe, `DailyBoxService.cs:58`), `CompleteChildDaysAsync` (`:420`) und damit
       `MetricsService.cs:44`/`:47`/`:61`.
     - **muss:** `ChildrenDashboardService.cs:82` und `:84` lesen `plan.Active` selbst. Beide Stellen brauchen „aktiv an
       dem Tag“, sonst bleibt AK 6/7 in der Zeitachse rot.
     - **muss:** `PlanDueForPeriod` (`:505`, gerufen `:614`) prüft `ActivatedAt`.
     - **unverändert:** `IsGoalMetAsync` (`:144`), `EvaluateAndAwardAsync` (`:448`). Eine Belohnung für einen Tag vor
       der Pflicht, etwa über die Vaterrunde, geht zu Gunsten des Kindes und gehört nicht zu Entscheidung 1.
       `PlanPlayableForChild` (`PositionPlayService.cs:66`) prüft nur heute. `ComputeChildDayAsync` (`:318`) rechnet
       nur den heutigen Tag, und an dem gilt jede Pflicht schon (`FirstDueDay <= heute`).
  3. **Wochenpflicht und Einschalttag.** Für die Serie reicht die Tagesgrenze. In der Abrechnung liefert
     `ClosedPeriods(Weekly, …)` die Woche ab `WeekMonday(windowStart)` (`:519`). Die Woche, in der eine Position
     angelegt **oder** ein Plan eingeschaltet wurde, wird dadurch heute schon ganz abgerechnet. **Annahme, vom Vater bestätigt (2026-11-17):**
     Für `ActivatedAt` gilt eine Periode nur als fällig, wenn sie ganz nach dem Einschalten liegt (`from >= ActivatedAt`).
     Das entspricht „zu Gunsten des Kindes“. Bei Tagespflicht ist `from == to`, die Annahme betrifft also nur Wochen.
     Das Verhalten von B-432 für eine nachgetragene Wochen-Position bleibt, wie es ist.
  4. **Setzen nur beim Zustandswechsel.** `VaterPlanDetail.tsx:60` schaltet um. Das Positions-Formular schickt `cadence`
     aber bei **jedem** Speichern mit (`frontend/src/vater/PlanPositions.tsx:332`). Würde jedes PATCH mit `cadence:
     Daily` „Pflicht seit“ neu setzen, verschöbe ein bloßes Speichern die Untergrenze. Dasselbe gilt für ein wiederholtes
     `active: true`.
     - `DutySince` = heute nur bei `pos.Cadence == None` → Takt (`PlanPositionsController.cs:317`).
     - `ActivatedAt` = heute nur bei `Active` false → true (`StudyPlansController.cs:214`).
     - Daily ↔ Weekly und Pflicht → Kür ändern nichts (AK 9).
  5. **Alle Schreibpfade.**
     - Erzeugt wird an `StudyPlansController.Create` (`:108`), `PlanPositionsController.Create` (`:187`),
       `WeakWordReviewService.cs:133` und `Data/Seed.cs:380`–`:492`.
     - **Annahme, vom Vater bestätigt (2026-11-17):** `Create` setzt `ActivatedAt = StartDate`, nicht heute. Das entspricht dem Backfill.
       Ein rückdatierter Plan gilt seit seinem Start als eingeschaltet, und die Positionen begrenzt `FirstDueDay` ohnehin.
       So bleibt das Nachtragen vergangener Runden (`VaterRundeZaehltFuersKindTests`) unverändert. Mit „heute“ würden
       die rückdatierten Tage nur zu Gunsten zählen; für AK 1 macht das keinen Unterschied.
     - Ein Eintrag, der das Feld nicht setzt (`default` = `0001-01-01`), fällt über das `max(…)` auf das heutige
       Verhalten zurück. Das ist ein stiller Rückfall, kein Fehler. Darum muss jeder Schreibpfad einen eigenen Testfall
       bekommen.
  6. **Pflicht → Kür → Pflicht.** Der zweite Wechsel setzt „Pflicht seit“ wieder auf heute. Die Pflicht-Tage davor fallen
     damit rückwirkend aus Serie und offener Abrechnung. Das geht zu Gunsten des Kindes, und nur der Vater kann es
     auslösen. Gebuchte Strafen bleiben (B-432). Laut Entscheidung 2 ist „die Unschärfe immer zu Gunsten des Kindes“ –
     als Annahme notiert, vom Vater am 2026-11-17 bestätigt.
  7. **UTC-Tagesgrenze.** Beide Daten kommen aus `time.GetUtcNow()` (`TimeProvider`), wie `CreatedAt` an
     `PlanPositionsController.cs:191`, nicht aus `DateTime.UtcNow` der Entitäts-Defaults (`StudyPlanEntities.cs:91`). Unter
     Test mischt `DailyBoxTests.cs:267` beide Uhren. Ein Plan, den der Vater abends nach 1 Uhr MEZ einschaltet, zählt
     ab dem UTC-Tag; das ist dieselbe Unschärfe wie überall (Fallstrick „Zeit/UTC“).
  8. **AK 10 gegen Entscheidung 1.** Der Backfill allein ändert nichts: `DutySince = date(CreatedAt)` ergibt dasselbe
     `FirstDueDay`, und `ActivatedAt = StartDate` heißt „aktiv seit Start“ wie heute. Die **Code**-Änderung heilt aber
     jede Serie, in der in der echten DB schon eine Position nachgetragen wurde (`CreatedAt` nach `StartDate`, Plan
     aktiv). Die Serie steigt dort, sie fällt nie. AK 10 („Serie ändert sich nicht“) gilt daher nur für Kinder ohne
     solche Positionen. Die Probe muss diese Positionen vorher per SQL auflisten und darf nur dort eine **höhere** Serie
     zulassen. Den Malus-Stand ändert die Story nicht, denn gebuchte Strafen bleiben.
  9. **Tests, die auf dem Defekt beruhen – einer mehr als in der Story.** Neben `TestApi.cs:327`,
     `KindTagessichtTests.cs:33` und `ChildrenOverviewTests.cs:173` gibt es noch `DailyBoxTests.cs:274`
     (`SiebenTageStreak_SkaliertDieBoxUeberDieBasisspanneHinaus`). Der Test legt die Position heute an, schiebt den
     Start 10 Tage zurück und erwartet eine 7-Tage-Serie. Nach der Korrektur ist die Serie 1, und der Test wird rot.
     Er muss `CreatedAt` zurücksetzen (Muster `ChildrenOverviewTests.SeedSecondPosition`, `:188`).
     - Vermutlich unberührt, beim Lauf zu prüfen: `VaterRundeZaehltFuersKindTests.cs:39` (Belohnung, nicht Serie),
       `VerzeihendeAbstufungTests.cs:173` und `KartenstufeTests.cs:514` (Box bzw. eigenes `CreatedAt`),
       `TopLevelPagingTests.cs:81`.
     - `PflichtMalusTests.SeedPenaltyPlan` (`:35`) legt `new StudyPlan` ohne `ActivatedAt` an. Über den `max`-Rückfall
       (Risiko 5) bleibt das grün; `InaktiverPlan_ErzeugtKeinenMalus` (`:375`) ebenso.
  10. **Echte DB.** Die Migration läuft auf der `pugling.db`, mit der Friedrich und Theodor lernen. Die Probe läuft
      zuerst gegen eine Kopie (`scripts/heimnetz.ps1 -Database <kopie> -Port <anderer>`).
- **Angriffsplan:**
  1. Modell: zwei Felder, `migrations add`. Snapshot-Diff lesen: nur die zwei Spalten, kein Neubau. Backfill per
     `migrationBuilder.Sql` in `Up`. `SchemaGuardTests` grün.
  2. Migrationstest nach Vorbild `DailyBoxOpenedAtMigrationTests` (Kette bis zur Migration, einen Schritt zurück und
     wieder vor). Dann die Probe gegen eine Kopie der echten DB: Serie je Kind (`student/today`) und Summe der
     `GoalPenalty`-Buchungen vor und nach dem Start mit dem neuen Code, Abweichungen nur nach Risiko 8.
  3. `FirstDueDay` erweitern, Helfer `IsObligationOn`, Pflichtfilter in `DutyStateAsync` und `ComputeDayAsync`
     (Risiko 1).
  4. „Aktiv an dem Tag“ in `DutyStateAsync`, `ChildrenDashboardService.cs:82`/`:84`, `PlanDueForPeriod` (Risiko 3).
  5. Schreibpfade: `StudyPlansController` Create/PATCH, `PlanPositionsController` Create/PATCH (nur beim Übergang),
     `WeakWordReviewService`, `Seed`.
  6. Testaufbauten nachziehen (Risiko 9) und die neuen Fälle schreiben. Englische Kommentare an `IsCompleteDay`
     (`:343`–`:346`) und `PlanDueForPeriod` (`:501`–`:503`) anpassen: „keine Aktivierungshistorie“ stimmt dann nicht mehr.
- **Testweg:**
  - **Neue Klasse `RueckwirkendePflichtTests`** (`backend/Pugling.Api.Tests/`), je AK ein Fall, jeweils mit der
    Änderung, die ihn rot macht:
    - AK 1: Plan B rückdatiert, Pflicht heute → Serie 4 (rot ohne Pflichtfilter in `DutyStateAsync`).
    - AK 2: Pflicht in Plan A nachgetragen, einmal per `POST …/positions`, einmal per `…/positions/from-words` → Serie 4
      (rot, wenn `WeakWordReviewService` „Pflicht seit“ nicht setzt und der `max`-Rückfall greift – nur sichtbar, wenn
      der Fall `CreatedAt` **nicht** zurückdatiert, also über die API anlegt).
    - AK 3: gestern angelegt, gestern offen → Serie bricht gestern (rot bei `FirstDueDay + 1`).
    - AK 4: `MetricsService` `DaysComplete`/`StreakDays` vor und nach dem Anlegen von Plan B gleich.
    - AK 5: siebter Serientag → Truhe Faktor 1,5 (über `DailyBoxService`, Stufen aus `appsettings.json:30`).
    - AK 6: Zeitachse `supervisor/…/timeline`: Tag vor dem Anlegetag hat keine offene Pflicht (rot ohne Filter in
      `ComputeDayAsync`).
    - AK 7: B aus, A gleiches Fach aktiv und erfüllt, B per PATCH wieder an → Tage bleiben vollständig, in Serie
      **und** Zeitachse (rot ohne `ActivatedAt` in `DutyStateAsync` bzw. `ChildrenDashboardService.cs:84`).
    - AK 8: Fall 7 mit `PenaltyCoins > 0`, `SettleClosedPeriodsAsync` → keine Buchung für die Aus-Tage (rot ohne
      `ActivatedAt` in `PlanDueForPeriod`).
    - AK 9: Kür vor drei Tagen angelegt, heute PATCH `cadence: Daily` → Serie, Missionstage und Malus der Vortage
      unverändert. Zweiter Fall: ein Speichern mit `cadence: Daily` auf einer schon täglichen Pflicht verschiebt „Pflicht
      seit“ **nicht** (rot, wenn der Setter den Übergang nicht prüft, Risiko 4). Dritter Fall: Pflicht → Kür lässt die
      Vortage unverändert.
    - Gegenprobe zu AK 7: ein zweites `active: true` auf einem aktiven Plan verschiebt `ActivatedAt` nicht.
  - **AK 10:** `ActivatedAtMigrationTests` (Muster `DailyBoxOpenedAtMigrationTests`): ein Plan mit Start vor dem
    Anlegen und eine Pflicht-Position mit bekanntem `CreatedAt` → nach der Migration `ActivatedAt = StartDate` und
    `DutySince = date(CreatedAt)`. Rot ohne das `UPDATE`, weil dann `0001-01-01` drinsteht. Dazu die Probe gegen die
    DB-Kopie (Angriffsplan 2).
  - **AK 11:** `KindTagessichtTests`, `ChildrenOverviewTests`, `DailyBoxTests` mit nachgezogenem Aufbau, Zusicherungen
    unverändert. Der Diff dieser Dateien darf nur die Aufbau-Zeilen zeigen. `PlanSubjectTests.cs:225` bleibt grün.
  - **Frontend/E2E:** nicht betroffen (kein Vertragsfeld, keine Anzeige neu). Kein Vitest, kein Playwright.
  - Reviewer: `pugling-reviewer` (Schwerpunkt Risiko 1, 4 und 5). Danach `/smoke-test` und ein Rollengang als Vater:
    Position nachtragen, Sohn-Startseite zeigt dieselbe Serie.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 83 (🟡, `pugling-reviewer`), `entgangen_bei: [B-606]`.
- **2026-11-17** — ausformuliert: am Code belegt, breiter als die Idee – schon eine in einen laufenden Plan
  nachgetragene Position (auch die Schwächen-Wiederholung) bricht die Serie rückwirkend; das Wiedereinschalten bucht
  zusätzlich rückwirkend Malus. Präzisiert: Serie fällt auf 0 bzw. 1, gebuchte Missionen und Truhen bleiben. Der
  Realfall Plan 12 (Start = heute) ist nicht betroffen. Die B-606-Tests beruhen auf dem Defekt (Aufbau mit
  `CreatedAt` = heute bei rückdatiertem Start).
- **2026-11-17** — gegrillt mit dem Vater: Entscheidungen 1–5 (5 gegen die Empfehlung: Kür → Pflicht kommt mit hinein); Titel um den Malus erweitert; Akzeptanzkriterien final (AK 9 und 10 neu).
- **2026-11-17** — geschätzt: L wie E6/B-643 (eine Migration mit zwei Feldern und Backfill, Untergrenze im Kern der Tagesregel; kein Frontend, kein Vertragsfeld), keine Teilung. Untergrenze als Pflichtfilter in `DutyStateAsync` und `ComputeDayAsync`, nicht in `IsGoalMetAsync`; Datum nur beim Zustandswechsel setzen; `DailyBoxTests.cs:274` beruht ebenfalls auf dem Defekt; AK 10 gilt nur ohne schon nachgetragene Positionen.
- **2026-11-17** — Annahmen der Schätzung vom Vater bestätigt: `Create` setzt `ActivatedAt = StartDate`; eine Malus-Woche zählt nur ganz nach dem Einschalten; Pflicht → Kür → Pflicht setzt „Pflicht seit" neu.
- **2026-11-18** — in Arbeit (Sprint 87, [PM-Sitzung Runde 3](../pm-sitzung-2026-11-18.md)). Gebaut wie im
  Angriffsplan: Migration `20261118181817_DutySinceActivatedAt` (zwei Spalten, Backfill), Senken `DutyBegins`,
  `IsObligationOn`, `ActiveOn` in `PositionProgressService`; Takt-Maske in `ComputeDayAsync`; `PlanDueForPeriod` schuldet
  nach einem Wiedereinschalten nur Perioden ganz danach, **nur wenn `ActivatedAt > StartDate`** – die Annahme aus Risiko 3
  wörtlich (`from >= ActivatedAt` immer) hätte die erste angebrochene Woche **jedes** Plans straffrei gestellt (Create und
  Backfill setzen `ActivatedAt = StartDate`) und AK 10/11 gebrochen.
- **2026-11-18** — Fälle (alle in `backend/Pugling.Api.Tests/RueckwirkendePflichtTests.cs`, Mutanten-Tabelle unten, gemessen nach der letzten Bearbeitung):
  AK 1 rückdatierter Plan → `:165`, U1 → rot · AK 2 nachgetragen per `POST positions` → `:181`, U1 → rot; per `from-words`
  → `:195`, U1 → rot (ein fehlendes `DutySince` dort ist verhaltensgleich – `CreatedAt` sagt auch „heute“ –, W8 hält es als
  Datenzusicherung) · AK 3 Grenze am Anlegetag → `:223`, U2 (`>` statt `>=`) → rot · AK 4 Missionstage → `:239`, U1 → rot ·
  AK 5 Truhe am 7. Tag (`StreakAtClaim` 7) → `:258`, U1 → rot · AK 6 Zeitachse → `:280`, U3 (Maske weg) → rot · AK 7
  Wiedereinschalten Serie und Zeitachse → `:302`, U4 (`DutyStateAsync`) und U5 (Zeitachse) → rot · AK 8 kein rückwirkender
  Malus → `:325`, U6 und W2 → rot · Gegenprobe zweites `active: true` → `:346`, W1 → rot · AK 9 Kür → Pflicht (Serie,
  Missionstage, Malus) → `:370`, U8/U9/W7 → rot; Speichern mit gleichem Takt → `:398`, W6 → rot · AK 10 Backfill →
  `ActivatedAtMigrationTests.cs:28`, M1/M2 → rot; erste angebrochene Woche bleibt fällig → `:417`, U7 (Guard weg) → rot ·
  AK 11 Aufbau von `KindTagessichtTests`, `ChildrenOverviewTests`, `DailyBoxTests` zieht `CreatedAt` auf den Planstart,
  Zusicherungen unverändert; vor dem Nachziehen genau diese 7 Fälle rot (gemessen) · Schreibpfade anlegen → `:498`, W4/W5/W9/W10 → rot.
- **2026-11-18** — **AK 9, letzter Satz nicht gehalten:** „Pflicht → Kür ändert nichts Rückwirkendes“ stimmt nicht, wenn
  es die letzte Pflicht eines Plans war – die vergangenen Tage verlieren ihre Pflicht, die Serie fällt (bestand schon vor
  dem Sprint). Bewusst nicht hier: braucht eine Pflicht-Historie (zweite Migration) → [B-659](B-659-pflicht-wird-kuer-bricht-die-serie.md).
- **2026-11-18** — Review `pugling-reviewer`: keine 🔴. 🟡1 (ein vorgezogener Start eines Zukunftsplans ließ
  `ActivatedAt` auf dem alten Start, verpasste Tage danach kosteten nichts) behoben – `Update` zieht das Einschaltdatum eines
  aktiven Plans bei verlegtem Start auf `max(Start, heute)` vor; Fall `:465`, W3 → rot (und mein erster Einbau verglich
  gegen den schon überschriebenen Start – derselbe Fall war rot, bevor er stimmte). 🟡2 → B-659. 🟢 Texte nachgezogen.
- **2026-11-18** — Probe an einer **Kopie** der echten DB (Sicherungs-API, nur lesend; alter Code im Wegwerf-Worktree gegen
  neuen): eine nachgetragene Pflicht-Position im Bestand (Position 49, Plan 7, Testjunge, inaktiv); Serie aller vier Kinder
  gleich, vollständige Tage nur beim Testjungen 1 → 3 (Risiko 8), kein neuer Malus.
- **2026-11-18** — Rollengang: **nicht an der Oberfläche gefahren** – die Fälle brauchen vergangene Tage, die nur die
  Datenbank herstellt. Statt dessen die API-Fälle oben (Vater-Schreibpfade über die echten Endpunkte, Sohn-Serie über
  `student/children/{id}/today`) und als Regressionszeugen die E2E `vater-zeitachse`, `schwache-woerter-zuweisen`,
  `zwei-faecher`, `pfad`, `full-flow` (alle grün). Ein Mensch prüft beim nächsten Unit-Wechsel in einem echten Plan, dass
  die Serie des Kindes stehen bleibt. **Vor dem nächsten Heimnetz-Start:** der Neustart spielt die Migration ein (das
  Startskript sichert vorher).
- **2026-11-18** — abgenommen: `fe13531c`. `dotnet test Pugling.sln -c Release` 1872/1872, Vitest 1228/1228, E2E 11 passed;
  Mutanten 26/26 rot am richtigen Ort (Tabelle unten).

**Mutanten (Backend, nach der letzten Bearbeitung):**

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
| U1 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:452 | red | red |  |
| U2 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:75 | red | red |  |
| U3 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:312 | red | red |  |
| U4 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:451 | red | red |  |
| U5 | backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:80 | red | red |  |
| U6 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:664 | red | red |  |
| U7 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:664 | red | red |  |
| U8 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:694 | red | red |  |
| U9 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:66 | red | red |  |
| W1 | backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:220 | red | red |  |
| W2 | backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:220 | red | red |  |
| W3 | backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:228 | red | red |  |
| W4 | backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:120 | red | red |  |
| W5 | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:215 | red | red |  |
| W6 | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:350 | red | red |  |
| W7 | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:351 | red | red |  |
| W8 | backend/Pugling.Api/Services/Supervisor/WeakWordReviewService.cs:136 | red | red |  |
| W9 | backend/Pugling.Api/Data/Seed.cs:501 | red | red |  |
| W10 | backend/Pugling.Api/Data/Seed.cs:504 | red | red |  |
| M1 | backend/Pugling.Api/Data/Migrations/20261118181817_DutySinceActivatedAt.cs:30 | red | red |  |
| M2 | backend/Pugling.Api/Data/Migrations/20261118181817_DutySinceActivatedAt.cs:31 | red | red |  |
| R1 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:408 | red | red |  |
| R2 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:417 | red | red |  |
| R3 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:369 | red | red |  |
| R4 | backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:268 | red | red |  |
| R5 | backend/Pugling.Api/Controllers/Student/ChildTodayController.cs:37 | red | red |  |

Frontend (B-612):

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
| F1 | frontend/src/sohn/SohnHome.tsx:81 | red | red |  |
| F2 | frontend/src/sohn/SohnHome.tsx:80 | red | red |  |
| F3 | frontend/src/sohn/SohnHome.tsx:143 | red | red |  |
| F4 | frontend/src/sohn/SohnBonus.tsx:97 | red | red |  |
| F5 | frontend/src/sohn/pfad.ts:47 | red | red |  |
| F6 | frontend/src/sohn/SohnHome.tsx:112 | red | red |  |

- **2026-11-18** — nachgeschaut (Nachtlauf 2026-11-18, `pugling-reviewer` über `fe13531c`): ein Durchrutscher, [B-672](B-672-startverlegung-zurueck-holt-unspielbare-tage-zurueck.md) (zurückverlegter Start hebt `ActivatedAt` nie an – Serie bricht, Malus für Ferientage). Geprüft ohne Befund: alle Schreibwege von `DutySince`/`ActivatedAt` (grep `new PlanPosition`/`new StudyPlan`/`.Active =`), Migrations-Rückfüllung, Leser `DutyStateAsync`/`ComputeDayAsync:325`/`ChildrenDashboardService:80-83`/`FirstDueDay`. Älterer Nebenbefund ohne Zuordnung: [B-673](B-673-plan-auf-anderes-kind-umgehaengt-erbt-dessen-vergangenheit.md).
