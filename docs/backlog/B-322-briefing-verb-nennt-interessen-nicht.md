---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [briefing ohne Interessen-Labels, vorgegebene Interessen ungenannt]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf H Teil 1 in Lauf H, Teil 2 (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-264]
wartet_auf: ""
---

# B-322 · Das Verb `briefing` verweist auf vorgegebene Interessen, ohne sie zu nennen

Seit [B-264](B-264-interessen-ohne-kind-kleiden-nicht-ein.md) sagt das kindlose Briefing „altersgerechte Situationen aus
den vorgegebenen Interessen“; die Labels stehen aber nur im Auftragsteil (`DraftPrompts.cs:61-62`).
`AgentCommands.ShowBriefingAsync` druckt allein `ToPromptText()` – das Verb, das zeigen soll, worauf der Agent zuschneidet,
nannte die Interessen nicht (`CreatorBriefing.cs:122-126`, Stand `c195ef2`).

## User Story

Als Creator, der mit `pugling-creator briefing` vorab sehen will, was der Agent tut, möchte ich die vorgegebenen
Interessen dort lesen.

## Ist-Stand am Code

Siehe oben.

## Akzeptanzkriterien

1. Das kindlose Briefing mit `--interests` nennt deren Labels selbst.

## Entscheidungen

1. Die Labels in die Briefing-Zeile, nicht in `ShowBriefingAsync` – so sieht auch das Modell sie an der Stelle, die
   auf sie verweist. Kosten: im Modell-Prompt stehen sie zweimal (Briefing und Auftrag).

## Schätzung

XS, `backend` (Agent), keine Migration, kein Vertragsbruch.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau zu Lauf H, Teil 1 (`c195ef2`, frischer `pugling-reviewer`, 🟡) in Lauf H,
  Teil 2 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); im selben Sprint gebaut.
  `Das_kindlose_Briefing_nennt_die_vorgegebenen_Interessen_selbst` → **1 failed** vor dem Fix, danach grün;
  `CreatorAgentTests` **79/79**, Suite **1001/1001 `exit=0`**. Rollengang: Tests sind der Weg. **`abgenommen`.**
