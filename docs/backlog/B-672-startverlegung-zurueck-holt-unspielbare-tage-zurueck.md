---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Start zurückverlegt bricht Serie, Ferienpause kostet Malus]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachtlauf 2026-11-18, Nachschau von Sprint 87 (fe13531c, pugling-reviewer) – docs/pm-sitzung-2026-11-18.md, Runde 5"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-613]
nachgeschaut: "2026-11-21"
---

# B-672 · Ein zurückverlegter Planstart holt Tage zurück, an denen der Plan nicht spielbar war

Verlegt der Vater den Start eines aktiven Plans nach hinten in die Vergangenheit, zählen Tage als Pflicht, an denen
das Kind den Plan nicht spielen konnte. Das kostet rückwirkend Münzen (Malus) oder bricht die Serie – ausgerechnet
nach einer Korrektur durch den Vater, etwa nach einer Ferienpause, die er wieder aufhebt.

## User Story

Als Vater möchte ich den Start eines laufenden Plans verlegen können, ohne dass Tage zur Pflicht werden, an denen der
Plan für mein Kind nicht spielbar war, damit eine Korrektur am Plan keinen rückwirkenden Malus und keine gebrochene
Serie auslöst, die das Kind nicht abwenden konnte.

## Ist-Stand am Code

Der Schalter `StudyPlan.ActivatedAt` (`backend/Pugling.Api/Models/StudyPlanEntities.cs:97`) markiert den ersten Tag,
ab dem ein aktiver Plan als Pflicht zählt (B-613). Gesetzt wird er an drei Stellen:

1. **Anlegen:** `ActivatedAt = start` (`backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:120`).
2. **Einschalten:** beim Wechsel inaktiv → aktiv `ActivatedAt = today` (`StudyPlansController.cs:220`). Wird ein Plan
   mit Zukunftsstart vorzeitig eingeschaltet, steht dort also „heute“, obwohl er erst später spielbar ist.
3. **Start verlegt** (`StudyPlansController.cs:225-229`): `playableFrom = max(neuer Start, heute)`, und nur
   `if (playableFrom < plan.ActivatedAt) plan.ActivatedAt = playableFrom;` – der Wert wird **nur gesenkt, nie
   angehoben**. Der alte Start geht dabei verloren: `plan.StartDate` wird schon vorher überschrieben
   (`StudyPlansController.cs:216`), `startMoved` ist nur ein Bool (`StudyPlansController.cs:211`).

Der Kommentar dort (`StudyPlansController.cs:222-224`) verspricht, Tage vor `playableFrom` seien nicht spielbar gewesen
und blieben zugunsten des Kindes. Das gilt nur, wenn `ActivatedAt` jünger ist als `playableFrom`.

Was spielbar ist, entscheidet `PositionPlayService.PlanPlayableForChild`:
`plan.Active && plan.StartDate <= today && today <= plan.EndDate`
(`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:66-67`). Vor dem jeweiligen Start war der Plan also nicht
spielbar – die Behauptung der Notiz zu den Voraussetzungen stimmt.

Zwei Verbraucher von `ActivatedAt`, beide am Code nachgelesen (nicht per Test nachgestellt):

- **Malus:** `PlanDueForPeriod` = `plan.Active && (plan.ActivatedAt <= plan.StartDate || from >= plan.ActivatedAt)
  && from <= plan.EndDate && to >= plan.StartDate`
  (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:677-679`), aufgerufen aus der Abrechnung
  (`PositionProgressService.cs:788`). Gilt `ActivatedAt <= StartDate`, greift die reine Überlappungsregel.
- **Serie/Tage:** `ActiveOn(plan, day) = plan.Active && day >= plan.ActivatedAt`
  (`PositionProgressService.cs:82`), genutzt in `DutyStateAsync` (`PositionProgressService.cs:464`) und im Dashboard
  (`backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:80`). `ChildStreakAsync` läuft nur über Tage
  mit `StartDate <= day` (`PositionProgressService.cs:491`).

**Malus-Fall, nachvollzogen am Code:** Plan seit 1.11. (`ActivatedAt` 1.11.); am 5.11. Start auf 15.11. – `playableFrom`
15.11., nicht kleiner als 1.11., `ActivatedAt` bleibt; 6.–14.11. liegen vor dem Start und sind nicht fällig, weil
`to >= StartDate` fehlt. Am 20.11. Start zurück auf 1.11. – `playableFrom` = 20.11., `ActivatedAt` bleibt 1.11. und
ist `<= StartDate`; jetzt überlappen die Perioden 6.–14.11. den Plan, die Abrechnung bucht Malus für Tage, an denen der
Plan nicht spielbar war.

**Serien-Fall, gegenüber der Notiz korrigiert:** Plan mit Start 1.12., am 18.11. eingeschaltet (`ActivatedAt` 18.11.,
Zeile 220). Am 25.11. Start auf 24.11. – `playableFrom` = 25.11., `ActivatedAt` bleibt 18.11. Die Tage 18.–23.11. sind
**nicht** betroffen, weil `ChildStreakAsync` nur Tage ab `StartDate` läuft (`PositionProgressService.cs:491`). Betroffen
ist der Tag **24.11.** (neuer Start bis gestern): er liegt vor dem heutigen Verlegen, war nicht spielbar, zählt aber über
`ActiveOn` als Pflichttag ohne erfülltes Ziel und bricht die Serie. Die Notiz sprach von „Tagen ab 18.11.“; das Fenster
beginnt am neuen Start, nicht an `ActivatedAt`.

**Tests:** Einziger Test zur Startverlegung ist `RueckwirkendePflichtTests.Vorgezogener_Start_Gilt_Ab_Heute_Als_Eingeschaltet`
(`backend/Pugling.Api.Tests/RueckwirkendePflichtTests.cs:465`) – Zukunftsstart auf heute vorgezogen; er trifft nur den
Fall, den die bestehende Absenkung schon löst. Ein Test für „Start zurück in die Vergangenheit“ wurde nicht gefunden.

## Die echte Lücke

`ActivatedAt` wird beim Verlegen nur gesenkt. Fehlt: **anheben** auf den Tag, ab dem der Plan nach der alten Lage
tatsächlich spielbar war – also `min(alter Start, heute)`. Ohne den alten Start (er wird überschrieben, bevor er
gebraucht würde) lässt sich das nicht herleiten; der Fix muss ihn vor Zeile 216 festhalten.

Schmaler als die Notiz in einem Punkt: die Serien-Wirkung betrifft den Streifen zwischen neuem Start und heute, nicht
alles ab dem früheren `ActivatedAt`. Der Malus-Fall stimmt wie beschrieben.

Nicht Teil der Lücke: Tage vor dem Verlegen, die gespielt werden konnten (1.–5.11. im Malus-Fall), werden mit dem
Anheben ebenfalls großzügig freigestellt, weil nur der letzte Schaltzeitpunkt bekannt ist – das ist die
dokumentierte Unschärfe zugunsten des Kindes (`StudyPlanEntities.cs:93-96`).

## Offene Punkte

1. ~~**Genügt `ActivatedAt = max(ActivatedAt, min(alter Start, heute))` vor dem Absenken?**~~ → Entscheidung 1.
2. ~~**Soll die Freistellung auch Tage erfassen, die vor dem Verlegen spielbar waren** (1.–5.11. im Malus-Fall)?~~ →
   Entscheidung 2.
3. ~~**Bereits gebuchter Malus und gebrochene Serien** aus früheren Verlegungen~~ → Entscheidung 3.
4. ~~**Gilt dasselbe, wenn das Verlegen eines inaktiven Plans später aktiviert wird?**~~ → Entscheidung 4.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

Keine Malus- oder Münzregel ist neu: B-613 Entscheidung 2 und der Kommentar `StudyPlansController.cs:222-224`
versprechen schon, dass Tage vor dem ersten spielbaren Tag „zugunsten des Kindes“ bleiben. Der Code löst das nur für die
Richtung „Zukunftsstart vorgezogen“ ein. Die Entscheidungen stellen die versprochene Regel für die Gegenrichtung her.

1. **Vor dem Absenken wird `ActivatedAt` auf `max(ActivatedAt, min(alter Start, heute))` angehoben, nur bei
   `startMoved && plan.Active`.** Der alte Start wird dafür vor `StudyPlansController.cs:216` in einer Variablen
   festgehalten. Die Reihenfolge ist Anheben, dann das bestehende Absenken (`:225-229`), beides im selben Block.
   *Begründung:* `min(alter Start, heute)` ist der erste Tag, an dem der Plan nach der **alten** Lage spielbar war
   (`PositionPlayService.cs:66-67`); alles davor war nicht spielbar und darf nicht zählen. Gerechnet an den Fällen der
   Story: Serie `max(18.11., min(1.12., 25.11.))` = 25.11., der 24.11. ist frei; Malus `max(1.11., min(15.11., 20.11.))` =
   15.11., die Perioden 6.–14.11. fallen aus der Fälligkeit, weil `from >= ActivatedAt` gilt und `ActivatedAt <= StartDate`
   nicht mehr (`PositionProgressService.cs:677-679`). Der Test `Vorgezogener_Start_Gilt_Ab_Heute_Als_Eingeschaltet`
   (`RueckwirkendePflichtTests.cs:465`) bleibt grün: `max(start+5, min(start+5, heute))` ändert nichts, das Absenken
   setzt danach weiter auf heute. *Kosten:* ein weiterer Schritt in einem ohnehin dichten Block, und der Controller muss
   den alten Start kennen.
   *Verworfene Alternative:* `ActivatedAt = playableFrom` bei **jedem** Verlegen. Das wäre kürzer, gäbe aber dem Vater mit
   jeder Startkorrektur um einen Tag die gesamte spielbare Vergangenheit des Plans frei – Malus und Serie der letzten
   Wochen verfielen. Das widerspräche dem Kern „verpasste Pflicht kostet“ und der Story, die nur **unspielbare** Tage
   freistellen will.
   *Ausgeschlossener Fall (gegen die User Story gerechnet):* Ein Tag, an dem der Plan nach dem alten Start spielbar war,
   bleibt Pflicht, auch wenn der Vater den Start danach hinter ihn schiebt – das Kind konnte an diesem Tag spielen. Eine
   Wochenpflicht, deren Woche vor dem alten Start beginnt, gilt weiter als nicht fällig (`from >= ActivatedAt`,
   `PositionProgressService.cs:673-679`); das ist die abgenommene Regel „nur Perioden ganz nach dem Einschalten“ von B-613
   und ändert sich hier nicht.
2. **Keine Historie der Starts, kein neues Feld, keine Migration.** Die Freistellung erfasst auch Tage, die vor dem
   Verlegen spielbar waren (1.–5.11. im Malus-Fall), weil nur der **letzte** Schaltzeitpunkt bekannt ist. *Begründung:*
   Das ist der ausdrückliche Vertrag der Spalte (`StudyPlanEntities.cs:93-96`: „the blur always goes in the child's
   favour“) und B-613 Entscheidung 2. *Kosten:* In seltenen Doppelverlegungen schenkt der Fix dem Kind ein paar Tage, die
   es spielen konnte. *Verworfene Alternative:* Startverlauf als Tabelle (Migration über die echte DB, Schema-Tore) für
   eine Handvoll Tage in einer Korrektur, die der Vater selbst auslöst.
3. **Bereits gebuchter Malus und gebrochene Serien aus früheren Verlegungen werden nicht rückwirkend geändert.** Der Fix
   wirkt ab dem nächsten Verlegen. *Begründung:* Der Ledger ist idempotent und wird nicht umgeschrieben (Kommentar
   `PositionProgressService.cs:780`), und eine Heilung bräuchte ein Skript an der echten `pugling.db`. *Kosten:* Ein Kind,
   dem so schon ein Malus gebucht wurde, behält ihn; der Vater kann ihn über das Verschenken (`POST children/{}/points`,
   Druckventil gegen Malus-Schulden, `backend/Pugling.Api/CLAUDE.md`) ausgleichen.
4. **Das Verlegen eines inaktiven Plans ändert `ActivatedAt` nicht.** *Begründung:* Das spätere Einschalten setzt
   `ActivatedAt = today` (`StudyPlansController.cs:220`); die Anhebung ist mit `plan.Active` gegated. Wird im selben
   Request eingeschaltet **und** der Start verlegt, setzt `:220` zuerst heute, und `max(heute, min(alt, heute))` bleibt
   heute. *Kosten:* keine.

**Überlappung:** Die Entscheidungen berühren [B-613](B-613-rueckdatierter-plan-bricht-die-serie.md) (abgenommen), ändern
aber keine seiner Entscheidungen; sie lösen deren Versprechen („Tage davor nur zugunsten des Kindes“) für das
Zurückverlegen ein. Keine Überlappung mit einer anderen offenen Story gefunden.

## Akzeptanzkriterien

Jedes Kriterium nennt die Änderung, die es rot macht. Testklasse: `RueckwirkendePflichtTests` (Uhr per
`factory.Clock.Advance`, Muster `:465`).

1. **Malus:** Plan mit Start heute T und Tagespflicht; Start auf T+5 verlegt; Uhr auf T+7; Start zurück auf T: die
   Abrechnung bucht für die Tage T+1 bis T+4 keinen Malus. Rot, wenn der Anhebungsschritt fehlt (`ActivatedAt` bleibt T,
   `ActivatedAt <= StartDate` – heute wird für T+1 bis T+4 gebucht) oder wenn er `heute` statt `min(alter Start, heute)`
   nimmt (dann fielen auch T+5 und T+6 aus der Pflicht).
2. **Serie:** Plan mit Start T+13, am Tag T eingeschaltet (`ActivatedAt` T); Uhr auf T+7; Start auf T+6 verlegt: der Tag
   T+6 zählt nicht als Pflichttag, die Serie des Kindes bleibt. Rot, wenn die Anhebung fehlt oder erst nach dem Absenken
   steht, oder wenn der alte Start nach `:216` gelesen wird (dann ist es der neue und `min` liefert T+6).
3. **Vorziehen bleibt:** `Vorgezogener_Start_Gilt_Ab_Heute_Als_Eingeschaltet` bleibt grün – ein Zukunftsstart, auf heute
   vorgezogen, schuldet ab heute. Rot, wenn die Anhebung das Absenken verdrängt (`ActivatedAt` bliebe T+5, der verpasste
   Tag fiele aus).
4. **Nichts verlegt, nichts angehoben (Datenzusicherung, ausdrücklich beschriftet):** Plan mit `ActivatedAt` T und Start
   T+10 (am Tag T eingeschaltet), Uhr auf T+12: ein PATCH ohne `startDate` – und ebenso einer mit dem unveränderten Start
   oder bei `Active == false` – lässt `ActivatedAt` bei T. Das Verhalten ändert sich dabei nicht (der Tag liegt vor dem
   Start), die Zusicherung hält nur fest, dass die Anhebung auf `startMoved && plan.Active` begrenzt bleibt. Rot, wenn die
   Begrenzung fällt (`ActivatedAt` stünde danach bei T+10).
5. **Spielbare Tage bleiben Pflicht:** Plan mit Start T und Tagespflicht, am Tag T+3 der Start auf T+2 gesetzt (später,
   aber nicht nach heute): ein an T+2 verpasster Tag kostet Malus. Rot bei der verworfenen Kurzregel
   `ActivatedAt = playableFrom` bei jedem Verlegen (T+2 fiele frei) und bei einer Anhebung über `min(alter Start, heute)`
   hinaus.

## Schätzung

**Größe: S** — wie „`childId` aus dem Test-Pfad ziehen“ (B-01): eine Stelle im Controller (vier Zeilen und eine
Variable), dazu fünf Testfälle in einer vorhandenen Klasse.

- **`migration: nein`** — `StudyPlan.ActivatedAt` gibt es schon (`StudyPlanEntities.cs:97`); es ändert sich nur, wann der
  Controller den Wert schreibt, nicht das Schema (Entscheidung 2: keine Startverlaufs-Tabelle).
- **`vertragsbruch: nein`** — `UpdatePlanDto` (Vergleich `dto == new UpdatePlanDto(null, null, null, null, false)`,
  `StudyPlansController.cs:208`) und die Antwort bleiben unverändert; geändert wird nur ein interner Spaltenwert.
- **`wo: backend`** — das Frontend schickt weiter nur `startDate`.

### Risiken

- **Reihenfolge im Block:** Anheben muss vor dem Absenken (`:225-229`) stehen, und der alte Start muss vor `:216`
  gelesen werden; liest man ihn danach, liefert `min` den neuen Start (AK 2 fängt das).
- **Gleichzeitiges Einschalten und Verlegen:** `:220` setzt `ActivatedAt` zuerst auf heute; die Anhebung darf das nicht
  nach hinten schieben (Entscheidung 4, gedeckt durch das `plan.Active`-Gate und `max`). Eigener Testfall im Bau.
- **Vorhandener Test:** `Vorgezogener_Start_Gilt_Ab_Heute_Als_Eingeschaltet` (`RueckwirkendePflichtTests.cs:465`) ist die
  Gegenprobe (AK 3) und muss grün bleiben.
- **Bereits gebuchter Malus** bleibt (Entscheidung 3); kein Skript an der echten `pugling.db`.

### Angriffsplan

1. `StudyPlansController.Update`: alten Start vor `:216` festhalten.
2. Im Block `if (startMoved && plan.Active)` zuerst anheben (`ActivatedAt = max(ActivatedAt, min(alter Start, heute))`),
   dann das bestehende Absenken.
3. Fünf Fälle aus den Akzeptanzkriterien in `RueckwirkendePflichtTests`; vorher rot gemessen.
4. `dotnet build Pugling.sln` und `dotnet format Pugling.sln` vor dem Commit.

### Testweg

- `RueckwirkendePflichtTests` (Uhr per `factory.Clock.Advance`, Muster `:465`): AK 1 bis 5 als eigene Fälle. Ohne den
  Anhebungsschritt sind AK 1 und AK 2 rot (Malus gebucht für T+1..T+4 bzw. Serie gebrochen am T+6); AK 3 und AK 4 sind
  Gegenproben und im Ausgangszustand grün – AK 4 ist als Datenzusicherung beschriftet, AK 5 fällt nur bei der
  verworfenen Kurzregel.
- Vor der Abnahme: Agent `pugling-reviewer`; der Rollengang als Vater (Start verlegen) gegen die laufende App oder, wo
  das ausfällt, eine Zeile im `## Verlauf`, die das benennt.

## Verlauf

- **2026-11-18** — angelegt (Quelle: Nachtlauf 2026-11-18, Nachschau von Sprint 87, pugling-reviewer; Defekt, entgangen
  bei B-613).
- **2026-11-19** — ausformuliert im Vorbereitungslauf: Ist-Stand mit Fundstellen am Code belegt; Malus-Fall bestätigt,
  Serien-Fall korrigiert (betroffen ist der Streifen neuer Start bis heute, nicht alles ab `ActivatedAt`);
  `unverifiziert` entfernt; Akzeptanzkriterien als Entwurf, Offene Punkte mit Empfehlung. Nicht gegrillt.
- **2026-11-19** — gegrillt im Vorbereitungslauf (Sonnet), stellvertretend und unbestätigt: vier Entscheidungen
  (Anhebung `max(ActivatedAt, min(alter Start, heute))` vor dem Absenken, kein Startverlauf, keine rückwirkende Heilung,
  inaktive Pläne unberührt); Akzeptanzkriterien final mit je einer roten Änderung. Keine neue Malus-Regel.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): S, backend, keine Migration, kein Vertragsbruch; Block in
  `StudyPlansController.cs:211-229` am Code nachgelesen, die Fälle AK 1, 2, 5 gegen die Formel durchgerechnet.
- **2026-11-21** — in-arbeit (Nachtlauf 2026-11-21, Sprint 102). Je AK-Fall der Test und die Mutante:
  - AK 1, Malus nach Zurückverlegen → `RueckwirkendePflichtTests.Zurueckverlegter_Start_Bucht_Keinen_Malus_Fuer_Unspielbare_Tage`;
    rote Probe ohne Fix: erwartet `[T+5, T+6]`, gemessen T+1..T+6 (sechs Tage). Mutanten M1 (Anhebung weg), M2
    (Anhebung = heute, dann gar kein Malus), M3 (alter Start nach dem Überschreiben gelesen) → rot.
  - AK 2, Serie → `Zurueckverlegter_Start_Bricht_Die_Serie_Nicht`; rote Probe ohne Fix: erwartet 11, gemessen 1. M1, M3 → rot.
    Der Test verlängert Plan A (der geseedete Plan endet bei T+5), sonst bräche die Serie am Testaufbau statt am Fehler.
  - AK 3, Vorziehen bleibt → bestehender `Vorgezogener_Start_Gilt_Ab_Heute_Als_Eingeschaltet`; M7 (Absenken weg) → rot.
  - AK 4, Datenzusicherung (beschriftet) → `Ohne_Verlegung_Wird_Nichts_Angehoben`, drei Fälle (kein Start, gleicher
    Start, inaktiver Plan); M4 (`Active`-Gate weg), M5 (`startMoved`-Gate weg) → rot. Im Ausgangszustand grün.
  - AK 5, spielbare Tage bleiben Pflicht → `Spielbare_Tage_Bleiben_Pflicht`; M6 (Kurzregel `ActivatedAt = playableFrom`)
    und M2 → rot. Im Ausgangszustand grün (Gegenprobe gegen die verworfene Regel).
  - **Äquivalent, kein Test möglich:** die Reihenfolge „anheben, dann absenken“ (Risiken, AK 2). Der Block ist ein Clamp
    zwischen `min(alter Start, heute)` und `max(neuer Start, heute)`; beide Reihenfolgen liefern denselben Wert
    (Review-Hinweis 🟢). Ebenso tut der Block bei „einschalten + verlegen“ in einem PATCH nichts (`:221` setzt heute, der
    Clamp lässt heute stehen); der unter Risiken angekündigte eigene Testfall wäre nur eine Absichtserklärung und ist
    darum nicht gebaut.

  Gemessen an `560bd2f0 + Arbeitsbaum geändert` mit `mutant-probe.py` (Debug), Grundlinie danach grün:

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | M1 | `StudyPlansController.cs:231` Anhebung weg | red | red |
  | M2 | `:230` Anhebung = heute | red | red |
  | M3 | `:212` alter Start = neuer Start | red | red |
  | M4 | `:226` ohne `plan.Active` | red | red |
  | M5 | `:226` ohne `startMoved` | red | red |
  | M6 | `:231` Kurzregel `playableFrom` | red | red |
  | M7 | `:233` Absenken weg | red | red |

  Review (`pugling-reviewer`): kein 🔴, ein 🟡 außerhalb des Diffs → [B-783](B-783-verlaengerter-plan-holt-abgelaufene-tage-zurueck.md)
  (verlängerter, schon abgelaufener Plan, ohne Zählung), drei 🟢: Spalten-Doku `StudyPlanEntities.cs` ergänzt, die beiden
  äquivalenten Fälle oben vermerkt. **Zähler 0 von 5.**
- **2026-11-21** — abgenommen in Commit `095b6e01` (Sprint 102). Backend 1942/1942 (Release).
  **Rollengang:** Die Wirkung braucht vergehende Tage, die laufende App hat keine verstellbare Uhr; ein echter Gang im
  Browser kann den Fall darum nicht zeigen. Ersatzbeleg sind die Integrationsfälle, die den PATCH des Vaters über die API
  fahren (Uhr per `factory.Clock`). Regressions-E2E auf 5201/5174 nach der letzten Änderung frisch gestartet:
  `full-flow`, `vater-schmal` (zwei Fälle), `heute-dran-nach-antworten` – 4 passed. Vater abgenommen auf diesem Beleg,
  Sohn und Creator als Regressionszeugen (keine ihrer Flächen im Diff).
- **2026-11-21** — nachgeschaut (Nachschau Sprint 102 in der Retro von Sprint 103). Befund aus dem Review schon als
  [B-783](B-783-verlaengerter-plan-holt-abgelaufene-tage-zurueck.md) angelegt und in Sprint 103 gebaut. Zusätzlich
  geprüft: der Rundweg des echten Clients – `VaterPlanDetail.tsx:159-160` schickt `startDate`/`endDate` nur, wenn sie
  sich gegenüber dem geladenen Plan geändert haben, im selben ISO-Format; ein Speichern ohne Datumsänderung löst
  `startMoved` also nicht aus. Kein weiterer Befund.
