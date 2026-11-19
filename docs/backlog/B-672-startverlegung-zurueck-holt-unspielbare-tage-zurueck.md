---
tags: [typ/story, status/ausformuliert, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Start zurückverlegt bricht Serie, Ferienpause kostet Malus]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "Nachtlauf 2026-11-18, Nachschau von Sprint 87 (fe13531c, pugling-reviewer) – docs/pm-sitzung-2026-11-18.md, Runde 5"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-613]
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

1. **Genügt `ActivatedAt = max(ActivatedAt, min(alter Start, heute))` vor dem Absenken?** Empfehlung: ja. Serie:
   `max(18.11., min(1.12., 25.11.))` = 25.11. – der 24.11. ist frei. Malus: `max(1.11., min(15.11., 20.11.))` =
   15.11. – 6.–14.11. fallen aus der Fälligkeit (`from >= ActivatedAt` greift, weil `ActivatedAt > StartDate`).
   Das anschließende Absenken bleibt unverändert und verhindert, dass ein Vorziehen auf heute das Ergebnis zerstört.
2. **Soll die Freistellung auch Tage erfassen, die vor dem Verlegen spielbar waren** (1.–5.11. im Malus-Fall)?
   Empfehlung: ja, hinnehmen. Die Alternative (Historie der Starts speichern) wäre ein Schema; die Unschärfe zugunsten
   des Kindes ist bereits Vertrag der Spalte.
3. **Bereits gebuchter Malus und gebrochene Serien** aus früheren Verlegungen: Empfehlung: nicht rückwirkend ändern
   (Ledger bleibt, siehe Kommentar `PositionProgressService.cs:780`); der Fix wirkt ab dem nächsten Verlegen.
4. **Gilt dasselbe, wenn das Verlegen eines inaktiven Plans später aktiviert wird?** Nicht betroffen:
   Einschalten setzt `ActivatedAt = today` (Zeile 220) und das Verlegen prüft `plan.Active` (Zeile 225).
   Empfehlung: nichts tun.

## Entscheidungen

Noch keine – Stufe `ausformuliert`; die Punkte oben warten auf den Grill.

## Akzeptanzkriterien

Entwurf (je mit der Änderung, die es rot macht):

1. **Malus:** Plan seit Tag X gespielt, Start auf Tag Y > heute verlegt, dann zurück auf X: die Abrechnung bucht für
   Perioden zwischen X und Y keinen Malus. Rot, wenn `ActivatedAt` beim Verlegen nicht auf `min(alter Start, heute)`
   angehoben wird (heute: Malus gebucht).
2. **Serie:** Plan mit Zukunftsstart, vorzeitig eingeschaltet, dann Start auf gestern verlegt: die Serie des Kindes
   bleibt, der Tag „neuer Start bis gestern“ zählt nicht als Pflichttag. Rot, wenn die Anhebung fehlt oder den alten
   Start schon überschrieben vorfindet.
3. **Vorziehen bleibt:** die Zukunftsstart-auf-heute-Probe (`Vorgezogener_Start_Gilt_Ab_Heute_Als_Eingeschaltet`) bleibt
   grün – ein am heutigen Tag fälliger Plan schuldet ab heute. Rot, wenn die Anhebung das Absenken verdrängt.
4. **Unverlegter Plan und Verlegen eines inaktiven Plans** ändern `ActivatedAt` nicht. Rot, wenn die Anhebung nicht auf
   `startMoved && plan.Active` begrenzt ist.
5. **Später verlegt** (Start nach hinten in die Zukunft verschoben, dann gespielt): ein Tag, an dem der Plan nach dem
   alten Start spielbar war, bleibt Pflicht. Rot, wenn die Anhebung über `min(alter Start, heute)` hinausgeht.

## Verlauf

- **2026-11-18** — angelegt (Quelle: Nachtlauf 2026-11-18, Nachschau von Sprint 87, pugling-reviewer; Defekt, entgangen
  bei B-613).
- **2026-11-19** — ausformuliert im Vorbereitungslauf: Ist-Stand mit Fundstellen am Code belegt; Malus-Fall bestätigt,
  Serien-Fall korrigiert (betroffen ist der Streifen neuer Start bis heute, nicht alles ab `ActivatedAt`);
  `unverifiziert` entfernt; Akzeptanzkriterien als Entwurf, Offene Punkte mit Empfehlung. Nicht gegrillt.
