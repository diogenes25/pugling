---
tags: [typ/story, status/idee, bereich/ki-agent, rolle/creator]
aliases: [--general --use-weak zieht die schwachen Wörter trotzdem heran]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: pugling-reviewer zu B-21 (dritter Nachtlauf, docs/pm-sitzung-2026-10-23-nacht.md), angelegt in Lauf C (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-279 · `--child X --general --use-weak` zieht die schwachen Wörter weiter in den Pflicht-Wortschatz

`--general` lässt beim KI-Creator das Kind als Zuschnitt fallen: Die Übung soll allgemein sein, nicht für dieses
Kind. `BriefingBuilder.BuildAsync` übernimmt die schwachen Wörter des Kindes aber weiter in `RequiredWords`
(`backend/Pugling.Agent.Creator/Briefing/BriefingBuilder.cs`, Vorrang „ausdrücklich > schwache Wörter des Kindes“),
sobald `--use-weak` gesetzt und ein Kind angegeben ist. Eine „allgemeine“ Übung trägt dann den Förderwortschatz
eines bestimmten Kindes. Das Verhalten ist älter als [B-21](B-21-ki-creator-foerdermodus.md), der Reviewer hat es dort
benannt. Aus Code gelesen, nicht gemessen.

Zu entscheiden: Bricht die Kombination ab wie `--use-weak` ohne `--child`, oder wird `--use-weak` unter `--general`
still ignoriert? Abbrechen ist die Linie von B-21 AK 4.

## Verlauf

- **2026-10-26** — angelegt in Lauf C ([Protokoll](../pm-sitzung-2026-10-26-tag.md)) aus dem Review-Hinweis zu B-21
  vom 2026-10-23 („eigene Story wert“).
