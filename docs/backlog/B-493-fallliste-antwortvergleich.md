---
tags: [typ/story, status/idee, bereich/werkzeug, rolle/student]
aliases: [gemeinsame Fälle für Normalize, Vorschlag CO]
status: idee
prio: P2
art: Aufräumen
groesse: ""
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

## Verlauf

- **2026-11-12** — angelegt nach der Entscheidung des Nutzers über die Retro-Vorschläge des Backlog-Laufs.
