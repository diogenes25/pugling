---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/training, rolle/student]
aliases: [Learning Queue, Lern-Warteschlange, Heute-Woche-Überfällig]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/khan-academy-vergleich-plan.md#wo-khan-weiter-ist
grund: ""
ersetzt_durch: []
---

# B-220 · Eine Learning Queue in der Sohn-Arcade: heute, diese Woche, überfällig

Die Sohn-Arcade zeigt über `overview` die Tagesmission. Khan Academys „Reimagined"-Dashboard ordnet
alles nach Fälligkeit in Tages-, Wochen- und Unit-Missionen, und Überfälliges rutscht automatisch in die
Tagesliste. Die Idee: eine Warteschlange aus Pflichtpositionen, Missionen und (nach
[B-218](B-218-mischwiederholung-ueber-alle-positionen.md)) der Mischwiederholung, gruppiert nach
„heute", „diese Woche" und „überfällig", damit das Kind sieht, was es **jetzt** kostet, wenn es nichts
tut. Zu klären ist, ob die Gruppierung ein neuer Server-Endpunkt ist oder aus `overview` und den
Missionen clientseitig entsteht.

## User Story

Als *Schüler* möchte ich auf einen Blick sehen, was heute, diese Woche und bereits überfällig ist und was es mich kostet,
wenn ich es liegen lasse, damit ich die Reihenfolge meines Lernens nach dem Preis des Nichtstuns wählen kann.

## Ist-Stand am Code

Gemessen am 2026-11-19 auf `main`; Pfade relativ zu `backend/Pugling.Api/` bzw. `frontend/src/`, wenn nicht anders genannt.

- **Es gibt schon einen kind-weiten Tages-Endpunkt.** `GET student/children/{childId}/today`
  (`Controllers/Student/ChildTodayController.cs:14,27`, B-606) liefert alle Pflichten aller aktiven Pläne mit Serie und Truhe;
  der Vertrag `TodayResponse` (`Contracts/Student/ProgressDtos.cs:32-34`) trägt `Outstanding`, `RewardCadence` und die
  `PositionStatus`-Liste. Die Startseite gruppiert das clientseitig nach Fach (`sohn/SohnHome.tsx:84`, `sohn/tagessicht.ts` `nachFach`) –
  Gruppierung nach Fälligkeit gibt es dort nicht.
- **Der Preis des Nichtstuns steht im Vertrag.** `PositionStatus.PenaltyCoins` (`Contracts/Shared/ProgressDtos.cs:48`, B-525) ist
  der Malus je Position und wird am Pfad-Knoten gezeigt (`sohn/SohnHome.tsx:304,357`). Damit ist ein Teil von Entscheidung 1 gebaut.
- **Kein Periodenende je Position.** `PositionStatus` trägt `Cadence`, `GoalMet`, `DueCount`, aber kein Datum
  (`Contracts/Shared/ProgressDtos.cs:45-49`). Die Perioden-Logik ist da, aber privat: `PeriodRange`/`PeriodStart`
  (`Services/Shared/PositionProgressService.cs:34-47`, Woche = `WeekMonday` bis +6); Abrechnung gerissener Perioden
  `SettleClosedPeriodsAsync` (Zeile 754, lazy bei Login/Kauf, es gibt keinen Scheduler).
- **„Überfällig" existiert für drei Dinge, für Pflichten bewusst nicht.** Objectives: `GoalStatus.Overdue`
  (`Services/Shared/ObjectiveEvaluationService.cs:55`, UI `sohn/MyObjectives.tsx:88`). Karten: `DueCount` und
  `PositionItemProgress.DueOn` (`Models/PlanPositionEntities.cs:256`). Klassenarbeiten: kein Student-Zugriff
  (siehe B-248). Eine abgelaufene Pflicht-Periode ist gebucht und nicht nachholbar (Verlauf 2026-10-23).
- **Missionen tragen ihre Restzeit schon:** `DaysLeft` (`Contracts/Student/LearnProgressDtos.cs:26-31`, B-528); die Missions-Tafel gruppiert
  nach Takt (`sohn/GamificationPanels.tsx:12`).
- **Mischwiederholung und Countdown fehlen** (B-218 und B-248 sind beide `idee`/`ausformuliert`, kein Code); `student/children/{id}/queue`: nicht gefunden.
- **Zeit ist UTC.** Alle Perioden rechnen auf dem UTC-Kalendertag (`ChildTodayController.cs:31`, Fallstrick im Repo-Root); „bis heute Abend"
  wäre für ein Kind in Deutschland ein Zeitpunkt, den der Server nicht kennt.

## Die echte Lücke

Nicht „es fehlt eine Warteschlange", sondern: Der Server liefert die Zutaten schon einzeln (`today`, `PenaltyCoins`, `DaysLeft`,
`DueCount`, `Overdue` der Objectives), aber **kein Fälligkeitsdatum je Eintrag** und **keine gemeinsame Sortierung**. Die Queue ist im Kern ein
neues Lese-Modell mit einem Feld `bis`, das es heute nirgends gibt. Weil B-218 und B-248 noch nicht gebaut sind, hätte eine erste Fassung nur
drei Eintragsarten (Pflicht, fällige Karten, Objective/Mission) – und die Karten-Gruppe überschneidet sich mit der Mischwiederholung.

## Offene Punkte

Bereits entschieden (Nutzer 2026-10-23 und Vater 2026-11-17, siehe Verlauf): zeigt, was Nichtstun kostet; Server-Endpunkt; „überfällig" nur für
Karten, Objectives, Klassenarbeiten; keine Sortierung nach Stundenplan. Offen:

1. **Neues Feld in `today` oder eigene Route?** Empfehlung: eigene Route `student/children/{childId}/queue`, gespeist aus
   `ComputeChildDayAsync` und den Missions-/Objective-Diensten; `today` bleibt schlank, beide teilen den Dienst. Additiv, kein Vertragsbruch.
2. **Welches Datum und welche Beschriftung?** Empfehlung: je Eintrag `DueBy` als `DateOnly` (Tag: heute; Woche: Sonntag) und
   der Text „heute"/„bis Sonntag" ohne Uhrzeit, wegen UTC. Die Perioden-Funktion aus `PositionProgressService` wird öffentlich statt
   im Client nachgebaut.
3. **Eintragsarten der ersten Fassung?** Empfehlung: Pflichten (mit `PenaltyCoins`), fällige Karten, offene Missionen, überfällige Objectives;
   Mischwiederholung und Klassenarbeit kommen als weitere Art, wenn B-218/B-248 gebaut sind – darum `Kind` als offene Zeichenkette im Vertrag, nicht als
   geschlossenes Enum.
4. **Ersetzt die Queue die Tagessicht (B-606) oder ergänzt sie?** Empfehlung: ergänzt, als Block auf der Startseite; die Fach-Gliederung bleibt, die
   Queue ordnet nach Fälligkeit.
5. **Sortierung innerhalb einer Gruppe.** Empfehlung: nach `PenaltyCoins` absteigend, dann Fach; ohne Preis ans Ende.
6. **Soll das Kind die Queue als Pflicht lesen, auch wenn ein Eintrag freiwillig ist?** Empfehlung: Pflicht-Einträge tragen ein Merkmal `Duty`, damit „Kür" nicht
   wie „kostet etwas" aussieht.

## Akzeptanzkriterien (Entwurf)

1. Eine Tagespflicht mit `PenaltyCoins` 20 steht unter „heute" mit dem Preis 20 und `DueBy` = heute. *Rot, wenn der Eintrag den Preis nicht trägt.*
2. Eine Wochenpflicht steht unter „diese Woche" mit `DueBy` = dem Sonntag der Woche, nicht dem Tag. *Rot, wenn die Perioden-Funktion `Weekly` wie `Daily` behandelt.*
3. Eine erfüllte Pflicht steht nicht mehr in der Queue. *Rot, wenn die Queue `GoalMet` ignoriert.*
4. Eine Karte mit `DueOn` vor heute steht unter „überfällig", eine Pflicht mit abgelaufener Periode nie. *Rot, wenn die Queue Pflichten überfällig nennt.*
5. Eine Position mit `Cadence = None` taucht nicht als Eintrag mit Preis auf (Preis immer 0, nie abgerechnet). *Rot, wenn freie Positionen als Pflicht erscheinen.*
6. Ein anderes Kind desselben Vaters und ein fremdes `childId` werden abgewiesen. *Rot, wenn die Route ohne `ChildOwnershipFilter` hängt.*
7. Die Queue und `today` nennen für dieselbe Pflicht dasselbe `GoalMet`. *Rot, wenn die Queue eine zweite Erfüllt-Regel einführt.*

## Verlauf

- **2026-10-17** — angelegt (Quelle: Khan-Academy-Vergleich, Etappe 3 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): **zum Teil schon da, eine Annahme falsch.** `SohnHome.tsx:61-150` zeigt die Positionen des Tages mit Takt-Pill, „offen“ und „N fällig“, die Missionen gruppieren nach „Heute“/„Diese Woche“ (`GamificationPanels.tsx:7-11`). „Überfällig“ gibt es für Pflichtziele nicht – eine abgelaufene Periode wird als Malus gebucht und ist nicht nachholbar (`PositionProgressService.cs:34-46`); sinnvoll nur für Leitner-Karten (`DueCount`), Objectives (`Overdue`) und Klassenarbeiten. „Was es kostet“ braucht `PenaltyCoins` in `PositionStatus` (heute nur im Supervisor-DTO) – additiv.
- **2026-10-23** — **Zwei Entscheidungen des Nutzers** in der Grill-Runde G-5 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): (1) Die Queue **zeigt, was Nichtstun kostet**: je Pflicht „bis heute Abend, sonst −N Münzen“. Dazu kommen die fälligen Karten, die Mischwiederholung (B-218) und der Klassenarbeits-Countdown (B-248) in einer Liste. `PenaltyCoins` kommt additiv in den Student-Vertrag. „Überfällig“ gilt nur für Karten, Objectives und Klassenarbeiten, nicht für Pflichten. (2) Die Queue ist ein **Server-Endpunkt** (API-First, eine Sortierlogik), den B-250 (Stundenplan) später mitnutzt. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-20); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an. Erst nach B-218, die Countdown-Zeile erst nach B-248.
- **2026-11-13** — Teilbefund am Code (Tageslauf, letzte Minuten): Positionen haben kein Periodenende im Vertrag.
- **2026-11-13** — Backlog-Abgleich gegen den Code: **Teil von Entscheidung 1 ist gebaut:** `PenaltyCoins` steht durch [B-525](B-525-sohn-startseite-als-schlangenpfad.md) im Student-Vertrag (`Contracts/Shared/ProgressDtos.cs:25-29`) und wird am Pfad-Knoten gezeigt (`SohnHome.tsx:191`). Offen bleiben der Queue-Endpunkt und das Periodenende je Position. Die Pfade im Teilbefund oben sind korrigiert; `SohnHome.tsx:61-150` (2026-10-23) ist durch den Schlangenpfad überholt.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt (`ProgressDtos.cs:25-29` mit `PenaltyCoins`, ohne Periodenende; `SohnHome.tsx:191,214` zeigt den Malus; kein Queue-Endpunkt unter `Controllers/Student`). Reihenfolge nach B-218 und B-248 gilt weiter.
- **2026-11-17** — [B-250](B-250-stundenplan-in-der-tagesmission.md) ist `verworfen` (ersetzt durch [B-615](B-615-tagesmission-faechermix-nach-staerke-und-stundenplan.md)): der Stundenplan erzeugt jetzt eine Pflicht statt einer Sortierung. Der in Entscheidung 2 genannte spätere Mitnutzer der Queue entfällt damit; ob die Queue trotzdem nach Stundenplan sortiert, ist in B-615 als offener Punkt 13 zurückgestellt.
- **2026-11-17** — **Entscheidung des Vaters** (B-615 Entscheidung 13): Die Queue sortiert **nicht** nach Stundenplan; die Stundenplan-Pflicht aus B-615 genügt. Der offene Punkt aus dem vorigen Eintrag ist damit erledigt.
- **2026-11-17** — B-615 ist geteilt worden; die Stundenplan-Pflicht, auf die die beiden vorigen Einträge verweisen, tragen jetzt [B-621](B-621-stundenplan-pflicht-backend.md) und [B-622](B-622-stundenplan-pflicht-vater-web-und-sohn-app.md).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): Ist-Stand am Code neu gemessen. Neu gegenüber der Notiz: der kind-weite `today`-Endpunkt (B-606) existiert (`ChildTodayController.cs:14,27`), `PenaltyCoins` ist im Vertrag; es fehlen nur ein Fälligkeitsdatum je Eintrag und die gemeinsame Sortierung. Sechs offene Punkte mit Empfehlung, keine Entscheidung getroffen. Überschneidung: B-218, B-248, B-606/B-525.
