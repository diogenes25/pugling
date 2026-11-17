---
tags: [typ/story, status/abgenommen, rolle/student, lerntechnik/vokabeln]
aliases: [Buchstabensalat bei mehreren Wörtern, Scramble je Wort]
status: abgenommen
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
- **2026-11-17** — gebaut (Nachtlauf, Sprint 85). Regel an der Senke `StageMechanics`: `IsScramblable` nimmt Wörter mit
  je 2–12 Buchstaben/Ziffern, durch einzelne Leerzeichen getrennt; `ScrambleLetters` mischt Wort für Wort (das erste Wort
  mit dem Seed selbst, ein Ein-Wort-Salat bleibt also gleich); die Wortgrenzen reisen als `answerPattern`
  (`ScrambleWordPattern`, additiv). `LetterTiles` legt je Wort einen Kachelblock und eigene Felder, eine Kachel landet nur
  in ihrem Wort, die Leerzeichen setzt die Komponente. Gilt für alle Typen (Vokabel wie Lückentext). Die Story nennt
  „a family of four“ als Beispiel; nach AK 1 (2–12 Buchstaben je Wort) fällt sie wegen „a“ auf Kästchen zurück – so gebaut,
  wie das AK sagt.
  - AK 1 → `LueckentextEingabeartenTests.Salat_Mischt_Mehrere_Woerter_Je_Wort`, `LetterScrambleTests.Mehrere_Woerter_Werden_Je_Wort_Gemischt`, M5/M6/M7 → rot; Vitest `LetterTiles` „legt mehrere Wörter je Wort“, F1 → rot.
  - AK 2 → `Mehrere_Woerter_Werden_Je_Wort_Gemischt` (erstes Wort = Ein-Wort-Salat mit demselben Seed); Bestandstests `LetterScrambleTests` grün.
  - AK 3 → `Nur_Kurze_Woerter_Werden_Gemischt` (13 Buchstaben, Apostroph, Doppel-Leerzeichen, „a family“) und `Antwort_Mit_Bindestrich_Kommt_Als_Buchstabenkaestchen`.
  - AK 4 → `Mischen_Behaelt_Die_Buchstaben_Und_Ist_Je_Seed_Fest` und der Mehrwort-Fall (kein Wort steht schon richtig).
  - Nachgezogen, weil sie den alten Rückfall „mehrere Wörter → Kästchen“ zusicherten: `LetterScrambleTests` (jetzt Bindestrich) und
    `e2e/buchstaben-ordnen.spec.ts` (Kästchen jetzt mit „T-shirt“, dazu „as soon as“ per Tastatur je Wort).
- **2026-11-17** — Review (Nachtlauf): Fünfer-Zähler von Sprint 85 überschritten (8 Funde 🟡/🔴), Lauf beendet; Funde offen, siehe `docs/pm-sitzung-2026-11-17-nachtlauf-lernkarte.md` → „Review Sprint 85 und Halt des Laufs“. Bleibt `in-arbeit`, nicht committet.
- **2026-11-17** — Review-Funde behoben (Fortsetzung nach „Ja, weitermachen“ des Vaters): Fokus und Ruhe am Leichter-Knopf, Fehleransage, Fokus nach dem Schalten, Nacharbeit fällt beim Typwechsel, toter Tastatur-Zweig, Wort-Gruppen für Screenreader, offene Nacharbeits-Runde wird fortgesetzt und `Review` nimmt nur die Karte am Cursor, `EasierStage` nie zurück auf eine gezeigte Form. Belege und Mutanten-Tabelle im Protokoll `docs/pm-sitzung-2026-11-17-nachtlauf-lernkarte.md`. Endstand: `dotnet test` 1820/1820, Vitest 1168/1168 am gestagten Stand, E2E `nacharbeit.spec.ts` + `buchstaben-ordnen.spec.ts` **2 passed**.
- **2026-11-17** — abgenommen (`29df59d4`). Rollengang: **Sohn** maschinell per E2E `nacharbeit.spec.ts` gegen frisch gestarteten Server (Ausweichports 5211/5181; leichter → Kästchen, Fokus im Feld, „Noch nicht erledigt“, Zähler, nächste Runde 1/1) und `buchstaben-ordnen.spec.ts` (Salat je Wort); **Vater** über `RemediationPositionTests` (Schalter per PATCH) und Vitest `PlanPositionsStufe` (Schalter nur beim Lückentext, fällt beim Typwechsel); **Creator** über Vitest `exerciseConfig` (Vorgabe hin und zurück) und `ClozePlayTests`. Ein Gerätecheck am Handy durch einen Menschen steht aus (Gefühl der Leiter, Lesbarkeit langer Vorgaben).
