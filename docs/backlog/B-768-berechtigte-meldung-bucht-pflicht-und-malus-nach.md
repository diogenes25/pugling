---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/gamification, rolle/supervisor, rolle/student]
aliases: [Nachbuchen, Malus-Rücknahme]
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: "B-627"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-768 · Eine berechtigte Meldung rechnet den Test neu und bucht Pflicht, Belohnung und Malus nach (Backend)

Der zweite Teil von [B-627](B-627-berechtigte-meldung-bucht-nach.md): die Bestätigung „berechtigt“ rechnet den Versuch über eine **Korrekturspur** neu, bucht eine ausgebliebene Ziel-Belohnung nach und nimmt einen Malus durch eine
**positive Gegenbuchung** zurück. **Baut nach B-767.** Geld und Pflicht: das Risiko dieser Story liegt hier.

## User Story

Als **Supervisor** möchte ich mit „berechtigt“ den Fehler korrigieren, den mein Kind wegen einer fehlerhaften Übung im Test gemacht hat, damit es nicht für meinen Übungsfehler bezahlt.

## Ist-Stand am Code

Pfade relativ zu `backend/Pugling.Api/`; gemessen am 2026-11-20.

- Der Versuch trägt `CorrectItems`, `ScorePercent`, `Passed` (`Models/StudyPlanEntities.cs:192-195`), gesetzt beim Abgeben: `Passed = ScorePercent >= passPercent`, die Schwelle ist `pos.GoalThreshold` **zum Abgabezeitpunkt**, sonst ein Standardwert
  (`Controllers/Student/PositionTestsController.cs:451-460`). Die Schwelle steht **nicht** am Versuch.
- **Zwei** Leser von `Passed` in Abfragen: die Pflicht (`Services/Shared/PositionProgressService.cs:204`, `IsGoalMetAsync`) und die Missionszahl (`Services/Shared/MetricsService.cs:35-36`); dazu zwei Anzeigen (`PositionTestsController.cs:348`, `:481`) und ein Index
  über `Passed` (`Data/PuglingDbContext.cs:825`).
- Die Pflicht wird nur über einen **bestandenen Test der Periode** erfüllt (bei prüfbaren Typen, `:195-205`); das Üben verfehlt sie nie.
- Malus: `SettleClosedPeriodsAsync` (`:754`), idempotent über `PositionGoalPenalty` mit Schlüssel `(PlanPositionId, Cadence, PeriodStart)` (`Models/PlanPositionEntities.cs:223-237`); **überspringt Perioden mit vorhandener Strafzeile** (`:791-794`), der Stamp wird bei Abbuchung gebumpt (`:827`).
  Rückerstattung: **nicht gefunden**.
- Belohnung: `EvaluateAndAwardAsync` (`:533-575`) bucht `PointKind.Goal` einmal je Periode (`PositionGoalReward`); sie bumpt den Stamp **nicht** (Zubuchung).
- Wochen-Verdienst nimmt ein positives `GoalPenalty` ausdrücklich als „correction“ aus der Verdient-Summe (`Services/Shared/WalletService.cs:43-48`), gesichert durch `WochenvergleichTests.cs:59-70`.
- Beschriftung der Art: `frontend/src/lib/labels.ts:19` („Pflicht gerissen (Malus)“), `i18n/de.ts:173`, `i18n/en.ts:161`.

## Die echte Lücke

Korrekturspur am Versuch, ein einziger Helfer für „bestanden“, eine Strafzeile mit Rücknahme-Marke, und der Weg, der alles in der richtigen Reihenfolge bucht.

## Entscheidungen

*Entscheidungen stammen aus [B-627](B-627-berechtigte-meldung-bucht-nach.md): stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt. Die Nummern verweisen dorthin.*

1. **Der Versuch behält seinen Befund, die Korrekturspur rechnet neu** (B-627 Entscheidung 4): nullbare Felder am Versuch (Zahl der berichtigten Antworten, bestanden nach Korrektur); `TestItemResult.WasCorrect` und `ScorePercent` bleiben. Gelesen wird „bestanden“ über **einen**
   Ausdruck („nach Korrektur, sonst `Passed`“), an beiden Lesestellen. *Kosten:* Migration, der Ausdruck an zwei Stellen.
2. **Gebucht wird, wie die Welt bei richtiger Wertung gewesen wäre, nicht mehr** (B-627 Entscheidung 5): ist die Periode dadurch erfüllt und war eine Strafe gebucht, die Gegenbuchung; fehlt die Ziel-Belohnung, über den vorhandenen idempotenten Weg nachbuchen; bleibt die Periode
   mit der Korrektur offen, bleibt der Malus. Der Lernstand der Karte (Kasten, Beherrschung) wird **nicht** zurückgebucht.
3. **Die Rücknahme ist eine positive `GoalPenalty`-Gegenbuchung, die Strafzeile bleibt** (B-627 Entscheidung 3): `Reason` nennt die Meldung; sonst würde `SettleClosedPeriodsAsync` den Malus innerhalb der 14 Tage neu buchen (`:791-794`).
4. **Wallet-Invariante** (B-627 Entscheidung 6): jeder Buchungsweg bumpt `child.ConcurrencyStamp`, ohne Ausnahme; der Bump kostet nur einen Wiederholungsversuch eines parallelen Kaufs.
5. **Serie, Missionen, Kiste** (B-627 Entscheidung 7): Missionen, Auszeichnungen und eine geöffnete Kiste werden nicht neu bewertet; die Serie folgt der Korrekturspur von selbst, weil sie `IsGoalMetAsync` berechnet.
6. **Beim Schätzen nachgetragen (Annahme, unbestätigt; weicht vom Wortlaut von B-627 Entscheidung 8 ab): die Rücknahme-Marke sitzt an der Strafzeile (`PositionGoalPenalty`), nicht nur an der Meldung.** Zwei verschiedene Meldungen zu zwei Antworten
   derselben Periode könnten sonst jede für sich dieselbe Strafzeile zurückbuchen, und die Strafe würde doppelt gutgeschrieben. *Kosten:* eine dritte Tabelle in der Migration. *Verworfen:* die Marke nur an der Meldung. *Schließt aus:* eine Strafe zweimal zu erstatten.
7. **Beim Schätzen nachgetragen (Annahme, unbestätigt): „bestanden nach Korrektur“ wird beim Bestätigen einmal berechnet und gespeichert,** mit der Schwelle des Bestätigungszeitpunkts; spätere Änderungen der Schwelle verschieben bereits Gebuchtes nicht.
   *Kosten:* eine nachträglich geänderte Schwelle kann die Korrektur anders ausfallen lassen, als der Versuch es damals tat; das ist die Welt von heute, nicht die von damals.

## Akzeptanzkriterien

Nummerierung folgt B-627.

1. (AK 3) „Berechtigt“ rechnet den Versuch über die Korrekturspur neu (`CorrectItems`, bestanden); `TestItemResult.WasCorrect` und der Originalbefund bleiben, eine andere Antwort desselben Kindes bleibt unverändert. *Rot, wenn das Ergebnis überschrieben wird oder die
   Umbuchung alle Antworten zur Karte trifft.*
2. (AK 4) Pflicht und Missionszahl lesen „bestanden“ über denselben Ausdruck. *Rot, wenn `PositionProgressService.cs:204` oder `MetricsService.cs:36` weiter `t.Passed` liest (Test: Pflicht der Periode nach der Korrektur erfüllt).*
3. (AK 5) Hing ein gebuchter Malus nur an diesem nicht bestandenen Test, entsteht eine **positive `GoalPenalty`-Gegenbuchung** in Höhe der Strafzeile; die `PositionGoalPenalty`-Zeile bleibt, der Stamp ist gebumpt, die Gegenbuchung zählt nicht in `WeekCoinsAsync`.
   *Rot, wenn die Zeile gelöscht wird (die Abrechnung bucht den Malus neu), der Stamp gleich bleibt oder die Gegenbuchung als verdient zählt.*
4. (AK 6) Hing der Malus an etwas anderem (zweiter, ebenfalls nicht bestandener Versuch), bleibt er. *Rot, wenn die Umbuchung den Malus blind aufhebt.*
5. (AK 7) War die Periode ohne Strafe, aber ohne Ziel-Belohnung offen, bucht die Bestätigung die Belohnung einmal nach. *Rot, wenn eine zweite Bestätigung eine zweite Belohnung schreibt (`PositionGoalReward` ist der Schlüssel).*
6. (AK 8, geschärft nach Entscheidung 6) Dieselbe Meldung zweimal bestätigt bucht nicht doppelt, und **zwei Meldungen zu zwei Antworten derselben Periode erstatten dieselbe Strafzeile nur einmal**. *Rot, wenn die Marke nur an der Meldung hängt.*
7. (AK 9, Teil) Nur der Supervisor dieses Kindes bestätigt; das Kind erhält `403`, ein fremder Supervisor `404`. *Rot, wenn die Rollenprüfung fehlt.*
8. (AK 11, **Absichtserklärung**) Der Kasten der Karte wird nicht zurückgebucht. Der Ausgangszustand erfüllt es schon; rot würde nur, wer `PositionItemProgress` in die Umbuchung zieht.
9. (**neu, Wallet-Invariante**) Jeder Weg dieser Umbuchung bumpt `ConcurrencyStamp`, auch der, der nur zubucht. *Rot, wenn ein Weg den Stamp unverändert lässt (Test: Stamp vor und nach jeder der drei Wirkungen).*

## Schätzung

**Größe: L** – wie eine DB-Umbau-Etappe (E6): Geld und Pflicht, drei Tabellen in der Migration, ein Ausdruck, der in Abfragen übersetzbar sein muss, und sechs Fälle (Strafe da oder nicht, Belohnung da oder nicht, Korrektur reicht oder nicht). Nicht M, weil ein Fehler
Münzen doppelt erstattet oder einen Malus still zurückbringt.

Nachgesehen am 2026-11-20:

- **`wo: backend`.** Die Beschriftung eines positiven Eintrags der Art `GoalPenalty` in der Kontoansicht gehört zu B-769.
- **`migration: ja`.** `TestAttempt` (Korrekturspur, nullbar), `PositionGoalPenalty` (Rücknahme-Marke, nullbar); Einträge in `SchemaGuardTests`. Die Spalten der Meldung stehen in B-767.
- **`vertragsbruch: nein`.** Die Antwort des Versuchs und der Meldungsliste bekommt additive Felder; kein neuer `PointKind` (ein positives `GoalPenalty`).
- **Wallet-Invariante geprüft:** (1) die Gegenbuchung **bucht zu**, braucht den Bump nicht, wird aber nach B-627 Entscheidung 6 **ohne Ausnahme** mitgebumpt; (2) `EvaluateAndAwardAsync` bumpt **nicht** – der Weg dieser Story muss es **selbst** tun, wenn er die
  Belohnung darüber nachbuchen lässt; (3) das Abgeben der Bestätigung muss gegen einen parallelen Kauf serialisiert sein: Kind zuerst laden, wie bei `ExtraTrainingService.cs:146`.

### Risiken

1. **Der Ausdruck für „bestanden“ in EF-Abfragen:** `MetricsService` und `IsGoalMetAsync` filtern in der Datenbank; ein Instanz-Helfer ist dort nicht übersetzbar. Ein gemeinsamer **Ausdruck** (`Expression<Func<TestAttempt,bool>>`) nutzen. Der Index über `Passed` (`PuglingDbContext.cs:825`) greift bei „nach Korrektur, sonst `Passed`“ schlechter; bei der Größe der Tabelle unkritisch.
2. **Die Schwelle ist nicht am Versuch** (Entscheidung 7); eine geänderte `GoalThreshold` zwischen Abgabe und Bestätigung bleibt sichtbar als Abweichung.
3. **Rücknahme-Marke an der Strafzeile** (Entscheidung 6) weicht von B-627 Entscheidung 8 im Wortlaut ab; der Nutzer sollte das bestätigen.
4. **Belohnung nachbuchen über `EvaluateAndAwardAsync`:** die Funktion bucht für den Tag des Versuchs (`attempt.Day`); die Periode des Versuchs und die Wochenpflicht müssen übereinstimmen.
5. **Zeitfenster:** eine Strafzeile aus einer Periode über 14 Tage her wird vom Abrechnen nicht mehr berührt; die Gegenbuchung funktioniert dennoch, ist aber nie „nur Korrektur“, sondern echtes Gutschreiben.
6. **Gegenbuchung sichtbar machen:** ein positiver `GoalPenalty`-Eintrag ist in der Kontoansicht mit „Pflicht gerissen (Malus)“ falsch beschriftet (B-769).
7. **Nebenfund:** `ChildrenController.AddPoints` bucht negative Beträge ohne Stamp-Bump (`Controllers/Supervisor/ChildrenController.cs:279-290`); Eigene Story, hier nicht mitgenommen.

### Angriffsplan

1. Modell, Migration, Tor-Einträge (zwei Tabellen).
2. Gemeinsamer Ausdruck „bestanden“; beide Lesestellen und die Anzeigen umstellen (eigener Schritt mit Regressionstest, noch ohne Korrektur).
3. `FeedbackCorrectionService`: Kind laden, Bezug aus B-767 lesen, Korrekturspur setzen, Periode neu bewerten, Gegenbuchung oder Belohnung buchen, Marke setzen, Stamp bumpen, speichern.
4. Aufruf aus dem Erledigen mit Ausgang „berechtigt“ (B-767).
5. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`; `DocsCaptureTests` neu erzeugen.

### Testweg

- Neue Klasse `MeldungBuchtNachTests` (Vorbild `PflichtMalusTests` für Strafe und Idempotenz, `ExerciseFeedbackTests` für den Aufbau der Meldung, `WochenvergleichTests` für „zählt nicht als verdient“): AK 1 bis 9.
- Rote Proben je Kriterium stehen oben; besonders AK 2 (eine Lesestelle bleibt auf `Passed`), AK 3 (die Strafzeile wird gelöscht), AK 6 (zwei Meldungen, eine Strafzeile).
- `MeldungBuchtNachMigrationTests` (neu, nach `ActivatedAtMigrationTests`): nach der Migration ist keine Korrekturspur gesetzt und kein Kontostand verändert.
- Vorhandene `PflichtMalusTests` und `ExerciseFeedbackTests` bleiben unverändert grün.
- Vor der Abnahme: `pugling-reviewer`; `/smoke-test` für Test abgeben, Login nach der Periode, Meldung, Bestätigen, Kontostand.

## Verlauf

- 2026-11-20 · aus B-627 geteilt und geschaetzt (Vorbereitungslauf, Sonnet): L, backend, Migration ja, kein Vertragsbruch. Zwei Annahmen nachgetragen, unbestätigt: Marke an der Strafzeile (weicht von B-627 Entscheidung 8 im Wortlaut ab) und „bestanden nach Korrektur“ einmal berechnet. Baut nach B-767.
