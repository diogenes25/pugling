---
tags: [typ/story, status/ausformuliert, bereich/lehrplan, rolle/supervisor]
aliases: [Stufenwächter am Include, PATCH-Stufenprüfung bedingt]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Code-Review 2026-08-04 der Commits 3be7409…f8b0c99 (B-70/B-78/B-79)
grund: ""
ersetzt_durch: []
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

## Akzeptanzkriterien

1. Die Stufenprüfung im PATCH-Pfad ist nicht mehr an eine `null`-Bedingung geknüpft: das Entfernen des
   `Include` in `FindAsync` bricht die **Kompilierung** oder führt zu einer klaren Fehlerantwort — nicht zu
   einer übersprungenen Prüfung.
2. Die bestehenden Fälle bleiben grün (`PlanPositionCrudTests`, insbesondere der Stufen-Fall aus B-79) — bei
   `art: Aufräumen` ändert sich für niemanden ein Verhalten.

## Verlauf

- **2026-08-04** — angelegt aus dem Code-Review der B-70/B-78/B-79-Commits; Ist-Stand am Code belegt
  (bedingter Wächter, `FindAsync`, POST-Gegenstück, die formulierte Regel in `ConfigOf`), darum gleich
  `ausformuliert`.
