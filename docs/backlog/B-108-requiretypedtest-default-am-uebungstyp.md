---
tags: [typ/story, status/ausformuliert, bereich/backend, rolle/creator]
aliases: [DefaultRequireTypedTest ohne Typ-Pruefung]
status: ausformuliert
prio: P3
art: Aufräumen
quelle: pugling-reviewer-Befund zu B-93 (2026-08-05) — `ExerciseControllerBase.cs:274` (Create) und `:346`
  (Update) lassen `DefaultRequireTypedTest = true` auf einer Uebung ohne getippte Stufe (z. B. Birkenbihl)
  zu, ohne `IExerciseType.SupportsRequireTypedTest` zu pruefen — dieselbe direkt daneben stehende Stelle
  (`:251`/`:327`) prueft `StageValidation.ProblemText` bereits
---

# B-108 · `DefaultRequireTypedTest` am Übungstyp selbst ungeprüft — dieselbe Fehlerklasse eine Ebene höher als B-93

## User Story

Als Creator möchte ich eine Rückmeldung bekommen, wenn ich `DefaultRequireTypedTest: true` an einer
Übung ohne getippte Stufe setze, damit ich den Fehler dort sehe, wo ich ihn gemacht habe — nicht erst,
wenn später ein Supervisor eine Position dafür anlegt.

## Ist-Stand am Code

- `ExerciseControllerBase.cs:251` (Create) und `:327` (Update) prüfen `body.DefaultStage` über
  `StageValidation.ProblemText(registry.ByKey(TypeKey), …)` — dieselbe geteilte Prüfstelle wie
  `PlanPositionsController.cs:109` (`StageValidation` ist bewusst `public static`, weil zwei
  Schreibwege dieselbe Ausspielung erreichen).
- `ExerciseControllerBase.cs:274` (Create) und `:346` (Update) setzen `DefaultRequireTypedTest`
  dagegen ungeprüft durch.
- Die einzige Prüfung ist `RequireTypedTestProblem` in `PlanPositionsController.cs:118-121`, gegen
  `IExerciseType.SupportsRequireTypedTest` (`IExerciseType.cs:117`; Default `true` in
  `ExerciseTypeBase.cs:64`; `false` für Birkenbihl in `BuiltInExerciseTypes.cs:153`).
- Sie greift **zweimal**, beide Male gegen den materialisierten
  `effectiveRequireTypedTest = dto.RequireTypedTest ?? exercise.DefaultRequireTypedTest`
  (Create: `PlanPositionsController.cs:158-159`; Update: `:222`, nur bei explizitem `dto.RequireTypedTest`).
- `PlanPosition.RequireTypedTest` (`PlanPositionEntities.cs:74`) ist ein **materialisierter Snapshot**
  zum Anlegezeitpunkt der Position, keine live abgeleitete Ableitung von `Exercise.DefaultRequireTypedTest`
  — eine spätere Änderung am Exercise-Default wirkt sich also nicht rückwirkend auf schon angelegte
  Positionen aus.

## Die echte Lücke

Ein Creator kann `DefaultRequireTypedTest: true` an einer Übung ohne getippte Stufe (z. B. Birkenbihl)
setzen — die API nimmt es mit 200/201 an. **Heutiger Schaden: keiner**, das ist am Code verifiziert:
jede *neue* Position, die diesen Default erbt, wird beim Anlegen über `effectiveRequireTypedTest`
korrekt mit `400` abgewiesen. Aber der `400` erscheint beim **Supervisor**, an einer Stelle, die mit dem
fehlerhaften Default nichts zu tun hat — der Creator, der ihn gesetzt hat, bekommt nie eine Rückmeldung,
außer ein Supervisor versucht später, genau diese Übung zu verplanen.

## Offene Punkte

1. Lohnt sich eine zusätzliche Prüfung am Ort der Ursache (`Exercise.DefaultRequireTypedTest` in
   `ExerciseControllerBase`), obwohl die Wirkung (Position) schon abgesichert ist? — **Empfehlung: ja**,
   dieselbe Begründung wie bei `StageValidation`: zwei Schreibwege erreichen dieselbe Ausspielung: hier
   ist zwar ein Weg schon geschlossen, aber die Fehlermeldung landet an der falschen Stelle.
2. Falls ja: die Prüfung in `ExerciseControllerBase` duplizieren oder in eine geteilte Stelle heben
   (Vorbild `StageValidation`, bewusst `public static` aus genau diesem Grund)? — **Empfehlung: teilen**,
   sonst driftet die Fehlermeldung zwischen den beiden Controllern auseinander.
3. `art` steht auf `Aufräumen`, der Inhalt ist aber eher eine Frage/ein Defekt (fehlende Prüfung nach dem
   `StageValidation`-Präzedenzfall) — auf welchen Wert korrigieren?
