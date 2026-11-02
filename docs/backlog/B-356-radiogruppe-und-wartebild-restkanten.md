---
tags: [typ/story, status/idee, bereich/frontend, rolle/vater, rolle/sohn]
aliases: [radioGroupKeys Restkanten, Wartebild Streak nach dem Verlassen]
status: idee
prio: P4
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf N, Teil 1 (docs/pm-sitzung-2026-11-02-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-356 · Restkanten an Radiogruppen und Wartebild (Nachschau über `b73bcde`)

Fünf 🟢 der Nachschau über B-350/B-352, keiner eine Entgleitung:

1. **`radioGroupKeys` klickt die schon gewählte Option erneut** (`frontend/src/lib/ui.ts:56-57`), wenn der Schritt auf
   sie zeigt (Home auf der ersten, End auf der letzten, eine einzige freie Option). Im Assistenten setzt das
   `setTouchedMode(true)`, solange „Bestehendes Kind" gesperrt ist – die spät ankommende Liste belegt dann nicht mehr vor
   (B-177). Ein Mausklick tut dasselbe. Weg: `if (to === at) return;`.
2. **`preventDefault` an Pfeiltasten trägt keine Zusicherung** (`lib/ui.test.ts`): die Mutante „ohne `preventDefault`"
   bliebe grün; im Browser scrollten Pfeil oben/unten und Home/End dann die Seite.
3. **Der Wächter nimmt eine ganze Datei aus, sobald ein natives Radio darin steht** (`radioGroups.test.ts:21`); eine
   Knopf-Gruppe neben nativen Radios (`PlanPositions.tsx`) bliebe ungeprüft. Dazu nicht erkannt: `role={"radio"}`.
4. **Nach dem Verlassen frischt `finish` nur das Guthaben auf** (`SohnTest.tsx:68`), nicht die Serie in der Kopfzeile.
5. **Der Fall „fertiger Versuch" prüft `celebrate` nur negativ** (`SohnTest.test.tsx:333-334`), ohne festzustellen, dass
   `submitTest` erreicht wurde – ein zusätzliches `await` vor dem Absenden machte ihn leer.

## Verlauf

- **2026-11-02** — angelegt aus der Nachschau zu Lauf N, Teil 1 (`frontend-reviewer` über `b73bcde`, 🟢 ×5). Aus Code
  gelesen.
