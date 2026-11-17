---
tags: [typ/story, status/geschaetzt, rolle/student, lerntechnik/vokabeln]
aliases: [Buchstabensalat bei mehreren Wörtern, Scramble je Wort]
status: geschaetzt
prio: P1
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-632
grund: ""
ersetzt_durch: []
---

# B-634 · Buchstabensalat auch bei Lösungen aus mehreren Wörtern, je Wort gemischt

Teil B aus [B-632](B-632-schultest-nacharbeiten-mit-leichter-schalten.md). In der Nacharbeit gibt es Lösungen aus
mehreren Wörtern: *Western Europe*, *instead of*, *as soon as*, *a family of four*. Der Buchstabensalat geht bisher
nur bei einem einzelnen Wort. Die Stufe soll bei jeder Aufgabe gleich aussehen.

## User Story

Als *Sohn* möchte ich auch bei einer Lösung aus mehreren Wörtern die Buchstaben jedes Wortes ordnen können, damit
die Hilfe dieselbe bleibt, egal wie lang die Lösung ist.

## Ist-Stand am Code

Gelesen am 2026-11-17 (`b1bf80c8`):

- `StageMechanics.IsScramblable` verlangt 2–12 Zeichen, nur Buchstaben oder Ziffern
  (`backend/Pugling.Api/Services/Shared/StageMechanics.cs:107-108`, `MaxScrambleLength = 12` in `:100`).
- Beim Lückentext liefert `LetterTileSource` die Kacheln nur, wenn die Lösung so ein Wort ist
  (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:211-212`). Sonst wird die Karte als Buchstabenkästen
  gespielt (`:206-208`). Bei der Vokabel ist es genauso (`TestStage.LetterScramble`,
  `backend/Pugling.Api/Models/StudyPlanEntities.cs:51-54`).
- Die Kacheln zeigt `frontend/src/components/LetterTiles.tsx`.

## Die echte Lücke

Lösungen aus mehreren Wörtern fallen von der Salat-Stufe auf die Buchstabenkästen zurück. Eine Stufe der Leiter
fehlt damit genau bei diesen Aufgaben.

## Entscheidungen

Aus B-632, Entscheidung 2: Die Buchstaben jedes Wortes werden für sich gemischt. Die Wortgrenzen bleiben stehen wie
bei den Buchstabenkästen. Die Grenze von 12 Zeichen gilt je Wort.

## Akzeptanzkriterien

1. Eine Lösung aus mehreren Wörtern, deren Wörter je 2–12 Buchstaben haben, wird auf der Salat-Stufe gespielt. Jedes
   Wort hat seine eigenen gemischten Kacheln, und die Wortgrenzen sind sichtbar.
2. Ein einzelnes Wort wird genau wie bisher gespielt.
3. Eine Lösung mit einem Wort über 12 Buchstaben oder mit anderen Zeichen als Buchstaben, Ziffern und Leerzeichen
   fällt wie bisher auf die Buchstabenkästen zurück.
4. Die Mischung verrät die Lösung nicht. Sie ist je Sitzung deterministisch, wie heute.

## Schätzung

**S.** Die Regel in `StageMechanics`, die Kachelquelle je Wort und die Darstellung je Wort in `LetterTiles`.

- **`migration: nein`.**
- **`vertragsbruch: nein`:** Die Kachelliste der Karte wird additiv um Wortgrenzen erweitert. Die genaue Form
  entscheidet der Bau.
- **Risiko:** Die Regel gilt für alle Typen. Bestehende Vokabel- und Lückentext-Karten mit einem Wort dürfen sich
  nicht ändern.
- **Testweg:**
  - `StageMechanicsTests.cs`: mehrere Wörter, ein Wort, ein Wort über 12 Zeichen.
  - `ClozePlayTests.cs`: eine Karte mit mehreren Wörtern auf der Salat-Stufe.
  - `LetterTiles.test.tsx`.
  - Die E2E-Spec `buchstaben-ordnen.spec.ts` bleibt grün.

## Verlauf

- **2026-11-17** — aus B-632 geteilt (Teil B), auf `geschaetzt` übernommen: Ist-Stand, Entscheidung und Schätzung
  stammen aus B-632.
