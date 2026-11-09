---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [articleRequired im Editor]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-441 · Der Vokabel-Editor liest „Artikel verlangt" nicht aus der gespeicherten Config zurück

`configToEditorState` (`frontend/src/vater/exerciseConfig.tsx`, Fall Vocabulary) setzt fest `articleRequired: false`, statt
`c.articleRequired === true` zu lesen. Heute folgenlos: der Bearbeiten-Weg baut die Config über `opts.base` (Server-Stand), nur der
Anlege-Zweig liest das Feld. Aber die Rücklese-Funktion gibt einen gespeicherten Haken als „aus" zurück – eine Falle für den nächsten
Umbau (Hausregel „Editor beidseitig: build + read-back", `frontend/CLAUDE.md`). Fix: aus der Config lesen, Rundlauf-Test.

## Verlauf

- **2026-11-09** — angelegt aus der Nachschau zu Sprint 23 (Retro Sprint 24, Lauf AF, frischer `pugling-reviewer`), 🟢.
