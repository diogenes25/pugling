---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [B-398 Mutantenzeile falsch]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-y
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-398, B-397]
---

# B-399 · Eine Belegzeile in B-398 nennt eine Mutante, die nicht kompiliert, und B-397 eine verrutschte Zeile

Der `## Verlauf` von [B-398](B-398-pflichtfelder-die-optional-gemeint-sind.md) behauptet „`?? ""` im Satz-Handler entfernt → rot `:193`".
Die Mutante kompiliert nicht (`body.NaturalTranslation.Trim()` auf `string?` → CS8602, Warnungen sind Fehler); mit unterdrückter Warnung
fiele schon `:192` (Null-Dereferenz → 500). `:193` fällt allein nur unter einer Mutante, die `null` speichert (etwa `?.Trim()`). Die Zeile
stand dort als **ungemessen**, aber ohne diese Kennzeichnung im Satz selbst. Daneben nennt [B-397](B-397-null-element-in-config-liste-ist-500.md)
`CatalogReadDeleteTests.cs:379-381` für die drei Batch-Fälle; richtig ist `:380-382` (`:379` ist `[Theory]`).

Zu tun: die Mutante `?.Trim()` **messen** und die Zeile berichtigen; die Zeilenangabe in B-397 berichtigen.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf Y, Sprint 12 (Retro Sprint 13; frischer `pugling-reviewer`, `git show 49e773d`, 🟡
  bzw. 🟢). Increment **dieses** Laufs: **Limit 2**, Lauf Y endet nach Sprint 13.
