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
wartet_auf: ""
---

# B-703 · Der KI-Creator erzeugt zu einer Ziel-Übung leichtere Vorstufen

Teil 4 von 5 aus [B-536](B-536-aufbau-uebungen-statt-versagen.md): B-536 Entscheidung 2 – „der KI-Creator erzeugt die Vorstufen, der Vater wählt aus“. Die Rollenfrage aus dem Leitbild vom 2026-11-19 ([B-680](B-680-taeglicher-lehrplan-je-fach-just-in-time.md), [B-681](B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md)) ist am 2026-11-20 stellvertretend entschieden (Rahmenentscheidungen A und G, unbestätigt); geschätzt wird die Story als Nächstes.

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

## Offene Punkte

Alle Punkte entschieden; die Empfehlung steht durchgestrichen daneben.

1. ~~**Wer wählt aus: der Vater (B-536 Entscheidung 2) oder der Lehrer-Agent (Leitbild)?** *Empfehlung:* das Verb erzeugt Vorstufen als **Entwurf ohne Zuweisung** (sie liegen im Katalog, hängen per B-700 an der Ziel-Übung); ob ein Mensch sie freigibt oder der Lehrer-Agent sie direkt in die Stufen-Position übernimmt, entscheidet der Rollenschnitt. Technisch ändert sich dafür nichts am Verb – nur, ob ein Freigabeschritt dazwischen steht. *Kosten der Gegenseite:* ein Freigabeschritt bremst einen Agenten, der täglich unbeaufsichtigt Pläne baut (B-680); ohne ihn landen ungeprüfte Modell-Aufgaben beim Kind.~~ → Entscheidung 1.
2. ~~**Darf das Verb vorhandene Übungen als Vorstufen übernehmen (ohne LLM, wie `assign`)?** *Empfehlung:* ja, als zweiter Modus – der Katalog enthält schon leichtere Übungen derselben Unit (Auswahl-Cloze), die nur nicht verknüpft sind. Das entschärft die Modellqualität und ist ohne Rollenfrage.~~ → Entscheidung 2.

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

## Entscheidungen

Entscheidung 1 ist die Rahmenentscheidung G, Entscheidung 3 die Rahmenentscheidung A (führend B-681).

1. **Modellerzeugter Inhalt wird erst durch einen Menschen Pflicht (Rahmen G, führend B-681).** Förder-Übungen und Vorstufen aus dem Agenten entstehen als Katalog-Entwurf; Pflicht werden sie erst, wenn ein Mensch sie in einen Plan übernimmt (`assign`, Positionsformular, Einschalten der Stufung). Der Planer wählt nur aus Positionen des aktiven Plans und legt aus Modellinhalt keine Position selbst an. Begründung: Das ersetzt die heutige Sicherung „KI-Plan entsteht inaktiv“ (`PlanPlanner.cs:101`) durch die kleinste menschliche Handlung statt einer Freigabe je Aufgabe; C# prüft die Lösbarkeit (B-460), nicht die Didaktik, und die Qualität hängt am Modell. Kosten: Neue Förder-Übungen erreichen das Kind erst nach einem Vater-Klick; der Agent kann nicht unbeaufsichtigt Inhalt bis zum Kind durchreichen. Verworfen: Der Planer nimmt frische Modellübungen selbst auf, Schattenwoche plus Deckel und Boden als einzige Sicherung. Schließt aus: den täglich vollautomatischen Kreislauf „Agent baut, Planer verteilt, Kind lernt ohne Vater“, den das Leitbild andeutet. (Beantwortet Punkt 1: Der Vater übernimmt die Vorstufen, indem er die Stufung an der Position einschaltet, B-701 Entscheidung 2; ein Freigabeschritt dazwischen entfällt, der Agent liefert Entwürfe.)
2. **Das Verb hat einen zweiten Modus ohne Sprachmodell (`build --existing`), der vorhandene leichtere Übungen derselben Unit mit `BuildTargetExerciseId` und `BuildLevel` an die Ziel-Übung hängt.** Begründung: Der Katalog enthält schon leichtere Übungen derselben Unit (Auswahl-Cloze), die nur nicht verknüpft sind; das entschärft die Modellqualität und hat keine Rollenfrage. Kosten: Die Auswahl der passenden Übungen nach Typ und Schwierigkeit ist eine Regel in C#, die falsche Treffer kennt; der Vater sieht sie im Editor (B-704). Verworfen: nur der Modell-Modus. Schließt aus: Vorstufen aus anderen Units – die User Story spricht vom Katalog, nicht von fremden Stoffen.
3. **Der serverseitige Planer entscheidet den Tagesplan, nicht ein Sprachmodell (Rahmen A, führend B-681).** Der Tagesplan entsteht durch den Planer aus B-644 mit festen Regeln in C#; „Lehrer-Agent“ ist der Name dieses Planers gegenüber dem Kind, kein Modellaufruf. Der Konsolen-Agent (`Pugling.Agent.Creator`) bleibt das Werkzeug für Inhalte und entscheidet keine Pflicht. Begründung: Die Pflicht kostet Münzen und muss nachrechenbar und wiederholbar sein (B-644 Entscheidung 10); ein Agent mit Plan-Steuerung bräuchte außerdem ein eigenes Konto und einen Zugriff auf Kinder, den es nicht gibt (Eigentum läuft über `SupervisorLink`, `AuthAccess.cs:112`). Kosten: Der Planer ist nur so klug wie seine Regeln; ein unvorhergesehenes Muster bleibt unentdeckt, bis jemand eine Regel schreibt. Verworfen: ein Sprachmodell, das den Plan je Kind täglich selbst festlegt – nicht nachrechenbar und mit dem lokalen Modell ausfallgefährdet. Schließt aus: einen frei „erfundenen“ Plan je Kind, wie ihn der Satz „Lehrer-Agent erzeugt jeden Tag den Plan“ nahelegt; das Kind bekommt einen täglichen Plan nach seinem Lernstand, kein Modell-Ergebnis.
4. **Das Verb `build` legt Vorstufen als Katalog-Übungen an und hängt sie per B-700 an die Ziel-Übung; es legt keine Position, keinen Plan und keine Zuweisung an.** Begründung: Rahmen G – die Übernahme in einen Plan bleibt eine menschliche Handlung. Kosten: Vorstufen sind angelegt, aber unsichtbar für das Kind, bis der Vater die Stufung einschaltet. Verworfen: das Verb schaltet die Stufung selbst ein. Schließt aus: ungeprüfte Modell-Aufgaben beim Kind (die User Story verlangt Prüfung vor dem Zuweisen).

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht.

1. Das Verb `build` mit einer Ziel-Übung erzeugt 1 bis 2 Übungen derselben Unit, jede mit `BuildTargetExerciseId` der Ziel-Übung und aufsteigendem `BuildLevel`. *Rot, wenn* das Verb die Verknüpfung nicht setzt.
2. Eine Antwort, die C# nicht als lösbar prüfen kann, wird verworfen und nicht angelegt. *Rot, wenn* die Pipeline Modell-Lösungen ungeprüft übernimmt.
3. `--dry-run` legt nichts an. *Rot, wenn* ein Dry-Run den Katalog schreibt.
4. `build --existing` verknüpft vorhandene Übungen nur innerhalb derselben Unit und ruft kein Sprachmodell auf (`FakeChatClient` zählt null Aufrufe). *Rot, wenn* eine Verknüpfung über Units hinweg entsteht oder ein Aufruf stattfindet.
5. `build` legt weder eine `PlanPosition` noch einen `StudyPlan` an. *Rot, wenn* nach dem Lauf eine Position existiert.

## Verlauf

- **2026-11-19** — Teil von B-536, geteilt im Vorbereitungslauf (Sonnet). Nicht entschieden, weil der Rollenschnitt
  Vater/Lehrer-Agent offen ist (B-681); `gegrillt` mit offener Frage, `wartet_auf` gesetzt.
- **2026-11-20** — Die offenen Punkte stellvertretend entschieden (Vorbereitungslauf, Sonnet, auf Anweisung des Nutzers, unbestätigt), Stufe bleibt `gegrillt`, `wartet_auf` geleert. Kern: Vorstufen entstehen als Entwurf ohne Zuweisung, der Vater übernimmt sie durch Einschalten der Stufung; zweiter Modus ohne Sprachmodell für vorhandene Übungen derselben Unit.
