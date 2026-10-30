---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Vokabel-Batch nicht mehr idempotent]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf K, Teil 2 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-277]
wartet_auf: ""
---

# B-345 · Der Vokabel-Batch prüfte die Grundform vor dem belegten Schlüssel und war nicht mehr idempotent

Nachgetragen fürs Wirkungsmaß: `0fcff19` ([B-277](B-277-schnellanlage-legt-doppelten-store-eintrag-an.md)) zog die
`BaseFormKey`-Prüfung in `CreateCoreAsync` vor die Prüfung auf einen belegten expliziten Schlüssel („Before the reuse
branch"). Vorher kam der Konflikt zuerst, und ein wiederholter Batch meldete wie dokumentiert `existing`. Danach
scheiterte er an der Grundform – eine Regression.

**Behoben vom Nutzer außerhalb der Schleife** in `3190f9e` („Vokabel-Batch: belegter expliziter Schlüssel meldet
‚existing' vor der Grundform-Prüfung").

## User Story

Als **Creator**, der denselben Vokabel-Batch zweimal schickt, möchte ich beim zweiten Mal `existing` je Eintrag, wie
dokumentiert, statt eines Grundform-Fehlers.

## Ist-Stand (am Code belegt)

Siehe oben: Reihenfolge in `CreateCoreAsync` vor und nach `0fcff19`; heute meldet ein belegter expliziter Schlüssel
wieder zuerst `existing` (`3190f9e`).

## Entscheidungen

Keine eigenen – vom Nutzer in `3190f9e` gebaut; hier nur nachgetragen.

## Akzeptanzkriterien

1. Ein wiederholter Batch mit explizitem Schlüssel meldet `existing` vor der Grundform-Prüfung (`3190f9e`, Tests in
   `VocabularyStoreTests`).

## Schätzung

`XS`, Backend, keine Migration, kein Vertragsbruch (gebaut in `3190f9e`).

## Verlauf

- **2026-10-30** — angelegt in der Nachschau zu Lauf K, Teil 2 (`pugling-reviewer`, Diff von `0fcff19` gegen seinen
  Vorgänger gelesen): Fluchtweg von B-277, außerhalb der Schleife gefunden und behoben. `abgenommen` steht für
  `3190f9e` des Nutzers.
