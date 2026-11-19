---
tags: [typ/story, status/gegrillt, bereich/api, bereich/gamification, bereich/lehrplan, rolle/student, rolle/supervisor]
aliases: [Abstieg ohne Malus, Pflichtziel der aktuellen Stufe, Stufen und Stick]
status: gegrillt
prio: P1
art: Wunsch
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: B-536
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-702 · Gestufte Position: Abstieg ohne Malus und Pflichtziel auf der aktuellen Stufe

Teil 3 von 5 aus [B-536](B-536-aufbau-uebungen-statt-versagen.md). Der Teil, der den **Stick** berührt: wann steigt das Kind auf eine Vorstufe ab, und was verlangt das Pflichtziel einer gestuften Position – und damit, wann kostet ein Verpasst Münzen. Der Nutzer hat am 2026-11-20 delegiert („Entscheide die offenen Fragen nach deiner Empfehlung“); die Entscheidungen unten sind stellvertretend und unbestätigt. Geschätzt wird die Story als Nächstes.

## User Story

Als *Sohn* möchte ich nach zwei misslungenen Tests auf die leichtere Stufe zurück, ohne dafür Münzen zu verlieren,
und als *Vater* möchte ich, dass die Pflicht der Woche die Stufe verlangt, auf der mein Kind gerade steht, damit
die Pflicht erreichbar bleibt und der Stick trotzdem gilt, wo das Kind nur nicht übt.

## Ist-Stand am Code

- Der Malus bucht `PositionProgressService.SettleMissedAsync` (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:755-830`):
  je geschlossene Periode ohne erreichtes Ziel `PenaltyCoins` als negativer `GoalPenalty`-Eintrag, idempotent über
  `PositionGoalPenalty`; „Ziel erreicht" entscheidet allein `IsGoalMetAsync` (`:195`).
- `IsGoalMetAsync` kennt keine Stufe und misst bestandene Versuche der Position
  (`TestAttempt.PlanPositionId`, `backend/Pugling.Api/Models/StudyPlanEntities.cs:176`).
- Der Durchgang der Pflicht ist `Passed = ScorePercent >= GoalThreshold` (`PositionTestsController.cs:460`).
- Verwandt, schon gebaut: der Pfad „Nacharbeit" (B-635) lässt das Kind je Karte die Hilfe schrittweise einschalten
  und zählt eine Aufgabe erst, wenn sie frei getippt stimmt (`PlanPosition.Remediation`,
  `backend/Pugling.Api/Models/PlanPositionEntities.cs:131-135`). Das ist die *Hilfe je Karte*, kein Abstieg der
  Stufe; B-536 Entscheidung 5 („kein Knopf ‚zu schwer‘ für das Kind") gilt dort nicht, weil Nacharbeit der Vater
  je Pflicht einschaltet.
- Nicht gefunden: eine Zählung „zwei Tests unter 50 % in Folge" je Position.

## Offene Punkte

Alle Punkte entschieden; die Empfehlung steht durchgestrichen daneben.

1. ~~**Abstieg ohne Malus (B-536 Entscheidung 3, zweiter Halbsatz).** Nach zwei Tests unter 50 % steigt das Kind eine Stufe ab, ohne Malus für das Scheitern an der zu hohen Stufe. *Empfehlung:* ja, aber der Abstieg selbst bucht nichts; ein **verpasster Zeitraum** kostet weiter `PenaltyCoins`. Damit entsteht keine Ausnahme im Malus-Pfad, nur eine Änderung dessen, was „Ziel" ist (Frage 2). *Kosten der Gegenseite:* ohne Abstieg bleibt das Kind an der zu schweren Stufe hängen, die Pflicht ist unerreichbar, der Malus läuft.~~ → Entscheidung 1.
2. ~~**Das Pflichtziel einer gestuften Position ist die aktuelle Stufe (B-536 Entscheidung 4).** *Empfehlung:* ja, mit Gegengewicht: ein Test der Vorstufe erfüllt das Ziel der Periode, **zahlt aber die Zielpunkte nur zur Hälfte** und zählt nicht für Serie und Wochentruhe. Ob und wie stark ein Test der Vorstufe weniger zahlt, ist eine Münz-Ökonomie-Entscheidung und gehört dem Nutzer; die schlichte Fassung „zählt wie jeder Test" lässt zu, dass ein Kind den Stick mit der leichtesten Stufe dauerhaft umgeht. *Kosten der Gegenseite:* ein Pflichtziel auf der Endstufe sperrt die Woche (Befund von B-535).~~ → Entscheidung 2.
3. ~~**Wie lange darf ein Kind auf einer Vorstufe bleiben, ohne dass der Vater es merkt?** *Empfehlung:* die Vater-Auswertung zeigt je gestufte Position die aktuelle Stufe und die Zeit auf ihr (siehe B-704); eine harte Frist gibt es nicht. Gehört zur Rollenfrage, weil das Leitbild vom 2026-11-19 den Vater zum Belohner macht ([B-681](B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md)).~~ → Entscheidung 3.

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

## Entscheidungen

Entscheidungen 4 und 5 sind die Rahmenentscheidungen E und F (führend B-681).

1. **Der Abstieg selbst bucht nichts und liegt nicht im Malus-Pfad; Auslöser sind zwei aufeinanderfolgende Versuche des Kindes unter 50 %.** Nach zwei aufeinanderfolgenden Versuchen mit `ScorePercent` unter 50 auf einer Stufe über 1 sinkt `CurrentBuildLevel` um eins; ein Versuch ab 50 % (bestanden oder nicht) setzt die Zählung auf 0; Versuche des Vaters (`BySupervisor`) zählen nicht; unter Stufe 1 gibt es keinen Abstieg. Begründung: B-536 Entscheidung 3 – das Kind soll „gerade so schaffen“; unter 50 % heißt zu schwer, ein knapp verfehlter Test (70 % bei Schwelle 80 %) nicht; ein Zähler auf „nicht bestanden“ ließe bei hoher Schwelle zu früh absteigen. Der Abstieg läuft atomar im Abschlusspfad des Versuchs wie der Aufstieg (B-701 Entscheidung 4). Kosten: eine nullable Zählspalte an `PlanPosition` (Migration, Bestand 0); zwei schlechte Versuche am selben Tag (Tageslimit `MaxTestAttemptsPerDay` = 2) genügen für den Abstieg; ein verpasster Zeitraum kostet weiter `PenaltyCoins`. Verworfen: Abstieg nach „nicht bestanden“ (zu früh) und Abstieg allein von Hand des Vaters (B-536 Entscheidung 5 verlangt die Messung). Schließt aus: ein sofortiges Aufgeben für ein Kind, das nur knapp unter der Schwelle liegt; bleibt es dauerhaft bei 60 bis 79 %, erfüllt es das Ziel nicht und der Malus läuft – das sieht der Vater an der Zeit auf der Stufe (Entscheidung 3).
2. **Das Pflichtziel einer gestuften Position ist die aktuelle Stufe: Ein bestandener Test der aktuellen Stufe erfüllt das Ziel der Periode, auf einer Stufe unter der Endstufe zahlt es nur die halben Zielpunkte, und Serie, Truhe und Malus-Prüfung zählen den Test voll.** `IsGoalMetAsync` verlangt nicht die Endstufe. Die halbe Zahlung ist abgerundet, mindestens 1, wenn das Ziel überhaupt Punkte zahlt; maßgeblich ist der Versuch, der das Ziel der Periode erstmals erfüllt (eine Buchung je Periode, `PositionGoalReward`). Begründung: Sonst bleibt die Woche gesperrt (B-535). Die Empfehlung nahm Serie und Truhe aus; abgewichen davon, weil `IsGoalMetAsync` die eine Quelle für Malus, Zielpunkte, Serie (`IsCompleteDay`) und Truhenfreigabe ist (`PositionProgressService.cs:72`, `:468`, `:502`) – eine Ausnahme bräuchte ein zweites Prädikat „erfüllt, zählt aber nicht“ und ließe einen Tag ohne Malus die Serie reißen. Das Umgehen des Sticks mit der leichtesten Stufe begrenzt der Aufstieg: Jeder bestandene Test hebt die Stufe (B-701 Entscheidung 3), auf einer Vorstufe bleibt nur, wer nicht besteht – und der erfüllt das Ziel nicht (Rahmen F: Münzen hängen an der erfüllten Pflicht). Kosten: Der Münz-Gegenwert der Vorstufe ist nur die halbe Zielzahlung; die Endstufe zahlt voll; ein Kind kann Serie und Truhe auf einer Vorstufe halten. Verworfen: Serie und Truhe auf der Vorstufe auszunehmen (Empfehlung) und die schlichte Fassung „zählt wie jeder Test“ ohne Abschlag. Schließt aus: ein Pflichtziel auf der Endstufe, das die Woche sperrt, und ein Kind, das dauerhaft auf der Vorstufe Münzen zum vollen Preis holt.
3. **Wie lange ein Kind auf einer Vorstufe bleibt, zeigt die Vater-Auswertung je gestufte Position (aktuelle Stufe und Zeit auf ihr, B-704); es gibt keine harte Frist.** Begründung: Eine Frist wäre eine zweite Regel neben dem Abstieg; der Vater (Rahmen B: Leitplanken und Notbremse) soll es sehen und eingreifen können. Kosten: Ein Kind kann lange auf einer Vorstufe bleiben, wenn der Vater nicht hinsieht; die Zeit auf der Stufe braucht ein Datum an der Position (`CurrentBuildLevelSince`, im selben Migrationsschritt). Verworfen: automatische Rückstufung oder Meldung nach n Tagen. Schließt aus: eine Stufe, die das Kind verlässt, ohne dass es jemand merkt – es ist sichtbar, nicht erzwungen.
4. **Der Tagesplan steht ab 6 Uhr fest, und Malus kostet nur, was das Kind sehen konnte (Rahmen E, führend B-681).** Der Plan des Tages ist ab 6 Uhr Ortszeit eingefroren (B-644 Entscheidung 9); eine verpasste Pflichtaufgabe eines gesehenen Plans kostet genau einmal den Malus-Betrag des Kindes (B-644 Entscheidung 5); ein Tag, an dem nie ein Plan entstand, zählt wie eine Pause (B-644 Entscheidung 19). Begründung: Heute steht die Pflicht Wochen vorher an der Position (`PlanPosition.PenaltyCoins`); eine Pflicht, die erst heute früh entsteht, darf nicht rückwirkend oder unsichtbar kosten (Leitbild: „nicht unfair machen“). Kosten: Wird der Plan um 6 Uhr nicht erzeugt, entgeht dem Kind ein Pflichttag (als Pause beim Vater gemeldet); ein erzeugter Plan lässt sich nur über die Notbremse des Vaters ändern. Verworfen: Malus über die Pflicht an der Position auch für Planer-Kinder. Schließt aus: Strafe für Aufgaben, die das Kind nie angezeigt bekam oder die der Planer nach dem Erzeugen verändert hätte.
5. **Münzen hängen an der erfüllten Pflicht, nicht an Notenstufe oder Klausurnote (Rahmen F, führend B-681).** Zielpunkte, Truhe und Serie bleiben wie heute an der erfüllten Pflicht; in diesem Zyklus gibt es keine Münzprämie für eine Notenstufe oder eine Klausurnote, die Notenstufe ist Auszeichnung ohne Münzwert (`RewardPoints` 0). Begründung: Die Stufe ist aus Lerndaten geschätzt und keine harte Leistung wie ein bestandener Test, die Note wird von Hand eingetragen; eine Münzprämie darauf trüge Prognosefehler und Eintragsfehler in das Guthaben (Wallet-Invarianten). Die Probeklausur B-249 ist ebenfalls ohne Münzen entschieden. Kosten: Eine gute Klausur belohnt der Vater von Hand (`POST children/{}/points`, Manual); für die Stufe gibt es Ehre statt Geld. Verworfen: Klausurprämie in Münzen. Schließt aus: ein Kind, das für eine Eins in der Klausur automatisch Münzen bekommt.

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht.

1. Zwei aufeinanderfolgende Versuche des Kindes unter 50 % auf einer Stufe über 1 senken `CurrentBuildLevel` um eins; ein Versuch ab 50 % dazwischen setzt die Zählung zurück. *Rot, wenn* auf „nicht bestanden“ statt „unter 50 %“ gezählt wird oder der Zähler nie zurückgesetzt wird.
2. Versuche des Vaters (`BySupervisor`) zählen nicht. *Rot, wenn* die Zählung den Filter weglässt.
3. Auf Stufe 1 gibt es keinen Abstieg, die Stufe sinkt nie unter 1. *Rot, wenn* der Boden fehlt.
4. Der Abstieg bucht keinen `GoalPenalty` und keine Zielpunkte. *Rot, wenn* der Abstieg im Punkte- oder Malus-Pfad etwas schreibt.
5. Ein verpasster Zeitraum einer gestuften Position kostet `PenaltyCoins` wie bisher, einmal je Periode (`PositionGoalPenalty`). *Rot, wenn* gestufte Positionen von der Abrechnung ausgenommen werden.
6. Ein bestandener Test der aktuellen Stufe, auch der Stufe 1, erfüllt das Ziel der Periode. *Rot, wenn* `IsGoalMetAsync` die Endstufe verlangt oder die Stufe ignoriert.
7. Auf einer Stufe unter der Endstufe zahlt das Ziel die halbe Zielpunktzahl (abgerundet, mindestens 1 bei Punkten über 0), auf der Endstufe die volle. *Rot, wenn* alle Stufen denselben Betrag zahlen.
8. Ein Tag, an dem nur ein Test der Vorstufe das Ziel erfüllt, zählt als vollständig für Serie und Truhenfreigabe. *Rot, wenn* die Serie an einem solchen Tag reißt.
9. Zwei parallele Abschlüsse senken die Stufe nicht doppelt (idempotent über den Versuch). *Rot, wenn* der Abstieg ohne Ref-Gate oder Nebenläufigkeitsschutz läuft.
10. Die Migration nimmt vorhandene Positionen mit Zähler 0 und ohne Stufen-Datum mit. *Rot, wenn* die Spalten nicht nullbar sind oder Bestandszeilen verändert werden.

## Verlauf

- **2026-11-19** — Teil von B-536, geteilt im Vorbereitungslauf (Sonnet). Bewusst **nicht** entschieden, weil die
  Punkte die Malus-Regel und die Münz-Ökonomie berühren; Stufe `gegrillt` mit offener Frage, `wartet_auf` gesetzt.
- **2026-11-20** — Die offenen Punkte stellvertretend entschieden (Vorbereitungslauf, Sonnet, auf Anweisung des Nutzers, unbestätigt), Stufe bleibt `gegrillt`, `wartet_auf` geleert. Kern: Abstieg nach zwei Versuchen unter 50 % ohne Buchung; Ziel ist die aktuelle Stufe mit halben Zielpunkten auf Vorstufen, aber Serie und Truhe zählen voll (Abweichung von der Empfehlung, begründet); keine harte Frist, nur Sichtbarkeit.
