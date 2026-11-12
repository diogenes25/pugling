---
tags: [typ/story, status/idee, bereich/lehrplan, rolle/supervisor]
aliases: [Nachschau-Reste Sprint 32, Dosis wirkt nicht und wird angezeigt]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 32)
unverifiziert: true
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-480 · Nachschau-Reste Sprint 32: eine Dosis, die nicht wirkt, steht weiter da

Drei 🟢 der Nachschau von Sprint 32 (frischer `pugling-reviewer`, gelesen, nicht nachgestellt) – keine Durchrutscher im
Sinn der Abnahme, die Kosten standen in den Entscheidungen; gesammelt, damit sie nicht verloren gehen:

1. **Plan-Seite zeigt eine Dosis, die nichts tut** (`PlanPositions.tsx:742`, Editor um `:228`): nach dem Umstellen einer
   Liste auf „beliebige Reihenfolge" steht weiter „· 2 neue je Tag", obwohl das Spiel sie ignoriert (B-478,
   Entscheidung 2). Abhilfe: eine *wirksame* Dosis neben `NewItemsPerDay` in der Antwort, oder ein Hinweis daneben.
2. **Überblick des Assistenten, wenn alle Übungen Listen sind** (`VaterWizard.tsx:654`): „höchstens 10 neue … (außer
   Listen)", obwohl jede Position 0 bekommt. Abhilfe: dann „keine – …".
3. **Nur per API**: `PATCH {useLeitner:false}` ohne `newItemsPerDay` auf einer umgestellten Liste scheitert an „needs the
   Leitner box", obwohl die gespeicherte Dosis nicht wirkt (`PlanPositionsController.cs:256`). Die Oberfläche trifft das
   nicht – sie leert die Dosis selbst.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 32 im Backlog-Lauf).
