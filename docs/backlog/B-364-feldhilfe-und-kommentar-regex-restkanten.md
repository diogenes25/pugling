---
tags: [typ/story, status/idee, bereich/frontend, bereich/test]
aliases: [liveRegions Regex zwei Blockkommentare]
status: idee
prio: P4
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf P, Teil 2 in Lauf Q, Teil 1 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-364 · Restkanten der Nachschau über `a25a1a6`: zwei Blockkommentare, ⓘ-Schleife der Tablet-Spec

Zwei 🟢 der Nachschau über Lauf P, Teil 2, keiner eine Entgleitung:

1. **`liveRegions.test.ts:79`** – kein Fall mit zwei Blockkommentaren hintereinander; die gierige Mutante `[\s\S]*` statt
   `[\s\S]*?` verschluckte `/* a */ x && /* b */` unbemerkt. Weg: ein Selbsttest-Fall mit zwei Blockkommentaren.
2. **`e2e/responsive-vater.spec.ts:69-77`** – seit der Anker-Prüfung je Frame (B-273) schließt das ⓘ bei *jeder* Bewegung des
   Knopfs, auch durch spät nachladenden Inhalt nach `networkidle`; `popoverFit` riefe dann `querySelector` auf ein fehlendes
   Element. Ungemessen, geringes Risiko.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf P, Teil 2 (`frontend-reviewer`, 🟢 ×2). Aus Code gelesen.
