---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [B-291 Mutantenzeilen veraltet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-z
unverifiziert: false
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
- **2026-11-05** — erledigt in Lauf AA, Sprint 16. **Alle sechs** Mutanten der B-291-Tabelle am heutigen Teststand neu gemessen (Skript mit
  Build-Prüfung, nach dem letzten Test-Edit von B-401 – Vorschlag (BU) angewandt): Übungs-Themen weg → `Thema_An_Einer_Uebung` +
  `Nur_Die_Eigenen`; Unit-Zählung weg → `Thema_An_Einer_Unit` + `Nur_Die_Eigenen`; Kategorie weg → nur `Kategorie…`; Clear ignoriert →
  beide „Fach leeren"-Fälle; Reihenfilter Übungen weg → `Nur_Die_Eigenen` + beide „Fach leeren"; Reihenfilter Units weg → nur
  `Nur_Die_Eigenen`. Jede Bedingung hat damit mindestens einen roten Fall. Tabelle ersetzt, Spalte heißt jetzt „Mutante und was sie rot
  macht" (die alte Überschrift „nur ihn" war für drei Zeilen nie wahr). Kein Code; kein Review-Agent und kein Rollengang (Doku-Berichtigung,
  die Messung ist der Beleg).
- **2026-11-05** — **abgenommen.** Commit: siehe nächster Eintrag.
