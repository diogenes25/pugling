---
tags: [typ/story, status/abgenommen, bereich/test, bereich/frontend, rolle/student]
aliases: [progress.days objectives.count ohne Pluralfall]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf G Teil 1 in Lauf G, Teil 2 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-304]
wartet_auf: ""
nachgeschaut: "2026-10-28"
---

# B-316 · Die Zählschlüssel aus B-304 haben keinen Singular-/Plural-Fall

`progress.days`, `objectives.count` und `badges.fresh` (dazu jetzt `practice.cards` aus B-305) tragen `_one`/`_other`,
aber kein Fall prüft 1 gegen 2 – das Vorbild B-298 (`i18n.test.ts`) wurde nicht nachgezogen. Mutante (Nachschau,
hergeleitet): `days_one`/`count_one` in de **und** en streichen → Parität grün, sichtbar „1 Tage“/„1 goals“.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf G, Teil 1 (`95f611b`), Fund 2 (🟡); Increment dieses Laufs →
  **Limit 2**. Als `Defekt` ausformuliert, geschätzt (XS) und im laufenden Sprint behoben.
- **2026-10-27** — gebaut: acht Zeilen in der Tabelle von `i18n.test.ts` (`progress.days`, `objectives.count`,
  `objectives.stagesDone`, `practice.cards`, je de/en mit 1 und 2) → am echten Code grün (Testlücke). Mutante
  `objectives.count_one` in de und en gestrichen → **2 failed** (je Sprache einer). `badges.fresh_one` bleibt
  bewusst ohne Fall – der Aufrufer übersetzt nur bei mehr als einem. **`abgenommen`** mit Teil 2.
- **2026-10-28** — nachgeschaut (Lauf H, Teil 1, frischer `frontend-reviewer` am Commit `43b5eef`): die acht neuen Zeilen in
  `i18n.test.ts` gegen `de.ts:144-160`/`en.ts:138-154` nachgerechnet; alle Aufrufstellen (`SohnProgress.tsx:54`,
  `MyObjectives.tsx:43/63`, `SohnPractice.tsx:211`) übergeben `count`; Ausnahme `badges.fresh` begründet
  (`GamificationPanels.tsx:89`). Mutante „`count_one` streichen“ → rot hergeleitet. Kein Entgleiten.
