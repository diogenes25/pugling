---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/student]
aliases: [gemeinsame Fälle für Normalize, Vorschlag CO]
status: abgenommen
prio: P2
art: Aufräumen
groesse: "S"
wo: "backend, frontend, .claude/skills/lehrwerk-aufbereiten"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Vorschlag (CO), Sprint 31; vom Nutzer am 2026-11-12 freigegeben)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-493 · Eine Fallliste für den Antwortvergleich, gelesen von Server, Frontend und Lader

Die Regel, wann zwei Antworten gleich sind (`StageMechanics.Normalize`: Groß/Klein, Schlusszeichen, Apostroph,
Leerzeichen), steht heute dreimal: im Server, im Editor des Vater-Webs und seit B-487 als `graded()` im Lader. Jede
Kopie wurde von Hand nachgezogen; das Review von B-487 fand eine Abweichung (`graded()` trennte an jedem Leerraum, der
Server nur am Leerzeichen).

**Freigabe:** der Nutzer hat am 2026-11-12 „Ja, mit drei Lesern" gewählt. Ein neues Tor – eigener Sprint, danach Halt.

## Skizze

- Eine Datei mit Fällen `given`, `expected`, `equal` (Ort beim Grillen festlegen; Vorschlag aus der Retro:
  `docs/api-examples/answer-normalize.json`).
- Drei Leser: eine xUnit-Theory gegen `StageMechanics.Normalize`/`AnswerGrader`, ein Vitest-Fall neben
  `exerciseConfig.test.ts`, der Python-Test aus [B-492](B-492-lader-test-in-ci.md) gegen `graded()`. Ändert jemand nur
  eine Seite, fällt der Test der anderen.
- Nicht gedeckt: Zwillinge anderer Regeln (z. B. `gapProblem` im Editor) – beim Grillen auflisten.

## Entscheidungen

1. **Ort:** `docs/fallliste-antwortvergleich.json` (`a`, `b`, `gleich`, `warum`), mit ASCII-Escapes für unsichtbare
   Zeichen (NBSP, U+FEFF, Tab) – als Literal überleben sie keinen Editor und kein Werkzeug.
2. **Drei Leser:** `FalllisteAntwortvergleichTests` (xUnit, `StageMechanics.Normalize` – die Wahrheit),
   `answerNormalize.test.ts` (Vitest, `normalizeAnswer`), `test_laden.py` (Python, `graded()`).
3. **Gleich beim Bau gefunden:** der Lader und das Vater-Web behandelten U+FEFF anders als der Server seit B-501 (heute) –
   genau die Drift, für die die Liste da ist. Beide nachgezogen.
4. Nicht gedeckt: die Artikel-Toleranz (eigene Regel, `ArticleOptionalIn`) und `gapProblem` im Editor.

## Akzeptanzkriterien

1. Alle drei Leser lesen dieselbe Datei. 2. Eine Abweichung einer Seite macht deren Test rot (gemessen: U+FEFF im Wort).

## Verlauf

- **2026-11-12** — angelegt nach der Entscheidung des Nutzers über die Retro-Vorschläge des Backlog-Laufs.
- **2026-11-13** — ausformuliert und geschätzt S (Tageslauf, Sprint 62; Freigabe des Nutzers vom 2026-11-12 liegt vor).
- **2026-11-13** — gebaut (Sprint 62). `docs/fallliste-antwortvergleich.json` (17 Fälle), drei Leser. Erster Lauf: Lader
  (2 rot) und Vater-Web (1 rot) wichen beim U+FEFF vom Server ab – beide nachgezogen. R1 (Vater-Web), R2 (Lader), R5
  (Server) → je rot an ihrem Leser.
  - Verifikation: `dotnet test Pugling.sln -c Release` 1462/1462, Vitest 854/854, `tsc -b` sauber, Python-unittest grün,
    Playwright 140/140 auf 5201/5174 (vor den letzten Beschriftungs- und U+FEFF-Nachzügen, die Komponententests tragen);
    Mutanten nach der letzten Änderung rot (Tabellen im Protokoll, Sprints 60–62).
  - Rollengang: Vater (Plan-Seite, Assistent, Ziel-Wähler) per E2E und Komponententests; Creator (Konsole) gegen den
    In-Process-Server; Sohn Regressionszeuge. Abgenommen.
