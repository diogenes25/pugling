---
tags: [typ/story, status/geschaetzt, bereich/agent, bereich/katalog, rolle/creator, rolle/supervisor]
aliases: [Vorstufen erzeugen, Agent-Verb build, Aufbau-Übungen per KI]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
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

## Schätzung

**Größe: M** – wie der vokabel-basierte Batch-Pfad im `MediaSelector` (B-03): ein neues Verb mit zwei Betriebsarten,
das die vorhandene Pipeline und den Katalog-Zugriff des Agenten wiederverwendet. Kein Schema, keine Oberfläche. Nicht S,
weil das Verb zwei Wege trägt (Modell und ohne Modell) und die Auswahlregel von `--existing` neu ist.

- **`migration: nein`** – nachgesehen: der Agent schreibt über `Pugling.Client` in die API; er trägt kein eigenes Schema.
  Die Spalten entstehen in [B-700](B-700-aufbau-stufe-am-katalog.md).
- **`vertragsbruch: nein`** – nachgesehen: `Pugling.Contracts` ändert diese Story nicht; sie **liest** die von B-700
  additiv ergänzten Felder (`BuildLevel`, `BuildTargetExerciseId`). `Pugling.Client` bekommt höchstens einen Durchreich-Parameter
  an `CreateExerciseAsync`/`UpdateExerciseAsync` (`CreatorApi.cs:224`, `:234`), additiv.
- **`wo: backend`** – der Agent ist eines der fünf Backend-Projekte (`backend/Pugling.Agent.Creator`); eine Oberfläche gibt es
  nicht, der Vater sieht die Entwürfe im Katalog-Editor ([B-704](B-704-aufbau-weg-in-der-oberflaeche.md)).
- **Abhängigkeit:** B-700 (Felder und Validierung am Katalog). Ohne B-700 gibt es keinen Ort für die Verknüpfung.

### Risiken

- **Auswahlregel von `build --existing` ist noch nicht ausformuliert.** Entscheidung 2 verlangt „Typ und Schwierigkeit“,
  nennt aber keine Rangfolge der Typen; das Modell des Katalogs kennt keine Schwierigkeit (B-700 Entscheidung 1: die Stufe
  ist erst die Verknüpfung). Beim Bauen ist eine feste, kleine Regel zu wählen (Vorschlag: gleiche Unit, anderer Typ als die
  Ziel-Übung oder dieselbe Typfamilie mit leichterer Spielform, Stufe 1 vor 2 nach Typ-Reihenfolge Auswahl, Cloze, Freitext)
  und als Absicht zu beschriften. **Die Schätzung nimmt sie als klein an; wird sie groß, ist das ein eigener Schnitt.**
- **Das Modell trifft „leichter“ nicht zuverlässig** (keine Messung vorhanden). C# prüft nur Lösbarkeit (B-460), nicht die
  Didaktik; die Qualität bleibt Sache des Vaters, der die Entwürfe sieht (Entscheidung 1).
- **Der Konstruktor von `AgentCommands` wächst.** Ein neues Verb braucht einen neuen Dienst; genau ein Test konstruiert ihn
  von Hand (`AssignAgentTests.cs:401`) und muss nachziehen. Der Aufruf liegt in `Program.cs` über die Abhängigkeitsinjektion.
- **Dubletten:** zweimal `build` zur selben Ziel-Übung darf keine zweite Reihe Vorstufen anlegen. Entscheidung 4 sagt nichts
  dazu; der Agent prüft vor dem Schreiben, was an der Ziel-Übung schon hängt, wie `assign` es für Positionen tut.
  Als Testfall führen.
- **Stufen-Obergrenze:** B-700 erlaubt nur `BuildLevel` 1 und 2 und keine Kette; `build` darf nicht mehr als zwei Stufen
  und nie eine Vorstufe als Ziel anlegen, sonst lehnt der Server mit 400 ab und der Lauf bricht mit Exit 1.

### Angriffsplan

1. B-700 muss stehen. `Pugling.Client` ergänzen (Durchreichen der beiden Felder).
2. Neuer Dienst `BuildPlanner` neben `PlanAssigner` (`backend/Pugling.Agent.Creator/PlanAssigner.cs`): liest die Ziel-Übung
   und ihre Unit, prüft Unit, Typ und vorhandene Vorstufen, **bevor** das erste Schreiben passiert.
3. Modus Modell: ein Pipeline-Lauf je Vorstufe über `CreatorPipeline.CreateAsync` (eigener Selbsttest je Teil), danach die
   Verknüpfung setzen; ein gescheiterter Teil macht den Lauf sichtbar unvollständig (Exit 1, Muster `ExamPlanner`).
4. Modus `--existing`: nur Lesen und Verknüpfen, ohne Modell, nur innerhalb der Unit.
5. Verb `build` in `AgentCommands.RunAsync` und `PrintUsage`, Flags in `CommandLine`; `--dry-run` schreibt nichts.
6. Skill `ki-creator` und `backend/Pugling.Agent.Creator/CLAUDE.md` um das Verb ergänzen (Doku, nach dem Bauen).
7. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Klasse `BuildAgentTests` in `backend/Pugling.Api.Tests` (Muster `AssignAgentTests`: `PuglingWebAppFactory`, echte API,
  `FakeChatClient`): AK 1 (Verknüpfung und aufsteigende Stufe), AK 2 (nicht prüfbare Antwort wird nicht angelegt), AK 3
  (`--dry-run` ändert die Katalog-Zählung nicht), AK 5 (nach dem Lauf keine `PlanPosition`), dazu je ein Fall für Dublette
  und Stufen-Obergrenze.
- AK 4: `Build_Existing_Braucht_Kein_Sprachmodell` nach dem Muster `Assign_Braucht_Kein_Sprachmodell`
  (`AssignAgentTests.cs:388`): `FakeChatClient.Calls == 0`, plus ein Fall mit einer Übung aus einer **anderen** Unit, die
  nicht verknüpft werden darf.
- `CreatorAgentTests` und `AssignAgentTests` bleiben unverändert grün; `ConventionGuardTests` bleibt grün.
- Vor der Abnahme: Agent `pugling-reviewer`; ein Lauf des Verbs gegen die Wegwerf-DB (`/smoke-test`) und ein Gang als Vater im
  Katalog-Editor, sobald B-704 steht.

## Verlauf

- **2026-11-19** — Teil von B-536, geteilt im Vorbereitungslauf (Sonnet). Nicht entschieden, weil der Rollenschnitt
  Vater/Lehrer-Agent offen ist (B-681); `gegrillt` mit offener Frage, `wartet_auf` gesetzt.
- **2026-11-20** — Die offenen Punkte stellvertretend entschieden (Vorbereitungslauf, Sonnet, auf Anweisung des Nutzers, unbestätigt), Stufe bleibt `gegrillt`, `wartet_auf` geleert. Kern: Vorstufen entstehen als Entwurf ohne Zuweisung, der Vater übernimmt sie durch Einschalten der Stufung; zweiter Modus ohne Sprachmodell für vorhandene Übungen derselben Unit.
- **2026-11-20** — geschaetzt (Vorbereitungslauf, Sonnet, auf Anweisung des Nutzers): M, backend, keine Migration, kein Vertragsbruch. Hängt an B-700; die Auswahlregel von `build --existing` ist beim Bauen festzulegen (Risiko), der Konstruktor von `AgentCommands` wächst (ein Test zieht nach).
