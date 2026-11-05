---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/doku]
aliases: [frontend/CLAUDE.md wuchs doch]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#retrospektive-sprint-2
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-233]
---

# B-382 · `frontend/CLAUDE.md` wuchs entgegen B-233 AK 3, und vier Angaben aus Sprint 1 stimmten nicht

Die Nachschau zu `c6876b4` (frischer `frontend-reviewer`, read-only) fand:

1. **B-233 AK 3** („die Hausregel steht in `frontend/CLAUDE.md`, ohne dass die Datei wächst"): gemessen am Blob 10347 → 10389 B,
   also +42 B. Die Angabe „10347 → 10333 B" stammte von vor dem 🟢-4-Fix des Reviews, der den Regel-Satz in zwei Sätze teilte.
2. Zeilenverweise: B-231 `HistoryTab.test.tsx:127` (richtig `:129`), B-233 Ist-Stand `:447`/`:740` (richtig `:446`/`:739`),
   B-234 Einleitung `CatalogAdmin.tsx:210` (richtig `:212`).
3. Zählungen in B-380 und Protokoll: „zehn Formulare" (richtig fünfzehn, drei fehlten in der Liste), „`Name der neuen Reihe`
   elfmal" (richtig zehnmal).

## User Story

Als **Leser** einer abgenommenen Story möchte ich, dass ihre Zahlen und Verweise stimmen.

## Ist-Stand am Code (Stand `2361974`)

- `git show c6876b4:frontend/CLAUDE.md | wc -c` = 10389, Elternstand `37e6f6e` = 10347.
- Die drei Verweise und zwei Zählungen wie oben; je per `sed -n`/`grep` nachgerechnet.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. Die Datei verdichten statt die Behauptung zurückzunehmen; Verweise und Zählungen berichtigen. Keine Code-Änderung.

## Akzeptanzkriterien

1. `wc -c frontend/CLAUDE.md` ≤ 10347 (Stand vor Sprint 1).
2. Die genannten Verweise und Zählungen stimmen mit `grep`/`sed -n` überein.

## Schätzung

`XS`, nur Doku. Testweg: `wc -c`, `grep`.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Sprint 1 und im selben Sprint 2 behoben, ohne Zählung (nur Text). AK 1: erwartet
  ≤ 10347, gemessen 10325 B. AK 2: `HistoryTab.test.tsx` Zeile 129 ist das `it(` des Falls; die alten Prüfzeilen standen am Elternstand
  `37e6f6e` auf 446/739; `grep -rn '"Name der neuen Reihe", { exact: true }'` liefert 10; die drei nachgetragenen Formulare per `sed -n`
  gelesen (`VaterProfil.tsx:111`, `VaterZiele.tsx:433`, `PlanPositions.tsx:490`). **Abgenommen** mit Sprint 2 (Rollengang: der
  Entwickler ist die Rolle; Doku, kein Verhalten). Eine Entgleitung im Increment dieses Laufs: **Limit 2**.
- **2026-11-05** — Commit `b32460e`.
