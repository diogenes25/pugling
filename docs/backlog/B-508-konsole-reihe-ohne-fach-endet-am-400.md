---
tags: [typ/story, status/idee, bereich/werkzeug, rolle/creator]
aliases: [series_without_subject im Agenten, Reihe ohne Fach roher 400]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Sprint 47, gefunden beim Testen von B-506)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-508 · Konsole: eine Reihe ohne Fach endet bei `create`/`plan`/`exam` erst am rohen 400 des Servers

Gefunden beim Bauen von B-506 (Sprint 47), bewusst nicht dort gebaut – das Sprint-Ziel („die Konsole wählt nur, was das
Briefing annimmt") hält ohne.

- `BuildRequestAsync` (`backend/Pugling.Agent.Creator/AgentCommands.cs`, „The exercise's subject …") nimmt für eine Reihe
  ohne Fach das Fach des Profils (B-488); das Briefing (`ProfileFit.Problem`) nimmt das Profil an.
- Der Server lehnt jede Übung in einer Reihe ohne Fach ab: `ExerciseControllerBase.cs:349`, `series_without_subject`.
- Folge: `create` läuft durch Briefing und Modellaufruf und endet dann an einer `PuglingApiException` (400) statt an einer
  Fehlbedienung mit Ausweg („setze ein Fach im Vater-Web"). Gemessen im ersten Lauf von
  `CreatorAgentTests.Reihe_ohne_Fach_nimmt_den_Fachlehrer` (vor dem Umstellen auf `briefing`). Das gilt schon vor
  B-506 für den gewöhnlichen Fall (Buchfach = Profilfach auf einer fachlosen Reihe); die bestehenden Tests
  (`Nur_mit_Kind_in_einer_Reihe_ohne_Fach_spricht_der_Fachlehrer`) weichen auf `briefing` aus.

Frage fürs Ausformulieren: die Fehlbedienung vor dem ersten Modellaufruf für jedes schreibende Verb (wie `--types`,
B-313), während `briefing`/`--dry-run` weiter das Profilfach zeigen dürfen – oder der Fallback auf das Profilfach
entfällt ganz, weil er nur noch für nicht schreibende Verben trägt.

## Verlauf

- **2026-11-12** — angelegt (Sprint 47, beim Testen von B-506).
