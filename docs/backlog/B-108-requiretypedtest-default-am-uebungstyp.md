---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [DefaultRequireTypedTest ohne Typ-Pruefung]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
nachgeschaut: ""
wartet_auf: ""
quelle: pugling-reviewer-Befund zu B-93 (2026-08-05) — `ExerciseControllerBase.cs:274` (Create) und `:346`
  (Update) lassen `DefaultRequireTypedTest = true` auf einer Uebung ohne getippte Stufe (z. B. Birkenbihl)
  zu, ohne `IExerciseType.SupportsRequireTypedTest` zu pruefen — dieselbe direkt daneben stehende Stelle
  (`:251`/`:327`) prueft `StageValidation.ProblemText` bereits
---

# B-108 · `DefaultRequireTypedTest` am Übungstyp selbst ungeprüft — dieselbe Fehlerklasse eine Ebene höher als B-93

**Umklassifiziert von `Aufräumen` auf `Defekt`** (siehe Entscheidung 3): der Fix ändert sichtbares
Verhalten — ein heute mit 200/201 angenommener Create/Update wird danach mit 400 abgewiesen. Das ist keine
„alles bleibt so grün wie vorher"-Reparatur.

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

## Entscheidungen

1. **Prüfung am Ort der Ursache ergänzen.** `ExerciseControllerBase.Create`/`Update` prüfen
   `body.DefaultRequireTypedTest` an derselben Stelle, an der `StageValidation.ProblemText` bereits gegen
   `body.DefaultStage` prüft (:251/:327), bevor der Wert auf die Entität geschrieben wird. Begründung:
   identisch zu `StageValidation` — zwei Schreibwege erreichen dieselbe Ausspielung, und die Fehlermeldung
   soll dort erscheinen, wo die Ursache gesetzt wird, nicht erst beim Supervisor. Kosten: zwei zusätzliche
   Zeilen je Action.
2. **In eine geteilte Stelle heben, nicht duplizieren.** Neue `public static class RequireTypedTestValidation`
   neben `StageValidation` (`Exercises/RequireTypedTestValidation.cs`), mit derselben `ProblemText(...)`-
   Signaturform. `PlanPositionsController.RequireTypedTestProblem` (bisher `private static`) wird durch den
   Aufruf der geteilten Methode ersetzt, nicht daneben stehen gelassen — sonst driftet die Fehlermeldung
   zwischen den beiden Controllern auseinander, exakt die Begründung, warum `StageValidation` selbst
   `public static` ist. Kosten: eine neue Datei, zwei Call-Sites geändert.
3. **`art` auf `Defekt` korrigiert.** Die Reparatur ändert sichtbares Verhalten am Create/Update-Endpunkt
   (ein heute angenommener Request wird künftig mit `400` abgewiesen) — das ist keine „alles bleibt grün"-
   Aufräumarbeit, sondern eine fehlende Validierung, die jetzt nachgezogen wird (dieselbe Fehlerklasse wie
   B-93, dort schon `Defekt`). Kosten: keine — die Story war ohnehin für den autonomen Lauf erreichbar
   (Freigabe 1 lässt `Defekt` **und** `Aufräumen` zu).

## Akzeptanzkriterien

1. `POST` und `PUT` einer Übung mit `defaultRequireTypedTest: true` auf einem Typ ohne getippte Stufe
   (z. B. Birkenbihl) antworten mit `400 validation_error` — nicht mehr mit `200`/`201`.
2. Die Fehlermeldung ist wortgleich mit der, die `PlanPositionsController` heute schon für denselben
   Fall liefert (geteilte Stelle, Entscheidung 2).
3. Der bestehende Positions-Weg (`PlanPositionsController`) bleibt unverändert grün — er ruft jetzt die
   geteilte Methode statt seiner eigenen privaten Kopie.
4. Eine rote Probe vor dem Fix belegt den heutigen Zustand (200/201 trotz ungültigem Default) mit der
   erwarteten/gemessenen Zahl im `## Verlauf`.

## Schätzung

**Größe S** (Anker: „`childId` aus dem Test-Pfad ziehen", B-01, mit einer neuen geteilten Datei plus zwei
Call-Sites etwas größer) — `wo: backend`. `migration: nein` (kein Entity ändert sich). `vertragsbruch: nein`
(400 `validation_error` ist an beiden Actions bereits als möglicher Status deklariert; kein neues
Responseschema).

### Testweg

Neuer Fall in den bestehenden `ExerciseControllerBase`-Tests je Typ ohne getippte Stufe (Birkenbihl) für
Create **und** Update — rot vor dem Fix, grün danach, Zahl im `## Verlauf`. Bestehende
`PlanPositionCrudTests`/`ExerciseTypeManifestTests` bleiben unverändert grün.

## Verlauf

- **2026-08-05** — angelegt aus dem `pugling-reviewer`-Befund zu B-93; Ist-Stand am Code belegt
  (`ExerciseControllerBase.cs:274`/`:346` ungeprüft, `PlanPositionsController.cs:118-121` als einzige
  Prüfstelle), darum gleich `ausformuliert`.
- **2026-08-06** — gegrillt und geschätzt: autonom, Nachtlauf-Freigabe 1 (die drei offenen Punkte decken
  `art: Aufräumen` **und** `Defekt` ab, beide sind freigegeben). Alle drei offenen Punkte in Entscheidungen
  überführt (Prüfung am Ort der Ursache, geteilte Stelle statt Duplikat, `art` auf `Defekt` korrigiert);
  Größe S/`backend` gesetzt.
- **2026-08-06** — abgenommen: `ExerciseControllerBase.Create`/`Update` prüfen `defaultRequireTypedTest`
  jetzt über die neue geteilte `RequireTypedTestValidation` (Sprint 2 von `docs/pm-sitzung-2026-08-06.md`,
  zusammen mit B-95). Neuer Test `BirkenbihlExerciseTests.DefaultRequireTypedTest_AufEinemTypOhneGetippteStufe_WirdAbgewiesen`
  rot vor dem Fix (1 Failed/8 Passed, erwartet `BadRequest`, gemessen `Created`), grün danach (9/9); volle
  Suite 749/749. `pugling-reviewer` bestätigt AK1–3 und die Konventionen, keine Blocker. Rollengang
  ausdrücklich ausgefallen: die neue Ablehnung trifft den Creator beim Anlegen einer Übung ohne getippte
  Stufe — ein seltener, gezielter Fall, kein Playwright-Pfad dafür vorhanden; Ersatz ist der rot→grün-Beleg
  über die echte API (Testweg) plus Reviewer.
