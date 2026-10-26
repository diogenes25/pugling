---
tags: [typ/story, status/idee, bereich/frontend, bereich/a11y, rolle/supervisor]
aliases: [Fokus fällt auf body nach „Reihe anlegen & wählen“]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Review Sprint 11 zu B-64 (sechster Nachtlauf, docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-268 · Nach „Reihe anlegen & wählen“ fällt der Fokus auf `body`

Das Inline-Anlegen einer Reihe im Lehrbuch-Formular ([B-64](B-64-textbook-vs-series.md),
`frontend/src/vater/ChildMaterialSection.tsx`, `reiheAnlegen`) schließt nach Erfolg sein Panel – samt dem gerade
fokussierten Knopf. Der Fokus landet auf `body`; eine Tastaturnutzerin verliert ihren Platz. Nach „+ Neue Reihe“
springt der Fokus außerdem nicht ins Namensfeld.

Vorschlag (vom `frontend-reviewer`): `autoFocus` am Namensfeld, nach Erfolg den Fokus auf `#${id}-series`; Test als
Komponententest (`document.activeElement`).

**Nicht in diesem Lauf behoben:** Der Reviewer stufte es als Hinweis (🟢) ein. Eine Behebung hätte den Fund-Zähler
von Sprint 11 über die Grenze von fünf gehoben (Freigabe 3). Das ist hier offen benannt, die Entscheidung liegt
beim Nutzer.

## Verlauf

- **2026-10-26** — angelegt aus dem `frontend-reviewer` zu B-64 im sechsten Nachtlauf (Fund 4).
