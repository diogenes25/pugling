# Pugling.Agent.Creator – KI-Creator

Konsolen-App, die die Creator-Rolle übernimmt – lokal gegen Ollama
(`Microsoft.Extensions.AI`/`IChatClient` + OllamaSharp). Aufrufe und Betriebsarten stehen im Skill
`ki-creator`, die Bedienung im [README](README.md).

**Deterministische Pipeline**: C# besitzt den Ablauf (Briefing → Entwurf → Regelprüfung mit
Reparatur-Runde → Anlegen → nebenwirkungsfreier Selbsttest über `preview`/`preview/check`), das Modell
liefert nur strukturierten Inhalt – kein Tool-Calling. Fachliche Kernregel: **Interessen kleiden den Stoff
ein, sie ersetzen ihn nie** (bei vorgegebenem Wortschatz deterministisch erzwungen).

Das `CreatorBriefing` hat **zwei Quellen**: `ProfileFacts` (der Fachlehrer samt Reihe/Unit-Stoff) und
**optional** `ChildFacts` – daran hängt die Betriebsart: mit Kind entsteht eine *individuelle* Übung, ohne
eine *allgemeine* für den geteilten Katalog (dann kommen Klassenstufen-Bereich/Schulart/Quelle aus dem
Profil, und der Agent braucht kein Betreuungsrecht). Ohne `--profile` sucht der Agent selbst das
bestpassende (`profiles/match`).

Der `ExamPlanner` baut **Übungsklausuren**: ein Pipeline-Lauf je Typ (jeder mit eigenem Selbsttest),
danach – nur mit Kind – ein kind-skopierter Tag und eine `Klassenarbeit` (Status *geplant*) mit genau
diesen Übungen; ein gescheiterter Teil bricht die Klausur nicht ab, macht sie aber sichtbar unvollständig
(Exit 1).

Neuer Typ = eine Klasse auf `ExerciseStrategy<TDraft,TConfig>`. Getestet ohne Ollama via `FakeChatClient`
(`CreatorAgentTests`).
