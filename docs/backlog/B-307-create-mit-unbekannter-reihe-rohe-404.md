---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [create --series unbekannt, rohe 404 im create-Verb]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf F Teil 2 (docs/pm-sitzung-2026-10-27-nacht.md, Lauf F Teil 4)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-22, B-106]
wartet_auf: ""
nachgeschaut: "2026-10-28"
---

# B-307 · `create --series 99999 --series-unit 1` endet als rohe 404 statt als Fehlbedienung

Das Geschwister von [B-301](B-301-fill-mit-unbekannter-reihe-endet-als-rohe-404.md) im Verb `create`: sind `--series`
und `--series-unit` beide gegeben, übernimmt `AgentCommands.ResolveLocationAsync` sie ungeprüft. Danach lesen
`ResolveSubjectIdAsync` (ohne Profil) bzw. `BriefingBuilder.BuildAsync` (mit Profil, Plan, Klausur) die Reihe per
`GetSeriesAsync` – ohne `NotFound`-Fang. Ergebnis: „Die Pugling-API antwortete mit 404“, Exit 1, statt Exit 2 mit
Gebrauchstext.

**Behebung:** an beiden Stellen derselbe Fang wie in `UnitNotesFiller` („Reihe {id} gibt es nicht – prüfe die Id im
Vater-Web.“); je ein Test, der den Typ, die Id und `chat.Calls == 0` zusichert.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf F Teil 2 (`26e731a`) in Lauf F, Teil 4
  ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)), 🟢 des frischen Prüfers außerhalb des Diffs; `entgangen_bei: B-22`
  (Verb) und `B-106` (seitdem hängt `create` an Reihe/Unit). Als `Defekt` unter Freigabe 1 ausformuliert, geschätzt
  (XS) und in den laufenden Sprint genommen – derselbe rote Faden („kein Agent-Verb endet roh“).
- **2026-10-27** — gebaut: `Create_mit_unbekannter_Reihe_ist_eine_Fehlbedienung` (Befehlszeile) und
  `Das_Briefing_mit_unbekannter_Reihe_ist_eine_Fehlbedienung` (Pipeline) → rote Probe **2 failed** (gemessen: rohe
  `PuglingApiException` 404 an `textbook-series/987654`). Fix an beiden Stellen → grün. Mutante je Fang
  „`NotFound` → `Gone`“ → je **1 failed**, jeweils genau am eigenen Test. `CreatorAgentTests` **62/62**, Suite
  **984/984 `exit=0`**.
- **2026-10-27** — Review (`pugling-reviewer`): beide Testpfade nachvollzogen (`--child` ohne `--profile` erreicht
  `ResolveSubjectIdAsync`; die Pipeline erreicht den Briefing-Fang); die übrigen `Get…Async` im Briefing lesen nur
  Ids aus Profil/Reihe/Kind, nie getippte. 🟡 im Diff (zählt nicht – Behebung ändert nur Kommentare): die neuen Tests
  hatten sich zwischen `<summary>` und `[Fact]` des Intensitäts-Tests geschoben → Block zurückverschoben. 🟢
  zurückgestellt: dieselbe Meldung steht jetzt dreimal (ein Helfer verhindert Drift); `plan`/`exam` wiederholen die
  Fehlbedienung je Teil, `profiles --child` evtl. roh → [B-308](B-308-agent-fehlbedienung-plan-exam-profiles.md).
  Rollengang: Agent ohne Oberfläche, Tests sind der Weg. **`abgenommen`.**
- **2026-10-28** — nachgeschaut (Lauf H, Teil 1, frischer `pugling-reviewer` am Commit `d8e4a0a`): jedes Verb mit einer
  getippten unbekannten Reihen-Id durchgespielt (`create`/`briefing` mit und ohne Profil-Fach, `exam`/`plan` mit und ohne
  `--title`, `fill`, `profiles`); Mutanten beider Fälle hergeleitet (Fang `AgentCommands.cs:476` bzw.
  `BriefingBuilder.cs:31` entfernt), je rot. **Entgangen:** `--series 987654` ohne `--series-unit` bekam weiter die falsche
  Diagnose – identisch mit [B-311](B-311-agent-reihensuche-liest-nur-50-reihen.md), dort ergänzt und im Sprint behoben.
  🟢: `Assert.Equal(0, chat.Calls)` kann nie fallen → als Absichtserklärung beschriftet (Kommentar, ohne Zählung).
