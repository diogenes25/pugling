---
tags: [typ/story, status/abgenommen, bereich/training, rolle/supervisor, rolle/creator]
aliases: [KI-Supervisor-Agent, assign-Verb]
status: abgenommen
prio: P3
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: memory/ki-agenten-semantic-kernel.md
grund: ""
ersetzt_durch: []
---

# B-20 · KI-Agent: Übungen per `assign` einem laufenden Plan zuweisen

Der Creator-Agent (`Pugling.Agent.Creator`) erzeugt Übungen im Katalog und baut mit `plan` (B-19) ganze neue
Lehrpläne. Was fehlt, ist der kleine Schritt dazwischen: vorhandene Übungen – frisch erzeugt, per Id gewählt
oder über einen Klausur-Tag gebündelt – einem **bestehenden, laufenden** Plan als Positionen hinzuzufügen.
Seit der Entscheidung des Nutzers vom 2026-10-23 ist das ein **`assign`-Verb im Creator-Agenten**, kein eigenes
Konsolenprojekt.

## User Story

Als *Supervisor* (bzw. sein KI-Agent in seinem Namen) möchte ich eine oder mehrere bereits im Katalog
vorhandene Übungen mit einem Kommando in den laufenden Lehrplan eines Kindes übernehmen, damit die vom Agenten
erzeugten Übungen nicht mehr von Hand als Position angelegt werden müssen.

## Ist-Stand am Code (2026-11-14)

- **Das Verb fehlt:** `backend/Pugling.Agent.Creator/AgentCommands.cs:186-213` kennt `types`, `profiles`,
  `briefing`, `create`, `exam`, `plan`, `fill`, `images`, `help`.
- **Der Agent hält schon beide Clients:** `AgentCommands.cs:15` bekommt `CreatorApi` **und** `SupervisorApi`
  injiziert; `PlanPlanner.cs:105` und `ExamPlanner.cs:46` nutzen `SupervisorApi` bereits. Das Konto des Agenten
  (`Pugling:Pin`) trägt also schon die Supervisor-Rolle – kein neues Setup.
- **Client-Schicht:** `Pugling.Client/SupervisorApi.cs:100` (`ListPlansAsync(childId)`), `:104` (`GetPlanAsync`),
  `:123` (`ListPositionsAsync`), `:130` (`AddPositionAsync`) sind da. `Pugling.Client/CreatorApi.cs:160-170`
  hat `ListTagsAsync(childId)` und zum Tag nur den POST `TagExercisesAsync`; der **GET**
  `creator/tags/{tagId}/exercises` steht am Server (`Pugling.Api/Controllers/Creator/TagsController.cs:205-208`,
  paginiert mit `skip`/`take`, `ExerciseBrief`), aber ohne Client-Wrapper.
- **Welcher Plan läuft:** `PlanResponse.IsPlayable` (`Pugling.Contracts/Supervisor/StudyPlanDtos.cs:7-16`) ist
  die serverseitige Antwort auf „aktiv **und** heute in der Laufzeit“ – der Agent muss die Regel nicht
  nachbauen.
- **Positions-Defaults:** `CreatePositionDto` braucht nur `ExerciseId`; `null` erbt die Server- bzw.
  Übungsvorgaben (`Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:169`, `:196`).
- **Doppelte Positionen:** der Server lehnt eine zweite Position derselben Übung **nicht** ab
  (`PlanPositionsController.cs:152-196`: geprüft werden Existenz, Typ-Regel `RequireTypedTest`, Ausführbarkeit,
  keine Eindeutigkeit) – die Idempotenz muss im Agenten sitzen.
- **Doku:** `backend/Pugling.Agent.Creator/README.md` erwähnt das Zuweisen nicht mehr; Skill
  `.claude/skills/ki-creator/SKILL.md` kennt kein `assign`.
- **Testmuster:** `Pugling.Api.Tests/CreatorAgentTests.cs:18` (In-Process-Server, echter `Pugling.Client`,
  Ablaufklassen direkt instanziiert, z. B. `Filler(...).RunAsync(...)` `:83`).

## Die echte Lücke

Ein Verb, eine Ablaufklasse und ein Client-Wrapper. Alles andere – Rollen, Positions-Defaults,
Spielbarkeits-Regel – liefert der Server schon.

## Offene Punkte

1. ~~Welche Client-Methoden fehlen?~~ → nur der GET-Wrapper zum Tag (Entscheidung 4).
2. ~~Eigenes Projekt oder Verb?~~ → Verb (Entscheidung 2, Nutzer 2026-10-23).
3. ~~Vorhandene Position derselben Übung?~~ → Entscheidung 6.
4. ~~`--tag` im ersten Schritt?~~ → Entscheidung 4.
5. ~~Positions-Vorgaben?~~ → Entscheidung 7.
6. ~~In welchen Plan?~~ → Entscheidung 5.
7. ~~Was bei einem Schreibfehler mitten in der Liste?~~ → Entscheidung 8.

## Entscheidungen

1. **Erster Baustein ist „Zuweisen“, nicht Missionen/Ziele/Klassenarbeiten** (2026-08-03, unverändert).
   Begründung: schließt den Workflow-Bruch nach `create`/`exam`; Missionen (`CreateMissionDto` ohne Defaults)
   bräuchten eigene Heuristiken. Kosten: kein vollständiger „Supervisor-Agent“.
2. **Kein zweites Konsolenprojekt – `assign` ist ein Verb im Creator-Agenten.** **Entscheidung des Nutzers**
   in der Grill-Runde der Backlog-Sichtung 2026-10-23 (Linie von B-19, Entscheidung 2 dort). Die frühere
   Entscheidung „eigenes Projekt `Pugling.Agent.Supervisor`“ (2026-08-03) ist damit ~~aufgehoben~~. Kosten: der
   Agent heißt weiter „Creator“, obwohl er hier Supervisor-Arbeit tut.
3. **Kein Sprachmodell für `assign`** (2026-08-03, unverändert). Begründung: Zuweisen ist deterministisch.
   Kosten: `assign` läuft auch ohne Ollama – der Host darf beim Start für dieses Verb kein Modell verlangen.
4. **`--exercise-id` (wiederholbar) oder `--tag <name>`; der Client-Wrapper wird mitgebaut.**
   Entscheidung im Auftrag des Nutzers vom 2026-11-05 ([Protokoll](../pm-sitzung-2026-11-05-delegation.md)).
   `--tag` löst den Namen über `ListTagsAsync(childId)` (Tags sind kind-skopiert) und dann über den neuen
   `CreatorApi.ListTagExercisesAsync(tagId, skip, take)` auf, mit `take = 500` (`MaxTake`). Beide zugleich ⇒
   Fehlbedienung. Kosten: ein einzeiliger Wrapper; ein Tag mit mehr als 500 Übungen wird abgeschnitten – der
   Agent meldet das, statt still zu kürzen (Kopf `X-Total-Count` lesen).
5. **Zugewiesen wird in den spielbaren Plan des Kindes (`IsPlayable`); `--plan-id` wählt ausdrücklich einen
   anderen eigenen Plan desselben Kindes.** Stakeholder-Entscheidung im Auftrag des Nutzers (präzisiert die
   Entscheidung vom 2026-08-03, die „aktiv“ sagte). Begründung: `IsPlayable` ist die serverseitige Regel „ein
   spielbarer Plan je Kind“ (Anti-Cheat); ein nur „aktiver“, aber abgelaufener Plan wäre das falsche Ziel. Ohne
   spielbaren Plan und ohne `--plan-id` ⇒ Exit 2 mit Hinweis auf `plan` bzw. `--plan-id`; ein `--plan-id` eines
   anderen Kindes ⇒ Exit 2. Kosten: ein Plan, der erst nächste Woche startet, braucht `--plan-id`.
6. **Eine schon vorhandene Position derselben Übung wird übersprungen und gemeldet; ist nichts neu, Exit 2.**
   Entscheidung im Auftrag des Nutzers vom 2026-11-05. Kosten: ein `ListPositionsAsync` vor dem Schreiben.
7. **Positions-Vorgaben bleiben Server-Defaults; optionale Overrides `--points`, `--goal-threshold`,
   `--cadence Daily|Weekly`.** Entscheidung im Auftrag des Nutzers vom 2026-11-05 (Defaults) und 2026-08-03
   (Overrides). Begründung: keine zweite Wahrheit über Defaults im Agenten. Kosten: der Agent setzt keine
   klugen Werte.
8. **Erst alles lesen, dann schreiben; der erste Schreibfehler bricht ab.** Stakeholder-Entscheidung im
   Auftrag des Nutzers. Begründung: Plan, vorhandene Positionen und Übungs-Ids werden vor dem ersten
   `AddPositionAsync` aufgelöst (`--dry-run` endet genau hier). Lehnt der Server eine Position ab (z. B.
   `exercise_not_executable`), nennt der Agent die schon angelegten und die abgelehnte Übung und endet mit
   Exit 1 – ein erneuter Lauf überspringt die schon angelegten (Entscheidung 6), ist also sicher wiederholbar.
   Kosten: kein Rollback – die angelegten Positionen bleiben stehen.
9. **Zurückgestellt: Bulk-POST für Positionen** (Eingang 2026-08-04, API-Design B7). Stakeholder-Entscheidung
   im Auftrag des Nutzers. Begründung: für Familien-Mengen reicht ein Aufruf je Übung; ein Batch-Endpunkt wäre
   ein Vertragszusatz ohne Not. Kosten: n Aufrufe statt einem. Wird er später gebraucht, gilt die Auflage aus
   dem Verlauf (Enum aus B-59, kein nackter Status-String).

## Akzeptanzkriterien

1. `assign --child <id> --exercise-id <a> --exercise-id <b>` legt im spielbaren Plan des Kindes je Übung eine
   Position an, alle Ziel-/Punktefelder `null`. *Rot, wenn* der Plan nach `Active` statt `IsPlayable` gewählt
   wird (Testdaten: ein aktiver, abgelaufener Plan neben dem spielbaren).
2. `assign --child <id> --tag <name>` legt für jede Übung des kind-skopierten Tags eine Position an. *Rot, wenn*
   der Tag-Name nicht über das Kind aufgelöst wird (Testdaten: gleichnamiger Tag an einem zweiten Kind mit
   anderen Übungen).
3. Ist eine Übung schon Position im Plan, wird sie übersprungen und gemeldet; sind alle schon da, Exit 2 und
   keine neue Position. *Rot, wenn* die Vorab-Prüfung fehlt (Server legt sonst eine Dublette an).
4. Kein spielbarer Plan, kein `--plan-id` ⇒ Exit 2, Meldung nennt `--plan-id`; `--plan-id` eines anderen
   Kindes ⇒ Exit 2. Kein schreibender Aufruf in beiden Fällen.
5. `--dry-run` druckt Kind, Plan, je Übung „neu“/„schon vorhanden“ und die gesetzten Overrides und macht
   **keinen** schreibenden Aufruf (Positionszahl vor = nach). *Rot, wenn* der Dry-Run-Zweig nach dem ersten
   `AddPositionAsync` greift.
6. `--points 30 --cadence Weekly` landet in der angelegten Position (`PointsGoalMet = 30`, `Cadence = Weekly`).
7. `--exercise-id` und `--tag` zusammen, oder keins von beiden ⇒ `AgentUsageException` (Fehlbedienung).
8. `assign` startet ohne erreichbares Ollama (kein `IChatClient`-Aufruf).
9. README (neuer Abschnitt „In einen laufenden Plan zuweisen (`assign`)“), `help`-Ausgabe und Skill
   `ki-creator` nennen das Verb mit Beispiel.

## Schätzung

**Größe: M** (vorher L) – nach Entscheidung 2 kein Projekt, keine Host-Verdrahtung, keine Projektdatei: ein
Verb, eine Ablaufklasse `PlanAssigner` nach dem Muster von `PlanPlanner`, ein Client-Wrapper, eine Testklasse,
Doku. Vergleich: ungefähr der Batch-Pfad im `MediaSelector` (B-03, M); deutlich unter einer DB-Etappe (E6, L).

- **`migration: nein`** – keine Schemaänderung.
- **`vertragsbruch: nein`** – kein DTO ändert sich; `Pugling.Client` bekommt eine **zusätzliche** Methode für
  einen schon bestehenden Endpunkt (`ClientRouteGuardTests` prüft die Route mit).

**Risiken**

- Der Host des Agenten könnte das Chat-Modell beim Start auflösen; dann bräche AK 8. Vor dem Bau
  `Program.cs` prüfen; ggf. `IChatClient` lazy beziehen.
- `ListTagExercisesAsync` liest eine paginierte Antwort: der Wrapper muss `X-Total-Count` erreichbar machen
  (gibt es dafür schon einen Paged-Helfer in `PuglingHttp`? sonst `IReadOnlyList` + Warnung bei genau 500).
- `exercise_not_executable` (Grant-Modell, `ExecutePublic`) ist der wahrscheinlichste Schreibfehler für
  fremde Übungen – AK-Test dafür über Entscheidung 8 (angelegte bleiben, Exit 1).
- Der In-Process-Server teilt die Ratenbremse beim Login – Tests nutzen die vorhandene Factory
  (`RateLimiting:LoginEnabled` aus).

**Angriffsplan (Backend)**

1. `CreatorApi.ListTagExercisesAsync(tagId, skip, take)` (einzeiliger Wrapper, englische `<summary>`).
2. `PlanAssigner` (Eingabe `AssignRequest(ChildId, PlanId?, ExerciseIds, TagName?, Points?, GoalThreshold?,
   Cadence?, DryRun)`, Ausgabe `AssignOutcome` mit angelegt/übersprungen/abgelehnt) – Lesen, dann Schreiben.
3. `AgentCommands`: `case "assign"`, Flag-Parsing, Exit-Codes 0/1/2, `PrintUsage`-Zeile; DI-Registrierung neben
   `PlanPlanner`.
4. Tests `AssignAgentTests` (AK 1–8).
5. README, Skill `ki-creator`, ggf. `backend/Pugling.Agent.Creator/CLAUDE.md` (Verb-Liste).

**Testweg**

- Integrationstest `Pugling.Api.Tests/AssignAgentTests.cs` (In-Process-`PuglingWebAppFactory`, echter
  `Pugling.Client`, `PlanAssigner` direkt instanziiert wie `Filler` in `CreatorAgentTests.cs:83`; kein
  `FakeChatClient`): je AK ein Fall; die „Rot, wenn“-Probe je AK am genannten Ort.
- Handlauf gegen `/smoke-test`-Instanz: `dotnet run --project backend/Pugling.Agent.Creator -- assign --child 1
  --exercise-id <id> --dry-run`, dann ohne `--dry-run`, dann erneut (⇒ Exit 2, alles übersprungen).

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert: Ist-Stand gegen den echten Code recherchiert (`Pugling.sln`,
  `Pugling.Client/SupervisorApi.cs`, `Pugling.Contracts/Supervisor/StudyPlanDtos.cs`,
  `Pugling.Agent.Creator/README.md`) — die Client-Schicht ist bereits vollständig, es fehlt nur das
  Konsolenprojekt samt einem Zuweisungs-Kommando; offene Punkte formuliert.
- **2026-08-03** — gegrillt: alle offenen Punkte in nummerierte Entscheidungen überführt (autonom
  getroffen, Nutzerauftrag 2026-08-04) — Scope auf „Zuweisen" begrenzt, kein Sprachmodell für diesen
  Schritt, Zuweisung nur in den aktiven Plan.
- **2026-08-03** — geschätzt: Größe L, `wo: backend`, `migration: nein`, `vertragsbruch: nein`, Risiken,
  Angriffsplan und Testweg ergänzt; kein XL-Split nötig (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-04** — **Eingang aus der API-Design-Runde** (`docs/api-design-bewertung.md`, Vorschlag B7,
  Arbeitsrunde PM/API-Designer/Entwickler): Ein **Bulk-POST für Plan-Positionen** gehört hierher und **nicht**
  in eine eigene Story — gemessen hat das Frontend genau **eine** Aufrufstelle, die genau **eine** Position je
  Aufruf setzt (`frontend/src/vater/PlanPositions.tsx:395`, ein `action.run` je Position), und der
  Lehrplan-Assistent hat **keinen** Bulk-Pfad. Ein Batch-Endpunkt hätte heute also keinen Aufrufer; der
  Supervisor-Agent dieser Story wäre der erste. Auflage, falls er ihn braucht: Er trägt das Enum aus
  [B-59](B-59-status-strings-ohne-werteliste.md) (Entscheidung 3), **keinen** neuen nackten Status-String —
  sonst wird B-59 dadurch teurer.
- **2026-08-07** — Autonomer Modus (Opt-in je Vorhaben, README → „Autonomer Modus") vom Nutzer im Dialog
  ausdrücklich freigegeben: ein Nachtlauf darf diese Story trotz `art: Wunsch` ohne weitere Rückfrage bauen
  (Rollengang/Reviewer bleiben Pflicht wie bei jeder Abnahme).
- **2026-10-23** — **Zurück auf `ausformuliert`** in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): B-19 (abgenommen, `adfa05e`) hat das Verb `plan` in den **Creator**-Agenten gebaut (`PlanPlanner.cs:180,199`) und in seiner Entscheidung 2 ein zweites Konsolenprojekt als „doppelte Infrastruktur“ verworfen – AK 1 dieser Story verlangt genau dieses Projekt. Das zitierte README („später vom Supervisor-Agenten“) gibt es nicht mehr. Für neue Pläne ist die Motivation erledigt; offen bleibt nur das Hinzufügen zu einem bestehenden, aktiven Plan. Neu zu entscheiden: eigenes Projekt oder `assign`-Verb im bestehenden Agenten – und lohnt die Story nach B-19 noch? Als Verb eher M als L. Für `--tag` fehlt der Client-Wrapper zu `GET creator/tags/{tagId}/exercises` (`TagsController.cs:205`).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): **kein zweites Konsolenprojekt** – die Story wird als **`assign`-Verb im bestehenden Creator-Agenten** neu geschnitten (Linie von B-19, Entscheidung 2 dort bleibt). Ziel: erzeugte bzw. per Id oder Tag gewählte Übungen einem bestehenden, aktiven Plan als Positionen hinzufügen. Vor `gegrillt` noch offen: Verhalten bei einer schon vorhandenen Position derselben Übung, ob `--tag` im ersten Schritt dabei ist (Client-Wrapper fehlt), und welche Positions-Vorgaben (Ziel, Punkte) das Verb setzt. AK 1 (eigenes Projekt) ist damit hinfällig.
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): (1) eine schon vorhandene Position derselben Übung wird übersprungen und gemeldet (Exit 2, wenn nichts neu ist); (2) `--tag` ist im ersten Schritt dabei, der Client-Wrapper wird mitgebaut; (3) Positions-Vorgaben bleiben Server-Defaults. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-13** — Backlog-Abgleich gegen den Code: Ist-Stand-Abschnitt nachgezogen (kein eigenes Projekt, `assign`-Verb fehlt, `--tag`-Wrapper fehlt). Lücke besteht.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt. Das `assign`-Verb fehlt weiter (`backend/Pugling.Agent.Creator/AgentCommands.cs:194-211` kennt `profiles`, `briefing`, `create`, `exam`, `plan`, `fill`, `images`, `help`); `CreatorApi.cs:171` hat zu `creator/tags/{tagId}/exercises` nur den POST, der GET steht am Server (`TagsController.cs:205`). Lücke besteht.
- **2026-11-14** — Refinement Backlog-Lauf: Ist-Stand neu am Code belegt (Agent hält schon `SupervisorApi`, `IsPlayable` statt „aktiv“, Server lehnt Dubletten nicht ab), Entscheidungen auf das `assign`-Verb umgeschrieben (Nutzer 2026-10-23, Auftrag 2026-11-05; neu als Stakeholder im Auftrag des Nutzers: Ziel `IsPlayable`, erst lesen dann schreiben, Bulk-POST zurückgestellt), AKs mit „Rot, wenn“; Schätzung L → M, backend, keine Migration, kein Vertragsbruch.
- **2026-11-14** — Sprint 76, gebaut: `CreatorApi.ListTagExercisesAsync` (neuer Helfer `PuglingHttp.GetPageAsync` + `PuglingPage<T>` mit `X-Total-Count`; `ClientRouteGuardTests.CallSitePattern` zählt `GetPage` mit), `CommandLine.Values` für wiederholbare Optionen (vorher gewann das letzte `--exercise-id`), `PlanAssigner` + `AssignRequest.Parse` + `AssignOutcome` (Exit-Code und Bericht am Ergebnis, damit ohne Konsolen-Mitschnitt prüfbar), Verb `assign` in `AgentCommands`, DI in `Program.cs`, `help`, README-Abschnitt, Skill `ki-creator`, Agent-`CLAUDE.md`. Tests `AssignAgentTests` (AK 1–8 + Entscheidung 8: abgelehnte Position bricht ab). Abweichung: AK 4/7 werden als `AgentUsageException` geworfen (Exit 2 über `Program`), nicht als Rückgabewert.
- **2026-11-14** — Sprint 76, Review-Funde behoben. **Stakeholder-Entscheidung im Auftrag des Nutzers:** „nichts Neues“ endet mit **Exit 3** statt 2 (Abweichung von Entscheidung 6 und AK 3). Begründung: 2 heißt in der ganzen Konsole „falsch aufgerufen“ (`Program.cs`, README), ein Wiederholungslauf, der alles schon vorfindet, war aber richtig aufgerufen – ein Skript soll beides unterscheiden können. Kosten: ein vierter Code in der Legende (Program, README, Skill, Agent-`CLAUDE.md`, `help`). Außerdem: ein Tag mit mehr Übungen als das Lesefenster (`PlanAssigner.TagWindow`, 500) bricht **vor** dem Schreiben mit Exit 1 ab statt teilweise zuzuweisen (verschärft Entscheidung 4: abbrechen statt melden); bei Tags, die sich nur in Groß-/Kleinschreibung unterscheiden, gewinnt die genaue Schreibweise, sonst Fehlbedienung; ein `--exercise-id` ohne Wert ist Fehlbedienung. Tests `AssignAgentTests.Tag_Ueber_Dem_Lesefenster_Bricht_Vor_Dem_Schreiben_Ab`, `…Tag_Nur_In_Schreibweise_Verschieden_Ist_Mehrdeutig`, `…Leeres_ExerciseId_Neben_Gueltigem_Ist_Fehlbedienung`.
- **2026-11-14** — **abgenommen** (Sprint 76). volle Suite 1650/1650 (Release, nach den Review-Fixes), Vitest 1079/1079, `tsc -b` grün; Rollengang als Playwright gegen frisch gestartete Server nach dem letzten Edit: 38 passed (Assistent inkl. `assistent-klassenarbeit`, Lehrwerk-/Reihen-/Fachlehrer-Formulare, `vater-von-null`, `kind-anlegen-id`, `uebung-teilspeichern`, `erstbegegnung`); Reviews `pugling-reviewer` + `frontend-reviewer`, Funde im Sprint behoben; Commit `8e970493`. Mutanten rot: Q3 aktiver statt spielbarer Plan (AK 1), Q4 vorhandene Position nicht übersprungen (AK 3), Q5 Lesefenster-Kürzung (Review-Fund), Q6 Exit 2 statt 3 (Review-Fund, Stakeholder-Entscheidung: 2 bleibt „falsch aufgerufen“), Q7 Trockenlauf schreibt (AK 5). Ungeprüft: ein Handlauf gegen eine laufende Instanz (`/smoke-test`) – die Tests fahren den Agent gegen einen In-Process-Server.
