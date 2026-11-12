---
tags: [typ/story, status/idee, bereich/ki, rolle/creator]
aliases: [Nachschau-Reste Sprint 33, doppeltes Grammatik-Label verschwindet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 33)
unverifiziert: true
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-482 · Nachschau-Reste Sprint 33: Kleinigkeiten an Ablage und NEU-Marker

Drei 🟢 der Nachschau von Sprint 33 (frischer `pugling-reviewer`, gelesen, nicht nachgestellt), gesammelt, damit
sie nicht verloren gehen:

1. **Doppeltes Label verschwindet** (`GrammarOrder.cs:39`): eindeutig ist nur `(SubjectId, Slug)`. Tragen zwei
   Themen dasselbe markierte Label, fallen beide aus dem Rest, aber nur eines steht vorn – eines fehlt im Briefing.
2. **Andere Trenner nach `NEU`** (`GrammarOrder.cs:13`): `NEU – question tags` oder `NEU „question tags"` greifen
   still nicht; `FORMAT.md` nennt nur „Label wörtlich", keine Trenner.
3. **Zwei Bücher derselben Reihe** (`AgentCommands.cs:434`): `FirstOrDefault` entscheidet nach Listenreihenfolge;
   dazu eine Zusicherung im Test `Ohne_Ortsangabe_…`, die nur den Ausgangszustand prüft (`Assert.Empty` auf Unit 1).

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 33 im Backlog-Lauf).
