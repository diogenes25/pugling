---
tags: [typ/story, status/geschaetzt, bereich/training, rolle/supervisor]
aliases: [KI-Supervisor-Agent]
status: geschaetzt
prio: P3
art: Wunsch
groesse: L
wo: backend
migration: nein
vertragsbruch: nein
quelle: memory/ki-agenten-semantic-kernel.md
grund: ""
ersetzt_durch: []
---

# B-20 · KI-Supervisor-Agent (Teil D) — erster Schritt: Übungen einem Plan zuweisen

Der Creator-Agent (`Pugling.Agent.Creator`) erzeugt Übungen im Katalog, weist sie aber laut eigener
Dokumentation bewusst **nicht** einem Lehrplan zu — das soll „von Hand (oder später vom
Supervisor-Agenten)" passieren. Diesen zweiten Agenten gibt es noch nicht. Der Ist-Stand zeigt aber: die
HTTP-Schicht dafür (`Pugling.Client.SupervisorApi`) ist bereits vollständig, nur das Konsolenprojekt und
sein Kommando fehlen.

## User Story

Als *Supervisor* (bzw. sein KI-Agent in seinem Namen) möchte ich eine bereits im Katalog vorhandene Übung
mit einem Kommando in den aktiven Lehrplan eines Kindes übernehmen, damit die vom Creator-Agenten
erzeugten Übungen nicht mehr von Hand als Position angelegt werden müssen.

## Ist-Stand am Code

- **Der Supervisor-Agent existiert nicht.** `Pugling.sln` listet nur ein Konsolenprojekt:
  `Project(...) = "Pugling.Agent.Creator", "backend\Pugling.Agent.Creator\Pugling.Agent.Creator.csproj"` —
  kein `Pugling.Agent.Supervisor`. Der Ordner ist nicht angelegt.
- **Der Creator-Agent dokumentiert die Lücke selbst**, statt sie zu füllen:
  `backend/Pugling.Agent.Creator/README.md:170-173` — „**In einen Lehrplan zuweisen.** Ziele, Punkte und
  Malus sind Sache des Supervisors – der Agent füllt den Katalog. Die erzeugte Übungs-Id wandert von Hand
  (oder später vom Supervisor-Agenten) in eine Lehrplan-Position."
- **Die Client-Schicht ist bereits vollständig** — entgegen der Annahme in der Memory-Notiz
  („ungeprüft, welche Client-Methoden noch fehlen"): `backend/Pugling.Client/SupervisorApi.cs` deckt
  Kinder (`ListChildrenAsync`/`GetChildAsync`), Study Plans (`ListPlansAsync`/`CreatePlanAsync`),
  Positionen (`AddPositionAsync:130`, `UpdatePositionAsync`, `DeletePositionAsync`), Objectives/Key
  Results, den Familien-Shop, Missionen/Auszeichnungen und Klassenarbeiten ab — jede Methode ein
  einzeiliger Wrapper wie von `CLAUDE.md` gefordert. Für das MVP dieser Story reichen
  `ListPlansAsync`/`GetPlanAsync` und `AddPositionAsync` — beide vorhanden, kein neuer Client-Code nötig.
- **`CreatePositionDto` braucht nur die Übungs-Id als Pflichtfeld** —
  `backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs:110-114`: `ExerciseId` ist der einzige
  nicht-nullable Parameter, alle anderen (`Cadence`, `PointsGoalMet`, `GoalThreshold`, …) haben laut
  XML-Doc (`:90-109`) Server-Defaults (`null` = 20 Punkte, 80 % Ziel, tägliche Fälligkeit etc.). Ein
  minimaler Aufruf ist also schon heute möglich, ganz ohne neue Vertragsfelder.
  `CreateMissionDto` (`backend/Pugling.Contracts/Supervisor/GamificationDtos.cs:11`) hat dagegen **keine**
  optionalen Felder (`Title`, `Metric`, `Target`, `Period`, `RewardPoints` allesamt Pflicht) — eine
  Missions-Vorschlagsfunktion bräuchte selbst entworfene Heuristiken/Inhalte und ist damit ungleich
  teurer als die reine Positions-Zuweisung.
- **Das Creator-Agent-Muster ist der Bauplan**: `backend/Pugling.Agent.Creator/Program.cs` zeigt
  Host-Aufbau, `CommandLine`/`AgentCommands` die Kommandostruktur, `CreatorPipeline.cs` die
  deterministische Ablauf-Klasse. `Pugling.Agent.Creator.csproj` referenziert nur `Pugling.Client` +
  `Pugling.Contracts` als Projekt-Referenzen, dazu `Microsoft.Extensions.AI`/`OllamaSharp` **weil** der
  Creator Inhalte generiert. Eine reine Zuweisungsfunktion braucht keinen dieser Sprachmodell-Bausteine.
- **`Pugling.Client.StudentApi.ListWordMasteryAsync(childId, onlyWeak, take)`** existiert bereits (genutzt
  von `BriefingBuilder.cs:151-152` für `--use-weak`) und wäre die Grundlage für eine spätere, ambitioniertere
  Missions- oder Zielvorschlags-Funktion — aber nicht Teil dieses ersten Schritts.

## Die echte Lücke

Nicht „welche Client-Methoden fehlen" (keine) und nicht „ob die deterministische Pipeline trägt" (sie
trägt, nur ohne Sprachmodell-Schritt) — sondern schlicht: **das Konsolenprojekt selbst fehlt**, plus ein
einziges Kommando, das eine vorhandene Übungs-Id in eine Plan-Position verwandelt. Ein vollständiger
„Supervisor-Agent" mit Missionen, Zielen (Objectives/KeyResults) und Klassenarbeiten-Planung ist deutlich
größer und bewusst **nicht** Teil dieses ersten Schritts (siehe Entscheidung 1).

## Offene Punkte

~~Welche Client-Methoden fehlen für einen Supervisor-Agenten?~~ → siehe Entscheidung 2 (keine — die
Client-Schicht ist vollständig).

~~Trägt die deterministische Pipeline des Creator-Agenten (C# besitzt den Ablauf, das Modell liefert nur
Inhalt) hier genauso?~~ → siehe Entscheidung 3.

1. Welche Supervisor-Fähigkeit ist der erste, in eine Sitzung passende Baustein — Zuweisen, Missionen,
   Ziele oder Klassenarbeiten? → siehe Entscheidung 1.
2. Braucht der erste Baustein überhaupt ein Sprachmodell (Ollama/`IChatClient`)? → siehe Entscheidung 3.
3. Woher bekommt der Agent die zuzuweisende Übungs-Id — freie Eingabe oder Kopplung an den
   Creator-Agenten? → siehe Entscheidung 4.
4. In welchen Plan wird zugewiesen, wenn ein Kind mehrere Pläne hat/hatte? → siehe Entscheidung 5.
5. Welche Ziel-/Punktewerte setzt der Agent, wenn der Nutzer nichts vorgibt? → siehe Entscheidung 6.

## Entscheidungen

1. **Erster Baustein ist „Zuweisen" (`assign`), nicht Missionen/Ziele/Klassenarbeiten.** Begründung: Es ist
   die Lücke, die der Creator-Agent selbst dokumentiert (README-Zitat oben) und schließt einen echten
   Workflow-Bruch: heute muss jede vom Creator erzeugte Übung von Hand in eine Position verwandelt werden.
   Missionen (`CreateMissionDto` ohne Defaults) und Objectives/KeyResults brauchen eigene inhaltliche
   Heuristiken (welches Ziel, welcher Titel) — das ist eine eigene, spätere Story. Kosten: die Story liefert
   bewusst *keinen* vollständigen „Supervisor-Agenten", nur seinen ersten, tragfähigen Ausschnitt; Folge-Ideen
   (Missions-Vorschlag, Objective-Planung) gehören als neue Backlog-Einträge nachgezogen, sobald dieser
   Baustein steht.
2. **Keine neuen Client-Methoden nötig.** Begründung: `SupervisorApi.ListPlansAsync`/`GetPlanAsync`/
   `AddPositionAsync` decken den gesamten MVP-Bedarf bereits ab (siehe Ist-Stand). Kosten: keine — das ist
   der Grund, warum diese Story trotz „neuer Agent" bei `vertragsbruch: nein` bleibt.
3. **Kein Sprachmodell für diesen Schritt.** Begründung: Zuweisen ist eine deterministische Entscheidung über
   Zahlen (Ziel, Cadence, Punkte) mit vorhandenen Server-Defaults — es gibt keinen Text- oder Inhalts-Anteil,
   den ein Modell beisteuern müsste. Das Creator-Agent-Muster „C# besitzt den Ablauf, das Modell liefert nur
   Inhalt" trägt hier in der Konsequenz sogar dahin, den Modell-Schritt ganz wegzulassen, statt ihn
   künstlich einzubauen. Kosten: kein `Microsoft.Extensions.AI`/`OllamaSharp`-Paket, kein Ollama-Setup nötig
   für dieses Projekt (schlankere csproj als beim Creator). Eine spätere, inhaltlichere Ausbaustufe
   (Missions-Text, Zielformulierung) kann das Muster bei Bedarf nachrüsten.
4. **Übungs-Id(s) per `--exercise-id` (wiederholbar) oder `--tag <name>`, keine harte Kopplung an den
   Creator-Agenten.** Begründung: Der Creator legt bei `exam` einen kind-skopierten Tag an
   (`backend/Pugling.Agent.Creator/README.md:161-166`) — darüber lässt sich eine ganze Klausur-Übungsmenge
   in einem Aufruf zuweisen, ohne dass der Supervisor-Agent den `ExamPlanner` kennen oder importieren muss.
   Kosten: eine zusätzliche Auflösung „Tag → Übungs-Ids" über die vorhandene Tag-Filterung der
   Katalog-Lese-Endpunkte (keine neue Client-Methode, falls ein `tag`-Query-Parameter dort schon existiert —
   sonst ein einzeiliger Ergänzungs-Wrapper).
5. **Zugewiesen wird in den aktiven Plan des Kindes** (`ListPlansAsync` gefiltert auf `Active`); ohne aktiven
   Plan bricht der Agent mit klarer Fehlermeldung ab, `--plan-id` überschreibt die Auswahl explizit.
   Begründung: konsistent mit der Anti-Cheat-Regel „genau ein aktiver+laufender Plan je Kind ist spielbar"
   (siehe Memory `ein-aktiver-plan-anti-cheat`) — der Agent soll nicht eigenmächtig einen zweiten Plan
   anlegen oder in einen inaktiven schreiben. Kosten: ein zusätzlicher Guard-Codepfad (kein aktiver Plan →
   Exit 2, keine stille Fallback-Erzeugung).
6. **Alle Ziel-/Punktefelder bleiben `null`, sofern nicht per Flag gesetzt** (`--goal-threshold`,
   `--points`, `--cadence` als optionale Overrides, analog zu den bestehenden Creator-Agent-Flags wie
   `--points`). Begründung: `CreatePositionDto` trägt die Server-Defaults schon (siehe Ist-Stand); sie ein
   zweites Mal im Agenten zu duplizieren wäre die Art Drift, die dieses Projekt teuer bezahlt hat. Kosten:
   der Agent ist beim ersten Wurf bewusst „dumm" (reine Übernahme der Server-Voreinstellung) — eine
   spätere Heuristik (z. B. Punkte aus `ExercisePayload.SuggestedBonus` ableiten) ist eine additive
   Erweiterung, kein Umbau.

## Akzeptanzkriterien

1. Ein neues Konsolenprojekt `backend/Pugling.Agent.Supervisor` (Sdk-Style, `net10.0`, referenziert nur
   `Pugling.Client` + `Pugling.Contracts`, **kein** `Microsoft.Extensions.AI`/`OllamaSharp`) ist in
   `Pugling.sln` eingetragen und baut mit `TreatWarningsAsErrors`/`GenerateDocumentationFile` wie die
   Nachbarprojekte.
2. Das Kommando `assign --child <id> (--exercise-id <id> [--exercise-id <id>…] | --tag <name>)
   [--plan-id <id>] [--goal-threshold <n>] [--points <n>] [--cadence <Daily|Weekly>] [--dry-run]` legt für
   jede aufgelöste Übung eine Position im aktiven Plan des Kindes an (`SupervisorApi.AddPositionAsync`).
3. Fehlt ein aktiver Plan und wird `--plan-id` nicht gesetzt, bricht der Agent mit einer verständlichen
   Fehlermeldung und Exit-Code 2 ab — kein stiller Fallback auf einen neuen oder inaktiven Plan.
4. `--dry-run` listet die geplanten Positionen (Kind, Plan, Übungs-Id, effektive Ziel-/Punktewerte) ohne
   einen einzigen schreibenden Aufruf.
5. Eine neue Testklasse `SupervisorAgentTests` (in `Pugling.Api.Tests`, In-Process-Server, echtes
   `Pugling.Client`, analog `CreatorAgentTests`) deckt: erfolgreiche Zuweisung per `--exercise-id`,
   Zuweisung per `--tag`, den Abbruch ohne aktiven Plan, und `--dry-run` ohne Schreibzugriff.
6. `backend/Pugling.Agent.Supervisor/README.md` und `CLAUDE.md` (Muster wie beim Creator-Agenten)
   dokumentieren Verwendung, Voraussetzungen (laufende API, Konto mit Supervisor-Rolle) und die bewusste
   Entscheidung gegen ein Sprachmodell für diesen ersten Baustein.
7. `dotnet build`/`dotnet test Pugling.sln -c Release` sind grün.

## Schätzung

**Größe: L** — ein komplett neues Konsolenprojekt (Host-Wiring, CLI-Parsing, ein Kommando, Tests, README +
CLAUDE.md) ist mehr als ein Anker-„M", bleibt aber unter der Größenordnung des Creator-Agenten selbst: kein
Sprachmodell, keine Prompt-/Reparatur-Logik, keine neuen Vertrags- oder Client-Typen (Entscheidungen 2 und
3). Ein Split in mehrere Stories war hier nicht nötig — der Zuschnitt auf „nur Zuweisen" (Entscheidung 1)
hält die Story bereits auf L, ohne dass etwas Wesentliches fehlt, um sie in einer Sitzung zu bauen.

- **`migration: nein`** — keine Schemaänderung, keine neue Entität.
- **`vertragsbruch: nein`** — kein neues/geändertes DTO in `Pugling.Contracts`; `CreatePositionDto`
  existiert unverändert.
- **Risiken**: (a) Mehrfachzuweisung derselben Übung an denselben Plan — zu prüfen, ob der Server das
  ablehnt oder stillschweigend eine zweite Position anlegt; falls Letzteres, braucht `assign` eine eigene
  Idempotenz-Prüfung (Positionsliste vorher lesen). (b) `--tag`-Auflösung setzt voraus, dass die
  Katalog-Lese-Endpunkte nach Tag filtern lassen — falls nicht, schrumpft dieser Teilaspekt auf
  `--exercise-id`-only und die Tag-Variante wird eine kleine Folge-Story.
- **Angriffsplan** (Backend zuerst, hier ist alles Backend): 1) `Pugling.Agent.Supervisor.csproj` anlegen,
  in `Pugling.sln` eintragen. 2) `CommandLine`/`AgentCommands`-Grundgerüst vom Creator-Agenten
  übernehmen und auf das eine Kommando kürzen. 3) `assign`-Ablauf: aktiven Plan ermitteln (oder
  `--plan-id`), Übungs-Ids auflösen (`--exercise-id`/`--tag`), je Übung `AddPositionAsync` aufrufen,
  `--dry-run` respektieren. 4) `SupervisorAgentTests` gegen den In-Process-Server. 5) README + CLAUDE.md
  des neuen Projekts schreiben.
- **Testweg**: neue Testklasse `SupervisorAgentTests` in `backend/Pugling.Api.Tests` (In-Process-`WebApplicationFactory`,
  echtes `Pugling.Client`, kein `FakeChatClient` nötig, da kein `IChatClient` existiert); ergänzend ein
  manueller Lauf über `/smoke-test`-Instanz mit `dotnet run --project backend/Pugling.Agent.Supervisor --
  assign --child 1 --exercise-id <id> --dry-run`.

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
