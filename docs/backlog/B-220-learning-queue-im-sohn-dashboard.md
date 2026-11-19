---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/training, rolle/student]
aliases: [Learning Queue, Lern-Warteschlange, Heute-Woche-Überfällig]
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/khan-academy-vergleich-plan.md#wo-khan-weiter-ist
grund: ""
ersetzt_durch: []
wartet_auf: ""
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

**Beim Grillen am Code ergänzt (2026-11-19):**

- **Es gibt eine dritte Taktung ohne Periodenende.** `GoalCadence.UntilDone` („täglich, bis erledigt“) endet nicht mit einem
  Datum, sondern mit der Erledigung (`PositionProgressService.HasDutyOn`, `Services/Shared/PositionProgressService.cs:52-55`,
  B-636); `PeriodRange` kennt nur `Weekly` und den Rest als Tag (`:34-39`). Die Queue braucht für `UntilDone` also ein
  `DueBy = null`.
- **`HasDutyOn` ist die eine „ist das eine Pflicht“-Regel** und öffentlich (`:52`); die Queue darf kein eigenes `Cadence != None`
  daneben stellen.
- **Der Endpunkt hängt am Eigentumsfilter des Kindes:** `ChildTodayController` trägt `[Authorize]` und
  `[ServiceFilter(typeof(ChildOwnershipFilter))]` (`Controllers/Student/ChildTodayController.cs:16-17`); die Queue nimmt dasselbe.

## Die echte Lücke

Nicht „es fehlt eine Warteschlange", sondern: Der Server liefert die Zutaten schon einzeln (`today`, `PenaltyCoins`, `DaysLeft`,
`DueCount`, `Overdue` der Objectives), aber **kein Fälligkeitsdatum je Eintrag** und **keine gemeinsame Sortierung**. Die Queue ist im Kern ein
neues Lese-Modell mit einem Feld `bis`, das es heute nirgends gibt. Weil B-218 und B-248 noch nicht gebaut sind, hätte eine erste Fassung nur
drei Eintragsarten (Pflicht, fällige Karten, Objective/Mission) – und die Karten-Gruppe überschneidet sich mit der Mischwiederholung.

## Offene Punkte

Bereits entschieden (Nutzer 2026-10-23 und Vater 2026-11-17, siehe Verlauf): zeigt, was Nichtstun kostet; Server-Endpunkt;
„überfällig“ nur für Karten, Objectives, Klassenarbeiten; keine Sortierung nach Stundenplan.

Die Queue **zeigt** nur: Sie liest `PenaltyCoins` und `DueCount`, sie ändert weder Malus-Regeln noch Münzwerte noch Pflichten.

1. ~~**Neues Feld in `today` oder eigene Route?**~~ → Entscheidung 1.
2. ~~**Welches Datum und welche Beschriftung?**~~ → Entscheidung 2.
3. ~~**Eintragsarten der ersten Fassung?**~~ → Entscheidung 3; die **Baureihenfolge** gegenüber B-218 ist zurückgestellt → Z1.
4. ~~**Ersetzt die Queue die Tagessicht (B-606) oder ergänzt sie?**~~ → Entscheidung 4.
5. ~~**Sortierung innerhalb einer Gruppe.**~~ → Entscheidung 5.
6. ~~**Soll das Kind die Queue als Pflicht lesen, auch wenn ein Eintrag freiwillig ist?**~~ → Entscheidung 6.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Eigene Route `GET student/children/{childId}/queue`, gespeist aus demselben Dienst wie `today`
   (`PositionProgressService.ComputeChildDayAsync`, `:407`); `today` bleibt unverändert.** Die Route trägt dieselben Attribute wie
   `today` (`[Authorize]`, `ChildOwnershipFilter`). Begründung: ein zweites Lese-Modell mit anderer Form statt eines Feldes, das jede
   Startseite mitträgt; additiv, kein Vertragsbruch. Verworfen: ein Feld in `TodayResponse`. Kosten: zwei Routen, die dasselbe
   `GoalMet` nennen müssen (Akzeptanzkriterium 8 hält das fest). Ausgeschlossener Fall: ein Client, der Tagessicht und Queue in einem
   Aufruf will; gegen die User Story („auf einen Blick“) genügt die Startseite mit zwei Aufrufen.
2. **Je Eintrag ein `DueBy` als `DateOnly`: Tagespflicht = heute, Wochenpflicht = der Sonntag der Woche, `UntilDone` = `null`
   („bis erledigt“).** Beschriftung „heute“ / „bis Sonntag“ / „bis erledigt“, ohne Uhrzeit. Die Perioden-Logik wird als öffentlicher
   Helfer `PeriodEnd(cadence, day)` neben `PeriodRange` (`PositionProgressService.cs:34-39`) herausgezogen und nicht im Client
   nachgebaut. Gruppen: „heute“ (`DueBy` = heute und `UntilDone`), „diese Woche“ (`DueBy` später in der Woche), „überfällig“
   (Karten und Objectives, nie Pflichten). Verworfen: eine Uhrzeit („bis 22 Uhr“). Kosten: Die Perioden rechnen auf dem UTC-Tag
   (`ChildTodayController.cs:31`); für ein Kind in Deutschland kippt „heute“ nachts um 1 bzw. 2 Uhr. Ausgeschlossener Fall: eine
   Pflicht „bis heute Abend, sonst −N Münzen“ mit echter lokaler Uhrzeit; gegen die User Story („was es jetzt kostet“) trägt der Tag,
   die Stunde nicht.
3. **Eintragsarten der ersten Fassung: Pflichten (mit `PenaltyCoins`), Karten (je Position, mit Anzahl), offene Missionen (mit
   `DaysLeft`), überfällige Objectives; `Kind` ist eine offene Zeichenkette im Vertrag.** Karten stehen unter „überfällig“, sobald
   eine Karte der Position vor heute fällig war, sonst unter „heute“. Mischwiederholung (B-218) und Klassenarbeit (B-248) kommen
   später als weitere `Kind`-Werte. Verworfen: ein geschlossenes Enum (bräche bei jeder neuen Art). Kosten: Clients müssen ein unbekanntes
   `Kind` still überspringen. Ausgeschlossener Fall: ein Eintrag je Karte (zu lang); gegen die User Story („Reihenfolge nach dem
   Preis“) zählt je Position der Preis, nicht je Karte.
4. **Die Queue ergänzt die Tagessicht (B-606) als Block auf der Startseite; die Fach-Gliederung bleibt.** Verworfen: die Tagessicht
   ersetzen (verlöre die Fach-Gruppen, `sohn/tagessicht.ts`). Kosten: zwei Sichten auf dieselben Pflichten. Ausgeschlossener
   Fall: eine einzige Liste; gegen die User Story trägt die Queue die Fälligkeit, die Tagessicht das Fach.
5. **Sortierung in der Gruppe: nach `PenaltyCoins` absteigend, dann Fachname; Einträge ohne Preis ans Ende.** Kosten: eine Kür mit
   hohem Nutzen steht unten. Ausgeschlossener Fall: Sortierung nach Nutzen; gegen die Story („nach dem Preis des Nichtstuns wählen“)
   ist der Preis das gewollte Kriterium.
6. **Jeder Eintrag trägt `Duty` (bool): `true`, wenn `PositionProgressService.HasDutyOn` für die Position zutrifft, sonst `false`;
   freie Positionen (`Cadence = None`) erscheinen nur als Karten-Eintrag mit `Duty = false` und ohne Preis.** Damit sieht
   „Kür“ nicht aus wie „kostet etwas“. Verworfen: eine zweite Pflicht-Regel in der Queue (Drift gegen `today`). Kosten: ein Feld.
   Ausgeschlossener Fall: ein Kind, das eine freie Position wegen ihrer Fälligkeit für Pflicht hält.

**Zurückgestellt – nicht entschieden:**

- **Z1 · Darf die erste Fassung vor B-218 gebaut werden?** Der Nutzer legte am 2026-10-23 „erst nach B-218, die Countdown-Zeile erst
  nach B-248“ fest (Verlauf); die Entscheidungen oben machen den Vertrag für spätere Arten offen. Ob diese Reihenfolge nun
  aufgeweicht wird, ist seine Entscheidung. *Empfehlung:* ja, ohne B-218 bauen, weil `Kind` offen ist und B-218 ohne Termin ist; sonst
  wartet die Queue auf einen Stoff, der nicht kommt.

## Akzeptanzkriterien

Je Kriterium: welche Änderung macht es rot.

1. Eine Tagespflicht mit `PenaltyCoins` 20 steht unter „heute“ mit dem Preis 20, `DueBy` = heute (UTC) und `Duty = true`. Rot, wenn der
   Eintrag den Preis nicht trägt.
2. Eine Wochenpflicht steht unter „diese Woche“ mit `DueBy` = dem Sonntag der Woche; am Sonntag selbst unter „heute“. Rot, wenn die
   Perioden-Funktion `Weekly` wie `Daily` behandelt.
3. Eine erfüllte Pflicht steht nicht in der Queue. Rot, wenn die Queue `GoalMet` ignoriert.
4. Eine Karte der Position mit `DueOn` vor heute macht deren Eintrag zu „überfällig“; eine gestern **nicht erfüllte** Tagespflicht
   steht gar nicht in der Queue (die Periode ist gebucht). Rot, wenn die Queue eine Pflicht „überfällig“ nennt.
5. Eine Position mit `Cadence = None` erscheint nur als Karten-Eintrag mit `Duty = false` und ohne Preis. Rot, wenn sie als Pflicht
   mit Preis erscheint.
6. Eine `UntilDone`-Pflicht steht mit `DueBy = null` unter „heute“, nach ihrer Erledigung nicht mehr. Rot, wenn `UntilDone` ein Datum
   bekommt oder nach der Erledigung bleibt (Regel: `HasDutyOn`).
7. Ein Kind eines anderen Vaters liefert denselben Fehler wie `today`. Rot, wenn der `ChildOwnershipFilter` an der Route fehlt.
8. Die Queue und `today` nennen für dieselbe Pflicht dasselbe `GoalMet`. Rot, wenn die Queue eine zweite Erfüllt-Regel einführt.
9. In einer Gruppe steht der Eintrag mit dem höchsten `PenaltyCoins` zuerst, Einträge ohne Preis zuletzt. Rot, wenn die Sortierung fehlt
   oder nach Fach vor Preis ordnet.
10. Die Startseite zeigt den Queue-Block mit den drei Gruppen und überspringt ein unbekanntes `Kind` still (Komponententest in
    `sohn/`). Rot, wenn der Block fehlt oder bei unbekanntem `Kind` bricht.

## Schätzung

*Geschätzt im Vorbereitungslauf 2026-11-19 (Sonnet); die Entscheidungen oben sind weiter unbestätigt, `wartet_auf` bleibt.*

**Größe: L** – ein neues Lese-Modell, das vier Quellen zusammenführt (Pflichten aus `ComputeChildDayAsync`, fällige Karten,
Missionen, Objectives), dazu ein herausgezogener öffentlicher Perioden-Helfer, ein neuer Karten-Zustand („überfällig“) und ein
Block auf der Startseite. Größer als M, weil kein Baustein fertig vorliegt: weder ein Periodenende je Position noch eine
Unterscheidung „überfällig“ gegenüber „fällig“ bei Karten. Nicht XL: ein Endpunkt, ein Dienst, ein Block.

- **`wo: beides`** – Backend zuerst (Endpunkt, Dienst, Helfer), danach der Queue-Block in `frontend/src/sohn/SohnHome.tsx`.
- **`migration: nein`** – nachgesehen: Die Queue liest `PlanPosition`, `PositionItemProgress.DueOn`, Missionen und Objectives;
  sie schreibt nichts und führt keine Spalte ein.
- **`vertragsbruch: nein`** – nachgesehen: neue Records (`QueueResponse`, `QueueEntry`) neben `TodayResponse`
  (`Pugling.Contracts/Student/ProgressDtos.cs:32`); `TodayResponse` und `PositionStatus` bleiben unverändert (Entscheidung 1).
  `Kind` ist eine offene Zeichenkette, spätere Arten sind kein Bruch.

### Reihenfolge und Abhängigkeiten

1. **B-606 ist `abgenommen`** (`ChildTodayController`, `ComputeChildDayAsync`): das Fundament steht; die Queue baut darauf auf.
2. **B-218** (Mischwiederholung, `ausformuliert`) ist **nicht** gebaut. Ob die Queue vor B-218 gebaut werden darf, ist **Z1** und
   bleibt dem Nutzer vorbehalten (seine Festlegung vom 2026-10-23: erst nach B-218). Die Schätzung gilt für die erste Fassung ohne
   B-218; kommt B-218 vorher, ändert sich nur ein weiterer `Kind`-Wert, nicht der Aufwand.
3. **B-248** (Klassenarbeits-Countdown) kommt als weitere Art danach und ist nicht Teil dieser Story.

### Risiken

1. **Pflicht und Karten derselben Position erscheinen doppelt.** Eine Tagespflicht und die fälligen Karten derselben Position sind
   zwei Einträge (Entscheidung 3); ein Kind kann das als zwei Aufgaben lesen. Der Bau entscheidet die Darstellung (eine Zeile mit
   zwei Zahlen oder zwei getrennte Gruppen); AK 1 und 4 bleiben gültig. Mit dem Nutzer bei der Bestätigung klären.
2. **„Überfällig“ bei Karten gibt es heute nicht:** `PositionStatus.DueCount` zählt `DueOn <= heute`
   (`PositionPlayService.cs:630`) und zählt nie gelernte Karten mit (`DueOn == null`). Überfällig braucht eine neue Abfrage auf
   `DueOn < heute` – ohne die nie gelernten Karten, sonst steht jede frische Position „überfällig“ (eigener Testfall).
3. **UTC-Tag:** „heute“ kippt für ein Kind in Deutschland nachts (Entscheidung 2 nimmt das hin; `ChildTodayController.cs:31`).
   Der Test setzt die Uhr über `TimeProvider`, nicht über die Systemzeit.
4. **Zwei Wahrheiten für „erfüllt“:** `today` und `queue` müssen dasselbe `GoalMet` nennen (AK 8). Wer die Queue aus einem zweiten
   Weg speist, bricht das still; darum denselben Dienstaufruf nehmen und `HasDutyOn` (`PositionProgressService.cs:52`) wiederverwenden.
5. **Der Malus je Eintrag ist der der Position** (`PenaltyCoins`, `cadence == None ⇒ 0`, `PositionProgressService.cs:340`). Die Queue
   rechnet nichts um und schreibt nichts; Malus-Regeln und Münzwerte bleiben unberührt.
6. **Missionen und Objectives haben je eine eigene Quelle** (`MissionStatus.DaysLeft`, `GoalStatus.Overdue` in
   `ObjectiveEvaluationService.cs:55`); die Queue ruft sie, kopiert aber keine Regel.

### Angriffsplan

1. `PeriodEnd(cadence, day)` öffentlich neben `PeriodRange` (`PositionProgressService.cs:34`); `UntilDone` liefert `null`.
2. Karten-Abfrage „überfällig“ (`DueOn < heute`, nie gelernte ausgenommen) im Wiedergabedienst neben `DueItemIndicesAsync`.
3. `Pugling.Contracts/Student`: `QueueResponse`/`QueueEntry` (`Kind`, `DueBy`, `Duty`, `PenaltyCoins`, `Count`, Beschriftung);
   `ChildQueueController` mit denselben Attributen wie `ChildTodayController` (`[Authorize]`, `ChildOwnershipFilter`).
4. Dienst, der aus `ComputeChildDayAsync`, Karten, Missionen und Objectives die Einträge baut, gruppiert und sortiert
   (Preis absteigend, dann Fach).
5. Frontend: Queue-Block unter der Tagessicht in `SohnHome.tsx`, unbekanntes `Kind` still überspringen (AK 10).
6. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`, Reviewer `pugling-reviewer` und `frontend-reviewer`.

### Testweg

- Neue Klasse `KindQueueTests` (Vorbild `KindTagessichtTests`, Aufbau mit `TestApi.SeedLeitnerPosition`, Uhr per `TimeProvider`):
  AK 1 bis 9. Rot-Proben: `Weekly` wie `Daily` behandeln (AK 2), `GoalMet` weglassen (AK 3), `UntilDone` ein Datum geben (AK 6),
  Filter von der Route nehmen (AK 7).
- `KindTagessichtTests`: AK 8 als Vergleich `today` gegen `queue` am selben Aufbau.
- `EndpointCoverageGuard` verlangt für die neue Action eine Abdeckung; der Zähler (derzeit 295 Actions) wird mit der Route
  nachgezogen.
- `frontend/src/sohn/SohnHome.test.tsx`: AK 10; vor der Abnahme der Rollengang als Sohn an der laufenden App.

**Bau-Sperre:** Z1 (Baureihenfolge gegenüber B-218) entscheidet allein der Nutzer; bis zur Bestätigung der Entscheidungen 1 bis 6 ist
die Story nicht baubar. Malus-Regeln, Münzwerte und Pflichten bleiben außerhalb der Story.

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
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt, `wartet_auf` gesetzt): Entscheidungen 1 bis 6.
  Neu am Code: `UntilDone` hat kein Periodenende (`DueBy = null`), `HasDutyOn` ist die eine Pflicht-Regel. **Eine Frage zurückgestellt (Z1):**
  die Reihenfolge „erst nach B-218“ des Nutzers vom 2026-10-23. Die Queue zeigt nur und ändert keine Malus-Regel. Akzeptanzkriterien final.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet; `wartet_auf` bleibt): L, beides (Backend zuerst), keine Migration, kein Vertragsbruch
  (neue Records neben `TodayResponse`). Fundament B-606 abgenommen; B-218 nicht gebaut, Z1 bleibt zurückgestellt und ist Bau-Sperre.
  Neu am Code: „überfällig“ bei Karten braucht eine neue Abfrage (`DueCount` zählt `<= heute` und nie gelernte Karten).
- **2026-11-19** — Entscheidungen 1 bis 6 bestätigt; Z1 mit Empfehlung entschieden: die erste Fassung darf vor B-218 gebaut werden. Stellvertretend auf Anweisung des Nutzers („nimm für alle Fragen deine Empfehlung“): die Empfehlung der Sonnet-Entscheidungen wurde übernommen.
