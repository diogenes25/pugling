---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [SohnTest Doppelklick ohne Fall]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf H, Teil 2 in Lauf I (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-320]
wartet_auf: ""
---

# B-325 · Die Doppelklick-Sperre der Klausur trägt keinen Fall

[B-320](B-320-klausur-fehler-erreicht-den-bildschirm-nicht.md) AK 3 („ein verworfener Doppelklick zeigt nichts“) und
das Ref-Gate aus B-49 in `frontend/src/sohn/SohnTest.tsx` (`action.run` um `answerTest`) hält kein Test. Mutante:
`action.run(…)` durch einen direkten Aufruf ersetzen → alle Fälle grün, ein Doppelklick schickt zwei `answerTest`.

Vorschlag: zweimal synchron klicken, solange `answerTest` hängt; `toHaveBeenCalledTimes(1)` und keine Meldung.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau (frischer `frontend-reviewer`, `git show f96d358`) in Lauf I
  ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); aus Code gelesen, Mutante hergeleitet, nicht gemessen.
