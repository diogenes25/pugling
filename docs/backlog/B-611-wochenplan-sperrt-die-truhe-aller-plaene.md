---
tags: [typ/story, status/geschaetzt, bereich/gamification, rolle/student]
aliases: [Wochenziel-Plan sperrt Truhe]
status: geschaetzt
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
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

1. ~~**Welche Regel gilt über den Kind-Tag?**~~ → Entscheidung 1.
2. ~~**Zählt ein Wochenziel an einem Tag mit Tageszielen gar nicht, auch nicht am Sonntag?**~~ → Entscheidung 2.
3. ~~**Gilt dieselbe Regel für das Bonus-Training wie für die Truhe?**~~ → Entscheidung 3.
4. ~~**Eine Story mit B-612 oder zwei?**~~ → Entscheidung 4.

## Entscheidungen

Grill-Runde mit dem Vater, 2026-11-17. Alle nach Empfehlung.

1. **Die Auswahl fällt über den Kind-Tag:** Gibt es in irgendeinem aktiven Plan Tagesziele, schließen die Tagesziele
   **aller** Pläne Truhe und Bonus auf; gibt es nirgends Tagesziele, alle Ziele mit Takt. *Begründung:* B-535
   Entscheidung 1 über den ganzen Tag statt je Plan gelesen; Plan-Grenzen sind Ablage des Vaters, kein
   Belohnungsbegriff, und „die Truhe für nichts" bleibt ausgeschlossen. *Kosten:* eine Änderung an der Senke
   (`ComputeChildDayAsync`/`RewardObligations`), Doku `PositionProgressService.cs:313-315` neu, ein neuer Test mit
   gemischtem Takt über zwei Pläne.
2. **Ein Wochenziel zählt an einem Tag mit Tageszielen nie für die Belohnung – auch nicht am Sonntag.** *Begründung:*
   Die Bremse für das offene Wochenziel ist der Wochen-Malus; eine Sonntagsausnahme wäre ein dritter Begriff von
   „Belohnungsziel", den Kind und Startseite nicht kennen. *Kosten:* Ein Kind, das den Malus hinnimmt, holt trotzdem
   täglich seine Truhe.
3. **Bonus-Training und Truhe folgen derselben Regel.** *Begründung:* eine Bedingung („Tagesziele geschafft") für das
   Kind; beide lesen schon dieselbe Senke (`DailyBoxService.cs:37`, `ExtraTrainingService.cs:261`). *Kosten:* keine
   zusätzlichen.
4. **Zwei Stories, ein Sprint, B-611 zuerst.** B-611 ist die Server-Regel (`wo: backend`), [B-612](B-612-startseite-zaehlt-belohnungsziele-anders-als-der-server.md)
   liefert die Zahlen an die Startseite und baut auf der fertigen API auf. *Kosten:* zwei Abnahmen statt einer.

## Akzeptanzkriterien

Final (Entscheidungen 1–3). Je Fall die Änderung, die ihn rot macht. Aufbau wie `KindTagessichtTests` (zwei
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

## Schätzung

**Größe: S** — wie „`childId` aus dem Test-Pfad ziehen" (B-01): ein Eingriff an **einer** Stelle, deren Wirkung
schon vorher klar ist. Die Auswahl wandert von „je Plan, dann vereinigen" (`PositionProgressService.cs:331`) zu „über
die vereinigte Positionsliste" – `RewardObligations` kann dafür unverändert bleiben, sie bekommt nur die flache Liste
aller Pläne statt einer Liste je Plan. Der Aufwand liegt in der Doku an fünf Stellen und im neuen Test mit gemischtem
Takt über zwei Pläne, nicht im Code. Kleiner als M (B-03), weil kein neuer Pfad entsteht, sondern ein bestehender
anders aggregiert.

- **`migration: nein`** — die Belohnungsauswahl wird bei jedem Aufruf aus `PlanPosition.Cadence` und dem Spielstand
  gerechnet; nichts wird gespeichert, kein Feld kommt hinzu. Gebuchte `DailyBoxClaim`-Zeilen bleiben gültig.
- **`vertragsbruch: nein`** — kein Feld ändert Form oder Namen. `TodayResponse.RewardsUnlocked` und
  `ExtraTrainingOverview.Unlocked`/`Outstanding` behalten ihren Typ; es ändert sich, *wann* sie `true`/leer sind – das
  ist der Fix. Geändert wird nur der Doku-Text am Vertrag (`backend/Pugling.Contracts/Student/ProgressDtos.cs:23`
  „The reward goals of every active plan are met", `backend/Pugling.Contracts/Student/ExtraTrainingDtos.cs:22-23`
  „all goals in a plan without daily ones"); das zieht `docs/openapi/v1.json` nach (schreibt
  `ContractDocumentTests`), `frontend/src/lib/contract.ts` ist erzeugt und nicht eingecheckt.
- **`wo: backend`** — die Startseite liest `rewardsUnlocked` schon am Server (`frontend/src/sohn/SohnHome.tsx:139`,
  `:144`); ihre Balken-Nachrechnung ist Gegenstand von B-612. Mit Regel (a) rechnet der Client flach dieselbe Auswahl
  (`SohnHome.tsx:353-361`), also verschwindet der sichtbare Widerspruch mit B-611 schon von selbst.

### Aufrufer der Senke

`ComputeChildDayAsync` hat genau vier Aufrufer (`grep ComputeChildDayAsync`); die neue Auswahl wirkt nur über die
Felder `RewardOutstanding`/`RewardsUnlocked` des `ChildDayRollup`
(`backend/Pugling.Api/Services/Shared/ChildDayRollup.cs:20-21`):

| Aufrufer | liest | bekommt die neue Auswahl? |
| --- | --- | --- |
| Truhe `DailyBoxService.EvaluateAndAwardAsync` (`backend/Pugling.Api/Services/Shared/DailyBoxService.cs:37`) | `RewardsUnlocked` | **ja** – gewollt (AK 1, 3, 4) |
| Bonus `ExtraTrainingService.GateAsync` (`backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:261-267`) | `RewardOutstanding` | **ja** – gewollt (Entscheidung 3) |
| Startseite `ChildTodayController.Get` (`backend/Pugling.Api/Controllers/Student/ChildTodayController.cs:32-36`) | `RewardsUnlocked`, dazu `DutyDone`/`Goals*`/`Outstanding` | **ja** für `rewardsUnlocked`; der Rest bleibt |
| Vater-Dashboard `ChildrenDashboardService.BuildAsync` (`backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:30-33`) | nur `Plans`, `GoalsTotal`, `GoalsMet`, `DutyDone` | **nein** – liest kein Belohnungsfeld |

Nicht über die Senke, also **unberührt**: die Plan-Übersicht `ComputeDayAsync` (`PositionProgressService.cs:279-283`,
`RewardsUnlocked` je Plan, gelesen von `PlanOverviewController.cs:40`), die Serie (`IsCompleteDay`, `:348-352`), die
Zeitleiste (`TimelineDutyDone`) und der Malus. Ebenso unberührt bleibt die Test-Naht
`DailyBoxService.AwardIfUnlockedAsync` (`DailyBoxService.cs:45-50`), die eine Plan-`DayOverview` nimmt
(`backend/Pugling.Api.Tests/DailyBoxRangeTests.cs:50`).

### Risiken

1. **`DutyDone`/`Outstanding` dürfen nicht mitwandern.** Sie stehen in derselben Methode zwei Zeilen daneben
   (`PositionProgressService.cs:329-337`). Wer die Rechnung „aufräumt" und beides aus derselben flachen Liste zieht,
   verengt die Pflicht auf die Tagesziele – Serie, Dashboard und `dutyDone` kippten. AK 5 hält das; der Test muss
   `dutyDone: false` **und** eine nicht gewachsene Serie im selben Fall prüfen.
2. **Bonus-Sperre und Truhe unterscheiden sich am Rand weiter, und das ist richtig.** Die Truhe verlangt mindestens
   ein Belohnungsziel (`rewardGoals.Count > 0`, `:338`), der Bonus öffnet bei leerem `RewardOutstanding`
   (`ExtraTrainingService.cs:265`), also auch ganz ohne Pflicht. AK 4 prüft nur die Truhe; beim Umbau nicht „vereinheitlichen".
3. **Die Doku sagt heute ausdrücklich das Gegenteil** und muss an fünf Stellen neu: `ComputeChildDayAsync`
   (`PositionProgressService.cs:313-315`), `RewardObligations` (`:286-291`, „when the plan has none"),
   `DailyBoxService.cs:26-30` („one plan done while another is open earns nothing" – stimmt für Tagesziele weiter,
   nicht mehr für Wochenziele eines anderen Plans), `ExtraTrainingService.cs:256-258` („every playable plan has met
   today's reward goals") und die zwei Vertragsstellen oben samt `ChildTodayController.cs:22-23` („once every plan's
   daily goals are met"). Die Vertragsdoku landet in `docs/openapi/v1.json`; die Doku-Tore in `ConventionGuardTests`
   (`:675`, `:752`) prüfen, dass sie dort ankommt.
4. **Die offene-Ziele-Liste des Bonus ändert ihren Inhalt.** Im Fall der Idee listet sie heute „… (Wochenziel) offen",
   danach nichts (AK 1). Gewollt – aber die Bonus-Seite zeigt bei einem Wochenziel-only-Tag weiter „Erst die
   Tagesziele" (`frontend/src/sohn/SohnBonus.tsx:95`); das ist B-612 Entscheidung 2, nicht hier.
5. **Stundenplan-Takt (B-621).** Eine dritte Taktart fiele in `RewardObligations` heute in „alle Ziele"; die
   Zwei-Wege-Auswahl bleibt eine Stelle, an der B-621 wieder ansetzen muss. Kein Bau hier, nur Hinweis.

### Angriffsplan

1. Roter Test zuerst: neue Klasse mit dem Aufbau aus `KindTagessichtTests` (zwei Pläne per DB-Setup,
   `backend/Pugling.Api.Tests/KindTagessichtTests.cs:17-37`), Plan B über
   `TestApi.SeedLeitnerPosition(…, cadence: GoalCadence.Weekly)` (`backend/Pugling.Api.Tests/TestApi.cs:259-266`);
   AK 1 muss heute an `rewardsUnlocked` fallen (rote Probe am richtigen Ort: an der Zusicherung, nicht im Aufbau).
2. `ComputeChildDayAsync`: `rewardGoals = RewardObligations(alle Positionen aller Pläne)` statt `SelectMany` je Plan
   (`PositionProgressService.cs:331`); `RewardObligations` bleibt `public static` und unverändert, nur ihre Doku sagt
   „eine Positionsliste – je Plan oder über den Kind-Tag".
3. Doku an den fünf Stellen aus Risiko 3, englisch.
4. AK 2–5 als weitere Fälle derselben Klasse; AK 6 = bestehende Suite unverändert grün.
5. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`, `docs/openapi/v1.json` mit committen.

### Testweg

- **Neue Integrationstestklasse** in `backend/Pugling.Api.Tests` (etwa `BelohnungUeberPlaeneTests`), je AK ein Fall mit
  benannter Rot-Änderung: `today` (`rewardsUnlocked`, `dutyDone`, `currentStreak`), Truhe über einen echten
  Abschluss (Übungsrunde oder Test, Muster `PassTestAsync`, `KindTagessichtTests.cs:41-47`) und `DailyBoxClaims`
  zählen, Bonus über `student/children/{id}/extra-training` (`unlocked`, `outstanding`). Für AK 5 die Serie mit
  einem vorher bestandenen Tag aufbauen (`SeedPassedTest`, `KindTagessichtTests.cs:50ff`), sonst prüft „wächst nicht"
  nur einen Ausgangszustand von 0.
- **AK 6:** `BelohnungenTagespflichtTests`, `DailyBoxTests`, `DailyBoxRangeTests`, `ExtraTrainingTests`,
  `KindTagessichtTests` ohne Änderung grün (Test-Tor `dotnet test Pugling.sln -c Release`).
- **Rollengang:** eine **eigene** E2E-Spec (eine Fläche je Spec, `frontend/CLAUDE.md`) nach dem Vorbild von
  `frontend/e2e/belohnung-trotz-wochenziel.spec.ts` (B-535, ein Plan), aber mit **zwei Fächern** – seit B-539 Teil A
  über die API anlegbar: Tagesziel-Plan erfüllt, Wochenziel-Plan offen ⇒ Truhe verdient, Bonus-Einstieg offen. Zählt
  als Rollengang; nur „N passed" zählt.
- **Reviewer:** `pugling-reviewer`.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 83 (🟡, `pugling-reviewer`), `entgangen_bei: [B-606]`.
- **2026-11-17** — ausformuliert: Auswahl je Plan belegt (`PositionProgressService.cs:331`, gewollt laut Doku `:313-315`,
  ohne Vater-Entscheidung dazu); Truhe, Bonus und `today` hängen alle an `RewardsUnlocked`/`RewardOutstanding`; kein
  Test mit gemischtem Takt über Pläne (`KindTagessichtTests.cs:26-27`, beide `Daily`); echte Pläne heute nur mit
  Tagespflichten, also noch nicht sichtbar. Am Code gelesen, nicht per Test nachgestellt.
- **2026-11-17** — gegrillt mit dem Vater: Entscheidungen 1–4, alle nach Empfehlung; Akzeptanzkriterien final.
- **2026-11-17** — geschätzt: S (wie B-01, eine Aggregation an der Senke `PositionProgressService.cs:331`), backend,
  keine Migration, kein Vertragsbruch (nur Doku-Text am Vertrag); vier Aufrufer der Senke geprüft – Truhe, Bonus und
  `today` bekommen die neue Auswahl, das Dashboard liest kein Belohnungsfeld; Testweg neue Integrationstestklasse plus
  eigene E2E mit zwei Fächern, Reviewer `pugling-reviewer`.
