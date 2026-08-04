---
tags: [typ/story, status/ausformuliert, bereich/backend, rolle/supervisor]
aliases: [RequireTypedTest bei Birkenbihl, Einstellung ohne Wirkung, Birkenbihl in der Klausur]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/backlog/B-78-birkenbihl-dekodierung-erreicht-kind-nicht.md (pugling-reviewer, Befund 4 + Notiz)
---

# B-93 · Zwei Birkenbihl-Einstellungen, die lautlos nichts tun

Seit [B-78](B-78-birkenbihl-dekodierung-erreicht-kind-nicht.md) ist `BirkenbihlExerciseType.IsTypedStage`
konstant `false` — richtig, denn die Methode lernt durch Lesen. Zwei Folgen davon sind aber Einstellungen,
die man **setzen kann** und die dann **nichts tun**; genau die Fehlerklasse, gegen die im selben Controller
`ThresholdProblem` und `TimeSlotProblem` ausdrücklich existieren.

## User Story

Als **Vater** möchte ich, dass eine Einstellung, die ich an einer Position setzen kann, auch wirkt — und dass
der Server sie ablehnt, wenn sie für den Übungstyp unerfüllbar ist. Sonst stelle ich eine Pflicht scharf, die
nie greift, und merke es erst, wenn mein Kind wochenlang kein Ziel erreicht hat.

## Ist-Stand am Code

1. **`RequireTypedTest` ist für diesen Typ unerfüllbar.** `PositionPracticeController` wertet nur, wenn
   `typed || !pos.RequireTypedTest` — bei konstant `typed == false` wertet eine Birkenbihl-Position mit
   `requireTypedTest: true` also **nie**. Auffallen würde es niemandem: bei `ExerciseCheckMode.None`
   entscheidet das Ziel über Übungsrunden, nicht über Tests. Kein Bestandsschaden (der Seed setzt es für die
   Birkenbihl-Position nicht).

   Am 2026-08-04 gegen den heutigen Code nachgeprüft (die Zeilen des Reviewers stimmen noch):
   `Exercises/BuiltInExerciseTypes.cs:146` (`IsTypedStage(int stage) => false`, mit Doku-Absatz `:142-144`,
   der genau diese Frage schon aufwirft), `Controllers/Student/PositionPracticeController.cs:337`
   (`var scored = prog is not null && (typed || !pos.RequireTypedTest) && due && !alreadyScoredToday;`) und
   `Services/Shared/PositionProgressService.cs:94` (`IsGoalMetAsync` wertet bei `CheckMode.None` über
   Übungsrunden statt über bewertete Versuche — darum bleibt die unerfüllbare Einstellung unsichtbar).
2. **Im Positions-Test deckt die Karte auf und fragt nichts.** `TestItem` trägt für diesen Typ jetzt
   `reveal` (die natürliche Übersetzung), aber **keine** Dekodierung, und der Versuch bleibt ungewertet.
   B-78 Entscheidung 4 begründete den Verzicht auf `TestItem` damit, dass Birkenbihl „keine `/check`-Oberfläche
   hat" — der **Positions-Test** ist aber eine andere, erreichbare Oberfläche. Entweder trägt sie die
   Dekodierung mit, oder eine Birkenbihl-Position sollte gar keinen Abschlusstest anbieten.

   Belegt am 2026-08-04 (Code-Review der B-70/B-78/B-79-Commits, unabhängig nachgeprüft): Die geteilte
   Facetten-Projektion **führt** die Dekodierung (`Services/Shared/PositionPlayService.cs:143`, gefüllt
   `:186`) und die Autoren-Vorschau reicht sie durch (`Services/Creator/ExercisePreviewService.cs:115`) —
   allein die Klausur-Projektion lässt sie fallen (`Controllers/Student/PositionTestsController.cs:73-77`
   listet `f.Decoding` nicht auf), und im Vertrag fehlt das Feld ganz
   (`Pugling.Contracts/Student/TestDtos.cs:30-32`). Das Gegenargument steht schon im Code, für den Lesetext:
   „sonst wäre die Klausur die härtere Aufgabe bei weniger Material" (`frontend/src/sohn/SohnTest.tsx:150`,
   `f.Passage` wird deshalb mitgegeben). Reichweite: das Kind stolpert **nicht** hinein — Birkenbihl ist
   `ExerciseCheckMode.None` (`Exercises/BuiltInExerciseTypes.cs:123`) und die Arcade zeigt den Test-Einstieg
   nur bei `pos.testable` (`frontend/src/sohn/SohnHome.tsx:141`); erreichbar bleibt er über die API und den
   Vater-Pfad, denn `PositionTestsController.Start` prüft den `CheckMode` nicht.

   Wenn Punkt 2 als „Dekodierung mittragen" entschieden wird, ist das Feld **additiv** (kein Vertragsbruch);
   der Riegel-Weg („kein Test für diesen Typ") braucht zusätzlich einen eigenen Fehlercode in `ApiErrors`,
   damit der Aufrufer die Ursache maschinell unterscheiden kann.

Beides ist heute niemandem passiert, und beides ist ein Weg, auf dem eine Einstellung eine Wirkung
verspricht, die sie nicht hat.

## Die echte Lücke

Nicht „Birkenbihl ist falsch gebaut" — dass der Typ nie getippt ist, ist die **richtige** Entscheidung aus
B-78. Die Lücke ist, dass zwei Stellen diese Entscheidung nicht kennen: eine Einstellung, die für den Typ
unerfüllbar ist, wird trotzdem angenommen (Punkt 1), und eine Oberfläche, die es gibt, bekommt weniger Stoff
als die Übung (Punkt 2). Beide sind je ein Ein-Zeiler in der Wirkung, aber sie brauchen eine
Entscheidung *vorher*: ablehnen oder mittragen.

Die zweite Hälfte teilt dabei ihre Ursache mit
[B-96](B-96-showboth-stufe-ohne-mechanik.md): eine Stufe bzw. Einstellung, die angeboten und validiert wird,
aber im Spielpfad keine Entsprechung hat. Wer beide baut, sollte die Prüf-Hilfsfunktion einmal schreiben.

## Offene Punkte

1. Punkt 1: Prüfung beim Anlegen/Ändern der Position (`requireTypedTest: true` für einen Typ, der nie
   getippt ist, → `400`)? Empfehlung: ja, analog `ThresholdProblem` — dieselbe Begründung, dieselbe Stelle.
   Braucht eine Auskunft „hat dieser Typ überhaupt eine getippte Stufe" statt einer Prüfung je Stufe.
2. Punkt 2: Trägt der Positions-Test die Dekodierung mit, oder verweigert eine Position dieses Typs den
   Abschlusstest? Empfehlung: mittragen — die Klausur soll nicht weniger zeigen als die Übung; „kein Test
   für diesen Typ" wäre die größere Änderung und nähme dem Vater eine Kontrollmöglichkeit.

## Akzeptanzkriterien

Entwurf — final erst nach der Grill-Runde, weil beide Punkte je zwei zulässige Auflösungen haben:

1. `POST`/`PATCH` einer Position mit `requireTypedTest: true` auf eine Übung, deren Typ **keine** getippte
   Stufe kennt, wird mit `ProblemDetails` und maschinenlesbarem `code` abgewiesen — oder die Einstellung
   wirkt (Entscheidung zu Punkt 1). Ein stilles Annehmen ohne Wirkung gilt in keinem Fall als erfüllt.
2. Der Abschlusstest einer Birkenbihl-Position zeigt dieselbe Wort-für-Wort-Dekodierung wie die Übungskarte
   — oder eine Position dieses Typs verweigert den Abschlusstest mit eigenem Fehlercode (Entscheidung zu
   Punkt 2).
3. Je Punkt ein Integrationstest, der **vor** der Änderung rot war (Abnahmeform für `art: Defekt`).
4. Die Prüfung hängt nicht an einem `Include`, das niemand einfordert — dieselbe Auflage wie in
   [B-95](B-95-stufenwaechter-haengt-am-include.md).

## Verlauf

- **2026-08-04** — angelegt aus dem `pugling-reviewer`-Befund zum B-78-Bau (Befund 4 und die Klausur-Notiz).
  Beide Punkte am Code belegt (der Reviewer nennt `PositionPracticeController.cs:337` und
  `PositionProgressService.cs:94`), aber nicht am laufenden System nachgespielt. `prio: P3`: kein
  Bestandsschaden, kein Kind betroffen — es ist die Möglichkeit, sich selbst in die Irre zu stellen.
- **2026-08-04** — Punkt 2 mit Belegen unterfüttert (Facetten, Vertrag, `CheckMode`, UI-Bedingung) aus dem
  Code-Review der B-70/B-78/B-79-Commits. Eine dort begonnene Zweitstory wurde eingezogen, statt eine
  Dublette zu führen (die Id `B-94` bleibt darum unbenutzt).
- **2026-08-04** — **ausformuliert** (autonom getroffen, Nutzerauftrag „fahre gemäß deiner eigenen
  Einschätzung fort"). User Story, „Die echte Lücke" und ein Entwurf der Akzeptanzkriterien ergänzt; die
  Belege des Reviewers zu Punkt 1 gegen den **heutigen** Code nachgeprüft (`BuiltInExerciseTypes.cs:146`,
  `PositionPracticeController.cs:337`, `PositionProgressService.cs:94` — alle drei stimmen noch). Das Feld
  `unverifiziert` ist entfallen, weil es laut [README](README.md) nur auf `idee` gehört — es war die einzige
  Meldung des Index-Wächters. Die Akzeptanzkriterien bleiben ein **Entwurf**: beide Punkte haben je zwei
  zulässige Auflösungen (ablehnen oder mittragen), und diese Wahl gehört in die Grill-Runde.
