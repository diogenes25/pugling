---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/reward, rolle/student, rolle/supervisor]
aliases: [Kennenlern-Tag ohne Malus, Erster Tag einer Position]
status: geschaetzt
prio: P3
art: Frage
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-419
grund: ""
ersetzt_durch: []
---

# B-432 · Prüfauftrag: darf der erste Tag einer Position am Tagesziel vorbei ohne Malus enden?

Zurückgestellt aus [B-419](B-419-erstbegegnung-neue-karte-erst-zeigen.md), offener Punkt 4. Am ersten Tag
einer Position sind alle Items neu: das Kind führt sie in Runde 1 ein (0 Punkte), fragt sie in Runde 2 ab und
soll im Test **am selben Tag** 80 % erreichen – sonst kostet das Tagesziel beim Assistenten-Standard
„Normal" 5 Münzen (`VaterWizard.tsx`, Intensität Normal: −5 🪙). Gemessen im Rollengang vom 2026-11-08:
0 Münzen nach Runde 1, 20 % im Test; der Malus wäre um Mitternacht gefallen. Zu entscheiden ist, ob der
**Einführungstag** einer Position als erfüllt gilt, sobald alle Items eingeführt wurden (Empfehlung des PM:
ja, als serverseitige Regel in `PositionProgressService`, ohne neues Feld – „Kennenlernen ist die Pflicht des
ersten Tages"), oder ob der Vater das je Position über den Assistenten („erster Tag ohne Malus") einstellt,
oder ob alles bleibt, weil die Intensität „Locker" (kein Malus) genau dafür da ist. Eine Wertfrage über den
„Stick" – nur im Dialog.

## User Story

Als **Sohn** möchte ich am ersten Tag einer neuen Position keine Münzen verlieren, wenn ich alle neuen Wörter
ordentlich kennengelernt habe, damit sich der Malus fair anfühlt – bestraft wird Nicht-Lernen, nicht dass der
Test über Wörter von vor zehn Minuten noch nicht sitzt. Als **Vater** möchte ich, dass der Malus sonst
unverändert greift, damit das Pflichtziel ab Tag 2 seinen Biss behält.

## Ist-Stand am Code

- `PositionProgressService.IsGoalMetAsync` (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:109-118`)
  kennt keine Ausnahme für den ersten Tag: prüfbare Typen gelten nur mit bestandenem Test als erfüllt (`:115-117`).
- Der Malus fällt in `SettleClosedPeriodsAsync` (`:325-379`): je geschlossener Periode ohne Belohnung, ohne Malus und
  mit `!IsGoalMetAsync` (`:357`) eine `PositionGoalPenalty` plus negative `GoalPenalty`-Buchung (`:359-376`).
  Abgerechnet wird lazy an Login (`AuthController.cs:85`) und Shop (`MeController.cs:281`).
- Der Wizard-Standard „Normal" setzt `penalty: 5` (`frontend/src/vater/VaterWizard.tsx:43`), „Locker" `0` (`:42`).
- Was der Code schon weiß: `PlanPosition.CreatedAt` (`backend/Pugling.Api/Models/PlanPositionEntities.cs:130`),
  `StudyPlan.StartDate` und je Item `PositionItemProgress.IntroducedAt` (`PlanPositionEntities.cs:216`), dazu die
  Tagesdosis `PositionPlayService.DailyDose` (`PositionPlayService.cs:31-41`). Ein neues Feld ist nicht nötig.
- Kennenlernen bringt bewusst **keine Punkte** (`PositionPracticeController.cs:399`: „must not be the cheapest way
  to coins").

## Die echte Lücke

Die Malus-Prüfung behandelt den Einführungstag wie jeden anderen Tag. Das Kind kann dort die Pflicht nur mit
einem Test über gerade erst gesehene Wörter erfüllen; tut es alles, was der Tag sinnvoll verlangt (einführen),
verliert es trotzdem Münzen.

## Offene Punkte

1. ~~Serverregel, Schalter im Assistenten, oder „Locker" genügt?~~ → Entscheidung 1.
2. ~~Was heißt „erster Tag"?~~ → Entscheidung 2.
3. ~~Gilt der Tag als *erfüllt* (Belohnung, Tagesbox) oder nur als *malusfrei*?~~ → Entscheidung 3.
4. ~~Was geschieht mit schon gebuchten Malus-Zeilen der echten DB?~~ → Entscheidung 4.

## Entscheidungen

1. **Serverregel ohne neues Feld, kein Schalter im Assistenten.** Begründung: die Empfehlung des PM; ein Schalter
   verlagert eine Fairness-Frage auf jeden Vater neu, und „Locker" nimmt den Malus für den ganzen Plan weg statt für
   einen Tag. Kosten: der Vater kann den Einführungstag nicht schärfer stellen; wer das will, braucht eine neue Story.
2. **Einführungstag = der erste fällige Tag der Position**, also `max(CreatedAt als UTC-Datum, plan.StartDate)`, und
   nur bei **täglicher** Taktung. Er ist malusfrei, wenn an genau diesem Tag die Tagesportion eingeführt wurde: alle
   Items des Pools, bei gesetzter `DailyDose` `min(Dosis, Pool)` Items mit `IntroducedAt == Einführungstag`.
   Begründung: an den Anlegetag gebunden, damit Schwänzen nicht den Ausnahmetag auf morgen verschiebt; die Wochenpflicht
   lässt ohnehin Tage zum Üben. Positionen ohne einführbaren Pool (Inhalt ohne Items) bleiben bei der alten Regel.
   Kosten: eine Position, die um 23:30 UTC angelegt wird, hat einen sehr kurzen Einführungstag – sie ist dann eben nur
   malusfrei, wenn das Kind in diesen Minuten einführt; die Regel ist nie strenger als heute.
3. **Nur malusfrei, nicht erfüllt.** Die Regel sitzt in `SettleClosedPeriodsAsync`, nicht in `IsGoalMetAsync`: keine
   Ziel-Punkte, keine Tagesbox, die Tagesübersicht zeigt das Ziel weiter offen, und ein bestandener Test bringt wie
   bisher die Belohnung. Begründung: Kennenlernen bringt bewusst keine Punkte (`PositionPracticeController.cs:399`);
   die Ausnahme soll Strafe verhindern, nicht Belohnung erzeugen. Kosten: der Sohn sieht am ersten Tag „Tagesziel
   offen", obwohl nichts abgezogen wird – ein Hinweis in der Oberfläche ist bewusst nicht Teil dieser Story.
4. **Keine Rückwirkung.** Schon gebuchte `PositionGoalPenalty`-Zeilen bleiben; die Regel greift nur für Perioden, die
   noch nicht abgerechnet sind. Begründung: die echte `pugling.db` der Söhne wird nicht umgeschrieben, und das
   Idempotenz-Netz (Unique-Index) bleibt unberührt. Kosten: ein früherer Einführungstags-Malus bleibt stehen; der Vater
   kann ihn über „Verschenken" ausgleichen.

Alle vier: Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13,
[Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

## Akzeptanzkriterien

1. Tägliche Position mit Malus 5, Einführungstag: das Kind führt alle Items ein, besteht den Test nicht → nach dem
   Abrechnen am Folgetag **kein** `GoalPenalty`-Eintrag für diesen Tag. *Rot macht ihn:* die Ausnahme fehlt oder
   prüft den falschen Tag.
2. Wie 1, aber das Kind führt am Einführungstag **nichts** (bzw. weniger als die Tagesportion) ein → Malus fällt
   wie bisher. *Rot macht ihn:* eine Ausnahme, die nur auf das Datum schaut und das Einführen nicht prüft.
3. Am **zweiten** Tag ohne bestandenen Test fällt der Malus wie bisher. *Rot macht ihn:* eine Ausnahme, die jeden Tag
   mit Einführungen freistellt (Dosis-Positionen führen täglich ein).
4. Mit Tagesdosis 3 bei 10 Items genügt am Einführungstag das Einführen von 3. *Rot macht ihn:* eine Regel, die
   den ganzen Pool verlangt und damit für Dosis-Positionen nie greift.
5. Am Einführungstag ohne bestandenen Test wird **keine** Ziel-Belohnung gebucht und die Tagesübersicht meldet das
   Ziel als offen. *Rot macht ihn:* die Ausnahme in `IsGoalMetAsync` statt in der Malus-Abrechnung.
6. Wöchentliche Positionen bleiben unverändert (erste Woche ohne Test → Malus). *Rot macht ihn:* eine Ausnahme ohne
   Taktungs-Prüfung.

## Schätzung

**Größe: S** — eine private Prüfung in `PositionProgressService` und ein Aufruf in `SettleClosedPeriodsAsync`, keine
Schemaänderung, kein Vertragsfeld.

- **Risiken:** Datums-Grenze `CreatedAt` (UTC) gegen `plan.StartDate` (Datum) – falsche Umrechnung verschiebt den Tag;
  ein zusätzlicher Query je abgerechneter Periode (nur für die eine Periode des Einführungstags, nicht für alle).
- **Angriffsplan (Backend):** (1) Tests in `PflichtMalusTests.cs` rot schreiben; (2) `IsIntroductionDaySparedAsync`
  in `PositionProgressService` neben `IsGoalMetAsync`, Pool über `play.ItemsOfAsync`/`PoolSize`, Dosis über
  `play.DailyDose`; (3) Aufruf vor dem Buchen in `SettleClosedPeriodsAsync`; (4) englischer `<summary>` mit dem Warum.
- **Testweg:** Integrationstests in `backend/Pugling.Api.Tests/PflichtMalusTests.cs` (AK 1–4, 6; Muster
  `SeedPenaltyPlan` mit Plan-Start/`CreatedAt` in der Vergangenheit plus direkt gesetzten `IntroducedAt`-Zeilen,
  abgerechnet über den Login wie in `GerissenePflicht_ZiehtMuenzMalusAb_…`) und `ErstbegegnungTests.cs` (AK 5, Tagesübersicht nach Einführen);
  danach `/smoke-test`. Rollengang: Sohn legt nichts an – der Vater legt eine Position an, der Sohn führt ein, der
  Login am Folgetag zeigt keinen Abzug.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Sohn ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  zurückgestellt aus B-419.
- **2026-11-13** — ausformuliert (Backlog-Lauf): Ist-Stand am Code belegt; die Ausnahme fehlt in
  `IsGoalMetAsync`/`SettleClosedPeriodsAsync`, alle nötigen Daten liegen schon vor.
- **2026-11-13** — gegrillt: vier Entscheidungen (Serverregel, Anlegetag + Tagesportion, nur malusfrei, keine
  Rückwirkung). Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13,
  [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — geschaetzt: S, backend, keine Migration, kein Vertragsbruch.
