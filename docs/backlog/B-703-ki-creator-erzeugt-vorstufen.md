---
tags: [typ/story, status/gegrillt, bereich/agent, bereich/katalog, rolle/creator, rolle/supervisor]
aliases: [Vorstufen erzeugen, Agent-Verb build, Aufbau-Übungen per KI]
status: gegrillt
prio: P1
art: Wunsch
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: B-536
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: Entscheidung des Nutzers (Rollenschnitt Vater/Lehrer-Agent, B-681)
---

# B-703 · Der KI-Creator erzeugt zu einer Ziel-Übung leichtere Vorstufen

Teil 4 von 5 aus [B-536](B-536-aufbau-uebungen-statt-versagen.md): B-536 Entscheidung 2 – „der KI-Creator erzeugt die
Vorstufen, der Vater wählt aus". Genau diese Rollenaufteilung ist seit dem Leitbild vom 2026-11-19
([B-680](B-680-taeglicher-lehrplan-je-fach-just-in-time.md), [B-681](B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md))
offen: dort erzeugt ein Lehrer-Agent den Plan und der Vater belohnt nur. Der Vorbereitungslauf entscheidet das nicht.
Die Story bleibt `gegrillt`, bis der Nutzer den Rollenschnitt festgelegt hat.

## User Story

Als *Vater* möchte ich zu einer zu schweren Übung leichtere Vorstufen erzeugen lassen, damit der Katalog nicht leer
bleibt (heute hat keine Grammatik-Übung von Friedrich eine einzige), und sie prüfen, ehe sie dem Kind zugewiesen
werden.

## Ist-Stand am Code

- Der Agent kennt die Verben `create`, `plan`, `assign` und weitere
  (`backend/Pugling.Agent.Creator/AgentCommands.cs:201-228`); `create` erzeugt Übungen zu einer Unit
  (`CreatorPipeline.cs`), kennt aber keine „leichter als Übung X".
- Die Pipeline entscheidet deterministisch in C#, was eine Antwort hat (B-460:
  `backend/Pugling.Agent.Creator/CLAUDE.md`); ein Vorstufen-Verb muss dieselbe Regel einhalten: das Modell schreibt
  Aufgaben, C# prüft die Lösung.
- Das Katalogfeld für die Verknüpfung fehlt bis [B-700](B-700-aufbau-stufe-am-katalog.md).
- Nicht gefunden: eine Messung, wie gut das Modell eine leichtere Variante derselben Grammatik trifft (Qualität hängt
  am Modell, B-536 Kosten).

## Offene Frage an den Nutzer

1. **Wer wählt aus: der Vater (B-536 Entscheidung 2) oder der Lehrer-Agent (Leitbild)?** *Empfehlung:* das Verb
   erzeugt Vorstufen als **Entwurf ohne Zuweisung** (sie liegen im Katalog, hängen per B-700 an der Ziel-Übung);
   ob ein Mensch sie freigibt oder der Lehrer-Agent sie direkt in die Stufen-Position übernimmt, entscheidet der
   Rollenschnitt. Technisch ändert sich dafür nichts am Verb – nur, ob ein Freigabeschritt dazwischen steht.
   *Kosten der Gegenseite:* ein Freigabeschritt bremst einen Agenten, der täglich unbeaufsichtigt Pläne baut
   (B-680); ohne ihn landen ungeprüfte Modell-Aufgaben beim Kind.
2. **Darf das Verb vorhandene Übungen als Vorstufen übernehmen (ohne LLM, wie `assign`)?** *Empfehlung:* ja, als
   zweiter Modus – der Katalog enthält schon leichtere Übungen derselben Unit (Auswahl-Cloze), die nur nicht
   verknüpft sind. Das entschärft die Modellqualität und ist ohne Rollenfrage.

## Entscheidungen

Keine. Stand: beide Punkte offen, mit Empfehlung. Nach der Entscheidung: Verb festlegen, Akzeptanzkriterien
finalisieren, schätzen (Größe voraussichtlich M: ein Verb, Briefing um die Ziel-Übung, Prüfung in C#, Tests mit
`FakeChatClient`).

## Akzeptanzkriterien (Entwurf, nach der Entscheidung final)

1. Das Verb `build` mit einer Ziel-Übung erzeugt 1–2 Übungen derselben Unit, jede mit `BuildTargetExerciseId` der
   Ziel-Übung und aufsteigendem `BuildLevel`. Rot macht ihn: ein Verb, das die Verknüpfung nicht setzt.
2. Eine Antwort, die C# nicht als lösbar prüfen kann, wird verworfen und nicht angelegt. Rot macht ihn: eine
   Pipeline, die Modell-Lösungen ungeprüft übernimmt.
3. `--dry-run` legt nichts an. Rot macht ihn: ein Dry-Run, der den Katalog schreibt.
4. Der Modus ohne LLM verknüpft vorhandene Übungen nur innerhalb derselben Unit. Rot macht ihn: eine Verknüpfung über
   Units hinweg.

## Verlauf

- **2026-11-19** — Teil von B-536, geteilt im Vorbereitungslauf (Sonnet). Nicht entschieden, weil der Rollenschnitt
  Vater/Lehrer-Agent offen ist (B-681); `gegrillt` mit offener Frage, `wartet_auf` gesetzt.
