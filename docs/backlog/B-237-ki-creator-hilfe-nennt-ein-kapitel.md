---
tags: [typ/story, status/idee, bereich/ki-creator]
aliases: [Agent.Creator --chapter, Zielkapitel in der Konsolenhilfe]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachtlauf 2026-10-23, Sprint 2 (Sweep zu B-166)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-237 · Die Hilfe des KI-Creators bietet `--chapter <id>` an – ein Kapitel gibt es seit B-106 nicht mehr

Beim Sweep zu [B-166](B-166-wiki-nennt-die-entfernte-kapitel-route.md) (tote Kapitel-Route in der Doku) fiel
`backend/Pugling.Agent.Creator/AgentCommands.cs:459` auf: die Konsolenhilfe nennt `--chapter <id>  Zielkapitel
(Standard: erstes Kapitel des Fachs; allein angegeben bestimmt es auch das Fach)`. Dieselbe Datei sagt in
`:359` selbst: „since B-106, exercises hang off a series unit, not a chapter."

**Nicht geprüft** (darum `unverifiziert`): ob `--chapter` noch geparst wird (und was es dann bedeutet – Alias
für eine Unit?), ob es nur toter Hilfetext ist, oder ob der Agent damit einen Aufruf gegen eine Route baut,
die 404 liefert. Der Befund ist der Hilfetext, nicht das Verhalten.

## Offene Punkte

1. Was macht `--chapter` heute? Erst `AgentCommands.cs` lesen (Parser + Auflösung ab `:359`), dann entscheiden:
   Alias auf `--unit` behalten (mit Hilfetext, der die Wahrheit sagt) oder entfernen.
2. Gibt es im `Pugling.Agent.Creator/CLAUDE.md` und im Skill `ki-creator` dieselbe Formulierung? Beim
   Ausformulieren greppen.

## Verlauf

- **2026-10-23** — angelegt im Nachtlauf (Sprint 2) aus dem B-166-Sweep über `chapter` in Backend-Kommentaren.
  **Bewusst nicht in B-166 mitgenommen:** dort geht es um Doku-Routen; hier hängt ein Verhalten des
  Konsolenagenten dran, das erst zu lesen ist.
