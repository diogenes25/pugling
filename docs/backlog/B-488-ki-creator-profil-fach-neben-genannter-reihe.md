---
tags: [typ/story, status/idee, bereich/ki, rolle/creator]
aliases: [werkunabhängiges Profil neben fremder Reihe, Fach des Profils gegen Fach der Reihe]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 37)
unverifiziert: false
entgangen_bei: [B-484]
grund: ""
ersetzt_durch: []
---

# B-488 · Ein werkunabhängiges Profil neben `--series` prüft das Fach nicht

Nachschau von Sprint 37 (frischer `pugling-reviewer` an `1c90c8d`; gelesen, gebaut, `CreatorAgentTests` 125/125, der
Fehlerfall gelesen, nicht gefahren). B-484 prüft das Fach eines werkunabhängigen Profils nur auf dem **abgeleiteten**
Pfad (`SeriesOfChildsBookAsync` nimmt nur Bücher im Fach des Profils) – nicht, wenn der Ort genannt ist. Und die
neue Fehlermeldung schickt genau auf diesen ungeprüften Pfad („gib --series <id> an").

1. **`create --profile <Latein, ohne Reihe> --series <Englisch-Reihe>`** (`AgentCommands.cs:502-510`, `:529`): der
   Request trägt `SubjectId = Latein` aus dem Profil, `SeriesId = Englisch`. Im Briefing filtert die Übungssuche das
   Fach über die Reihe (`BriefingBuilder.cs:73`) und findet nichts – keine Wortliste der Unit, keine „nicht
   wiederholen"-Liste, keine Grammatik-Themen (sie kommen aus dem Fach Latein). Der Prompt sagt „Fach: Latein" und
   „Lehrwerk: Access", die Übung landet im Englischbuch, Exit 0.

## Reste aus derselben Nachschau (🟢)

- `--profile P(Reihe Y) --series-unit U(Reihe X)` ohne `--series` meldet „Unit U gibt es nicht in Reihe Y", statt den
  Profil/Ort-Konflikt zu benennen (`AgentCommands.cs:507`, `:662`); der Test deckt nur `--series`.
- `--topic` fließt nicht mehr in den abgeleiteten Titel von `exam`/`plan` (`ExamPlanner.cs:124`, `PlanPlanner.cs:331`);
  ihre `<summary>` sagt noch „otherwise the topic". `CreatorBriefing.cs:6-7` nennt das Werk noch als Teil des Profils.
- `CreatorBriefing.SeriesUnitLabel`/`SeriesUnitId` und `Material.Unit` tragen denselben Wert – zwei Felder für eine
  Entscheidung.
- `--series-unit` **allein** (Reihe aus der Katalogsuche) läuft in keinem der neuen Tests; gelesen korrekt.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 37 im Backlog-Lauf).
