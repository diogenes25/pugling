---
tags: [typ/story, status/idee, bereich/katalog]
aliases: []
status: idee
prio: P3
art: Aufräumen
groesse: ""
wo: backend
migration: offen
vertragsbruch: offen
quelle: "Nachschau von B-700 im Nachtlauf 2026-11-23-c (docs/pm-sitzung-2026-11-23-c.md, pugling-reviewer 🟢)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-861 · Ein deutscher Block-Kommentar im Löschen einer Übung

`backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs:602-607` (`Delete`) trägt noch einen deutschen
`/* … */`-Block, älter als B-700, direkt neben dessen englischem Kommentar. Die Regel „Code-Doku auf Englisch“ gilt für
jeden Kommentar; die Tore prüfen offenbar nur `//` und `///`, nicht `/* */` – das wäre beim Ausformulieren zu prüfen.

## Verlauf

- **2026-11-23** — angelegt aus der Nachschau von B-700 (Nachtlauf 2026-11-23-c).
