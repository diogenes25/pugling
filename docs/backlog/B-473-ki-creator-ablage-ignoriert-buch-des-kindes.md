---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator]
aliases: [Ablage ohne --series, erste Reihe mit Unit]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: B-470 (Briefing-Probe beim Bau des Skills lehrwerk-aufbereiten, 2026-11-11)
unverifiziert: true
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-473 · Ohne `--profile` und `--series` legt der KI-Creator die Übung in der ersten Reihe überhaupt ab, nicht im Buch des Kindes

Probe auf einer frischen Seed-DB mit geladenem Access-Buch: Kind 1 hat Klasse 7, Gymnasium und als Englischbuch
„English G Access G9 – Band 3“ mit Unit 1. `briefing --child 1 --type Vocabulary` ergibt trotzdem „Fach:
Französisch, Unit: Unité 2 – En ville“, mit französischen Wörtern. `profiles --child 1` nennt Access als besten
Treffer.

Ursache, gelesen in `AgentCommands.BuildRequestAsync`/`ResolveLocationAsync`: Ohne `--profile` und
`--series`/`--series-unit` wird die Ablage zur „ersten Reihe mit Unit“ (im Seed „Découvertes 1“), und das Fach
kommt aus dieser Reihe. Erst danach sucht der Agent das Profil zum Kind, aber im falschen Fach. Das Lehrbuch des
Kindes zieht die Ablage nie heran. Das README beschreibt dieses Verhalten als Vorgabe; für einen Aufruf mit
`--child` ist es trotzdem ein stiller Griff ins falsche Regal.

Umgangen im Skill `lehrwerk-aufbereiten`: Der Aufruf nennt immer `--profile`, `--series-unit` und `--unit`.
Naheliegende Abhilfe: Mit `--child` und ohne Ortsangabe gilt das bestpassende Profil zum Kind (ohne Fachfilter)
mit der aktuellen Unit seines Buchs in dessen Reihe; sonst eine Fehlbedienung statt der ersten Reihe.

Nicht in dieser Sitzung gebaut, weil eine andere Sitzung gerade `BriefingBuilder.cs` und `CreatorAgentTests.cs`
umbaut (B-472).

## Verlauf

- **2026-11-11** — angelegt (beim Bau des Skills `lehrwerk-aufbereiten`).
