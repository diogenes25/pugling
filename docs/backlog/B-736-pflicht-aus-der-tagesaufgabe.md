---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/training, bereich/auswertung, rolle/student, rolle/supervisor]
aliases: [Pflicht aus der Tagesaufgabe, Malus je Aufgabe, scharfer Planer, Etappe E3a]
status: geschaetzt
prio: P1
art: Wunsch
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-644
grund: ""
ersetzt_durch: []
---

# B-736 · Die Pflicht kommt aus der Tagesaufgabe: Malus je Aufgabe, Serie, Rückstand (B-644, Etappe E3a)

Siebte Teilstory aus der Teilung von [B-644](B-644-tagesplaner-statt-fester-lauf.md). Bisher trägt die Position die Pflicht
(`Cadence`, `NewItemsPerDay`, `PenaltyCoins`). Hier wird die **scharfe Schaltung** gebaut: Ist der Modus
„scharf" ([B-734](B-734-planer-kern-und-schattenmodus.md), `PlannerSettings`), tragen die Tagesaufgaben Pflicht, Dosis und Malus;
Serie, Tagessicht und Malus-Abrechnung lesen sie. Im Schattenmodus bleibt alles beim Alten. Den Schalter selbst legt
[B-737](B-737-reset-zum-stichtag.md) zusammen mit dem Reset um.

**Stand:** Eintrittsbedingung `gegrillt` aus B-644 und B-680 geerbt. Entscheidungen 5 und 7 stammen vom Vater, 20 und 24 sind
stellvertretend und unbestätigt.

## User Story

Als *Vater* möchte ich, dass eine verpasste Pflichtaufgabe genau einmal den Malus-Betrag meines Kindes kostet und eine erledigte
nichts, damit der Stick wirkt. Als *Sohn* möchte ich nur für das Münzen verlieren, was ich an dem Tag wirklich sehen konnte.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`. Pfade relativ zu `backend/Pugling.Api/`.

- **Die Dosis hat eine Stelle:** `PositionPlayService.DailyDose(pos, types)` (`Services/Shared/PositionPlayService.cs:27`-`:41`) liest
  `pos.NewItemsPerDay`; die Vater-Maske zeigt dieselbe Funktion (`PlanPositionsController.cs:45`).
- **Der Malus hängt an der Position:** `SettleClosedPeriodsAsync` (`PositionProgressService.cs:754`) bucht `pos.PenaltyCoins` (`:807`,
  `:814`) je verpasster Periode, abgesichert durch `PositionGoalPenalty` (`Models/PlanPositionEntities.cs:223`), und bumpt
  `child.ConcurrencyStamp` (`:827`, Wallet-Invariante).
- **Pflichtzustand:** `DutyStateAsync` (HasDuty/Done/Active) speist Serie (`ChildStreakAsync` `:479`), Tagessicht (`ComputeChildDayAsync`
  `:407`), Zeitachse und Dashboard (`ChildrenDashboardService.cs:75`).
- **Belohnung und Truhe** hängen an „erst die Pflicht": `DailyBoxService` (`ConcurrencyStamp` `DailyBoxService.cs:142`), Tagesbox,
  Missionen, Bonus-Training.
- Eine Tagesaufgabe als Träger der Pflicht, eine Abrechnung je Aufgabe und einen Rückstand gibt es nicht.
- **Ohne Planer-Modus** kennt kein Pfad „scharf" (`PlannerSettings` entsteht in B-734).

## Entscheidungen (geerbt)

1. **Malus je verpasster Pflichtaufgabe, mit einem Betrag je Kind; verpasster Stoff kommt am nächsten Lerntag wieder, aber nur innerhalb
   des Deckels; die Serie reißt, sobald an einem Lerntag eine Pflichtaufgabe offen bleibt** (B-644 Entscheidung 5, Vater). Der
   Malus an der Position (`PlanPosition.PenaltyCoins`) verliert für Planer-Kinder seine Bedeutung.
2. **Eine bis Mitternacht nicht erledigte Pflichtaufgabe eines gesehenen Plans kostet genau einmal den Malus-Betrag; ein Tag ohne
   erzeugten Plan zählt wie eine Pause** (Entscheidungen 19 und 24, stellvertretend, unbestätigt). Schließt aus: Strafe für Aufgaben,
   die das Kind nie angezeigt bekam.
3. **Begonnene Aufgaben sind nicht streichbar** (Entscheidung 20): Die Abrechnung kann sich darauf verlassen, dass nur nicht begonnene
   Aufgaben verschwinden.
4. **Münzen hängen an der erfüllten Pflicht, nicht an der Notenstufe** (Entscheidung 6 in B-680; Rahmen F).
5. **Das alte Modell (feste Pflicht an der Position) fällt mit der scharfen Etappe weg** (Entscheidung 11, Vater): kein Parallelbetrieb
   je Kind.

### Schätz-Annahmen (stellvertretend, unbestätigt)

1. **Die Spalten `Cadence`, `NewItemsPerDay`, `PenaltyCoins` bleiben im Schema und werden im scharfen Modus ignoriert;** ein Abbau der
   Spalten ist eine spätere Aufräum-Story. Begründung: Die Migrationskette wird nur verlängert, ein Spaltenabbau mit
   Bestandsdaten ist eigener Aufwand ohne Nutzen für den Planer. Verworfen: die Spalten jetzt löschen.
2. **Erledigt-Zeit (`DoneAt`) wird an der Aufgabe festgehalten** und von demselben Pfad gesetzt, der heute das Tagesziel bucht
   (`IsGoalMetAsync`); die Abrechnung liest `DoneAt`, nicht das Ziel neu. Begründung: Wie die Belohnungssperre
   (`PositionGoalReward`) soll eine einmal erreichte Aufgabe nicht durch spätere Änderungen zurückfallen.
3. **Die Abrechnung läuft träge** wie `SettleClosedPeriodsAsync` (beim Kind-Login und in `MeController`, höchstens 14 Tage zurück) und in
   der Planer-Zeitschaltung aus B-734; ein Codepfad.
4. **Der Rückstand wird als Planer-Regel umgesetzt:** Nicht erledigte Pflichtwörter von gestern gehen in den Tagesplan von heute, geklemmt
   auf den Deckel; sie zählen gegen den Aufgaben-Deckel, nicht gegen den Wörter-Deckel, wenn es Wiederholungen sind.

## Akzeptanzkriterien

1. Im scharfen Modus entsteht Pflicht, Malus und Serie nur aus Tagesaufgaben; `Cadence` und `PenaltyCoins` der Positionen erzeugen
   weder Pflicht noch Malus. *Rot, wenn* eine Position mit Taktung `Daily` im scharfen Modus Pflicht oder Malus erzeugt.
2. Im Schattenmodus bleibt alles wie vorher (alte Pflicht, alter Malus, alte Serie). *Rot, wenn* ein Schattenplan Pflicht erzeugt oder die
   alte Abrechnung entfällt.
3. Eine bis Mitternacht offene Pflichtaufgabe eines gesehenen Plans bucht genau einmal `Child.MalusCoins` (Kassenbuch-Zeile mit Text der
   Aufgabe); ein zweiter Abrechnungslauf bucht nichts, eine erledigte Aufgabe kostet nichts. *Rot, wenn* doppelt gebucht wird (eindeutiger
   Index je Aufgabe) oder eine erledigte Aufgabe kostet.
4. Die Malus-Buchung bumpt `child.ConcurrencyStamp`: ein gleichzeitiger Shop-Kauf und eine Abrechnung ergeben keinen negativen Saldo.
   *Rot, wenn* der Stamp nicht gebumpt wird.
5. Ein Tag ohne erzeugten Plan, ein Sonntag und ein Pausentag kosten keinen Malus und lassen die Serie stehen. *Rot, wenn* dort
   gebucht wird oder die Serie bricht.
6. Eine vom Vater gestrichene Aufgabe kostet nichts. *Rot, wenn* eine gestrichene Aufgabe abgerechnet wird.
7. Eine nicht erledigte Pflicht erscheint am nächsten Lerntag wieder; der Tagesplan überschreitet dabei den Deckel nie. *Rot, wenn* der
   Rückstand den Deckel überschreitet oder nicht wiederkommt.
8. Die Serie zählt einen Lerntag als vollständig, wenn alle Pflichtaufgaben erledigt sind, und bricht an der ersten offenen; Sonntag
   und Pause werden übersprungen. *Rot, wenn* `ChildStreakAsync` im scharfen Modus noch aus Positionen rechnet.
9. Die Dosis des Tages (`DailyDose`) kommt im scharfen Modus aus der Aufgabe, nicht aus `NewItemsPerDay`; das Kind bekommt genau so viele
   neue Wörter, wie die Aufgabe nennt. *Rot, wenn* die Vater-Maske und das Spielen verschiedene Dosen kennen.
10. Tagesbox, Truhe und Missionen öffnen erst nach erfüllter Pflicht der Tagessicht. *Rot, wenn* die Box bei offener Pflichtaufgabe
    öffnet.
11. Die Migration lässt Bestandsdaten unverändert (neue Spalte und Tabelle leer). *Rot, wenn* die Migration fehlt (`SchemaGuardTests`, G1b).

## Schätzung

**Größe: L** — Anker: eine DB-Umbau-Etappe wie E6, am oberen Rand: eine Quellenumschaltung unter Serie, Tagessicht, Zeitachse,
Dashboard und Malus (alles, was `DutyStateAsync` liest), plus eine Abrechnung mit Münzen echter Kinder. Es ist die riskanteste
Teilstory von B-644. Reißt sie die L-Marke, ist die Abrechnung von der Quellenumschaltung abzuspalten (neue Id nach Nachzählen,
`quelle: B-736`).

- **`migration: ja`** — `DailyTask.DoneAt` (nullable) und Tabelle `DailyTaskPenalty` (Aufgabe eindeutig, Betrag, Buchungszeit), dazu
  die Verlängerung der Kette; die Bestandsdaten (leer, B-734 legt die Tabellen leer an) bleiben.
- **`vertragsbruch: nein`** — `TodayResponse` und `PositionStatus` behalten ihre Form; im scharfen Modus füllen die Aufgaben dieselben
  Felder. Die Begründung an der Aufgabe kommt additiv mit [B-738](B-738-eingriffe-bonus-und-sohn-begruendung.md).
- **`wo: backend`** — die Sohn-App und das Vater-Web lesen dieselben Felder; eine Oberfläche entsteht hier nicht.

### Risiken

- **Quellenumschaltung unter fünf Lesern:** Ein Leser, der noch auf Positionen rechnet, zeigt dem Kind ein anderes Bild als die
  Abrechnung. Vorab die Leserliste aus `DutyStateAsync` zusammenstellen und je Leser einen Testfall.
- **Münzen echter Kinder:** Ein Fehler trifft ab dem Stichtag direkt das Guthaben (B-644 Entscheidung 11, Kosten). Darum zuerst im
  Schattenmodus an Kopien prüfen, nie an `pugling.db`.
- **Wallet-Invariante:** Jeder abbuchende Pfad muss den Stamp bumpen (`PositionProgressService.cs:827`); ohne das gibt es
  Doppelspend bei parallelen Käufen.
- **Gleichzeitige Abrechnung:** Login und Zeitgeber laufen parallel; der eindeutige Index je Aufgabe ist die Sicherung, der
  Verlierer darf nicht scheitern (Klasse B-97/B-104).
- **Dosis als Vertragsfläche:** `DailyDose` wird auch von der Vater-Maske benutzt; im scharfen Modus zeigt sie die Aufgabe.

### Angriffsplan

1. Migration (`DoneAt`, `DailyTaskPenalty`), Modell, Migrationstest.
2. Lesequelle umschalten: ein Dienst „Pflichtzustand des Tages" mit zwei Quellen (Position, Aufgabe), Modus aus `PlannerSettings`;
   `DutyStateAsync`-Leser darauf umstellen.
3. `DailyDose` aus der Aufgabe im scharfen Modus.
4. Abrechnung je Aufgabe mit Wallet-Invariante, Index-Verlierer liest den Gewinner.
5. Rückstand als Planer-Regel; Serie auf Aufgaben.

### Testweg

- Neue Klasse `TagespflichtTests` (Integration, `TestClock`, Modus aus `PlannerSettings`): Pflicht, Malus einmal, Serie,
  Rückstand, gestrichene Aufgabe, Sonntag, Pause, Tag ohne Plan. Rote Probe je AK: den eindeutigen Index entfernen lässt AK 3 fallen,
  den Modus-Schalter fest auf „Position" lassen AK 1, den Stamp-Bump entfernen AK 4.
- `TagespflichtWalletTests` nach dem Muster der bestehenden Wallet-Tests: Kauf und Abrechnung parallel (die vorhandene Klasse für den
  Wallet-Wettlauf ist beim Bau über `ConcurrencyStamp` in `Pugling.Api.Tests` zu finden).
- Regressionsnetz: `PflichtMalusTests`, `RueckwirkendePflichtTests`, `BelohnungenTagespflichtTests`, `BelohnungUeberPlaeneTests`,
  `KuerMitMuenzenTests`, `DailyBoxTests`, `VierWochenTests`, `KindTagessichtTests` jeweils im Schattenmodus unverändert grün.
- E2E: `frontend/e2e/nacharbeit-bis-erledigt.spec.ts` und `frontend/e2e/belohnung-ueber-faecher.spec.ts` als Regression; neu
  `frontend/e2e/tagespflicht-scharf.spec.ts` (scharfer Modus: Aufgabe offen, Mitternacht, Malus, nächster Tag Rückstand).
- Vor der Abnahme: `pugling-reviewer`, `frontend-reviewer`, Rollengang als Vater und Sohn **und** Messung an einer Kopie der echten DB.

## Reihenfolge und Abhängigkeiten

Nach [B-734](B-734-planer-kern-und-schattenmodus.md), [B-735](B-735-stoffrahmen-ohne-termin.md) und
[B-680](B-680-taeglicher-lehrplan-je-fach-just-in-time.md); vor [B-737](B-737-reset-zum-stichtag.md) (das den Modus kippt) und
[B-738](B-738-eingriffe-bonus-und-sohn-begruendung.md). Ohne [B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md) (Malus-Betrag,
Lerntage) nicht baubar.

## Verlauf

- **2026-11-20** — angelegt durch Teilung von B-644 (Etappe E3a) und `gegrillt` → `geschaetzt` im Vorbereitungslauf (Sonnet):
  Eintrittsbedingung `gegrillt` aus B-644 und B-680 geerbt. Nachgesehen: Die Dosis hat eine Stelle (`DailyDose`), der Malus eine
  (`SettleClosedPeriodsAsync`), der Pflichtzustand fünf Leser. Spalten der Position bleiben (Schätz-Annahme). L am oberen Rand, backend,
  Migration, kein Vertragsbruch.
