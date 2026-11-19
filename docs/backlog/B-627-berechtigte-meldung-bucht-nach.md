---
tags: [typ/story, status/gegrillt, bereich/training, bereich/gamification, rolle/supervisor, rolle/student]
aliases: [berechtigte Meldung, Nachbuchen nach Rückmeldung]
status: gegrillt
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

Berührt Münz- und Malus-Regeln. Alle Punkte sind unten entschieden (durchgestrichen, mit Verweis auf die Entscheidung).

1. ~~**Welche Antwort wird umgebucht?** Optionen: nur die gemeldete / alle zu dieser Karte seit der Meldung / alle seit
   Bestehen. Empfehlung: **nur die gemeldete**, und die Meldung speichert beim Absenden den Bezug (Event-/Ergebnis-Id).
   Heuristik „jüngste Antwort“ ist für Geld zu unscharf.~~ → Entscheidung 1.
2. ~~**Üben, Test oder beides?** Empfehlung: **zuerst nur Test.** Beim Üben ist die Antwort für Nicht-Vokabel-Karten nicht
   gespeichert (`ExerciseFeedbackService.cs:119`–`:122`), und der Schaden dort ist klein (Kasten, wenige Münzen); der
   Test trägt Pflicht und Malus.~~ → Entscheidung 2.
3. ~~**Wird ein schon gebuchter Malus zurückgenommen?** Empfehlung: **ja, durch eine Gegenbuchung** (positiver
   `Manual`/eigener `PointKind`-Eintrag mit Begründung und Verweis auf die Meldung), nie durch Löschen der
   `GoalPenalty`-Zeile – das Ledger bleibt Historie. Neuer `PointKind` heißt Vertrags- und Währungs-Zuordnung
   (`PointKindCurrency`); ob eine zweite Währungsregel nötig ist, nicht geprüft.~~ → Entscheidung 3.
4. ~~**Was wird aus dem bestandenen/nicht bestandenen Test?** Empfehlung: `TestItemResult.WasCorrect` bleibt unverändert
   (Originalbefund), der Versuch bekommt eine **Korrekturspur**, die `Passed` neu rechnet; sonst wären Auswertungen
   (`ScorePercent`) Fälschungen. Alternative (Ergebnis überschreiben) ist einfacher, verliert den Beleg.~~ → Entscheidung 4.
5. ~~**Wallet-Invariante.** Eine Gutschrift bucht nicht ab, die Invariante verlangt den `ConcurrencyStamp`-Bump dennoch dort,
   wo der Saldo aus einer Rückabwicklung sinkt (z. B. Combo-Punkte, die wegfallen). Empfehlung: **jeden** Pfad der
   Umbuchung bumpen lassen, ohne Ausnahme.~~ → Entscheidung 6.
6. ~~**Missionen, Auszeichnungen, Serie.** Eine nachträglich richtige Antwort könnte eine Mission erfüllen oder eine Serie
   retten. Empfehlung: **nicht rückwirkend neu auswerten** (Belohnungen sind idempotent gebucht); die Story sagt das
   ausdrücklich und das Kind sieht es im Hinweis.~~ → Entscheidung 7.
7. ~~**Anti-Cheat.** Wer entscheidet? Empfehlung: nur **Supervisor des Kindes** (wie `SetStatusAsync`), nie das Kind; eine
   Meldung zählt höchstens einmal je Antwort; Obergrenze je Kind und Woche **als Frage an den Vater**, nicht als Regel.~~ → Entscheidung 8.
8. ~~**Reihenfolge mit B-624/B-625/B-626.** Nehmen diese die Streng-Fälle (Akzent, mehrere Bedeutungen, Platzhalter) vorab
   weg, schrumpft der Bedarf. Empfehlung: B-627 **nach** diesen schätzen oder bauen und am Rest messen, wie viele
   berechtigte Meldungen bleiben.~~ → Entscheidung 9.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

**Querfrage mit [B-687](B-687-hausaufgabe-mit-foto-nachweis.md) und [B-593](B-593-sparziel-im-familien-shop.md) (dort Entscheidungen 2
und 10, gleiche Antwort):** Eine **systemische** Rücknahme eines gebuchten Malus ist immer eine **positive Gegenbuchung der
Art `GoalPenalty`** mit Verweis in `Reason`, die Strafzeile bleibt, der `ConcurrencyStamp` wird gebumpt. Eine **Gnade** des
Vaters ist die vorhandene `Manual`-Gutschrift („Münzen schenken“). Beide zählen nicht als „verdient“ (Entscheidung 3).
**Überschneidung mit abgenommenen Entscheidungen, nicht geändert:** B-623 Entscheidung 6 (eine Meldung wirkt von sich aus
nichts, `FeedbackEntities.cs:8-10`) bleibt; hier wirkt erst die **Bestätigung des Supervisors**.

1. **Es wird die gemeldete Antwort umgebucht, nicht alle zur Karte.** Die Meldung speichert beim Absenden den Bezug zur
   Antwort: der Server sucht die jüngste gespeicherte Antwort des Kindes zur Karte und **friert** ihre Id in der Meldung ein.
   Das ist die Heuristik von `ExerciseFeedbackService.cs:123-151`, nur **zum Zeitpunkt der Meldung** statt beim Lesen. Das Kind
   meldet unmittelbar nach der Antwort, „jüngste“ trifft dann. Gibt es keine gespeicherte Antwort (Üben einer Nicht-Vokabel-Karte,
   `:119-122`), bleibt die Meldung ohne Bezug und kann nicht „berechtigt“ gebucht werden. *Kosten:* ein neues nullbares
   Bezugsfeld an der Meldung, Migration. *Verworfen:* alle Antworten seit der Meldung oder seit Bestehen (Geld braucht eine
   eindeutige Antwort), und ein Client, der die Id mitschickt (Vertragsänderung ohne Gewinn). *Schließt aus:* mehrere
   falsch bewertete Antworten derselben Karte mit **einer** Bestätigung zu heilen; je Antwort eine Meldung.
2. **Zuerst nur der Test, nicht das Üben.** Der Test trägt Pflicht und Malus (`PositionProgressService.cs:195-205`); das Üben
   verfehlt die Pflicht nie und kostet nur Kasten, Punkte und Combo, dazu ist die Antwort einer Nicht-Vokabel-Karte dort nicht
   gespeichert. *Kosten:* Ein falsch gewerteter Treffer im Üben bleibt gebucht. Der Vater kann ihn wie bisher mit einer
   `Manual`-Gutschrift ausgleichen. *Verworfen:* beides auf einmal (bis zu sechs Schreibstellen, drei davon ohne gespeicherte
   Antwort). *Schließt aus:* die Korrektur eines Üben-Fehlers durch die Bestätigung. *Gegen die User Story gerechnet:* „nicht
   für meinen Übungsfehler bezahlt (Kasten, Münzen, Malus)“ gilt für den Test; für das Üben nur über die Gutschrift.
3. **Ein gebuchter Malus wird durch eine Gegenbuchung zurückgenommen, nie durch Löschen.** Die Gegenbuchung ist ein
   **positiver `PointKind.GoalPenalty`-Eintrag** (Betrag der Strafzeile, `Reason` nennt die Meldung), kein neuer `PointKind`.
   *Begründung (Konvention des Repos, Fund- und Gegenfundstellen):* `WalletService.cs:43-48` nennt ein positives `GoalPenalty`
   ausdrücklich „a correction“ und nimmt es aus der Wochen-Verdienstsumme (`EarnedCoinKinds`); `WochenvergleichTests.cs:59-70`
   sichert das; der Kauf-Rückruf bucht dieselbe Art positiv (`ShopService.CancelPurchaseAsync`, `PointKind.ShopCoins`).
   Gegenfundstelle: die Oberfläche beschriftet die Art mit „Pflicht gerissen (Malus)“ (`frontend/src/lib/labels.ts:19`,
   `i18n/de.ts:173`, `en.ts:161`); ein positiver Eintrag braucht eine eigene Beschriftung nach Vorzeichen. Dass die Zeile
   bleiben **muss**, ist strukturell: `SettleClosedPeriodsAsync` überspringt Perioden mit vorhandener `PositionGoalPenalty`
   (`PositionProgressService.cs:791-794`); ohne die Zeile würde der Malus innerhalb der 14 Tage neu gebucht. *Kosten:* ein
   eindeutiger Schlüssel für die Idempotenz (die Meldung trägt „Rücknahme gebucht“), Beschriftung in zwei Sprachen. *Verworfen:*
   neuer `PointKind` (Währungs-Zuordnung, `PointKindCurrencyTests`, ein zweiter Ausschluss in `EarnedCoinKinds`) und Löschen
   der Zeile. *Schließt aus:* die Strafzeile als „nie passiert“; sie bleibt als Historie sichtbar.
4. **Der Ausgang „berechtigt“ ist ein eigenes Feld, der Test behält seinen Befund.** Der Supervisor erledigt eine Meldung mit
   „berechtigt“ oder „abgewiesen“; `Done` allein bleibt für Altbestand (neue nullbare Spalte an der Meldung, additives
   Vertragsfeld). `TestItemResult.WasCorrect` und `ScorePercent` bleiben der Originalbefund. Der Versuch bekommt eine
   **Korrekturspur** (nullbar), die `Passed` neu rechnet; gelesen wird `Passed` über **einen** Helfer
   (`PassedAfterCorrection ?? Passed`). Es gibt zwei Leser: `PositionProgressService.cs:204` (Pflicht) und
   `MetricsService.cs:36` (Missionen, Testzahl). *Begründung:* Würde das Ergebnis überschrieben, wären Auswertungen
   gefälscht. *Kosten:* zweite Migration an der Kette (Meldung und Versuch), der Helfer an zwei Stellen. *Verworfen:* das
   Ergebnis überschreiben (einfacher, verliert den Beleg).
5. **Die Umbuchung stellt die Welt her, wie sie bei richtiger Wertung gewesen wäre, aber nicht mehr.** Bei „berechtigt“:
   Korrekturspur setzen; ist die Periode dadurch erfüllt und war eine Strafe gebucht, die Gegenbuchung nach Entscheidung 3;
   fehlt die Ziel-Belohnung der Periode, wird sie über den vorhandenen idempotenten Weg (`PositionGoalReward`, `:533-568`)
   nachgebucht. Bleibt die Periode auch mit der Korrektur offen (zweiter, ebenfalls nicht bestandener Versuch), bleibt der
   Malus. Der **Lernstand der Karte** (Kasten, Beherrschung, `PositionTestsController.cs:449`) wird **nicht** zurückgebucht.
   *Begründung:* Eine Rückbuchung des Kastens könnte neuere Antworten überschreiben; der Schaden ist eine Wiederholung
   mehr. *Kosten:* Der Kasten bleibt eine Stufe tiefer. *Schließt aus:* Kasten und Combo aus dem Test nachzuführen.
6. **Wallet-Invariante: jeder Buchungsweg bumpt `child.ConcurrencyStamp`, ohne Ausnahme.** *Begründung:* Die Regel des Repos
   lautet für **abbuchende** Wege (`PositionProgressService.cs:827`, Kommentar); die Gutschrift braucht sie nicht, der Bump
   kostet nur einen Wiederholungsversuch eines parallelen Kaufs. Eine Ausnahme je Weg wäre die Stelle, an der sie vergessen
   wird. *Kosten:* keine messbaren. *Schließt aus:* nichts.
7. **Missionen, Auszeichnungen und eine schon geöffnete Tageskiste werden nicht neu bewertet; die Serie heilt sich selbst.**
   Belohnungen sind idempotent gebucht, die Kiste einmal je Tag. Die Serie dagegen wird **berechnet** (`ChildStreakAsync`
   liest `IsGoalMetAsync`) und folgt der Korrekturspur von selbst. *Kosten:* Das Kind sieht den Unterschied nicht als Buchung,
   der Hinweis nach der Bestätigung muss ihn nennen. *Schließt aus:* eine nachträglich erfüllte Mission.
8. **Wer entscheidet, wie oft.** Nur der Supervisor des Kindes (wie `SetStatusAsync`,
   `ExerciseFeedbackService.cs:154-165`), nie das Kind; eine Meldung wirkt höchstens einmal (Rücknahme-Marke, doppelte
   Bestätigung bucht nicht doppelt). **Keine** Wochenobergrenze in diesem Zuschnitt: Jede Bestätigung ist eine Handlung des
   Vaters, und eine Grenze träfe zuerst das Kind, das recht hat. *Kosten:* viele Meldungen kosten dem Vater Zeit.
9. **Reihenfolge.** B-627 wird **nach** B-624, B-625 und B-626 (alle `geschaetzt`) geschätzt oder gebaut und am Rest gemessen:
   Nehmen diese die Streng-Fälle vorab weg, schrumpft der Bedarf. *Kosten:* B-627 wartet in der Reihe, ohne Sperre.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. Eine Meldung zu einer Test-Antwort speichert beim Absenden den Bezug zu **dieser** Antwort (Id des `TestItemResult`),
   nicht nur zur Karte; gibt es keine gespeicherte Antwort, bleibt der Bezug leer. *Rot, wenn ein Integrationstest nach dem
   Absenden keinen Bezug in der Zeile findet, oder wenn der Bezug erst beim Bestätigen gesucht wird (Test: zwischen Meldung
   und Bestätigung beantwortet das Kind die Karte erneut, gebucht wird die gemeldete).*
2. Der Supervisor kann eine Meldung als „berechtigt“ **oder** „abgewiesen“ erledigen; `Done` allein bleibt für Altbestand.
   *Rot, wenn der Vertrag keinen Ausgang kennt (`FeedbackStatus` bleibt zweiwertig).*
3. „Berechtigt“ rechnet den Versuch über die Korrekturspur neu (`CorrectItems`, `Passed`); `TestItemResult.WasCorrect` und der
   Originalbefund bleiben, eine andere Antwort desselben Kindes bleibt unverändert. *Rot, wenn das Ergebnis überschrieben wird
   oder die Umbuchung alle Antworten zur Karte trifft.*
4. Die Pflicht und die Missionszahl lesen `Passed` über den Helfer. *Rot, wenn `PositionProgressService.cs:204` oder
   `MetricsService.cs:36` weiter `t.Passed` liest (Test: Pflicht der Periode nach der Korrektur erfüllt).*
5. Hing ein gebuchter Malus nur an diesem nicht bestandenen Test, entsteht eine **positive `GoalPenalty`-Gegenbuchung** in der
   Höhe der Strafzeile; die `PositionGoalPenalty`-Zeile bleibt, `ConcurrencyStamp` ist gebumpt, die Gegenbuchung zählt nicht in
   `WeekCoinsAsync`. *Rot, wenn die Zeile gelöscht wird (dann bucht die Abrechnung den Malus neu), der Stamp gleich bleibt
   oder die Gegenbuchung als verdient gezählt wird.*
6. Hing der Malus an etwas anderem (zweiter, ebenfalls nicht bestandener Versuch), bleibt er. *Rot, wenn die Umbuchung den
   Malus blind aufhebt.*
7. War die Periode ohne Strafe, aber ohne Ziel-Belohnung offen, bucht die Bestätigung die Belohnung einmal nach. *Rot, wenn
   eine zweite Bestätigung eine zweite Belohnung schreibt (`PositionGoalReward` ist der Schlüssel).*
8. Dieselbe Meldung zweimal bestätigt bucht nicht doppelt. *Rot, wenn ein zweiter Aufruf einen zweiten Eintrag schreibt.*
9. Eine Test-Meldung des Kindes ändert für sich nichts; nur die Bestätigung des Supervisors dieses Kindes wirkt, ein
   fremder Supervisor und das Kind selbst erhalten `403`. *Rot, wenn die Rollenprüfung fehlt.*
10. **Absichtserklärung:** Eine Meldung ohne Bestätigung ändert weiter nichts (B-623 Entscheidung 6). Der Ausgangszustand
    erfüllt das schon; rot würde nur, wer die Meldung selbst wirken ließe.
11. **Absichtserklärung:** Der Kasten der Karte wird durch die Bestätigung nicht zurückgebucht. Der Ausgangszustand erfüllt es
    schon; rot würde nur, wer `PositionItemProgress` in die Umbuchung zieht.

## Verlauf

- **2026-11-17** — angelegt als Folge-Story aus B-623, Entscheidung 6.
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): B-623 ist gebaut, aber die Meldung kennt die Antwort nicht
  (kein Bezug, beim Üben von Nicht-Vokabel-Karten nicht gespeichert); Malus hängt nur am nicht bestandenen Test, eine
  Rückerstattung gibt es nicht. Acht offene Punkte, alle mit Empfehlung, keine entschieden (Münz-/Malus-Regeln).
- **2026-11-20** — gegrillt (Vorbereitungslauf, Sonnet, auf Anweisung des Nutzers „Entscheide die offenen Fragen nach deiner Empfehlung“; unbestätigt): nur die gemeldete Antwort, Bezug beim Absenden eingefroren; zuerst nur der Test; Malus-Rücknahme als positive `GoalPenalty`-Gegenbuchung (Konvention `WalletService.cs:43-48`), Strafzeile bleibt; Korrekturspur statt Überschreiben; Kasten wird nicht zurückgebucht; Serie heilt sich selbst. Akzeptanzkriterien final.
