---
tags: [typ/story, status/idee, bereich/klassenarbeit]
aliases: []
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: "Review von B-757 im Nachtlauf I 2026-11-23 (docs/pm-sitzung-2026-11-23-i.md, frontend-reviewer 🟡)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-943 · Die Zusicherung „keine Zahl bei geschriebener oder abgesagter Arbeit“ kann nicht fallen

`VaterClassTests.test.tsx` gibt den Fällen `Written`/`Cancelled` `prepCardsPerDay: null` mit; entfernt man die Statusprüfung in
`VorbereitungsZelle`, steht „– · 7 Tage (Standard)“ da, ohne „Karten“ – der Test bleibt grün, obwohl sein Kommentar das
Gegenteil verspricht (Familie „leere Zusicherung“, Root-`CLAUDE.md`). Vorschlag: `prepCardsPerDay: 4` und `toBe("–")`.

## Verlauf

- **2026-11-24** — angelegt im Nachtlauf I (Sprint 134) aus dem Review, nicht behoben: der Fehlerzähler des Sprints stand
  bei 5, ein weiterer Fund hätte ihn überschritten (Freigabe 3/11) – der Lauf endet hier. Protokoll
  [pm-sitzung-2026-11-23-i.md](../pm-sitzung-2026-11-23-i.md).
