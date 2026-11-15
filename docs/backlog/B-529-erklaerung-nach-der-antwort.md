---
tags: [typ/story, status/abgenommen, bereich/api, bereich/frontend, rolle/creator, rolle/student]
aliases: [Erklärfeld, Erklärung im Blatt, Glühbirne]
status: abgenommen
prio: P2
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: "B-526, Entscheidung 8 (Grill-Runde 2026-11-13); docs/duolingo-snipwise-vergleich-plan.md"
grund: ""
ersetzt_durch: []
wartet_auf: ""
nachgeschaut: 2026-11-14
---

# B-529 · Erklärung nach der Antwort

Snipwise zeigt nach jeder Antwort hinter einer Glühbirne, **warum** sie richtig oder falsch war. Gewünscht ist ein
Satz je Inhalt („à + le wird zu au"), den das Blatt aus [B-526](B-526-lektionsbildschirm-festes-raster.md) nach der
Antwort zeigt. Beim Grillen von B-526 bewusst abgespalten. Geschnitten auf den Teil, für den es den Inhalt schon gibt:
die **Regel-Hinweise der Regelaufgaben**.

## User Story

Als *Sohn* möchte ich nach einer Regelaufgabe auf dem Blatt die Regel lesen („à + le wird zu au"), damit ich aus einem
Fehler lerne, statt nur die richtige Lösung zu sehen.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Worktree `backlog-lauf`, `699b02a7`):

- Ein Erklärfeld **gibt es schon – für Regelaufgaben**: `GrammarTask(Prompt, Answer, RuleHint)`
  (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:142-143`), dokumentiert als „Optional rule hint that may be shown
  **after** the answer". Der KI-Creator füllt es (`backend/Pugling.Agent.Creator/Drafting/GrammarStrategy.cs:80`).
- Ausgespielt wird es heute **vor** der Antwort: `GrammarExerciseType.ItemsOf` legt `RuleHint` in `ContentItem.Hint`
  (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:118`), `PositionPlayService.CardFacets` gibt `item.Hint` auf
  getippten Stufen als Tipp heraus (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:328`), die Arcade zeigt
  ihn hinter „Tipp" (`frontend/src/sohn/SohnPractice.tsx:454-458`).
- Die Rückmeldung nennt nur Lösung und Punkte: `ReviewOutcome(WasCorrect, Expected, Awarded, Box, DueOn, Combo,
  ComboBonus, SpeedBonus, Next, Done, Misspelled)` (`backend/Pugling.Contracts/Student/PracticeDtos.cs:107-108`), gebaut in
  `PositionPracticeController.cs:531-532`.
- Das Blatt: `frontend/src/sohn/Lektion.tsx:182-200` (`Rueckmeldung`), `:215ff` (`RueckmeldungsBlatt`); befüllt in
  `SohnPractice.tsx` (richtig ~`:244-249`, falsch ~`:258-261`). Der Test zeigt das Blatt nie (`Lektion.tsx`, Doku am Blatt).
- Vokabeln haben kein Erklärfeld: `VocabItem(Front, Back, Hint, …)` (`ExerciseConfigs.cs:58`) – `Hint` ist dort eine Hilfe
  *vor* der Antwort („beginnt mit b").
- `ContentItem` (`backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs:30ff`) ist intern, kein Vertrag.

## Die echte Lücke

Der Inhalt für Regelaufgaben ist da und wird nur am falschen Zeitpunkt (vor der Antwort, hinter einem Knopf) gezeigt; nach
der Antwort – wo der Vertrag ihn vorsieht – fehlt er. Für Vokabeln fehlt der Inhalt selbst.

## Offene Punkte

1. ~~Neues Feld an allen Übungsinhalten oder zuerst das vorhandene `RuleHint`?~~ → Entscheidung 1.
2. ~~Wie kommt die Erklärung an der Lösungsfeld-Regel vorbei?~~ → Entscheidung 2.
3. ~~Bleibt der Regel-Hinweis auch vor der Antwort?~~ → Entscheidung 3.
4. ~~Auch im Test?~~ → Entscheidung 4.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Erste Scheibe: `RuleHint` der Regelaufgaben, kein neues Feld.** *Begründung:* Der Inhalt existiert, der KI-Creator
   füllt ihn, und der Vertrag sieht ihn ausdrücklich nach der Antwort vor – Wert ohne Migration und ohne Creator-Arbeit.
   *Kosten:* Vokabeln bekommen noch keine Erklärung (siehe „Nicht in dieser Story").
2. **Ausgeliefert wird sie nur in der Rückmeldung `ReviewOutcome.Explanation` (string?, additiv)**, also erst nach der
   Antwort; intern über ein neues optionales `ContentItem.Explanation`. Der Name fällt nicht unter die Lösungsfeld-Regel
   (`Answer`/`Solution`/`CorrectAnswer`/`Translation`) und muss es auch nicht: die Antwort ist zu diesem Zeitpunkt
   gegeben, wie bei `Expected`. In einer Menge ohne getroffenen Eintrag bleibt sie `null` – dieselbe Regel wie `Expected`.
   *Kosten:* ein Feld im Vertrag, ein Parameter intern.
3. **Der Tipp vor der Antwort bleibt unverändert.** *Begründung:* Kleinste Scheibe; ob Tipps etwas kosten, entscheidet
   [B-223](B-223-gestufte-hinweise-vor-der-antwort.md). Wer den Tipp schon geöffnet hat, liest die Regel auf dem Blatt
   ein zweites Mal – harmlos. *Kosten:* Doppelung in diesem Fall.
4. **Kein Test.** *Begründung:* Der Test misst und zeigt kein Blatt (B-526; B-223 Rest-Entscheidung 2026-11-05).
   *Kosten:* keine.
5. **Darstellung:** eine Zeile „💡 Regel: …" auf dem Blatt, bei richtig **und** falsch. *Begründung:* Bei richtig
   bestätigt sie, warum; die Glühbirne ist das Bild aus der Vorlage. *Kosten:* das Blatt wird eine Zeile höher (das feste
   Raster aus B-526 muss sie tragen).

## Akzeptanzkriterien

1. Regelaufgabe mit `RuleHint`, falsche getippte Antwort: `review` liefert `explanation` = der Regel-Hinweis. *Rot, wenn*
   das Feld fehlt oder leer bleibt.
2. Dieselbe Aufgabe, richtige Antwort: `explanation` ebenfalls gesetzt. *Rot, wenn* nur Fehler eine Erklärung bekommen.
3. Vokabel-Übung: `explanation = null` (ihr `Hint` ist keine Erklärung). *Rot, wenn* `ContentItem.Hint` statt
   `Explanation` ausgegeben wird.
4. *(Absichtserklärung, kein eigener Test:)* Mengen-Übungen haben keinen `RuleHint`, ihre `explanation` ist also immer
   `null`; ein Fall dafür könnte nie rot werden. Die Regel aus Entscheidung 2 (über `creditedIndex`) wird wirksam, sobald
   ein Mengen-Typ eine Erklärung bekommt – dann gehört der Fall dazu.
5. Blatt: mit `explanation` steht „💡 Regel: …" sichtbar, ohne steht keine leere Zeile. *Rot, wenn* `SohnPractice` das
   Feld nicht ins Blatt reicht.

## Schätzung

**Größe: S** – ein internes und ein additives Vertragsfeld, eine Zuordnung im Regelaufgaben-Typ, eine Zeile im Blatt.
`wo: beides`, `migration: nein` (Inhalt liegt im Übungs-JSON), `vertragsbruch: nein`.

**Risiken:** Der Bestand der Söhne enthält vielleicht kaum Regelaufgaben – dann wirkt die Story erst mit neuen Inhalten
(beim Bau zählen und im Verlauf vermerken). `DocsCaptureTests` schreiben das neue Feld in die Beispiele.

**Angriffsplan:** (1) Fälle AK 1–3 in `PositionPracticeFlowTests.cs` (Regelaufgabe anlegen, üben), rot gegen den Ist-Vertrag;
(2) `ContentItem.Explanation`, Zuordnung in `BuiltInExerciseTypes.cs:118`, Feld in `ReviewOutcome` und `:531`;
(3) `pugling-reviewer`; (4) `Rueckmeldung.erklaerung`, Zeile im `RueckmeldungsBlatt`, Befüllung in `SohnPractice.tsx`;
(5) Vitest AK 5; (6) rote Probe; `frontend-reviewer`.

**Testweg:** `backend/Pugling.Api.Tests/PositionPracticeFlowTests.cs` (AK 1–3); `frontend/src/sohn/Lektion.test.tsx`
(Blatt mit/ohne Erklärung) und `frontend/src/sohn/SohnPractice.test.tsx` (AK 5, Feld wird durchgereicht).

**Nicht in dieser Story:** Erklärfeld für Vokabeln und andere Typen (neue Spalte am Übungsinhalt → Migration, Creator-
Editor, KI-Creator-Befüllung), Erklärungen in der Testauswertung, Erklärungen im Bonus-Training.

## Verlauf

- 2026-11-13 · `idee` – abgespalten aus B-526; Prio P2 ist ein Vorschlag.
- **2026-11-13** — `ausformuliert`: am Code belegt – `GrammarTask.RuleHint` ist schon ein Erklärfeld „nach der Antwort", wird aber als Tipp vorher ausgespielt; für Vokabeln fehlt der Inhalt.
- **2026-11-13** — `gegrillt`: Entscheidungen 1–5 (erste Scheibe über `RuleHint`, Auslieferung nur in `ReviewOutcome`, Tipp bleibt, kein Test); Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — `geschaetzt`: S, beides, ohne Migration und Vertragsbruch; Vokabel-Erklärungen ausdrücklich nicht in dieser Story.
- **2026-11-13** — `in-arbeit` (Sprint 72): internes `ContentItem.Explanation`, `GrammarExerciseType.ItemsOf` füllt es aus `RuleHint` (der Tipp vor der Antwort bleibt, Entscheidung 3); additives `ReviewOutcome.Explanation`, gesetzt nur über den getroffenen Eintrag (`creditedIndex`, dieselbe Regel wie `Expected`). Arcade: `Rueckmeldung.erklaerung`, Zeile „💡 Regel: …" im `RueckmeldungsBlatt` bei richtig und falsch, `SohnPractice` reicht das Feld in beiden Zweigen durch (de/en `lektion.rule`). Tests: `PositionPracticeFlowTests.Regelaufgabe_Erklaert_Nach_Der_Antwort_Die_Regel` (AK 1, AK 2), `…Vokabel_Hat_Keine_Erklaerung_Auch_Mit_Tipp` (AK 3); `Lektion.test.tsx` „zeigt die Erklärung bei falsch und bei richtig"/„zeigt ohne Erklärung keine Regelzeile", `SohnPractice.test.tsx` „reicht die Erklärung des Servers ins Blatt" (AK 5). AK 4 bleibt Absichtserklärung ohne Fall. Den Bestand an Regelaufgaben in der echten `pugling.db` nicht gezählt (Lauf arbeitet nur im Worktree) – beim Rollengang nachholen. Rote Probe:

  Gemessen an `36ebffe5 + Arbeitsbaum geändert`, 2026-11-13 23:39.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | M1 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:537 | red | red | Feld bleibt leer (AK 1) |
  | M2 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:537 | red | red | `Hint` statt `Explanation` (AK 3) |
  | M3 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:537 | red | red | nur Fehler bekommen eine Erklärung (AK 2) |
  | M4 | backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:121 | red | red | Regelaufgabe ordnet nichts zu (AK 1) |
  | M5 | frontend/src/sohn/Lektion.tsx:251 | red | red | leere Zeile ohne Erklärung (AK 5) |
  | M6 | frontend/src/sohn/Lektion.tsx:251 | red | red | Blatt zeigt keine Erklärung (AK 5) |
  | M7 | frontend/src/sohn/SohnPractice.tsx:256 | red | red | richtig-Zweig reicht nichts durch (AK 5) |
  | M8 | frontend/src/sohn/SohnPractice.tsx:271 | red | red | falsch-Zweig reicht nichts durch (AK 5) |

  Grundlinie nach dem Zurückspielen: green.
- **2026-11-13** — **abgenommen** mit Commit `ff6a08fb + fbd78e04` (Sprint 72, Backlog-Lauf 2026-11-13). Verifikation: Backend 1599/1599 grün (dotnet test Pugling.sln -c Release), Vitest 998/998, tsc -b sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: Code-Review ohne Befund an dieser Story (Erklärung bei richtig und falsch, reist als Platzhalter). Rollengang: volle Playwright-Suite gegen frisch gestartete Server auf Ausweichports (API 5216, Web 5189) nach dem letzten Edit: **145 passed** (alle Specs). Den Tab „Erfolge“ (Woche, Frist) fährt keine Spec eigens; er ist über RTL-Tests belegt – ein Handblick am Gerät bleibt für den Menschen.
- **2026-11-14** — Nachschau Backlog-Lauf: ohne Fund. `Explanation` entsteht nur in `GrammarExerciseType.ItemsOf` (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:118-121`; keine andere `ContentItem`-Konstruktion trägt eine Erklärung) und reist nur in `ReviewOutcome` über `creditedIndex` (`backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:580-582`), nie auf einer Karte und nicht im Test (`TestItem`, `AnswerAck` ohne Feld). Blatt: `frontend/src/sohn/Lektion.tsx:251`, Platzhalter unescaped wie überall (`frontend/src/lib/i18n.ts:44`).
