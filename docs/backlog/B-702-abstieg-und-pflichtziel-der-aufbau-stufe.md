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
wartet_auf: Entscheidung des Nutzers (Malus-Regel und Pflichtziel einer gestuften Position)
---

# B-702 · Gestufte Position: Abstieg ohne Malus und Pflichtziel auf der aktuellen Stufe

Teil 3 von 5 aus [B-536](B-536-aufbau-uebungen-statt-versagen.md). Der Teil, der den **Stick** berührt: wann steigt
das Kind auf eine Vorstufe ab, und was verlangt das Pflichtziel einer gestuften Position – und damit, wann kostet
ein Verpasst Münzen. Beides ändert die Malus-Regeln des Projekts. Der Vorbereitungslauf vom 2026-11-19 trifft diese
Entscheidung darum **nicht**; sie liegt als Frage mit Empfehlung beim Nutzer. Ohne sie bleibt die Story auf `gegrillt`
und wird nicht geschätzt.

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

## Offene Frage an den Nutzer

Diese beiden Punkte wurden am 2026-11-14 als Stakeholder-Entscheidung ins B-536 geschrieben; sie ändern die
Malus-Regel und sind seither nicht vom Nutzer bestätigt. Der Vorbereitungslauf übernimmt sie **nicht** als
Entscheidung, sondern führt sie als Empfehlung.

1. **Abstieg ohne Malus (B-536 Entscheidung 3, zweiter Halbsatz).** Nach zwei Tests unter 50 % steigt das Kind eine
   Stufe ab, ohne Malus für das Scheitern an der zu hohen Stufe. *Empfehlung:* ja, aber der Abstieg selbst bucht
   nichts; ein **verpasster Zeitraum** kostet weiter `PenaltyCoins`. Damit entsteht keine Ausnahme im Malus-Pfad,
   nur eine Änderung dessen, was „Ziel" ist (Frage 2). *Kosten der Gegenseite:* ohne Abstieg bleibt das Kind an der
   zu schweren Stufe hängen, die Pflicht ist unerreichbar, der Malus läuft.
2. **Das Pflichtziel einer gestuften Position ist die aktuelle Stufe (B-536 Entscheidung 4).** *Empfehlung:* ja, mit
   Gegengewicht: ein Test der Vorstufe erfüllt das Ziel der Periode, **zahlt aber die Zielpunkte nur zur Hälfte**
   und zählt nicht für Serie und Wochentruhe. Ob und wie stark ein Test der Vorstufe weniger zahlt, ist eine
   Münz-Ökonomie-Entscheidung und gehört dem Nutzer; die schlichte Fassung „zählt wie jeder Test" lässt zu, dass ein
   Kind den Stick mit der leichtesten Stufe dauerhaft umgeht. *Kosten der Gegenseite:* ein Pflichtziel auf der Endstufe sperrt die Woche (Befund von B-535).
3. **Wie lange darf ein Kind auf einer Vorstufe bleiben, ohne dass der Vater es merkt?** *Empfehlung:* die
   Vater-Auswertung zeigt je gestufte Position die aktuelle Stufe und die Zeit auf ihr (siehe B-704); eine harte
   Frist gibt es nicht. Gehört zur Rollenfrage, weil das Leitbild vom 2026-11-19 den Vater zum Belohner macht
   ([B-681](B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md)).

## Entscheidungen

Keine. Stand: alle drei Punkte offen, mit Empfehlung. Sobald der Nutzer entschieden hat: Entscheidungen nummerieren
(Begründung, Kosten), `wartet_auf` leeren, dann geschätzt (Größe voraussichtlich L: Zählung der Fehlversuche,
Abstieg, Änderung an `IsGoalMetAsync` und den Auswertungen, Migration für den Zähler).

## Akzeptanzkriterien (Entwurf, nach der Entscheidung final)

1. Zwei aufeinanderfolgende nicht bestandene Tests auf einer Stufe über 1 setzen `CurrentBuildLevel` um eins herab; ein
   bestandener Test dazwischen setzt die Zählung zurück. Rot macht ihn: eine Zählung, die nicht auf „in Folge"
   prüft.
2. Der Abstieg bucht keinen `GoalPenalty`-Eintrag. Rot macht ihn: ein Abstieg im Malus-Pfad.
3. Ein verpasster Zeitraum einer gestuften Position kostet `PenaltyCoins` wie bisher (Ergebnis von Frage 2). Rot
   macht ihn: ein Ziel, das gestufte Positionen von der Abrechnung ausnimmt.
4. Auf Stufe 1 gilt das Ziel der Periode nach einem bestandenen Test der Stufe 1 als erreicht, wenn der Nutzer
   Frage 2 so entscheidet. Rot macht ihn: ein `IsGoalMetAsync`, das die Stufe ignoriert.
5. Die Migration nimmt vorhandene Positionen mit Zähler 0 mit.

## Verlauf

- **2026-11-19** — Teil von B-536, geteilt im Vorbereitungslauf (Sonnet). Bewusst **nicht** entschieden, weil die
  Punkte die Malus-Regel und die Münz-Ökonomie berühren; Stufe `gegrillt` mit offener Frage, `wartet_auf` gesetzt.
