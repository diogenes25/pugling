---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Sprachwechsel behält Store-Bindung]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-x
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-396 · Wer im Übersetzungs-Editor nur das Sprachpaar ändert, behält die Bindung an die Vokabel des alten Sprachpaars

`frontend/src/vater/exerciseConfig.tsx:294` behält die `vocabularyId` eines Paars, solange Quelle und Ziel unverändert sind – auch wenn
`sourceLang`/`targetLang` der Übung geändert wurden. Das Paar zeigt dann auf einen Speicher-Eintrag des alten Sprachpaars. Zu klären:
ob ein Sprachwechsel alle Bindungen lösen soll (dann legt der Server seit B-392 neu an bzw. findet), und ob der Server das prüfen soll
(B-387 hat die Sprachpaar-Prüfung bewusst ausgelassen – dort für Birkenbihl).

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf W, Sprint 9 (Lauf X, Retro Sprint 10; frischer `pugling-reviewer`, 🟢). Älter als
  B-392, darum ohne `entgangen_bei`; am Code gelesen, nicht im Browser gemessen (`unverifiziert: true`).
