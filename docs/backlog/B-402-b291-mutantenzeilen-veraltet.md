---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [B-291 Mutantenzeilen veraltet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-z
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-291]
---

# B-402 · Zwei „gemessen"-Zeilen in B-291 gelten nicht mehr, seit der Review-Fall dazukam

Die Mutanten der AK-Tabelle von [B-291](B-291-fachwechsel-der-reihe-laesst-themen-stehen.md) wurden **vor** dem Review gemessen; danach kam
`Nur_Die_Eigenen_Traeger_Der_Reihe_Zaehlen` dazu, der „1 exercise(s) and 1 unit(s)" zusichert. Seitdem macht „Übungs-Themen-Klausel weg"
auch ihn rot (Zeile unvollständig) und „Unit-Zählung weg – gemessen: nur dieser Fall" ist falsch. Dieselbe Klasse wie B-399: eine
Belegzeile, die nach einer Änderung am Test nicht nachgemessen wurde.

Zu tun: die zwei Zeilen nachmessen und berichtigen.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf Z, Sprint 14 (Retro Sprint 15; frischer `pugling-reviewer`, `git show b6e3724`, 🟡),
  am Code nachgerechnet, nicht gemessen. Increment **dieses** Laufs: **Limit 2**, Lauf Z endet nach Sprint 15.
