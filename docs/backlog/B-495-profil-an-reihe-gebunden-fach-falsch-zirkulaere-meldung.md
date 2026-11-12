---
tags: [typ/story, status/idee, bereich/werkzeug, rolle/creator]
aliases: [ProfileFit zirkuläre Meldung, Profil an Reihe mit fremdem Fach]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 39 in der Retro von Sprint 40)
unverifiziert: false
entgangen_bei: [B-488]
grund: ""
ersetzt_durch: []
---

# B-495 · Ein an eine Reihe gebundenes Profil mit fremdem Fach schickt den Creator im Kreis

Nachschau von Sprint 39 (frischer `pugling-reviewer` an `ed43705`, Commit per `git archive` exportiert, dort gebaut und
getestet: 1385/1385; die Fehlerfolge gelesen, nicht gefahren).

Der Server nimmt ein Profil an, dessen `SeriesId` und `SubjectId` auseinanderlaufen
(`CreatorProfilesController.ReferenceProblemAsync` prüft nur, dass beide existieren; das Fach einer Reihe lässt sich
später per PATCH ändern). Profil P mit Reihe Y (Englisch) und Fach Latein:

1. `create --profile P` in Reihe Y → Fachprüfung in `ProfileFit` (`backend/Pugling.Agent.Creator/Briefing/ProfileFit.cs:14-21`)
   rät „wähle ein Profil für Englisch oder **eine Reihe in Latein**".
2. `--series L` (eine Latein-Reihe) → Reihenprüfung: „gehört zur Reihe Y … lass --series/--series-unit weg".
3. Zurück zu 1. Dasselbe mit nur `--child`: `BestProfileForChildAsync` wählt P über `series_match`.

Die Meldung nennt einen Ausweg, der an einer zweiten Sperre endet (Meldungsregel 2026-11-07). Vor `ed43705` lief dieses
Profil (mit falschem Fach), jetzt ist es gar nicht mehr nutzbar.

## Reste aus derselben Nachschau (🟢)

- Ein Profil mit Freitext-Fach (`SubjectId == null`, `SubjectName = "Latein"`, B-142) passiert `ProfileFit` immer; der
  Prompt nennt dann „Fach: Latein" (`CreatorBriefing.cs:101`) neben „Fach: Englisch" (`:109`).
- Ein direkter Pipeline-Aufruf mit einer Reihe ohne Fach prüft nicht, dass das Fach der Anfrage das des Profils ist
  (`BriefingBuilder.cs:57`); die Konsole erzeugt so eine Anfrage nicht (`AgentCommands.cs:531`).
- Veraltete Summary an `ResolveProfileAsync` (`BriefingBuilder.cs:230-235`); `ProfileFit.cs:16` gibt einen CLI-Tipp auch
  direkten Aufrufern.
- Der B-308-Test sagt „rot, wenn die Prüfung fehlt" – für die Plan-Hälfte allein nicht messbar (in B-488 offen benannt);
  ein Satz in der Summary fehlt.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 39, Retro Sprint 40 im Nachtlauf).
