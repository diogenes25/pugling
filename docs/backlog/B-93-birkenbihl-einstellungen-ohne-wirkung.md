---
tags: [typ/story, status/idee, bereich/backend, rolle/supervisor]
aliases: [RequireTypedTest bei Birkenbihl, Einstellung ohne Wirkung, Birkenbihl in der Klausur]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/backlog/B-78-birkenbihl-dekodierung-erreicht-kind-nicht.md (pugling-reviewer, Befund 4 + Notiz)
unverifiziert: false
---

# B-93 · Zwei Birkenbihl-Einstellungen, die lautlos nichts tun

Seit [B-78](B-78-birkenbihl-dekodierung-erreicht-kind-nicht.md) ist `BirkenbihlExerciseType.IsTypedStage`
konstant `false` — richtig, denn die Methode lernt durch Lesen. Zwei Folgen davon sind aber Einstellungen,
die man **setzen kann** und die dann **nichts tun**; genau die Fehlerklasse, gegen die im selben Controller
`ThresholdProblem` und `TimeSlotProblem` ausdrücklich existieren.

1. **`RequireTypedTest` ist für diesen Typ unerfüllbar.** `PositionPracticeController` wertet nur, wenn
   `typed || !pos.RequireTypedTest` — bei konstant `typed == false` wertet eine Birkenbihl-Position mit
   `requireTypedTest: true` also **nie**. Auffallen würde es niemandem: bei `ExerciseCheckMode.None`
   entscheidet das Ziel über Übungsrunden, nicht über Tests. Kein Bestandsschaden (der Seed setzt es für die
   Birkenbihl-Position nicht).
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

## Offene Punkte

1. Punkt 1: Prüfung beim Anlegen/Ändern der Position (`requireTypedTest: true` für einen Typ, der nie
   getippt ist, → `400`)? Empfehlung: ja, analog `ThresholdProblem` — dieselbe Begründung, dieselbe Stelle.
   Braucht eine Auskunft „hat dieser Typ überhaupt eine getippte Stufe" statt einer Prüfung je Stufe.
2. Punkt 2: Trägt der Positions-Test die Dekodierung mit, oder verweigert eine Position dieses Typs den
   Abschlusstest? Empfehlung: mittragen — die Klausur soll nicht weniger zeigen als die Übung; „kein Test
   für diesen Typ" wäre die größere Änderung und nähme dem Vater eine Kontrollmöglichkeit.

## Verlauf

- **2026-08-04** — angelegt aus dem `pugling-reviewer`-Befund zum B-78-Bau (Befund 4 und die Klausur-Notiz).
  Beide Punkte am Code belegt (der Reviewer nennt `PositionPracticeController.cs:337` und
  `PositionProgressService.cs:94`), aber nicht am laufenden System nachgespielt. `prio: P3`: kein
  Bestandsschaden, kein Kind betroffen — es ist die Möglichkeit, sich selbst in die Irre zu stellen.
- **2026-08-04** — Punkt 2 mit Belegen unterfüttert (Facetten, Vertrag, `CheckMode`, UI-Bedingung) aus dem
  Code-Review der B-70/B-78/B-79-Commits. Die Stufe bleibt `idee`: es fehlen weiter User Story,
  „echte Lücke" und Akzeptanzkriterien. Eine dort begonnene Zweitstory wurde eingezogen, statt eine
  Dublette zu führen (die Id `B-94` bleibt darum unbenutzt).
