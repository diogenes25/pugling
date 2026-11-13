---
tags: [typ/story, status/idee, bereich/gamification, rolle/student]
aliases: [Bonus-Deckel verbrennt Einmal-Übung]
status: idee
prio: P2
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Nachschau zu B-533 in der Retro von Sprint 69 (Backlog-Lauf 2026-11-13, docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-533]
wartet_auf: ""
---

# B-543 · Nach erreichtem Bonus-Deckel verbrennt eine Einmal-Übung für 0 Münzen

Das Bonus-Training ([B-533](B-533-bonus-training-extra-muenzen.md)) deckelt die Münzen je Tag. Der Deckel wirkt aber
erst bei der Abgabe: `ExtraTrainingService.StartAsync` prüft ihn nicht (`ExtraTrainingService.cs:78-93`),
`SubmitAsync` kappt die Münzen auf den Rest und setzt die Runde trotzdem auf `CompletedAt` (`:144-153`), und damit
gilt die Übung dauerhaft als erledigt (`:256`). Die Oberfläche lässt sie weiter starten (`SohnBonus.tsx:103`, `:124`)
und verspricht dabei „Tagesdeckel erreicht – … Münzen gibt es morgen wieder“ (`de.ts:160`).

**Szenario:** 30/30 Münzen erreicht, das Kind spielt eine Einmal-Übung im Wert von 4 Münzen → 0 Münzen, die Übung ist
für immer weg. Bei 1 Münze Rest zahlt sie 1 statt 4. Bei `BonusDailyCoinCap = 0` verbrennt jede Einmal-Übung. Kein
Betrug, aber unfair, und gegen B-533 Entscheidung 4 („eine Übung zahlt einmal“). Kein Test deckt den Fall.

**Randfall (geringe Wirkung, gleiche Stelle):** Gewertet wird gegen die *aktuellen* Items (`:110`, `:137`), nicht
gegen die beim Start festgehaltenen; das gespeicherte `ItemCount` wird nirgends benutzt. Ändert der Autor die Übung
zwischen Start und Abgabe, zählen neue Items als falsch.

## Verlauf

- **2026-11-13** — angelegt aus der Nachschau zu B-533 (`pugling-reviewer`, Retro Sprint 69), `entgangen_bei: [B-533]`.
