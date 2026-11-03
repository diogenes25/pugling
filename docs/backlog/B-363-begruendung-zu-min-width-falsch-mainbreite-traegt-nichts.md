---
tags: [typ/story, status/idee, bereich/frontend, bereich/test]
aliases: [vater-main Querachse]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf P, Teil 2 in Lauf Q, Teil 1 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-276]
wartet_auf: ""
---

# B-363 · B-276 begründet `min-width: 0` falsch, und eine Zusicherung der neuen Probe trägt nichts

`.app-vater` ist eine Flex-**Spalte** (`frontend/src/index.css:293`, selbst nachgelesen). Die Breite von `.vater-main` liegt
damit auf der Querachse, wo es keine automatische Mindestbreite gibt – darum trug `min-width: 0` nichts, **nicht** weil der
Überlauf nicht `visible` ist (so steht es im Kommentar an `.vater-main` und in B-276 Entscheidung 2). Ohne `overflow-x: auto`
behält `.vater-main` `width: 100%` und wächst **nicht** mit der Tabelle (anders als der Kommentar in
`e2e/responsive-vater.spec.ts:153` sagt); die Tabelle läuft über, `html` schneidet ab. Die Zusicherung `mainBreite <= WIDTH`
fällt unter der Mutante darum nicht – getragen hat `verschoben` (so auch gemessen: „1 failed an ‚scrollt seitlich'"). Die
Review-Zeile in B-276 „fallen `mainBreite`, `verschoben` und `dokument`" ist an `mainBreite` falsch.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf P, Teil 2 (frischer `frontend-reviewer` über `a25a1a6`, 🟡). Flex-Richtung
  selbst nachgelesen.
