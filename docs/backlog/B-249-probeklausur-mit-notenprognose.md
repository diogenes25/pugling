---
tags: [typ/story, status/ausformuliert, bereich/klassenarbeit, bereich/auswertung, rolle/student, rolle/supervisor]
aliases: [Probeklausur, Notenprognose]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/knowunity-vergleich-plan.md#wo-knowunity-weiter-ist
grund: ""
ersetzt_durch: []
---

# B-249 · Eine Probeklausur über das Bündel einer Klassenarbeit, mit Notenprognose

Knowunity bietet vollständige, bewertete Probeklausuren, deren Ergebnis in den Plan zurückfließt. Wir
haben einen Abschlusstest **je Position** und an der `Klassenarbeit` das Feld `Grade` (1,0–6,0) für die
echte Note, aber keine Probe über das ganze Bündel einer Arbeit. Die Idee: das Kind schreibt vor dem
Termin eine Probeklausur aus den Übungen der Klassenarbeit (strikt one-at-a-time wie der Klausur-Modus),
der Server rechnet das Ergebnis in eine Note um, und nach dem Eintragen der echten Note sieht der Vater
Prognose und Ergebnis nebeneinander. Hängt an [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md)
(Bündel-Auflösung, Sichtbarkeit beim Kind). Zu klären: Notenschlüssel (fest, je Schulart, je
Klassenarbeit einstellbar), ob die Probe Münzen bringt und wie oft sie wiederholt werden darf, ohne dass
Auswendiglernen der Reihenfolge die Prognose entwertet.

## User Story

Als *Schüler* möchte ich vor der Klassenarbeit eine Probe über deren Stoff schreiben und eine voraussichtliche Note sehen, damit ich weiß, wo ich stehe, und als *Vater*
möchte ich nach der echten Arbeit Prognose und Note nebeneinander sehen, damit ich der Prognose trauen oder sie ignorieren kann.

## Ist-Stand am Code

Gemessen am 2026-11-19 auf `main`; Pfade relativ zu `backend/Pugling.Api/`, wenn nicht anders genannt.

- **Der Test ist an eine Position gebunden.** `TestAttempt` trägt `StudyPlanId`, `PlanPositionId`, `StageValue`, `ScorePercent`, `Passed`
  (`Models/StudyPlanEntities.cs:170-203`); gespielt wird unter `…/study-plans/{planId}/positions/{positionId}/tests`
  (`Controllers/Student/PositionTestsController.cs:20`): `Start` (`:103`), `Next` (`:216`, eingefrorener Cursor, kein Zurück), `Answer` (`:252`),
  `Submit` (`:359`). Die Reihenfolge ist `attempt.Order` aus **einer** Übung (`:185`, `:233`). Eine Klausur über mehrere Übungen passt nicht in dieses
  Modell; ein Test über das Bündel einer Arbeit: nicht gefunden.
- **Strikt one-at-a-time ist da und wiederverwendbar:** Cursor in `TestAttempt.Order`/`Cursor`, Rückmeldung erst bei `Submit`
  (`PositionTestsController.cs:213-216`, `:481`); dazu die Tageskappe `TestAttemptsExhausted` (`:140-143`, `TestAttemptsLeftAsync`, gezählt wird der Start).
- **Bestehens-Grenze, aber keine Note.** `passPercent` aus `GoalThreshold` (Standard 80 %, `PositionTestsController.cs:453-460`). Ein Notenschlüssel
  (Prozent → 1,0–6,0): nicht gefunden – weder im Backend (`Notenschlüssel`/`PercentToGrade` ohne Treffer) noch in `frontend/src`.
- **Die echte Note hängt an der Klassenarbeit:** `Grade` und `GradeComment` (`Models/KlassenarbeitEntities.cs:82,84`), Wertebereich 1,0–6,0 geprüft in
  `KlassenarbeitenController.ValidateGrade` (`Controllers/Supervisor/KlassenarbeitenController.cs:46-47`); die Note wirkt nur in der Objective-Auswertung
  (`Services/Shared/ObjectiveEvaluationService.cs:89-95`) und in `repeat` (`:276`). `KlassenarbeitResponse` trägt keine Prognose (`Contracts/Supervisor/ClassTestDtos.cs`).
- **Die Bündel-Auflösung ist privat** (`KlassenarbeitenController.cs:316-327`, direkte Übungen plus Tag-Übungen) und für das Kind nicht erreichbar
  (Student-Endpunkt zur Klassenarbeit: nicht gefunden; das ist die Vorarbeit aus B-248).
- **Es gibt ein Vorbild für Läufe außerhalb einer Position:** `ExtraTrainingRun` mit Tag, Durchlauf und Form (`Models/GamificationEntities.cs:124-142`) im
  Bonus-Training (`Services/Student/ExtraTrainingService.cs:28`) – aber ein Lauf ist dort **eine** Übung.
- **Papier-Variante:** B-582 ist `verworfen` mit dem Grund, sie setze B-249 voraus und werde mit B-249 neu angelegt, falls diese gebaut ist.

## Die echte Lücke

Es fehlt nicht eine Auswertung, sondern ein **Test über mehrere Übungen**: das Test-Modell ist je Position, die Bündel-Auflösung liegt nur im Vater-Controller, und es gibt
weder Notenschlüssel noch einen Ort, an dem eine Prognose neben der echten Note steht. Die Entscheidungen von 2026-11-05 (keine Münzen, beliebig oft, jede Runde neu gemischt,
Prognose nur aus der ersten Runde je Tag) sind mit einem Kind kompatibel, das die Reihenfolge nicht auswendig lernt – aber nicht mit einer Probe, die dieselben Items zieht
(Offener Punkt 3). Hängt vollständig an B-248 (Student-Lesepfad, Bündel).

## Offene Punkte

Bereits entschieden (Nutzer 2026-10-23, Delegation 2026-11-05, siehe Verlauf): fester Default-Schlüssel, je Klassenarbeit durch den Vater änderbar; keine Münzen; beliebig oft
wiederholbar, jede Runde neu gemischt; Prognose zählt nur die erste Runde je Tag. Offen:

1. **Wo läuft die Probe?** Empfehlung: eigene Entität `ProbeRun` (Kind, Klassenarbeit, Tag, Reihenfolge-Cursor, Score) mit dem Cursor-Muster von `TestAttempt`, aber ohne
   Plan- und Positions-Bezug; nicht als Position in den Plan, sonst entstehen Pflicht, Malus und `GoalMet`. Schema-Eingriff mit Migration.
2. **Welche Items und wie viele?** Empfehlung: Stichprobe aus allen Bündel-Übungen (Obergrenze als Konfiguration, damit ein 300-Wörter-Bündel keine Dreistundenprobe wird), gleiche Stufe wie der
   Klausur-Modus (getippt, kein Hinweis, keine Lösung vor Abgabe).
3. **Wie bleibt die Prognose ehrlich, wenn jede Runde neu gemischt wird?** Empfehlung: die Stichprobe je (Kind, Klassenarbeit, Tag) deterministisch aus dem Tag ziehen und je Runde
   die **Reihenfolge**, nicht die Auswahl neu mischen; „erste Runde je Tag" zählt dann dieselbe Auswahl. Alternative: jede Runde neue Auswahl, Prognose als Mittel der ersten Runde je Tag.
   Entscheidung für das Grillen.
4. **Notenschlüssel.** Empfehlung: Tabelle in `appsettings.json` (wie `Scoring:TimeSlots`), Default Gymnasium-üblich, nullbare Überschreibung an der Klassenarbeit
   (Schema + Migration + `PatchSemantics` mit `ClearGradeScale`); Rundung auf Zehntel wie `Grade`. Welcher Default gilt (Schulart des Kindes?), sagt der Nutzer.
5. **Wo steht die Prognose?** Empfehlung: nur in der Auswertung des Vaters und beim Kind als Zahl mit „Prognose"-Beschriftung, nicht als Wert, der in die Objective-Auswertung
   (`ObjectiveEvaluationService.cs:89`) fließt – dort zählt nur die echte Note.
6. **Fließt das Ergebnis in den Plan zurück?** (Knowunity-Vorbild.) Empfehlung: nein in der ersten Fassung; die schwachen Items stehen im Probe-Ergebnis und können über
   B-584 (`from-words`, `PlanPositionsController.cs:275`) zur Wiederholungsposition werden – das ist ein Vaterhandgriff, keine Automatik. Berührt die Pflicht-Regeln, wenn automatisch: für den Nutzer.
7. **Papier-Variante** (B-582, verworfen): bleibt bis zum Bau von B-249 verworfen; nicht Teil dieser Story.
8. **Nur Typen mit automatischer Wertung.** Empfehlung: Typen mit `GradesAsSet`/Wertung wie im Positions-Test; Freitext-Typen ohne Serverwertung bleiben draußen.

## Akzeptanzkriterien (Entwurf)

1. Ein Kind startet eine Probe zu einer geplanten Klassenarbeit und bekommt Fragen aus direkten **und** Tag-Übungen; eine abgesagte oder geschriebene Arbeit lässt sich nicht proben.
   *Rot, wenn die Probe nur `KlassenarbeitExercise` liest oder `Status` ignoriert.*
2. Die Probe fragt strikt eine Frage nach der anderen, ohne Zurück, ohne Lösung vor Abgabe. *Rot, wenn der Cursor nicht eingefroren ist oder `Next`/`Answer` Lösungsfelder ausgibt
   (Lösungsfeld-Regel, `ConventionGuardTests`).*
3. Das Ergebnis enthält Prozent und Prognose-Note nach dem Schlüssel der Klassenarbeit; ohne eigenen Schlüssel gilt der Default. *Rot, wenn die Umrechnung fehlt oder die Überschreibung
   ignoriert wird.*
4. Die Prognose eines Tages zählt nur die erste Runde; eine zweite Runde am selben Tag ändert sie nicht. *Rot, wenn die letzte statt der ersten Runde zählt.*
5. Die Probe bucht keine Münzen, keinen Malus und berührt keine Pflicht-Periode. *Rot, wenn `ScoringService`, `GoalMet` oder `SettleClosedPeriodsAsync` aufgerufen wird.*
6. Trägt die Klassenarbeit eine echte Note, zeigt die Vater-Seite Prognose und Note nebeneinander; die Prognose fließt nicht in `ObjectiveEvaluationService`. *Rot, wenn die Objective-Auswertung
   eine Prognose als Note liest.*
7. Fremdes Kind und fremder Vater werden abgewiesen. *Rot, wenn die Routen ohne Betreuungs-Prüfung hängen.*
8. Ein Bündel ohne einen einzigen wertbaren Inhalt liefert `no_checkable_content` statt einer leeren Probe. *Rot, wenn eine leere Probe mit 0 % als Note 6 endet.*

## Verlauf

- **2026-10-23** — angelegt (Quelle: Knowunity-Vergleich, Etappe 2 des Übernahmeplans).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) (G-7): Der Notenschlüssel hat einen **festen Default** (ein üblicher Prozent-Schlüssel), den der Vater **je Klassenarbeit ändern** kann. Offen beim Ausformulieren: ob die Probe Münzen bringt und wie oft sie wiederholt werden darf, ohne dass das Auswendiglernen der Reihenfolge die Prognose entwertet. Hängt an B-248. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-22); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-11-05** — **Rest-Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05, [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): die Probeklausur bringt keine Münzen; beliebig oft wiederholbar, jede Runde neu gemischt, die Prognose zählt nur die erste Runde je Tag.
- **2026-11-13** — Backlog-Abgleich gegen den Code: besteht unverändert (kein Probeklausur-/Prognose-Endpunkt, kein Notenschlüssel an `KlassenarbeitEntities.cs:79-84`). Reihenfolge: nach [B-423](B-423-assistent-klassenarbeit-termin-und-stufenleiter.md) und B-248.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt – kein Probeklausur-/Prognose-Endpunkt und kein Notenschlüssel (`KlassenarbeitEntities.cs:79-84` trägt nur `ScheduledDate`/`Grade`/`GradeComment`); Reihenfolge nach B-423 und B-248 gilt weiter.
- **2026-11-15** — Prioritäts-Beleg von außen: RocketTutor meldet eine Probearbeit mit Punkten **und Note** zurück. Die Papier-Variante (Blatt drucken, Foto, Korrektur durch Vater bzw. Creator-Werkzeug) liegt als [B-582](B-582-probearbeit-auf-papier-mit-foto-korrektur.md) und baut auf dieser Story auf. Vergleich in `docs/rockettutor-vergleich-plan.md`. Keine Änderung an den Entscheidungen.
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): Ist-Stand am Code gemessen. Befund: das Test-Modell ist je Position (`TestAttempt`, eine Übung je Order), ein Test über mehrere Übungen und ein Notenschlüssel fehlen ganz; die Bündel-Auflösung ist privat im Vater-Controller. Acht offene Punkte mit Empfehlung, keine Entscheidung getroffen; Spannung zwischen „jede Runde neu gemischt" und ehrlicher Prognose (Offener Punkt 3). Überschneidungen: B-248 (Voraussetzung), B-582 (verworfen, hängt an dieser Story), B-680/B-683 (Prognose-Eichung).
