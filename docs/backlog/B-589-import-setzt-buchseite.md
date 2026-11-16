---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator]
aliases: [laden.py Buchseite, bookPages beim Import]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-588 (Verfeinerung im Backlog-Lauf 2026-11-16)
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-589 · Import und KI-Creator setzen die Buchseite und löschen sie nicht

[B-588](B-588-uebung-zeigt-ihren-lehrwerk-bezug.md) gibt der Übung ein Feld `bookPages`. Übungen werden per
PUT als volle Darstellung geändert, ein fehlendes Feld wird gelöscht. Ein `laden.py --ersetzen`-Lauf
(Skill `lehrwerk-aufbereiten`) schickt dieses PUT heute ohne `bookPages` – eine von Hand gepflegte Seite
ginge damit still verloren. Die Seiten-Markdowns kennen ihre Seiten (`seiten:`), `uebungen.json` und die
Vokabel-TSV nicht; der KI-Creator kennt sie ebenfalls nicht.

Gesucht: der Import übernimmt die Seite aus den Seiten-Markdowns (oder erhält mindestens den vorhandenen
Wert), der KI-Creator reicht sie durch, wo er sie kennt.

## Verlauf

- **2026-11-16** — angelegt als Folge aus der Verfeinerung von B-588; als `Defekt` geführt, weil B-588 den
  Datenverlust bei `--ersetzen` erst möglich macht.
