---
tags: [typ/story, status/geschaetzt, bereich/agent, bereich/katalog, rolle/creator]
aliases: [Rückmeldung im KI-Creator, Feedback im Briefing]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-623
grund: ""
ersetzt_durch: []
---

# B-630 · Der KI-Creator berücksichtigt die Rückmeldungen beim Erstellen neuer Übungen

Teil 3 von 3 aus [B-623](B-623-feedback-knopf-an-der-karte.md), beim Schätzen am 2026-11-17 geteilt. Hängt an der
API aus [B-628](B-628-rueckmeldung-backend.md).

## User Story

Als **Vater**, der Übungen über den KI-Creator erstellen lässt, möchte ich, dass er berücksichtigt, was meine
Kinder zu bisherigen Übungen gemeldet haben, damit neue Übungen nicht dieselben Fehler wiederholen.

## Ist-Stand am Code

Siehe [B-623 → Ist-Stand am Code](B-623-feedback-knopf-an-der-karte.md#ist-stand-am-code): Das Briefing
(`backend/Pugling.Agent.Creator/Briefing/CreatorBriefing.cs:17`–`:35`) kennt keine Rückmeldungen. Ergänzt beim
Schätzen (2026-11-17):

- Vorbild für kindbezogene Fakten im Briefing: die schwachen Wörter
  (`backend/Pugling.Agent.Creator/Briefing/ChildFacts.cs:23` `WeakWords`, geladen in
  `backend/Pugling.Agent.Creator/Briefing/BriefingBuilder.cs:71`, Obergrenze `:18`).
- Die Prompt-Regeln liegen in `backend/Pugling.Agent.Creator/Drafting/DraftPrompts.cs` und `DraftRules.cs`.
- Agent-Tests liegen in `backend/Pugling.Api.Tests` (Vorbild `AssignAgentTests.cs`).

## Entscheidungen

Übernommen aus [B-623](B-623-feedback-knopf-an-der-karte.md#entscheidungen). Diesen Teil binden **E2** (Reichweite
je Grund: kindbezogene Gründe nur beim Erstellen für dieses Kind, übungsbezogene für alle) und **E3** (wirkt
beim Erstellen neuer Übungen; kein Reparatur-Modus, keine Änderung bestehender Übungen).

## Akzeptanzkriterien

1. (B-623 AK8) Beim Erstellen für ein Kind (`--child`) enthält das Briefing dessen kindbezogene **und** alle
   übungsbezogenen Rückmeldungen der Unit; beim Erstellen für ein Profil nur die übungsbezogenen.
   Testfall: „Hatten wir noch nicht“ von Kind A erscheint nicht im Briefing für Kind B, „Falsch übersetzt“
   erscheint in beiden.
2. Die Rückmeldungen werden zu Regeln im Prompt: „nicht über den Stoff hinaus“ (mit den gemeldeten Wörtern),
   „Antwort nur die Vokabel“ bei `TooMuchAsked`, „Antworten mit wenig Akzent-Fallen“ bei `TooStrict`, gemeldete
   Fehlübersetzungen nicht wiederverwenden, bei `TooHard`/`TooEasy` die Schwierigkeit verschieben.
3. Erledigte Rückmeldungen zählen weiter (sie sind Erfahrung, nicht offene Arbeit); eine Obergrenze wie bei den
   schwachen Wörtern hält den Prompt klein.
4. `briefing` (ohne LLM) zeigt die übernommenen Rückmeldungen, damit der Vater sie sieht, bevor er erzeugt.

## Schätzung

**Größe: M** — ein neuer Faktenblock im Briefing nach dem Vorbild `WeakWords`, eine Client-Abfrage und
Prompt-Regeln; keine Schemaänderung.

- **`migration: nein`**, **`vertragsbruch: nein`** — liest nur die Endpunkte aus B-628 über `Pugling.Client`.

### Risiken

1. **Die Wirkung im Prompt ist ungetestet gegen echte Daten.** Bei Bau gibt es womöglich erst wenige
   Rückmeldungen; die Regeln werden am Briefing (deterministisch) geprüft, die Wirkung am Modell nur stichprobenhaft.
2. **Das kleine lokale Modell** (Ollama) verdaut lange Regelblöcke schlecht – deshalb die Obergrenze (AK3) und
   eine Regel je Grund, nicht je Meldung.
3. **Zugriff.** Erstellt der Agent für ein Profil, liest er mit einem Creator-Token; die Creator-Sicht trägt
   absichtlich keinen Freitext (E7). Der Agent arbeitet also mit Gründen und Karten, nicht mit Kindertexten.

### Angriffsplan

1. Client-Abfrage der Rückmeldungen je Unit (Supervisor-Sicht bei `--child`, Creator-Sicht sonst).
2. `FeedbackFacts` im Briefing, gefiltert nach Reichweite; Ausgabe in `briefing`.
3. Regeln in `DraftPrompts`/`DraftRules` je Grund.

### Testweg

- Integrationstest in `Pugling.Api.Tests` nach dem Vorbild `AssignAgentTests`: zwei Kinder, Meldungen beider
  Reichweiten, Briefing für Kind A, Kind B und ein Profil – AK1 als Tabelle.
- Prompt-Snapshot-Test: die Regeln aus AK2 stehen im Prompt, wenn der Grund im Briefing ist, und fehlen sonst.
- Stichprobe gegen Ollama mit `create --dry-run`; Reviewer: `pugling-reviewer`.

## Verlauf

- **2026-11-17** — angelegt beim Schätzen von B-623 (Teil 3 von 3: KI-Creator), geschätzt M.
