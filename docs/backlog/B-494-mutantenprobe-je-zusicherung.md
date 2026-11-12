---
tags: [typ/story, status/idee, bereich/werkzeug]
aliases: [mutant-probe misst je Zusicherung, Vorschlag CP]
status: idee
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

## Verlauf

- **2026-11-12** — angelegt nach der Entscheidung des Nutzers über die Retro-Vorschläge des Backlog-Laufs.
