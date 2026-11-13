---
tags: [typ/story, status/verworfen, bereich/ki-agent, rolle/creator]
aliases: [Gewähltes Profil setzt die Reihe, KI-Creator übernimmt Reihe des Fachlehrers]
status: verworfen
prio: P3
art: Wunsch
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Morgen-Entscheidung 2026-10-26 zu B-256 (docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: false
grund: "gegenstandslos durch B-473 (e94aa1e1): ohne --profile/--series nimmt der Agent das zum Kind beste Profil und dessen Reihe (AgentCommands.cs:507-513)"
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-271 · Das zum Kind gewählte Profil setzt Reihe und Fach des KI-Creators

## User Story

Als Creator, der den KI-Creator für ein Kind ohne `--profile` aufruft, möchte ich, dass die Übung in der Reihe des
Fachlehrers entsteht, der zum Kind gewählt wurde. Dann landet sie dort, wo der passende Fachlehrer sie erwartet.

## Ist-Stand (unverifiziert, aus B-256)

`BuildRequestAsync` lädt ein Profil nur bei ausdrücklichem `--profile` (`AgentCommands.cs`). Das zum Kind gewählte
Profil (`BriefingBuilder.ResolveProfileAsync`) wird erst nach der Ortsauflösung bestimmt und verschiebt sie nicht.
Hilfe und README sagen das seit [B-256](B-256-sprint-5-drei-saetze-nach-der-abnahme.md) ehrlich.

## Entscheidungen

1. **Ja:** Ohne `--profile` übernimmt der Agent Reihe und Fach des zum Kind am besten passenden Profils.
   `--series` übersteuert weiterhin. Entschieden vom Nutzer am 2026-10-26.

## Verlauf

- **2026-10-26** — angelegt aus der Morgen-Entscheidung zu B-256 ([Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)).
  Ausdrücklich **nicht** Teil der Nachtläufe vom 2026-10-26, darum keine Freigabe-Zeile.
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): Die Entscheidung des Nutzers vom 2026-10-26 gilt unverändert; neu ist nur die Freigabe. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-13** — `verworfen`: Backlog-Abgleich gegen den Code. Entscheidung 1 ist mit [B-473](B-473-ki-creator-ablage-ignoriert-buch-des-kindes.md) gebaut (`BestProfileForChildAsync`, `AgentCommands.cs:507-513`), verfeinert durch B-481/B-484. Nichts mehr zu bauen.
