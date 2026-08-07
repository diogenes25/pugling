---
tags: [typ/story, status/abgenommen, bereich/lehrplan, rolle/supervisor]
aliases: [Stufenwächter am Include, PATCH-Stufenprüfung bedingt]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-08-04 der Commits 3be7409…f8b0c99 (B-70/B-78/B-79)
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-08-07"
wartet_auf: ""
---

# B-95 · Die Stufenprüfung beim PATCH einer Position hängt an einem `Include`, das niemand einfordert

Der Stufen-Wächter aus B-79 läuft im PATCH-Pfad nur, **wenn** die Übung mitgeladen ist: `pos.Exercise is { }
exercise && StageProblem(…)`. Fällt das `Include` irgendwann weg oder wird der Helfer wiederverwendet,
verschwindet die Prüfung lautlos, und eine ungültige Stufe wird wieder mit `200` gespeichert. Das ist genau die
Fehlerklasse, gegen die `PositionPlayService.ConfigOf` ausdrücklich gebaut ist. Heute ist nichts kaputt — das
`Include` steht.

## User Story

Als **Entwickler** möchte ich, dass eine Prüfung, die eine Geschäftsregel hält, nicht davon abhängt, ob eine
Query weit oben in der Datei ein `Include` trägt — damit ihr Wegfall den Compiler stört und nicht das Kind.

## Ist-Stand am Code

- Der bedingte Wächter: `backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:200` →
  `if (pos.Exercise is { } exercise && StageProblem(exercise, dto.Stage, dto.StageSchedule) is { } stageProblem)`.
- Er funktioniert heute, weil der Lade-Helfer die Übung mitnimmt: `PlanPositionsController.cs:58-60`
  (`FindAsync` mit `.Include(p => p.Exercise)`).
- Der POST-Pfad hat das Problem nicht: `PlanPositionsController.cs:143` ruft `StageProblem(exercise, …)` mit
  einer Übung, die dort ohnehin nicht-nullbar vorliegt.
- Die Regel dagegen ist im Repo schon formuliert — an anderer Stelle und mit Begründung:
  `Services/Shared/PositionPlayService.cs:105-110` nimmt bewusst die `Exercise` statt der Position, weil ein
  `pos.Exercise?.…` „ein vergessenes `Include` verschluckt … So fragt der Compiler."

## Die echte Lücke

Kein Fehlverhalten heute, sondern eine **Prüfung, die von einer stillen Voraussetzung lebt**. Das Muster für
die Reparatur existiert im selben Projekt bereits; hier fehlt nur seine Anwendung: `pos.Exercise` hart
einfordern (früher `NotFound()`, oder der Lade-Helfer gibt Position **und** Übung als nicht-nullbares Paar
zurück), sodass ein weggefallenes `Include` nicht in eine übersprungene Prüfung mündet.

## Offene Punkte

1. **`NotFound()` oder `!`?** Empfehlung: **`NotFound()`** früh nach dem Laden. Eine Position ohne Übung ist
   ein Datenzustand, den das Schema nicht kennt — aber ein `!` würde ihn zu einem 500 machen, wo ein 404 die
   ehrlichere Antwort ist. Kosten: eine Guard-Clause mehr.
2. **Auch andere Stellen?** Zu prüfen, ob im Supervisor-Bereich weitere `pos.Exercise is { }`/`?.`-Zugriffe
   eine Prüfung tragen. Nicht verifiziert — der Review nennt nur diese Stelle.
3. **Reicht ein Test?** Ein Test kann das Muster nicht bewachen (er wäre vor und nach der Änderung grün, weil
   das `Include` steht). Empfehlung: keinen Test erfinden, der nichts prüft; wenn die Regel mechanisch halten
   soll, ist ihr Ort ein `ConventionGuardTests`-Fall — und der ist eigenständig zu entscheiden, weil ein
   reflexives Tor über „nullable Zugriff in einer Prüfung" schnell mehr Ausnahmen als Regeln trägt.

## Entscheidungen

1. **`NotFound()` früh nach dem Laden, statt eines `!`.** `if (pos.Exercise is not { } exercise) return
   NotFound();` direkt nach dem Null-Check auf `pos`, vor den beiden Prüfungen. Begründung: eine Position
   ohne Übung ist ein Datenzustand, den das Schema nicht kennt (`Exercise.SeriesUnitId`/`PlanPosition.ExerciseId`
   sind beide nicht-nullbare FKs) — ein `!` würde ihn in einen 500 verwandeln, ein früher 404 ist die
   ehrlichere Antwort und bereits an der Action deklariert (`[ProducesResponseType(StatusCodes.Status404NotFound)]`
   steht schon). Kosten: eine Guard-Clause mehr, kein Vertragsbruch (404 war schon dokumentiert).
2. **Andere Stellen nicht mitgezogen.** Der Review nennt nur diesen bedingten Wächter; eine Suche nach
   weiteren `pos.Exercise is { }`/`?.`-Zugriffen im Supervisor-Bereich ist eine eigene Frage und bleibt
   außerhalb dieser Story (kein Fund, keine neue Story nötig — offener Punkt 2 war ohnehin nur „zu prüfen",
   keine Behauptung).
3. **Kein neuer Wächter-Test.** Ein Test, der `Include` entfernt und rot werden lässt, wäre selbst der
   Beweis für den Fix, nicht eine zusätzliche Zusicherung — und ein `ConventionGuardTests`-Fall über
   „nullable Zugriff in einer Prüfung" trägt schnell mehr Ausnahmen als Regeln (offener Punkt 3). Bleibt
   unentschieden zurückgestellt, nicht gebaut.

## Akzeptanzkriterien

1. Die Stufenprüfung im PATCH-Pfad ist nicht mehr an eine `null`-Bedingung geknüpft: das Entfernen des
   `Include` in `FindAsync` bricht die **Kompilierung** oder führt zu einer klaren Fehlerantwort — nicht zu
   einer übersprungenen Prüfung.
2. Die bestehenden Fälle bleiben grün (`PlanPositionCrudTests`, insbesondere der Stufen-Fall aus B-79) — bei
   `art: Aufräumen` ändert sich für niemanden ein Verhalten.

## Schätzung

**Größe XS** (Anker: „`childId` aus dem Test-Pfad ziehen", B-01) — eine Guard-Clause in einer bestehenden
Action, `wo: backend`, kein neuer Test. `migration: nein`/`vertragsbruch: nein`.

### Testweg

`PlanPositionCrudTests` (bestehend, insbesondere der B-79-Stufen-Fall) bleibt grün — kein neuer Test, da
Entscheidung 3 keinen Wächter für das Muster selbst baut.

## Verlauf

- **2026-08-04** — angelegt aus dem Code-Review der B-70/B-78/B-79-Commits; Ist-Stand am Code belegt
  (bedingter Wächter, `FindAsync`, POST-Gegenstück, die formulierte Regel in `ConfigOf`), darum gleich
  `ausformuliert`.
- **2026-08-06** — gegrillt und geschätzt: autonom, Nachtlauf-Freigabe 1 (`art: Aufräumen`). Alle drei
  offenen Punkte in Entscheidungen überführt (NotFound() früh, keine weiteren Stellen mitgezogen, kein
  neuer Wächter).
- **2026-08-06** — abgenommen: `PlanPositionsController.Update` fordert `pos.Exercise` jetzt hart ein
  (Sprint 2 von `docs/pm-sitzung-2026-08-06.md`, zusammen mit B-108). Suite 749/749 grün,
  `pugling-reviewer` bestätigt (keine Verhaltensänderung für heutige Aufrufer, da `Exercise` heute nie
  null ist — die Guard-Clause ist Verteidigung gegen einen zukünftigen Fehler). Rollengang ausdrücklich
  ausgefallen: keine neue Fähigkeit, keine sichtbare Oberfläche — Ersatz ist die volle Suite plus der
  Reviewer-Lauf.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob `PlanPositionsController.Update` `pos.Exercise`
  weiterhin hart einfordert (`NotFound()`) statt an ein bedingtes `is { }` zu hängen — hält
  (`PlanPositionsController.cs:202-206`). Kein Fund.
