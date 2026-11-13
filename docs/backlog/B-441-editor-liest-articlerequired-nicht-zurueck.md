---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [articleRequired im Editor]
status: abgenommen
prio: P3
art: Defekt
groesse: "XS"
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

## Entscheidungen

1. `configToEditorState` liest `c.articleRequired === true`. Rundlauf-Test über `true`, `false` und „fehlt".

## Akzeptanzkriterien

1. Ein gespeicherter Haken kommt als „an" zurück; fehlt das Feld, „aus".

## Verlauf

- **2026-11-09** — angelegt aus der Nachschau zu Sprint 23 (Retro Sprint 24, Lauf AF, frischer `pugling-reviewer`), 🟢.
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt XS (Tageslauf, Sprint 52).
- **2026-11-13** — gebaut (Sprint 52). AK 1 → `exerciseConfig.test.ts` „liest den gespeicherten Haken zurück"; T1 → rot.
  Review: sauber.
  - Verifikation: Vitest 816/816, `tsc -b` sauber; Mutanten T1–T7 7/7 rot nach der letzten Änderung (Tabelle im
    Protokoll, Sprint 52); Playwright 140/140 auf 5201/5174 (vor dem Fokus-Nachzug T7, der einen Komponententest trägt).
  - Rollengang Vater/Sohn: E2E-Suite gegen echten Server und UI; Creator Regressionszeuge. Abgenommen.
