---
tags: [typ/story, status/abgenommen, bereich/training, bereich/gamification, rolle/student]
aliases: [Mischwiederholung Wertung, Mischrunde Münzen]
status: abgenommen
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
wartet_auf: ""
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
- 2026-11-24 · in-arbeit (Nachtlauf H, Sprint 130, Freigabe 8). Gebaut: `MixedReviewService.SubmitAsync` + Endpunkt `POST student/children/{childId}/mixed-review/runs/{runId}/submit` (nur Student), Contract `MixedReviewAnswer`/`SubmitMixedReviewDto`/`MixedReviewResult`/`MixedReviewOutcome`, Client `SubmitMixedReviewAsync`. `ExtraTrainingService.CoinsTodayAsync` summiert jetzt Bonus-Läufe **und** Mischläufe (Angriffsplan Schritt 1, die Senke des Deckels); jede Lesestelle (Bonus-Übersicht, -Start, -Abgabe, `CoinsLeftTodayAsync`, Mischrunde) liest damit den gemeinsamen Stand. `migration: nein` hält: alle Spalten kamen mit B-764. Kind zuerst geladen, Stamp gebumpt, ein `SaveChanges` für Kasten, Item-Historie (`ItemReviewSource.Practice` mit `PlanPositionId`, kein neuer Enum-Wert) und Ledger (`PointKind.ExtraTrainingCoins`, Grund „Mischrunde").
- 2026-11-24 · Auslegung innerhalb der Entscheidungen, zur Prüfung am Morgen benannt: (a) eine selbst eingeschätzte Karte (Stufe der Position ist Selbsteinschätzung) bewegt den Kasten wie die Selbstauskunft im Üben – hoch bei „gewusst", ohne als gewertete Wiederholung zu zählen – und zahlt **nichts** (Entscheidung 3 „nur geprüfte Treffer"; das Bonus-Training zahlt dort 1, B-657); (b) übersprungen und unbezahlt bleibt eine Karte, die bei der Abgabe nicht mehr fällig ist, deren Position inzwischen eine offene Pflicht trägt, kein Leitner mehr nutzt oder eine andere Übung spielt, oder deren Übung sich geändert hat (Risiko 5); (c) eine Runde von einem früheren Tag wird bei der Abgabe verworfen (`mixed_review_stale`) statt umgebucht; (d) eine Karte ohne Antwort zählt als falsch (Vertragstext, Muster des Bonus-Trainings) – der Reviewer schlug als 🟢 „ohne Antwort = übersprungen" vor; nicht übernommen, weil es eine Produktfrage ist, ob ein abgebrochenes Abgeben den Kasten schont: **offener Punkt fürs Grillen**.
- 2026-11-24 · AK-Fälle → Test (`backend/Pugling.Api.Tests/MischrundeWertungTests.cs`) → Mutante: AK 1 → `Richtige_Karte_Steigt_Im_Kasten_Ihrer_Position` (N2 `ApplyReview` weg → rot) · AK 2 → `Falsche_Karte_Faellt_Nach_Der_Regel_Ihrer_Position` (verzeihend 3→2, streng 3→1; N2 → rot) · AK 3 → `Muenzen_Nur_Fuer_Treffer_Bis_Zum_Gemeinsamen_Deckel` (N1 Deckel nur eine Quelle → rot, N12 ohne Deckel → rot) und `Abgabe_Bei_Offener_Tagespflicht_Gesperrt` (N6 → rot) · AK 4 → `Runde_Beruehrt_Keine_Pflicht` (**Absichtserklärung**: keine Schreibstelle im Diff, keine Mutante) · AK 5 → `Karte_In_Der_Stufe_Der_Position` (N13 feste Stufe beim Einfrieren → rot) · AK 6 → `Bonusrunde_Bewegt_Weiter_Keinen_Kasten` (**Absichtserklärung**, wie die Story sagt) · AK 7 → `Karte_Die_Heute_Wieder_Faellig_Ist_Zahlt_Nicht` (N5 Zahl-Tor weg → rot) · AK 8 → `Doppelte_Abgabe_Bucht_Einmal_Und_Bumpt_Den_Stempel` (N7 Bump weg → rot, N8 Abgeschlossen-Prüfung weg → rot) · Auslegung (a) → `Selbsteinschaetzung_Bewegt_Den_Kasten_Und_Zahlt_Nichts` (N3, N4, N15 → rot) · (b) → `Inzwischen_Geuebte_Karte_Wird_Uebersprungen` (N9), `Karte_Einer_Inzwischen_Pflichtigen_Position_Wird_Uebersprungen` (N14), `Position_Ohne_Leitner_Bei_Der_Abgabe_Wird_Uebersprungen` (N17), `Geaenderte_Uebung_Wird_Uebersprungen` (N10) · (c) → `Runde_Von_Gestern_Wird_Verworfen` (N11) · Review-Fund → `Zweite_Runde_Desselben_Tages_Zahlt_Die_Gezeigte_Antwort_Nicht` (N16).
- 2026-11-24 · Review `pugling-reviewer` (Sprint 130, B-764 + B-765 gemeinsam): keine 🔴. 🟡 1 **behoben**: eine in Runde 1 verfehlte Karte zeigt im Ergebnis ihre Lösung, ist sofort wieder fällig und zahlte in Runde 2 desselben Tages den vollen Satz – ein ungewusstes Wort wurde durch Abschreiben zu Münzen. Fix: eine Karte zahlt nur bei ihrer ersten Wiederholung des Tages (`LastReviewedAt` vor der Wertung, Lerntag über `LearningDay.DayOf`); der Kasten bewegt sich weiter. Das ist die Absicht von Entscheidung 5 („Mehrfachzahlung am selben Tag verhindern"), nicht eine neue Produktentscheidung – zur Bestätigung am Morgen benannt. Rote Probe: der neue Test erwartet `coinsAwarded` 0 und fällt mit Mutante N16 (Fix entfernt) am benannten Test; der Betrag, den er dann sieht, ist nicht ausgelesen (nach der Regel der Tippsatz 3). **Fehlerzähler Sprint 130: 2.** 🟢 ohne Zählung: Leitner bei der Abgabe neu geprüft (N17), Kommentar an der Überspring-Prüfung richtiggestellt (eine im Üben verfehlte Karte bleibt heute fällig und wird gewertet, wie das Üben sie am selben Tag wieder fragt); „ohne Antwort = übersprungen" zurückgestellt (s. o.).
- 2026-11-24 · Rollengang Sprint 130 (Live-Probe gegen die laufende API, Port 5221, Wegwerf-DB `pugling_h130.db`, Skript im Protokoll beschrieben): als Vater Kind/Fach/Reihe/Unit/Vokabelübung/Plan mit Leitner-Tagespflicht angelegt, als Sohn eine echte Lernrunde (vier Lernkarten, Fremdwort aus dem Gedächtnis → eingeführt und eingeprägt), den Test bestanden, dann Mischrunde: vor der Pflicht `extra_training_locked`, danach `dueCards` 4, Start, zweiter Start setzt fort, Abgabe 4/4, doppelte Abgabe `mixed_review_completed`, Bonus-Übersicht zeigt dieselbe Tagessumme, danach `mixed_review_empty`; Vater liest 200, startet 403. **Fund:** die vier Wörter waren **heute** gelernt und heute im Test gefragt (dessen Ergebnis die Lösungen zeigt) – und zahlten trotzdem 12 Münzen zum Tippsatz. Dieselbe Lücke wie der Review-Fund 🟡 1, nur über Lernkarte und Test statt über eine Vorrunde, die `LastReviewedAt` nicht stempeln. Behoben: „heute schon begegnet" heißt jetzt eingeführt heute, wiederholt heute **oder** in einem Test von heute gefragt (`TestAttempts.Results` der Position und des Tages); der Kasten bewegt sich weiter. **Fehlerzähler Sprint 130: 3** (eigener Fund, Behebung ändert Code, Freigabe 11). Test `Heute_Gelernte_Oder_Getestete_Karte_Zahlt_Nicht` (N18, N19). Zwischenstand beim Bauen: die erste Fassung der Test-Abfrage (`SelectMany` mit `!.Value` in der Projektion) lieferte 500 – vor dem Commit umgeschrieben, kein eigener Fund.
- 2026-11-24 · Mutanten-Probe nach der letzten Änderung (`.claude/scripts/mutant-probe.py`, Filter `MischrundeWertungTests`): gemessen an `64c14761 + Arbeitsbaum`, 02:28, ohne Mutante 17/17 grün: 19 von 19 rot am benannten Test, Grundlinie nach dem Zurückspielen grün (Tabelle unten). Eine erste Messung (00:53, vor dem Review-Fix) hatte N1–N15 15/15 rot, eine zweite (01:43, nach dem Review-Fix) N1–N17 17/17 rot.

| Mutante | Stelle | erwartet | gemessen |
|---|---|---|---|
| N1 | ExtraTrainingService.cs:314 (Deckel: zweite Quelle) | red | red |
| N2 | MixedReviewService.cs:227 (`ApplyReview`) | red | red |
| N3 | MixedReviewService.cs:229 (Selbstauskunft zählt nicht) | red | red |
| N4 | MixedReviewService.cs:240 (nur geprüft zahlt) | red | red |
| N5 | MixedReviewService.cs:240 (Zahl-Tor) | red | red |
| N6 | MixedReviewService.cs:149 (Sperre bei Abgabe) | red | red |
| N7 | MixedReviewService.cs:270 (Stamp) | red | red |
| N8 | MixedReviewService.cs:127 (schon abgegeben) | red | red |
| N9 | MixedReviewService.cs:191 (nicht mehr fällig) | red | red |
| N10 | MixedReviewService.cs:184 (Fingerabdruck) | red | red |
| N11 | MixedReviewService.cs:131 (Runde von gestern) | red | red |
| N12 | MixedReviewService.cs:250 (Deckel kappt) | red | red |
| N13 | MixedReviewService.cs:86 (Stufe eingefroren) | red | red |
| N14 | MixedReviewService.cs:181 (Pflicht bei Abgabe) | red | red |
| N15 | MixedReviewService.cs:410 (Satz 0 bei Selbstauskunft) | red | red |
| N16 | MixedReviewService.cs:240 (heute schon begegnet) | red | red |
| N17 | MixedReviewService.cs:181 (Leitner bei Abgabe) | red | red |
| N18 | MixedReviewService.cs:224 (heute eingeführt) | red | red |
| N19 | MixedReviewService.cs:226 (heute getestet) | red | red |
- 2026-11-24 · **abgenommen** (Nachtlauf H, Sprint 130). Commit `1e3b42f7`. Suite (Release) 2204/2204; `pugling-reviewer` (🟡 behoben, 🟢 behoben bzw. zurückgestellt); Mutanten 19/19 rot; Rollengang als Live-Probe gegen die laufende API (Abgabe 4/4, Kästen 1→2, doppelte Abgabe abgewiesen, Bonus-Übersicht mit derselben Tagessumme, 0 Münzen für heute gelernte Wörter) – Protokoll `docs/pm-sitzung-2026-11-23-h.md`, Runde 130. Zur Bestätigung am Morgen: die Auslegungen (a)–(d) und die Erweiterung des Zahl-Tors auf „heute schon begegnet“ (Review- und Rollengang-Fund).
