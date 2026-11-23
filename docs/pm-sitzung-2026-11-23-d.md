---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Nachtlauf D – die gestufte Position spielt ihre Stufe

**Datum:** 2026-11-23  ·  **Moderation:** PM (Nachtlauf, unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Den Aufbau-Weg aus B-700 vom Katalog bis in den Spielpfad tragen – B-848 zuerst, danach was davon abhängt.

Auftrag: „starte einen nachtlauf“ – gelesen als der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10.
Ausgangsstand `main` `4781af38`, sauber. Story-Ids dieses Laufs: ab B-870.

## Runde 1 — Auswahl

Kein neuer Befund unter `docs/anmerkungen/`. Offen sind 96 Stories. Die autonom grillbaren (`Defekt`/`Aufräumen`) sind nur
noch B-860 (vom letzten Lauf bewusst liegen gelassen: Wahl Sperre vs. DB-Regel) und B-863 (deutsche Kommentare in Tests).
Baubar nach Freigabe 8 sind die geschätzten Wünsche; der P1-Faden mit vom Nutzer bestätigten Entscheidungen ist der
Aufbau-Weg: **B-848** (Position spielt ihre Stufe, L) ist die Wurzel, B-849 (Aufstieg) und B-841 (Oberfläche) warten
ausdrücklich auf sie. Die B-644-Kette (B-731 ff.) bleibt liegen wie im letzten Lauf begründet.

| Sprint | Stories | Thema |
| --- | --- | --- |
| 121 | B-848 | Aufbau-Weg im Spielpfad: gespielte Übung, Lernstand je Übung, Start-Stufe |

## Sprint 121 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn spielt an einer Position, deren Start-Stufe der Vater auf eine Vorstufe gesetzt hat, in Üben und
Test die Vorstufe – mit ihrem Typ, ihrem eigenen Lernstand und ihrer Buchseite –, und der Vater sieht dabei das Ziel und
„Stufe n von m“.
**Umfang:** B-848 (L, backend, Migration).
**Entwickler-Brief:** Quelle der Wahrheit ist `PositionPlayService.PlayedExercise(pos)` – eine Naht für Inhalt, Typ,
Config, Vorgabe-Stufe, Item-Zahl und die Lernstand-Zeilen (`PlayedExerciseId`). Sie liest die neue Navigation
`Exercise.Prerequisites`; `null` heißt „nicht geladen“ und wirft auf einer gestuften Position (Tor gegen vergessene
Includes, statt still das Ziel zu spielen). Migration `BuildLevelPlayed`: `PlanPositions.CurrentBuildLevel`,
`PositionItemProgress.ExerciseId` (gefüllt aus der Position, FK Restrict), Schlüssel `(Position, Übung, Index)`. Guards:
Start-Stufe nur mit passender Vorstufe, keine Tages-/Wochenpflicht auf einem Weg; eine gespielte Vorstufe ist nicht
löschbar (409 statt 500). Testweg: `AufbauStufePlayTests` (AK 1–7), `AufbauStufePlayMigrationTests` (AK 8).

## Iteration 121 — umgesetzt

- **Backend (API-First):** `PlanPosition.CurrentBuildLevel`, `PositionItemProgress.ExerciseId` (Migration
  `BuildLevelPlayed`, Bestand aus der Position gefüllt, Schlüssel `(Position, Übung, Index)`, FK Restrict),
  `Exercise.Prerequisites` als Gegenstück zu `BuildTarget`. Naht `PositionPlayService.PlayedExercise`/`PlayedExerciseId`;
  alle Spielpfade (Üben, Antwort, Lernkarte, leichtere Stufe, Bild, Test Start/Next/Antwort/Abgabe), Tagesansicht,
  Bericht, Rückmeldung und Ziel-Regeln lesen sie. Positions-`Stage`/`StageSchedule` gelten nicht auf einer Vorstufe.
  Start-Stufe in Create/PATCH mit Prüfungen; `BuildPath` („Stufe n von m“) an `PositionStatus`, `PositionResponse`, `Report`;
  neuer 409 `build_level_in_play`; Löschschutz und Verwendungsanzeige zählen gespielte Vorstufen.
- **Frontend:** nur der deutsche Text zu `build_level_in_play` (`src/lib/api.ts`); die Oberfläche bringt B-841.
- **Verifikation:** 15 neue Fälle in `AufbauStufePlayTests`, ein Migrationstest; 14 Bestands-Testdateien säen ihre
  Lernstand-Zeilen jetzt mit Übung (`TestApi.ExerciseOf`), zwei ältere Migrationstests projizieren statt die ganze Entität
  zu lesen (`LegacyPositionRow`). Mutanten 27/27 rot nach der letzten Code-Änderung (Tabelle in B-848). Migration an einer
  Kopie der echten `pugling.db` geprobt: 662 Zeilen gefüllt, 0 Abweichungen. Suite 2148/2149 Release + Wächter 2/2 nach dem
  Fix; Vitest `src/lib` 196/196. Commit `828cb8e5`.
- **Review** (`pugling-reviewer`): kein 🔴, sieben 🟡 – vier behoben und gezählt, einer außerhalb des Diffs behoben, zwei in
  B-702/B-849 notiert; fünf 🟢, zwei billig behoben. Einzelheiten im Verlauf von B-848.

## Runde 121 — Abnahme

- **Sohn — abgenommen (maschinell):** „Ich spiele erst die leichtere Übung – mit ihrer Buchseite, und was ich dort lerne,
  zählt nicht schon als gelernt beim schweren Thema.“ Gegangen über die HTTP-Pipeline mit Kind-Login
  (`AufbauStufePlayTests`); eine UI-Änderung gibt es in diesem Sprint nicht (die Sohn-App zeigt Titel und Seite aus
  `PositionStatus`, die jetzt die Vorstufe tragen).
- **Vater — abgenommen (maschinell, API):** „Ich setze die Start-Stufe, bekomme eine klare Absage ohne Vorstufe oder mit
  Tagespflicht, und sehe Ziel plus ‚Stufe 1 von 2‘.“ Das Formular dafür kommt mit B-841.
- **Creator — Regressionszeuge:** `aufbau-weg-editor` (E2E) grün; neu für ihn: eine gespielte Vorstufe ist nicht löschbar
  und erscheint in „Verwendung“.
- **Offen für einen Menschen:** die Migration läuft beim nächsten Heimnetz-Start gegen die echte DB (an einer Kopie
  geprobt).

## Retrospektive Sprint 121

**Nachschau:** B-861 und B-862 (Sprint 120), Selbst-Check mit benannten Prüfpunkten (je Story im Verlauf):
Block-Kommentare per grep über alle Backend-Projekte – 0 deutsche Reste; `CoinsLeftToday` hat genau einen Erzeuger, Start und
Wiederaufnahme laufen durch ihn. Kein Befund, `nachgeschaut` gesetzt.

**Was die eigenen Tore durchgelassen haben:** Die Naht hat die *Spiel*-Regeln an die gespielte Übung gebunden, die
*Schreib*-Prüfungen im Positionsformular blieben an der Ziel-Übung – Ausführungsrecht, Nacharbeit, Positions-Stufe. Dreimal
dieselbe Familie, einmal selbst gefunden, zweimal vom Reviewer; keine Mutante konnte sie finden, weil es die Prüfung an der
richtigen Stelle noch gar nicht gab. Dazu ein Pfad, den kein Test fuhr (Sammel-Abgabe, M10 grün) – gefunden von der Probe.
Der rote `ProblemTextGuardTests` hat den fehlenden deutschen Text gefunden, wofür er gebaut ist.

**Vorschlag (nicht gelandet):** (DB) In `pm-loop` Step 4 neben „Put a rule at the sink“ ein Satz: *Wer eine neue Naht für
das Gespielte einführt, zählt auch die Schreib-Prüfungen der Größe, die sie ersetzt* – `grep` auf jede Validierung, die die
alte Größe (hier: `exercise` im Positionsformular) liest, und je Treffer die Frage „prüft sie, was das Kind spielt?“.

**Zur Entscheidung (Verfahren):** Freigabe 10 zählt wörtlich nur Reviewer-🟡/🔴. Ob ein eigener Fund und ein roter Wächter
auf den Fünfer-Zähler gehen, sagt sie nicht – mit ihnen stand dieser Sprint bei 6. Ich habe die strengere Lesart genommen.

## Abschluss des Laufs

**Halt nach Sprint 121, begründet:** Fehlerzähler nach strenger Lesart 6 (> 5) – Freigabe 3 beendet dann den ganzen Lauf.
Der Sprint ist trotzdem vollständig geschlossen (Review eingearbeitet, Probe 27/27, Rollengang, Retro), damit nichts
halbfertig liegt; ein weiterer Sprint wurde nicht begonnen.

**Ergebnis:** B-848 abgenommen; keine neue Story (Review-Reste in B-702/B-849 notiert). Commits auf `main`, **ungepusht**:
`828cb8e5` und der Abschluss-Commit.

**Zum Umstoßen am Morgen:**
1. Positions-`Stage`/`StageSchedule` gelten nicht, solange eine Vorstufe gespielt wird (die Vorstufe spielt ihre eigene
   Vorgabe-Stufe).
2. Stufenwechsel durch den Vater wird unter einer offenen Runde oder einem offenen Test von heute mit 409 abgewiesen.
3. `NewWords` zählt ein Wort, das auf Vorstufe und Ziel neu war, zweimal.

**Menschlicher Check offen:** Migration `BuildLevelPlayed` beim nächsten Heimnetz-Start (Startskript sichert vorher).

## Offene Roadmap (priorisiert)

Durable Liste: [docs/backlog/](backlog/INDEX.md). Als Nächstes B-849 (Aufstieg ab dem nächsten Lerntag – `PlayedExercise`
braucht dafür den Tag, das zieht sich durch alle Aufrufer der Naht), danach B-702 (Pflicht auf der aktuellen Stufe, hebt
die Kadenz-Sperre) und B-841 (Oberfläche). B-863 bleibt mechanische Übersetzungsarbeit, B-860 im Dialog.

## Nachtrag — Zählregel entschieden

Am 2026-11-23 im Dialog: Eigene Funde und rote Wächter zählen auf den Fünfer-Zähler (die strenge Lesart). Der Halt nach
Sprint 121 war damit richtig. Festgehalten als Freigabe 11 in [nachtlauf.md](nachtlauf.md).
