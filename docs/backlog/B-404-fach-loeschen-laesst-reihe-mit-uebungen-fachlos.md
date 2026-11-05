---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Fach löschen macht Reihe fachlos]
status: idee
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-z
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-404 · Ein gelöschtes Fach lässt Reihen mit Übungen fachlos zurück

`TextbookSeries.Subject` ist `OnDelete(SetNull)` (`PuglingDbContext.cs` um `:250`), und `SubjectsController.Delete` sperrt nur bei
Schlüsselergebnissen und Stundenplan-Einträgen (B-144). Ein gelöschtes Fach erzeugt damit genau den Zustand, den
[B-401](B-401-fach-leeren-laesst-uebungen-ohne-fach.md) beim Leeren verweigert: eine Reihe ohne Fach, die Übungen trägt. Die E2E
„Ein gelöschtes Fach bleibt als Freitext sichtbar…" in `lehrwerk-bearbeiten.spec.ts` erzeugt ihn sogar absichtlich.

**Frage:** soll das Löschen eines Fachs mit 409 verweigert werden, solange eine Reihe dieses Fachs Übungen trägt – oder ist „Fach weg,
Name bleibt als Freitext" (B-142) der gewollte Rückweg, und die Sperre aus B-106 T-01 gilt nur fürs Anlegen?

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-401 (Lauf Z, Sprint 15; `pugling-reviewer`, 🟡). Als `Frage`, nicht im Lauf entschieden:
  die Antwort widerspricht einer bestehenden, absichtlich gebauten E2E und ist darum eine Produktentscheidung, kein Herstellen.
