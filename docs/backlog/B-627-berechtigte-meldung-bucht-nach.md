---
tags: [typ/story, status/ausformuliert, bereich/training, bereich/gamification, rolle/supervisor, rolle/student]
aliases: [berechtigte Meldung, Nachbuchen nach Rückmeldung]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "B-623, Entscheidung 6 (Grill-Runde mit dem Vater, 2026-11-17)"
grund: ""
ersetzt_durch: []
---

# B-627 · Eine vom Vater als berechtigt bestätigte Meldung bucht den Fehler des Kindes nach

Meldet das Kind zu einer Karte „falsch übersetzt“ oder „zu streng bewertet“ ([B-623](B-623-feedback-knopf-an-der-karte.md))
und hat es recht, zählt seine Antwort trotzdem als Fehler: Kasten zurück, keine Münzen, womöglich Malus für
die verpasste Pflicht. B-623 hat bewusst entschieden, dass eine Meldung selbst nichts bewirkt (Anti-Cheat).
Die Idee hier: Der Vater setzt eine Meldung auf **„berechtigt“**, und der Fehler, den das Kind an dieser
Karte gemacht hat, wird nachträglich als richtig gewertet – Kasten und Punkte nachgebucht, ein Malus dieses
Tages entfällt, wenn er nur daran hing.

Zu klären beim Ausformulieren: welche Antworten genau umgebucht werden (nur die gemeldete, alle seit der
Meldung, alle zu dieser Karte), wie das mit der Wallet-Invariante (`ConcurrencyStamp`) und der Tageslogik der
Pflicht zusammengeht, und ob ein bereits verbuchter Malus zurückerstattet wird.

## User Story

Als **Supervisor** möchte ich eine Meldung meines Kindes als berechtigt bestätigen und damit den Fehler korrigieren,
den es an dieser Karte wegen einer fehlerhaften Übung gemacht hat, damit das Kind nicht für meinen Übungsfehler
bezahlt (Kasten, Münzen, Malus).

Als **Student** möchte ich, dass eine bestätigte Meldung meinen Fehler nicht stehen lässt, damit ich weiter melde,
statt hinzunehmen, dass die App unrecht hat.

## Ist-Stand am Code

Recherchiert am 2026-11-19 gegen `47791819`. B-623 ist gebaut (B-628 `abgenommen`); die Meldung gibt es.

**Die Meldung kennt keine Antwort.**

- `ExerciseFeedback` trägt Kind, Position, Übung, `ItemIndex`, `ExerciseItemId`, `VocabularyId`, Grund, Zähler, Text,
  `Status` (`backend/Pugling.Api/Models/FeedbackEntities.cs:12`–`:67`). Ein Verweis auf die **Antwort** (Event,
  Ergebniszeile, Testversuch) ist **nicht gefunden**.
- Der Zustand kennt nur `Open`/`Done` (`backend/Pugling.Contracts/Common/FeedbackBaseTypes.cs:40`–`:46`); ein Ausgang
  „berechtigt“ ist nicht gefunden (Suche nach `berechtigt`/`Justified` in den Feedback-Dateien ohne Treffer).
  `SetStatusAsync` setzt nur Status, `DoneAt`, `DoneByAdultId` und löscht `SeenByChildAt`
  (`backend/Pugling.Api/Services/Shared/ExerciseFeedbackService.cs:154`–`:165`); der Endpunkt dazu ist
  `backend/Pugling.Api/Controllers/Supervisor/FeedbackController.cs:51`–`:62`.
- Die Anzeige der Antwort im Vater-Web ist eine **Heuristik**: die jüngste gespeicherte Antwort des Kindes zur Karte aus
  Tests und – nur für Vokabel – aus der Antwort-Historie (`ExerciseFeedbackService.cs:123`–`:151`). Im Kommentar steht,
  was für B-627 entscheidend ist: „A practice answer to a non-vocabulary card is not stored anywhere“ (`:119`–`:122`).
- Entscheidung 6 von B-623 steht im Code als Vertrag: die Meldung wirkt weder auf Lernstand noch Punkte, Pflicht, Malus
  oder Cursor (`FeedbackEntities.cs:8`–`:10`).

**Was eine Antwort schreibt – das wäre umzubuchen.**

- *Üben:* ein `ReviewEvent` nur mit Richtig/Falsch und Zeit, **ohne Karte**
  (`backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:709`–`:717`); ein `ItemReviewEvent` mit
  `GivenAnswer`/`WasCorrect`, **nur für Vokabel mit stabiler `ItemId`** (`:729`–`:733`; Entität
  `backend/Pugling.Api/Models/ItemProgressEntities.cs:61`–`:89`); der Kasten über `play.ApplyReview` und
  `PositionItemProgress` (`:749`; `backend/Pugling.Api/Models/PlanPositionEntities.cs:244`–`:274`); Münzen über
  `ScoringService.ScoreReview` mit Combo, Geschwindigkeit und Neu-Bonus (`:741`–`:761`; `NewBonusPaidAt` `:744`–`:745`);
  bei Nacharbeit zusätzlich `SolvedByStage`/`RemediationDoneAt` (`:795`–`:806`). Danach `gamification.EvaluateAndAward…`
  (`:818`–`:819`) – Missionen und Auszeichnungen sind idempotent belohnt und nicht zurückrechenbar (nicht im Detail
  gelesen).
- *Test:* ein `TestItemResult` je Karte mit `GivenAnswer`/`WasCorrect`
  (`backend/Pugling.Api/Models/StudyPlanEntities.cs:209`–`:226`), dazu die Summen im `TestAttempt`:
  `CorrectItems`, `ScorePercent`, `Passed` (`backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:457`–`:460`,
  Feld `StudyPlanEntities.cs:192`–`:195`) und der Item-Lernstand (`PositionTestsController.cs:449`).

**Wann ein Fehler Malus kostet – schmaler als die Idee annimmt.**

- Die Pflicht ist erfüllt, wenn bei prüfbaren Typen ein Testversuch der Periode **bestanden** ist, sonst wenn eine
  Lern-Runde weit genug gespielt wurde (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:195`–`:205`,
  `:232`–`:254`). Eine falsche Antwort **im Üben** verfehlt die Pflicht also nie; sie kostet Kasten, Punkte, Combo. Malus
  hängt an einem **nicht bestandenen Test** (`Passed`).
- Der Malus entsteht lazy beim Login und Shop-Kauf (`SettleClosedPeriodsAsync`, `PositionProgressService.cs:754`–`:840`;
  Aufrufer `backend/Pugling.Api/Controllers/AuthController.cs:85`, `…/Student/MeController.cs:296`), höchstens
  14 Tage zurück (`:25`), idempotent über `PositionGoalPenalty` (`PlanPositionEntities.cs:223`–`:237`) und bucht einen
  negativen `GoalPenalty`-Eintrag samt `ConcurrencyStamp`-Bump (`:810`–`:827`).
- **Rückerstattung gibt es nicht:** nichts entfernt je eine `PositionGoalPenalty` oder buchte sie zurück (Suche nach
  `PositionGoalPenalties.Remove` ohne Treffer). Vorhanden ist nur das Druckventil „Münzen schenken“ (`POST
  children/{}/points`, `PointKind.Manual`; beschrieben in `backend/Pugling.Api/CLAUDE.md`, Abschnitt Reward-Ökonomie).
- Ein bestandener Test nach dem Malus schließt den Eintrag nicht: `SettleClosedPeriodsAsync` überspringt nur Perioden
  mit Reward oder vorhandener Penalty (`:791`–`:794`); ein später gebuchter Reward läge neben einer bestehenden Penalty.
- Ein Test darf am selben Tag wiederholt werden (Tagesdeckel, Kommentar `StudyPlanEntities.cs:184`–`:188`); wie viele
  Versuche genau, **nicht nachgesehen**.

## Die echte Lücke

Drei Dinge fehlen, und das erste ist die Voraussetzung der anderen:

1. **Die Meldung muss auf die beanstandete Antwort zeigen.** Heute zeigt sie auf eine Karte; die Antwort wird nachträglich
   geraten (jüngste Antwort), und beim Üben einer Nicht-Vokabel-Karte gibt es gar keine gespeicherte Antwort. Ohne diesen
   Bezug lässt sich nichts umbuchen, was nicht bloß „eine“ Antwort trifft.
2. **Der Ausgang „berechtigt“** ist nicht im Vertrag; `Done` sagt nicht, ob der Vater recht gab.
3. **Eine Umbuchung, die jede Wirkung einer Antwort kennt** – Kasten, Punkte, Combo, Test-Summen, Pflicht – und den
   Malus. Das ist keine „Korrektur eines Flags“, sondern die Rückabwicklung von bis zu sechs Schreibstellen.

Und die Idee ist **schmaler und größer zugleich**: Beim Üben bleibt die Pflicht unberührt (nur Kasten/Münzen), der Malus
hängt allein am nicht bestandenen Test.

## Offene Punkte

Berührt Münz- und Malus-Regeln – deshalb alle als **Fragen mit Empfehlung**, keine Entscheidung.

1. **Welche Antwort wird umgebucht?** Optionen: nur die gemeldete / alle zu dieser Karte seit der Meldung / alle seit
   Bestehen. Empfehlung: **nur die gemeldete**, und die Meldung speichert beim Absenden den Bezug (Event-/Ergebnis-Id).
   Heuristik „jüngste Antwort“ ist für Geld zu unscharf.
2. **Üben, Test oder beides?** Empfehlung: **zuerst nur Test.** Beim Üben ist die Antwort für Nicht-Vokabel-Karten nicht
   gespeichert (`ExerciseFeedbackService.cs:119`–`:122`), und der Schaden dort ist klein (Kasten, wenige Münzen); der
   Test trägt Pflicht und Malus.
3. **Wird ein schon gebuchter Malus zurückgenommen?** Empfehlung: **ja, durch eine Gegenbuchung** (positiver
   `Manual`/eigener `PointKind`-Eintrag mit Begründung und Verweis auf die Meldung), nie durch Löschen der
   `GoalPenalty`-Zeile – das Ledger bleibt Historie. Neuer `PointKind` heißt Vertrags- und Währungs-Zuordnung
   (`PointKindCurrency`); ob eine zweite Währungsregel nötig ist, nicht geprüft.
4. **Was wird aus dem bestandenen/nicht bestandenen Test?** Empfehlung: `TestItemResult.WasCorrect` bleibt unverändert
   (Originalbefund), der Versuch bekommt eine **Korrekturspur**, die `Passed` neu rechnet; sonst wären Auswertungen
   (`ScorePercent`) Fälschungen. Alternative (Ergebnis überschreiben) ist einfacher, verliert den Beleg.
5. **Wallet-Invariante.** Eine Gutschrift bucht nicht ab, die Invariante verlangt den `ConcurrencyStamp`-Bump dennoch dort,
   wo der Saldo aus einer Rückabwicklung sinkt (z. B. Combo-Punkte, die wegfallen). Empfehlung: **jeden** Pfad der
   Umbuchung bumpen lassen, ohne Ausnahme.
6. **Missionen, Auszeichnungen, Serie.** Eine nachträglich richtige Antwort könnte eine Mission erfüllen oder eine Serie
   retten. Empfehlung: **nicht rückwirkend neu auswerten** (Belohnungen sind idempotent gebucht); die Story sagt das
   ausdrücklich und das Kind sieht es im Hinweis.
7. **Anti-Cheat.** Wer entscheidet? Empfehlung: nur **Supervisor des Kindes** (wie `SetStatusAsync`), nie das Kind; eine
   Meldung zählt höchstens einmal je Antwort; Obergrenze je Kind und Woche **als Frage an den Vater**, nicht als Regel.
8. **Reihenfolge mit B-624/B-625/B-626.** Nehmen diese die Streng-Fälle (Akzent, mehrere Bedeutungen, Platzhalter) vorab
   weg, schrumpft der Bedarf. Empfehlung: B-627 **nach** diesen schätzen oder bauen und am Rest messen, wie viele
   berechtigte Meldungen bleiben.

## Akzeptanzkriterien (Entwurf)

1. Eine Meldung speichert beim Absenden den Bezug zur Antwort (Test: `TestItemResult`; Üben: Event), nicht nur zur Karte.
   *Rot, wenn:* ein Integrationstest nach dem Absenden keinen Bezug in der Zeile findet.
2. Der Supervisor kann eine Meldung als „berechtigt“ **oder** „abgewiesen“ erledigen; `Done` allein bleibt für Altbestand.
   *Rot, wenn:* der Vertrag keinen Ausgang kennt (`FeedbackStatus` bleibt zweiwertig).
3. „Berechtigt“ bucht die gemeldete Antwort auf richtig um: Kasten/Punkte (Üben) bzw. Summen und `Passed` (Test); eine
   andere Antwort desselben Kindes bleibt unverändert. *Rot, wenn:* die Umbuchung alle Antworten zur Karte trifft oder
   keine.
4. Hing ein gebuchter Malus nur an diesem nicht bestandenen Test, entsteht eine **Gegenbuchung** im Ledger, die
   `GoalPenalty`-Zeile bleibt, `ConcurrencyStamp` ist gebumpt. *Rot, wenn:* die Zeile gelöscht wird oder der Stamp
   gleich bleibt.
5. Hing der Malus an etwas anderem (zweiter, ebenfalls nicht bestandener Versuch), bleibt er. *Rot, wenn:* die
   Umbuchung den Malus blind aufhebt.
6. Dieselbe Meldung zweimal bestätigt bucht nicht doppelt. *Rot, wenn:* ein zweiter Aufruf einen zweiten Eintrag schreibt.
7. Eine Meldung ohne Supervisor-Bestätigung ändert weiter nichts (B-623, Entscheidung 6) – **Absichtserklärung**, der
   Ausgangszustand erfüllt sie bereits; rot würde nur, wer die Meldung selbst wirken ließe.

## Verlauf

- **2026-11-17** — angelegt als Folge-Story aus B-623, Entscheidung 6.
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): B-623 ist gebaut, aber die Meldung kennt die Antwort nicht
  (kein Bezug, beim Üben von Nicht-Vokabel-Karten nicht gespeichert); Malus hängt nur am nicht bestandenen Test, eine
  Rückerstattung gibt es nicht. Acht offene Punkte, alle mit Empfehlung, keine entschieden (Münz-/Malus-Regeln).
