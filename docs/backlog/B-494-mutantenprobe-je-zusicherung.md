---
tags: [typ/story, status/abgenommen, bereich/werkzeug]
aliases: [mutant-probe misst je Zusicherung, Vorschlag CP]
status: abgenommen
prio: P3
art: Aufräumen
groesse: ""
wo: ".claude/scripts"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Vorschlag (CP), Sprint 31; vom Nutzer am 2026-11-12 auf „später" gesetzt)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-494 · `mutant-probe.py` misst je Zusicherung, nicht nur je Test

`mutant-probe.py` zählt rot, wenn der genannte Test fällt – nicht, *welche* Zusicherung darin fällt. Eine Zusicherung,
die im Ausgangszustand nie fallen kann, bleibt so unentdeckt, solange eine andere im selben Test rot wird (vier Mal
gemessen, siehe Root-`CLAUDE.md`). Vorschlag: `fails` nimmt auch Meldungstexte einzelner Zusicherungen
(`expect(…, "die Seite scrollt nicht seitlich")`); rot zählt dann nur, wenn genau diese fällt. Nützlich vor allem für
Playwright-Specs mit eigener Meldung je `expect`; xUnit-Zusicherungen ohne Meldung bleiben grob.

**Entscheidung des Nutzers (2026-11-12):** später – kein Durchrutscher des Backlog-Laufs hätte sich damit gefangen.

## Ist-Stand am Code (2026-11-13)

- `mutant-probe.py` prüft `fails` als **Teilstring der ganzen Testausgabe** (`missing = [t for t in m.get("fails", []) if t
  not in out]`), nicht als Testnamen. Eine Zusicherungsmeldung als `fails`-Eintrag wirkt also schon heute – *wenn* der
  Runner sie nur beim Fehlschlag ausgibt. Playwright (`--reporter=line`) und Vitest drucken die Meldung eines `expect`
  nur, wenn es fällt; dort trägt die Idee ohne Code-Änderung.
- Grenze: xUnit-`Assert.True(x, warum)` druckt `warum` ebenfalls nur beim Fehlschlag, `Assert.Equal` ohne Meldung hat
  keinen Text – dort bleibt nur der Testname. Und ein Teilstring, der zufällig auch in anderer Ausgabe steht (Logzeilen
  des `WebServer`), träfe falsch.

## Die echte Lücke

Vermutlich nur Doku: der Docstring nennt `fails` „Testname-Teil". Zu klären beim Grillen, ob ein Hinweis „auch eine
Zusicherungsmeldung, sofern der Runner sie nur beim Fehlschlag druckt" reicht – oder ob die Story damit `verworfen` ist.

## Verlauf

- **2026-11-12** — angelegt nach der Entscheidung des Nutzers über die Retro-Vorschläge des Backlog-Laufs.
- **2026-11-13** — ausformuliert (Tageslauf, Restzeit): am Code gefunden, dass `fails` schon jeden Teilstring der Ausgabe nimmt; die Lücke ist vermutlich nur der Docstring. Entscheidung „später" des Nutzers bleibt – nichts gebaut.
- **2026-11-13** — abgenommen (Tageslauf, stellvertretend): die Lücke war nur Doku – der Docstring von `mutant-probe.py` nennt jetzt Zusicherungsmeldungen als gültige `fails`-Einträge samt Grenze (nur Text, den der Runner allein beim Fehlschlag druckt). Kein Code geändert, darum keine Mutante; geprüft: `python -c ast.parse` grün.
