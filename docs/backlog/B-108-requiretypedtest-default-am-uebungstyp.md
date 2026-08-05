---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [DefaultRequireTypedTest ohne Typ-Pruefung]
status: idee
prio: P3
art: Aufräumen
quelle: pugling-reviewer-Befund zu B-93 (2026-08-05) — `ExerciseControllerBase.cs:274` (Create) und `:346`
  (Update) lassen `DefaultRequireTypedTest = true` auf einer Uebung ohne getippte Stufe (z. B. Birkenbihl)
  zu, ohne `IExerciseType.SupportsRequireTypedTest` zu pruefen — dieselbe direkt daneben stehende Stelle
  (`:251`/`:327`) prueft `StageValidation.ProblemText` bereits
unverifiziert: true
---

# B-108 · `DefaultRequireTypedTest` am Übungstyp selbst ungeprüft — dieselbe Fehlerklasse eine Ebene höher als B-93

[B-93](B-93-birkenbihl-einstellungen-ohne-wirkung.md) hat `RequireTypedTest` an der **Position**
abgesichert (`PlanPositionsController`, `RequireTypedTestProblem`). Der Creator kann aber schon an der
**Übung selbst** `DefaultRequireTypedTest: true` setzen, ohne dass `ExerciseControllerBase` das gegen
`IExerciseType.SupportsRequireTypedTest` prüft — kein heutiger Schaden (jede Position, die diesen
Default erbt, wird beim Anlegen ohnehin über den *effektiven* Wert abgewiesen), aber die Quelle des
Fehlers bliebe unbemerkt: der Creator bekäme den 400 nie an der Stelle, wo die Einstellung tatsächlich
herkommt.

Noch nicht ausformuliert, ob sich der zusätzliche Aufwand lohnt (die Position fängt es ja ohnehin ab)
oder ob es bei "Position ist die eine Prüfstelle" bleiben soll.
