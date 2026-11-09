---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Arcade zeigt ISO-Daten]
status: idee
prio: P3
art: Aufräumen
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

# B-447 · Die Sohn-Arcade zeigt Daten als ISO-Text

Gefunden im Review zu B-427 (Lauf AG, Sprint 27, `frontend-reviewer`, 🟢). B-427 hat das Vater-Web vereinheitlicht; die Arcade
formatiert mit der gewählten Sprache (`SohnShop`, `toLocaleDateString(i18n.language)`), zeigt an drei Stellen aber rohes ISO:
`sohn/MyObjectives.tsx:88` (`t("objectives.due", { date: o.dueDate })` → „bis 2026-11-20"), `sohn/SohnPractice.tsx:470` („Nächste
Fälligkeit: 2026-11-10") und `sohn/SohnProgress.tsx:68` (`title={d.day}`). Reparatur: ein `DateOnly` per Textzerlegung (wie
`lib/datum.ts`), formatiert mit `i18n.language` – nicht mit `formatDate`, sonst stünde in der englischen Arcade ein deutsches Datum.

## Verlauf

- **2026-11-09** — angelegt aus dem Review zu B-427; außerhalb des Vater-Webs, darum nicht in diesem Sprint.
- **2026-11-09** — ergänzt aus der Nachschau zu Sprint 27: `SohnShop.tsx:33` liest Zeitstempel ohne Offset als Ortszeit (`new Date(iso)`); die Lese-Hälfte aus `lib/datum.ts` (`augenblick`) gehört mit in die Reparatur.
