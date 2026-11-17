---
tags: [typ/story, status/gegrillt, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Serie bricht nachträglich, Malus beim Wiedereinschalten, Kür wird Pflicht]
status: gegrillt
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 83 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-606]
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

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 83 (🟡, `pugling-reviewer`), `entgangen_bei: [B-606]`.
- **2026-11-17** — ausformuliert: am Code belegt, breiter als die Idee – schon eine in einen laufenden Plan
  nachgetragene Position (auch die Schwächen-Wiederholung) bricht die Serie rückwirkend; das Wiedereinschalten bucht
  zusätzlich rückwirkend Malus. Präzisiert: Serie fällt auf 0 bzw. 1, gebuchte Missionen und Truhen bleiben. Der
  Realfall Plan 12 (Start = heute) ist nicht betroffen. Die B-606-Tests beruhen auf dem Defekt (Aufbau mit
  `CreatedAt` = heute bei rückdatiertem Start).
- **2026-11-17** — gegrillt mit dem Vater: Entscheidungen 1–5 (5 gegen die Empfehlung: Kür → Pflicht kommt mit hinein); Titel um den Malus erweitert; Akzeptanzkriterien final (AK 9 und 10 neu).
