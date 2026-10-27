---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [Fehlbedienung je Teil wiederholt, profiles --child unbekannt]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Review zu B-306/B-307 in Lauf F, Teil 4 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-19, B-22]
wartet_auf: ""
---

# B-308 · Zwei Rest-Pfade des Agenten bei einer falschen Id: `plan`/`exam` melden sie je Teil, `profiles` evtl. roh

Zwei Hinweise des Prüfers aus Lauf F, Teil 4, beide außerhalb des Diffs, beide **nicht gemessen**:

1. `plan`/`exam` mit `--profile`, `--title`, einer unbekannten `--series` und einer `--series-unit`: die Fehlbedienung
   (seit [B-307](B-307-create-mit-unbekannter-reihe-rohe-404.md) keine rohe 404 mehr) kommt erst aus dem Briefing, und
   `PlanPlanner.RunAsync` (~Zeile 161) bzw. `ExamPlanner.RunAsync` (~Zeile 76) fangen `AgentUsageException` je Teil –
   dieselbe Meldung erscheint N-mal als gescheiterter Teil, statt den Lauf einmal mit Exit 2 zu beenden. Kein
   Modellaufruf geht verloren.
2. `profiles --child <unbekannt>`: `AgentCommands.ShowProfilesAsync` (~Zeile 161) ruft `MatchProfilesAsync` ohne
   `NotFound`-Fang. Ob der Server dort 404 antwortet, ist ungeprüft.

**Erster Schritt:** beide messen (Test mit `--series 987654` im `plan`-Verb; `profiles --child 987654`). Fällt keiner,
ist die Story `verworfen`.

## Verlauf

- **2026-10-27** — angelegt aus dem Review zu B-306/B-307 (Lauf F, Teil 4, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Funde 6 und 7 (🟢, außerhalb des Diffs). Eigene Story, weil das Sprint-Ziel ohne sie erfüllt ist; `idee`, weil
  unverifiziert.
- **2026-10-27** — in Lauf F, Teil 5 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)) als `Defekt` unter Freigabe 1
  ausformuliert, geschätzt (XS) und gebaut. **Gemessen:** `Plan_und_Klausur_mit_unbekannter_Reihe_brechen_einmal_ab`
  (plan/exam mit `--profile`, `--title`, Reihe 987654) → **2 failed** („No exception was thrown“ – je Teil
  verschluckt, Exit 1); `profiles --child 987654` → **1 failed** (rohe 404 an `profiles/match`). Fix: beide Planer
  briefen einmal vor den Teilen, ob ein Titel gegeben ist oder nicht (das Briefing liest nur); `ShowProfilesAsync`
  fängt 404 und 403 wie `LoadChildAsync`. Review (`pugling-reviewer`): kein Briefing-Fehler ist teil-spezifisch (nur
  der unbekannte Typ, und der kommt vor dem Briefing und bleibt je Teil); einzige Verhaltensänderung: ein 5xx im
  Vor-Briefing beendet einen `--title`-Lauf jetzt roh wie einen ohne Titel. **Zwei 🟡 im Diff, beide gezählt:** die
  403-Hälfte des Fangs war ungetestet → `Profile_fuer_ein_fremdes_Kind_sind_eine_Fehlbedienung` (Seed-Konto 2,
  Herr Schmidt, betreut kein Kind), Mutante „Fang nur `NotFound`“ → **1 failed**; `Assert.Equal(0, chat.Calls)` im
  Planer-Test konnte nie fallen (das Briefing scheiterte auch vorher vor jedem Modellaufruf) → gestrichen, im
  Kommentar begründet. 🟢 genommen: Meldung „es existiert nicht … Prüfe Pugling:AccountId/Pugling:Pin.“, Kommentar als
  Regel statt Umbau-Erzählung. 🟡 außerhalb → [B-310](B-310-profil-match-verraet-kind-ids.md). Suite **991/991
  `exit=0`**. Rollengang: Agent ohne Oberfläche, Tests sind der Weg. **`abgenommen`.**
