---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [fill --source-file - blockiert interaktiv stumm]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf E Teil 5 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-22]
wartet_auf: ""
---

# B-293 · `fill --source-file -` wartet am Terminal stumm auf ein Dateiende, und Ctrl+C wird verschluckt

`ReadSourceAsync` (`AgentCommands.cs:86`) liest bei `-` die Standardeingabe bis zum Ende. Ohne Umleitung oder Pipe
blockiert das am Terminal, der `CancellationToken` greift im synchronen Konsolenleser nicht, und weil `Program.cs`
bei `CancelKeyPress` `e.Cancel = true` setzt, wird auch Ctrl+C geschluckt – der Prozess sitzt still, bis jemand
Ctrl+Z+Enter (Windows) bzw. Ctrl+D tippt. Weder der Usage-Text („'-' liest die Quelle von Stdin“) noch README oder
Skill sagen, dass `-` eine Umleitung erwartet oder wie man die Eingabe beendet.

**Behebung (Vorschlag des Prüfers):** ein Halbsatz in Usage, README und Skill („nur mit Umleitung/Pipe sinnvoll;
interaktiv mit Ctrl+Z bzw. Ctrl+D beenden“) und optional ein Hinweis auf `stderr`, wenn
`!Console.IsInputRedirected`.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 5 (`f033f70`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 1 des frischen Prüfers; `entgangen_bei: B-22`.
- **2026-10-27** — in Lauf E, Teil 7, autonom gegrillt (Freigabe 1, `Defekt`), geschätzt (XS, Doku + ein Hinweis) und gebaut: Usage, README und Skill sagen „nur mit Umleitung oder Pipe sinnvoll, interaktiv mit Ctrl+Z+Enter bzw. Ctrl+D beenden“; `ReadSourceAsync` schreibt bei `-` ohne umgeleitete Eingabe einen Hinweis nach `stderr`, bevor es liest. Kein Test: `Console.IsInputRedirected` ist im Testlauf immer wahr, das Blockieren selbst ist nicht automatisiert prüfbar – benannt, nicht verschwiegen.
- **2026-10-27** — Review (`pugling-reviewer`): Hinweis steht vor dem blockierenden Lesen, nur bei `-` ohne Umleitung; Usage/README/Skill decken sich mit dem Verhalten. Ohne Zählung. `abgenommen`; Commit `4a3fa61`.
