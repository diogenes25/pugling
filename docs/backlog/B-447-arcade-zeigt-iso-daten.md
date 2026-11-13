---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Arcade zeigt ISO-Daten]
status: abgenommen
prio: P3
art: Aufräumen
groesse: "S"
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
- **2026-11-13** — gebaut (Sprint 54), autonom gegrillt (Defekt): `arcadeDatum(value, language)` in `lib/datum.ts`, in
  `SohnShop`, `MyObjectives`, `SohnPractice` (nächste Fälligkeit) und `SohnProgress` (Kalender-Titel). Wächter in
  `datum.test.ts`: keine Arcade-Quelle baut oder formatiert ein Datum selbst (`new Date(`, `Date.parse(`,
  `Intl.DateTimeFormat(`, `toLocale…String(`); D5 (Shop wieder mit `new Date`) → rot; D6 (Zone kommt nicht an) → rot.
  - Verifikation: Vitest 820/820, `tsc -b` sauber; Mutanten D1–D6 und E1 nach der letzten Änderung (Tabelle im Protokoll,
    Sprint 54): D1, D2, D3, D5, D6, E1 rot; **D1b und D4 nicht rot – begründet**: ein `DateOnly` wird als Mittag UTC gelesen
    *und* in der Arcade in UTC formatiert; fällt eine Hälfte weg, trägt die andere für alle Zonen bis UTC+11 (D4 wirkt nur
    jenseits davon, ohne Zonenparameter für DateOnly nicht messbar). Playwright 140/140 auf 5201/5174.
  - Rollengang Sohn/Vater: E2E-Suite gegen echten Server und UI; Creator Regressionszeuge. Abgenommen.
