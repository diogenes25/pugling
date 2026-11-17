---
tags: [typ/story, status/ausformuliert, bereich/gamification, rolle/student]
aliases: [Wochenziel-Plan sperrt Truhe]
status: ausformuliert
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

# B-611 · Ein Plan nur mit Wochenzielen sperrt die Tagestruhe und den Bonus über alle Pläne

`PositionProgressService.ComputeChildDayAsync` wendet `RewardObligations` **je Plan** an: ein Plan ohne Tagesziele gibt
seine Wochenziele als Belohnungsziele ab (B-535: „sonst gäbe es die Truhe für nichts"). Seit B-606 gilt das über alle
Pläne des Kindes, seit B-539 hat ein Kind real mehrere: Französisch mit zwei Tageszielen, Mathe mit einem Wochenziel ⇒
am Montag ist Französisch erledigt, Truhe und Bonus bleiben trotzdem bis zum Mathe-Wochenziel zu – womöglich die ganze
Woche. Genau diese Wirkung wollte B-535 vermeiden (`PositionProgressService.cs` ~:265, Doku von `RewardObligations`);
deren Grund („Truhe für nichts") gilt nicht mehr, sobald ein anderer Plan Tagesziele hat.

Naheliegend: die Auswahl über den **Kind-Tag** treffen – Tagesziele aller Pläne, wenn es irgendwo welche gibt, sonst
alle Ziele. Kein Test hält heute gemischten Takt über Pläne (`KindTagessichtTests`).

## User Story

Als *Sohn* möchte ich die Tagestruhe und das Bonus-Training bekommen, sobald ich die Tagesziele des Tages geschafft
habe – auch wenn in einem anderen Fach ein Wochenziel noch offen ist –, damit mich eine schwere Wochenaufgabe nicht
sieben Tage lang um meine Belohnung bringt. Als *Vater* möchte ich, dass die Regel aus B-535 („Wochenziele sperren die
Truhe nicht, sie haben ihren eigenen Malus") unabhängig davon gilt, ob ich die Wochenziele in denselben Plan oder in
einen eigenen Fach-Plan lege.

## Ist-Stand am Code

Gelesen am 2026-11-17 auf `f9cd2a38`; nicht per Test nachgestellt. **Die Behauptung der Idee stimmt**, nur die
Zeilennummern waren verschoben.

**Die Auswahl fällt je Plan.**

- `RewardObligations` wählt aus **einer** Positionsliste: die Tagesziele, ohne sie alle Ziele mit Takt
  (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:292-297`; Doku mit der B-535-Begründung `:286-291`).
- `ComputeChildDayAsync` ruft sie **je Plan** auf und vereinigt erst danach:
  `planDays.SelectMany(p => RewardObligations(p.Overview.Positions))` (`PositionProgressService.cs:331`);
  `RewardsUnlocked` = es gibt Belohnungsziele und keines ist offen (`:338`).
- Das ist **gewollt gebaut**, nicht übersehen: die Doku von `ComputeChildDayAsync` sagt ausdrücklich „each plan
  contributes its own RewardObligations, so a plan of weekly goals keeps gating while another plan has daily ones"
  (`PositionProgressService.cs:313-315`). Grundlage war, dass die Bonus-Sperre schon vor B-606 je Plan prüfte (B-539,
  Ist-Stand „Was schon über alle aktiven Pläne rechnet"). Die Vorgabe im Angriffsplan von B-539 lautete nur „Pflicht/
  Belohnung = alle" (`docs/backlog/B-539-ein-aktiver-plan-je-fach.md:237`) – eine Entscheidung des Vaters zur
  Auswahl über Pläne hinweg gibt es nicht; B-606 Entscheidung 6 regelt Pflicht und Serie, nicht die Belohnungsziele.

**Wer daran hängt** (alle über dieselbe Senke, also alle gleich betroffen):

- Tagestruhe: `DailyBoxService.EvaluateAndAwardAsync` bucht nur bei `RewardsUnlocked`
  (`backend/Pugling.Api/Services/Shared/DailyBoxService.cs:37`).
- Bonus-Training: `ExtraTrainingService.GateAsync` sperrt, solange `RewardOutstanding` nicht leer ist
  (`backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:261-267`); die gelisteten offenen Ziele tragen die
  Beschriftung „(Wochenziel) offen" (`PositionProgressService.cs:300-301`).
- Startseite: `TodayResponse.RewardsUnlocked` (`backend/Pugling.Api/Controllers/Student/ChildTodayController.cs:35-36`,
  Vertrag `backend/Pugling.Contracts/Student/ProgressDtos.cs:23`, Doku „The reward goals of every active plan are met").

**Nicht betroffen:** Pflicht (`DutyDone`, alle Ziele aller Pläne, `PositionProgressService.cs:329-335`), Serie
(`IsCompleteDay` über die volle Pflicht, `:348-352`), Vater-Dashboard (liest nur `DutyDone`/`GoalsMet`,
`backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:30-33`) und der Malus. Die Plan-Übersicht
`overview` rechnet ihr `RewardsUnlocked` weiter je Plan (`PositionProgressService.cs:279-283`) – dort ist es richtig,
und die Startseite liest es nicht mehr.

**Der Fall der Idee, durchgerechnet:** Plan A (Französisch) mit zwei Tageszielen, Plan B (Mathe) mit einem Wochenziel.
Plan A gibt seine zwei Tagesziele ab, Plan B – weil ohne Tagesziel – sein Wochenziel. Sind am Montag beide Tagesziele
erfüllt, bleibt `RewardOutstanding` = [„… (Wochenziel) offen"], also keine Truhe und Bonus zu, bis das Mathe-Wochenziel
erfüllt ist. Im Extremfall die ganze Woche. Dasselbe gilt, wenn Plan A neben Tageszielen auch Wochenziele hat.
Lägen beide Ziele in **einem** Plan, gäbe es die Truhe am Montag (B-535, `BelohnungenTagespflichtTests.cs:84`).

**Tests:** Kein Test hält gemischten Takt über Pläne – die Idee stimmt.

- `KindTagessichtTests` legt beide Pläne mit `SeedLeitnerPosition` ohne `cadence` an
  (`backend/Pugling.Api.Tests/KindTagessichtTests.cs:26-27`), der Vorgabewert ist `Daily`
  (`backend/Pugling.Api.Tests/TestApi.cs:260`): zwei reine Tageszielpläne.
- `BelohnungenTagespflichtTests` prüft gemischten Takt, aber in **einem** Plan (`:84`, `:122`).

**Heute an den echten Kindern?** Nein, soweit die Plan-Notizen reichen: Friedrichs Pläne 8 und 12 und Theodors Pläne 9
und 10 tragen nur Tagespflichten (für Plan 12 belegt in `docs/backlog/B-620-friedrich-englisch-lehrwerk-und-plan.md:132-133`;
die übrigen nach den Plan-Notizen, an der echten DB nicht nachgesehen). Der Defekt wird real, sobald der Vater einen
Fach-Plan nur mit Wochenzielen anlegt – über die API seit B-539 Teil A möglich. Das Nachtlauf-Protokoll warnt den Vater
genau davor (`docs/pm-sitzung-2026-11-17-nachtlauf.md:206-208`).

## Die echte Lücke

Die Belohnungsregel aus B-535 hängt an der Plan-Grenze: ob ein Wochenziel die Truhe sperrt, entscheidet heute, in
welchem Plan es liegt, nicht was der Tag des Kindes enthält. Für die Startseite ist die Folge B-612. Mit dem Stundenplan-
Takt (B-621, Risiko 2) kommt eine dritte Taktart dazu, die dieselbe Regel braucht.

## Offene Punkte

1. **Welche Regel gilt über den Kind-Tag?** (a) Tagesziele **aller** Pläne, wenn es irgendwo welche gibt, sonst alle
   Ziele mit Takt (die Idee); (b) bleibt je Plan (heute; dann ist B-611 kein Defekt, sondern als `verworfen` zu
   begründen, und B-612 braucht die Serverzahlen erst recht); (c) nur Tagesziele, ein reiner Wochenziel-Tag hat keine
   Truhe. **Empfehlung (a):** sie ist B-535 Entscheidung 1, nur über den ganzen Tag statt je Plan gelesen – der Grund
   „sonst gäbe es die Truhe für nichts" bleibt gewahrt (ohne jedes Tagesziel gilt die alte Regel), und Plan-Grenzen
   sind Ablage des Vaters, kein Belohnungsbegriff. (c) bräche B-535 AK 2 (`BelohnungenTagespflichtTests.cs:122`).
2. **Zählt ein Wochenziel an einem Tag mit Tageszielen dann gar nicht für die Truhe – auch nicht am Sonntag, dem
   letzten Tag seiner Woche?** **Empfehlung: ja, gar nicht.** Das ist innerhalb eines Plans seit B-535 schon so, die
   Bremse ist der Wochen-Malus; eine Sonntagsausnahme wäre ein neuer, dritter Begriff von „Belohnungsziel", den das
   Kind nicht vorhersehen kann. Kosten (wie B-535): ein Kind kann ein Wochenfach liegen lassen und trotzdem täglich
   Truhen holen.
3. **Gilt dieselbe Regel für das Bonus-Training wie für die Truhe?** **Empfehlung: ja**, beide lesen schon heute dieselbe
   Senke (`DailyBoxService.cs:37`, `ExtraTrainingService.cs:261`), und B-535 Entscheidung 1 hat sie gemeinsam
   entschieden. Eine Trennung wäre neue Fachlichkeit, keine Reparatur.
4. **Eine Story mit B-612 oder zwei?** **Empfehlung: zwei Stories, ein Sprint, B-611 zuerst.** Mit Regel (a) stimmt die
   Startseite in den heute sichtbaren Fällen von selbst wieder (sie rechnet flach genau (a), siehe B-612); die Regel
   bleibt aber doppelt geführt, und das ist B-612s Teil.

## Akzeptanzkriterien

Entwurf, geschrieben für Regel (a). Je Fall die Änderung, die ihn rot macht. Aufbau wie `KindTagessichtTests` (zwei
aktive Pläne per DB-Setup oder seit B-539 Teil A über zwei Fächer).

1. Plan A mit zwei Tageszielen (beide erfüllt), Plan B nur mit einem Wochenziel (offen) ⇒ `student/children/{id}/today`
   meldet `rewardsUnlocked: true`, `dutyDone: false`; nach Abschluss einer Runde ist genau eine Truhe gebucht; das
   Bonus-Training ist offen und listet nichts als offen. *Rot mit der heutigen Auswahl je Plan
   (`PositionProgressService.cs:331`).*
2. Dieselben Pläne, Tagesziele **nicht** erfüllt, Wochenziel erfüllt ⇒ keine Truhe, Bonus gesperrt, die offenen Ziele
   sind die Tagesziele. *Rot, wenn ein Wochenziel ein Tagesziel ersetzen darf oder die Auswahl „irgendein Plan fertig"
   heißt.*
3. Zwei Pläne nur mit Wochenzielen, eins erfüllt, eins offen ⇒ keine Truhe, Bonus gesperrt; beide erfüllt ⇒ genau eine
   Truhe. *Rot, wenn „kein Tagesziel" als „nichts zu tun" gilt (die Truhe für nichts, B-535 AK 2).*
4. Ein Plan ohne jede Pflicht neben einem Plan nur mit Wochenzielen (offen) ⇒ keine Truhe. *Rot, wenn der pflichtlose
   Plan den Tag als „ohne Tagesziel, also frei" durchwinkt.*
5. Pflicht und Serie bleiben unberührt: im Fall von AK 1 zählt der Tag nicht als vollständig (`dutyDone: false`, die
   Serie wächst nicht). *Rot, wenn die neue Auswahl auch `DutyDone`/`IsCompleteDay` erfasst.*
6. Mit einem aktiven Plan unverändert: `BelohnungenTagespflichtTests`, `DailyBoxTests`, `ExtraTrainingTests` und
   `KindTagessichtTests` bleiben ohne inhaltliche Änderung grün.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 83 (🟡, `pugling-reviewer`), `entgangen_bei: [B-606]`.
- **2026-11-17** — ausformuliert: Auswahl je Plan belegt (`PositionProgressService.cs:331`, gewollt laut Doku `:313-315`,
  ohne Vater-Entscheidung dazu); Truhe, Bonus und `today` hängen alle an `RewardsUnlocked`/`RewardOutstanding`; kein
  Test mit gemischtem Takt über Pläne (`KindTagessichtTests.cs:26-27`, beide `Daily`); echte Pläne heute nur mit
  Tagespflichten, also noch nicht sichtbar. Am Code gelesen, nicht per Test nachgestellt.
