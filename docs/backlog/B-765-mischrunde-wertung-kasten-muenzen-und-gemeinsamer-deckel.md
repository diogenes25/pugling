---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/gamification, rolle/student]
aliases: [Mischwiederholung Wertung, Mischrunde Münzen]
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: backend
migration: nein
vertragsbruch: nein
quelle: "B-218"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-765 · Mischrunde: Wertung in der Stufe der Position, Kasten der Herkunftsposition, Münzen und gemeinsamer Tagesdeckel (Backend)

Der zweite Teil von [B-218](B-218-mischwiederholung-ueber-alle-positionen.md): die Antworten werden gewertet, der Kasten der **Herkunftsposition** bewegt sich, Münzen fließen,
und der Tagesdeckel gilt gemeinsam mit dem Bonus-Training. **Baut nach B-764.**

## User Story

Als *Schüler* möchte ich für richtig wiederholte Karten Münzen bekommen und die Karten im richtigen Kasten weiterrücken sehen; als *Vater* möchte ich, dass der Tagesdeckel für alle Zusatzrunden zusammen gilt.

## Ist-Stand am Code

Pfade relativ zu `backend/Pugling.Api/`; gemessen am 2026-11-20.

- Leitner-Bewegung: `PositionPlayService.ApplyReview` (`Services/Shared/PositionPlayService.cs:657-678`), richtig → Box + 1 und `DueOn = heute + Intervall`, falsch → Box 1 oder
  verzeihend (`ForgivingDemotion`), `DueOn` heute. Standardintervalle `[0, 1, 2, 4, 7, 14]` (`:44`): nach einer **richtigen** Antwort ist die Box mindestens 2, das Intervall mindestens 2 Tage.
- Die Wertung einer Antwort im Üben steckt **inline** im Controller (`Controllers/Student/PositionPracticeController.cs`, `review`, ab `:550`; Schreibstellen `:709-761`, `:795-819`); wiederverwendbar ist
  `ApplyReview`, nicht der Controller.
- Prüfung gegen die Lösung im Bonus-Training: `preview.CheckAsync(exercise, answers, stage)` (`Services/Student/ExtraTrainingService.cs:186`).
- Tagesdeckel: `ExtraTrainingService.CoinsTodayAsync` summiert nur `ExtraTrainingRuns.CoinsAwarded` (`:299-300`); Deckel `Child.BonusDailyCoinCap`, sonst `DailyCoinCap` = 90.
- Stufe der Position am Tag: `PositionPlayService.StageForDay` (`:73`).
- Auch das zubuchende Bonus-Training bumpt den Stamp: `ExtraTrainingService.cs:220`; die Reihenfolge „Kind zuerst laden“ (`:146`) macht das Submit serialisiert.

## Die echte Lücke

Wertung über Positionsgrenzen, Kasten pro Herkunftsposition, Münzen nach den Regeln der Bonus-Form, und eine Deckel-Summe über **zwei** Quellen.

## Entscheidungen

*Entscheidungen stammen aus [B-218](B-218-mischwiederholung-ueber-alle-positionen.md): stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt. Die Nummern verweisen dorthin.*

1. **Der Kasten der Herkunftsposition bewegt sich** (B-218 Entscheidung 2), über `ApplyReview` auf der `PositionItemProgress` dieser Position; keine eigene Abstiegsregel. B-533 Entscheidung 7 („eine
   Bonus-Runde bewegt keinen Kasten“) bleibt wörtlich wahr: die Mischrunde ist keine Bonus-Runde.
2. **Gewertet wird in der Stufe der Position am Tag**, nie leichter (B-218 Entscheidung 4).
3. **Münzen nur für geprüfte Treffer** in der Rate der gespielten Form, ohne Combo, Tempo- und Neu-Bonus, ohne Halbierung (B-218 Entscheidung 6); Art `PointKind.ExtraTrainingCoins`.
4. **Gemeinsamer Deckel:** `CoinsTodayAsync` summiert Bonus-Läufe **und** Mischläufe; die Runde öffnet nicht vor den Tageszielen.
5. **Zahl-Tor, beim Schätzen aus der offenen Kosten-Frage von B-218 Entscheidung 6 gelöst:** eine Karte zahlt nur, wenn ihr `DueOn` **nach** der Wertung in der Zukunft liegt. Mit den
   Standardintervallen ist das nach jeder richtigen Antwort so; mit eigenen Intervallen, die ab Box 2 den Wert 0 tragen, verhindert das Tor die Mehrfachzahlung am selben Tag.
6. **Keine Lern-Sitzung, keine Wirkung auf Pflicht, Serie, Kiste** (B-218 Entscheidung 5); `ItemReviewEvent` und `ItemProgress` werden **mitgeschrieben** (der plan-übergreifende Lernstand soll
   die Antwort sehen, wie bei jeder Vokabelantwort). *Annahme, unbestätigt;* die Schreibstellen entscheiden B-218 Entscheidung 8 und das Schätzen: Kasten ja, Münzen nach 3, Lern-Sitzung nein.

## Akzeptanzkriterien

Nummerierung folgt B-218.

1. (AK 4) Eine richtig beantwortete Karte steigt in der Box **ihrer Position** und bekommt dort ein neues `DueOn`; die Box einer anderen Position derselben Übung bleibt unberührt. *Rot, wenn auf `ItemProgress` statt `PositionItemProgress` gebucht wird.*
2. (AK 5) Eine falsch beantwortete Karte fällt nach der `ForgivingDemotion`-Regel ihrer Position. *Rot, wenn die Runde eine eigene Abstiegsregel mitbringt.*
3. (AK 6) Münzen nur für geprüfte Treffer, ohne Combo, Tempo und Neu-Bonus, höchstens bis zum **gemeinsamen** Tagesdeckel; nicht vor der Tagespflicht. *Rot, wenn Bonus-Runde plus Mischrunde zusammen den Deckel überschreiten oder die Runde das Tor umgeht.*
4. (AK 7) Die Runde legt keine `PracticeSession` mit `Mode = Lern` an und ändert `GoalMet`, Serie und Truhe nicht; verpasst das Kind sie, entsteht kein Malus. *Rot, wenn eine Lern-Sitzung entsteht oder `SettleClosedPeriodsAsync` die Runde liest.*
5. (AK 8) Die Karte wird in der Stufe der Position am Tag gewertet, nie in einer leichteren. *Rot, wenn die Runde eine eigene Stufe wählt.*
6. (AK 11, **Absichtserklärung**) Die Bonus-Runde über eine Übung bewegt weiter keine Box. *Rot, wenn jemand die Box-Buchung in den Bonus-Pfad legt.*
7. (**neu, aus Entscheidung 5**) Eine Karte, deren `DueOn` nach der Wertung heute oder früher ist, zahlt nicht. *Rot, wenn das Tor fehlt (Test mit eigenen Intervallen `[0, 1, 0, 0]`).*
8. (**neu, Wallet-Invariante**) Ein zweites Abgeben desselben Laufs (Doppeltipp) bucht nicht doppelt, und der `ConcurrencyStamp` des Kindes wird gebumpt. *Rot, wenn der Lauf zweimal zahlt oder der Stamp gleich bleibt.*

## Schätzung

**Größe: L** – wie eine DB-Umbau-Etappe (E6) im Risiko, nicht im Umfang: Geld, ein geteilter Deckel über zwei Tabellen, ein Kasten, den zwei Quellen schreiben, und Wertung über mehrere Positionen. Nicht M, weil ein
Fehler hier Münzen oder den Lernstand des Kindes still verfälscht.

Nachgesehen am 2026-11-20:

- **`wo: backend`.**
- **`migration: nein`**, sofern B-764 die Spalten für Münzen und Abschluss schon anlegt; fehlt dort eine, wird das hier eine additive Migration (dann `ja`).
- **`vertragsbruch: nein`.** Neue Antwort-Typen und Routen.
- **Wallet-Invariante geprüft:** die Runde **bucht Münzen zu** (kein Abbuchen), bumpt den Stamp **trotzdem** wie `ExtraTrainingService.cs:220` und B-627 Entscheidung 6; das Kind wird **zuerst** geladen (Serialisierung des Abgebens, `:146`). Der Deckel ist die äußere Grenze der Münzen.

### Risiken

1. **Deckel-Summe über zwei Quellen** (`CoinsTodayAsync`): die Zahl, die das Bonus-Training in seinem Antwort-Vertrag nennt (`CoinsToday`), muss beide Quellen zeigen; sonst sieht das Kind zwei Zahlen.
2. **Zwei Schreiber desselben Kastens:** Mischrunde und Positionsrunde am selben Tag. Der Kasten ist die einzige Regel; die zweite Runde derselben Karte am selben Tag findet `DueOn` in der Zukunft und zeigt sie nicht.
3. **Inline-Wertung im Controller:** wer die Logik aus `PositionPracticeController` ziehen will, riskiert die Pflicht-Runde; Empfehlung: nur `ApplyReview` und `CheckAsync` nutzen, den Controller nicht anfassen.
4. **Prüfung je Karte in je anderer Stufe:** `CheckAsync` nimmt eine Stufe je Aufruf; die Karten sind nach (Übung, Stufe) zu gruppieren.
5. **Veränderte Übung seit dem Start:** wie bei B-545 ein Fingerabdruck am Lauf; eine geänderte Übung verwirft die Karte unbezahlt.
6. **Überschneidung mit B-660:** B-660 zuerst.

### Angriffsplan

1. `CoinsTodayAsync` über beide Quellen summieren (kleiner, eigener Schritt mit Regressionstest an `ExtraTrainingTests`).
2. `MixedReviewService.SubmitAsync`: Kind zuerst laden, Lauf prüfen, nach (Übung, Stufe) gruppiert prüfen, `ApplyReview` je Karte, Zahl-Tor, Münzen bis zum Deckel, Stamp bumpen, speichern.
3. Endpunkt `submit`; Antwort mit Münzen, Deckelstand und Ergebniszeilen.
4. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Klasse `MischrundeWertungTests` (Vorbild `ExtraTrainingTests`, `VerzeihendeAbstufungTests` für die Abstufung, `ComboTests` als Gegenprobe): AK 1 bis 8.
- `ExtraTrainingTests`: ein Fall „Bonus-Runde plus Mischrunde zusammen unter dem Deckel“ (AK 3); die vorhandenen Fälle bleiben unverändert grün.
- Rote Proben: AK 3 – `CoinsTodayAsync` summiert nur eine Quelle; AK 7 – das Zahl-Tor fehlt; AK 8 – der Bump fehlt.
- Vor der Abnahme: `pugling-reviewer`; `/smoke-test` für Start, Abgeben, Kontostand.

## Verlauf

- 2026-11-20 · aus B-218 geteilt und geschaetzt (Vorbereitungslauf, Sonnet): L, backend, keine Migration (falls B-764 die Spalten anlegt), kein Vertragsbruch. Baut nach B-764. Zahl-Tor löst die offene Kosten-Frage der Mehrfachzahlung.
