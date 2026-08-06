---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/katalog, rolle/creator, rolle/supervisor]
aliases: [Schülerprofil-Generator, KI-Lehrplan]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: memory/schueler-profil-ki-lehrplan.md
---

# B-19 · Schülerprofil-getriebener KI-Lehrplan

> **Der Zuschnitt hat sich beim Ausformulieren geändert:** Die Notiz nannte drei offene Dinge (Generator,
> Frontend, Brief-Endpunkt) und vermutete XL/Teilungsfall. Die Recherche zeigt: nur **eines** davon ist eine
> echte, unabhängige Lücke — die anderen beiden sind entweder schon vorhanden (in anderer Form) oder
> architektonisch gar nicht so baubar, wie die Notiz es unterstellte. Siehe „Die echte Lücke".

## User Story

Als Vater möchte ich, dass der KI-Agent aus dem Profil meines Kindes (Klassenstufe, Schulart, Lehrwerk/Unit,
Interessen) **mehrere zueinander passende Übungen erzeugt und direkt zu einem spielbaren Lehrplan
zusammenstellt** (Pflichtziel, Punkte, Stufe je Position), damit ich nicht jede generierte Übung einzeln von
Hand in eine Position verwandeln muss.

## Ist-Stand am Code

**Das Datenfundament steht vollständig** — die Notiz nannte es als offen, ist aber selbst schon veraltet:

- `Child.Gender`/`.Interests`/`.ProfileNotes` existieren seit der Migration `StudentProfileAndTextbooks`
  ([AdminEntities.cs:80,88,92](../../backend/Pugling.Api/Models/AdminEntities.cs)), mit explizitem Kommentar,
  dass sie **für einen späteren KI-Generator** angelegt wurden (`:74-77`: „so that a later AI generator can
  derive an individual study plan from them").
- `Textbook` ist eine eigene Kind-Entität mit `SeriesId`/`CurrentUnitId`
  ([AdminEntities.cs:146-179](../../backend/Pugling.Api/Models/AdminEntities.cs)), CRUD unter
  `supervisor/children/{childId}/textbooks`.
- Die gewichtete Interessen-Taxonomie (`ChildInterest`, positiv **und** negativ) ist gebaut und wird bereits
  ausgewertet ([B-50](B-50-kind-beschreibt-sich-selbst.md) hat das im Detail belegt).

**Der Generator existiert bereits — nur nicht für einen ganzen Plan.** `Pugling.Agent.Creator` ist die
deterministische Pipeline, die genau das tut, was die Notiz für den „Lehrplan-Generator" beschreibt, aber
für **eine einzelne Übung**:

- `BriefingBuilder.BuildAsync`
  ([BriefingBuilder.cs:21-57](../../backend/Pugling.Agent.Creator/Briefing/BriefingBuilder.cs)) bündelt
  Profil, Reihe/Unit, Kind-Interessen (gewichtet **und** Freitext), Abneigungen, Lehrbuch und schwache Wörter
  zu einem `CreatorBriefing` — **exakt** der „Generierungs-Brief", den die Notiz als drittes offenes Ding
  nennt, nur clientseitig statt als Server-Endpunkt (siehe „Die echte Lücke", Punkt 3).
- `CreatorPipeline.CreateAsync`
  ([CreatorPipeline.cs:22-35](../../backend/Pugling.Agent.Creator/CreatorPipeline.cs)) erzeugt daraus **eine**
  Übung: Entwurf → Regelprüfung → Anlegen → Selbsttest. Die Kernregel „Interessen kleiden den Stoff ein, sie
  ersetzen ihn nie" ist hier bereits deterministisch erzwungen (`DraftRules.CoversRequiredWords`).
- `ExamPlanner.RunAsync` ([ExamPlanner.cs:59-118](../../backend/Pugling.Agent.Creator/ExamPlanner.cs)) zeigt
  das **Muster für „mehrere Übungen zu einem Bündel"** bereits vor: pro Typ ein Pipeline-Lauf, danach —
  nur mit Kind — ein Tag und eine `Klassenarbeit`. Für einen Lehrplan fehlt exakt dieses Gegenstück: **ein
  `StudyPlan` mit `PlanPosition`s statt eine `Klassenarbeit`**.
- **`Pugling.Client.SupervisorApi` trägt die nötigen Methoden bereits**: `CreatePlanAsync`
  ([SupervisorApi.cs:111](../../backend/Pugling.Client/SupervisorApi.cs)) und `AddPositionAsync` (`:130`).
  Sie werden heute nur vom Vater-Web genutzt (`VaterWizard.tsx`), nicht vom Agenten — der Agent hat schon ein
  Supervisor-fähiges Konto (README: „Konto-Anforderung: Creator **und** Supervisor in einem Konto"), ruft die
  Methoden aber nie auf.
- Das README dokumentiert die heutige Grenze **ausdrücklich als Absicht, nicht als Lücke**: „Was der Agent
  bewusst *nicht* tut … In einen Lehrplan zuweisen. … Die erzeugte Übungs-Id wandert von Hand (oder später
  vom Supervisor-Agenten) in eine Lehrplan-Position."
  ([README.md:170-173](../../backend/Pugling.Agent.Creator/README.md)).

**Der Lehrplan-Assistent im Vater-Web ist bereits ein „Frontend, das Interessen/Profil trifft" —
aber deterministisch, ohne LLM.** `VaterWizard.tsx` führt in fünf Schritten (Kind → Problemfeld → Übungen →
Feinschliff → Überblick) zu einem `StudyPlan` mit Positionen; Ziel/Punkte/Malus kommen aus einer
Intensitäts-Tabelle (`INTENSITY`, [VaterWizard.tsx:32-36](../../frontend/src/vater/VaterWizard.tsx)), die
Übungen wählt der Vater manuell aus dem Katalog. Das ist **kein** KI-Generator, zeigt aber, welche
Ziel/Punkte-Heuristik ein KI-erzeugter Plan übernehmen kann, statt eine zweite zu erfinden.

## Die echte Lücke

Nicht drei unabhängige Baustellen, sondern **eine** — die anderen zwei sind kein zusätzlicher Bauauftrag:

1. **Die echte Lücke: kein Weg von „mehrere KI-Übungen" zu „ein spielbarer Plan".** Der Agent kann heute
   Übungen erzeugen (`create`) und sie zu einer Klausur bündeln (`exam`), aber nicht zu einem `StudyPlan`
   mit `PlanPosition`s — obwohl die Client-Methoden dafür bereits existieren und ungenutzt sind. Das ist eine
   Erweiterung **nach dem Muster des `ExamPlanner`**, keine neue Architektur.
2. **Die „Frontend"-Lücke der Notiz ist so nicht baubar.** `Pugling.Agent.Creator` ist eine lokale
   Konsolen-App, die direkt gegen Ollama auf `localhost:11434` spricht — sie läuft **nicht** im Backend und
   ist über keine HTTP-Route vom Browser aus erreichbar. Ein „Knopf im Vater-Web, der die KI-Generierung
   anstößt" bräuchte einen serverseitig erreichbaren Agenten-Dienst — das widerspricht der bewussten
   Architekturentscheidung „kein LLM im Backend" (`Pugling.Agent.Creator/CLAUDE.md`) und wäre, falls je
   gewollt, ein eigenes, deutlich größeres Vorhaben (Hosting, Auth, Warteschlange), nicht dieser Story. Der
   generierte Plan taucht danach ganz gewöhnlich im Vater-Web auf (`/vater/plaene`) — dafür braucht es kein
   eigenes UI, das Web zeigt jeden Plan, egal wer ihn angelegt hat.
3. **Die „Brief-Endpunkt"-Lücke ist bereits geschlossen — nur nicht als Server-Endpunkt.** `BriefingBuilder`
   plus der Agenten-Verb `briefing` (README: „Worauf würde der Agent zuschneiden? (ohne Sprachmodell, ohne
   Schreibzugriff)") liefern genau das read-only Bündel, das die Notiz vorschlägt. Ein zusätzlicher
   *Server*-Endpunkt wäre nur für einen zweiten, nicht-C#-Konsumenten wertvoll — den gibt es heute nicht.

## Offene Punkte

1. ~~Ist das wirklich XL und ein Teilungsfall?~~ → siehe Entscheidung 1: nein — die einzige echte Lücke
   (Plan-Erzeugung) ist ein M-Umfang nach etabliertem Muster; die anderen beiden Notiz-Punkte sind kein
   zusätzlicher Bauauftrag.
2. **Wo lebt die Plan-Erzeugung: neuer Verb im bestehenden `Pugling.Agent.Creator`, oder Teil des noch
   ungebauten Supervisor-Agenten ([B-20](B-20-ki-supervisor-agent.md))?** *Empfehlung: im bestehenden
   Creator-Agenten* — siehe Entscheidung 2.
3. **Woher kommen Ziel/Punkte/Stufe je Position, wenn niemand sie eintippt?** *Empfehlung: dieselbe
   Intensitäts-Heuristik wie der Lehrplan-Assistent* — siehe Entscheidung 3.
4. **Wie viele Übungen/Kapitel umfasst ein generierter Plan?** *Empfehlung: ein Kapitel, mehrere Typen* —
   siehe Entscheidung 4.
5. **Was passiert mit der Regel „genau ein aktiver+laufender Plan je Kind" ([Ein aktiver
   Plan](../lehrplan-umbau.md))?** *Empfehlung: der neue Plan entsteht inaktiv* — siehe Entscheidung 5.

## Entscheidungen

1. **Kein Split, kein `verworfen: geteilt`.** Begründung: Von den drei in der Notiz genannten Dingen ist nur
   die Plan-Erzeugung eine echte, noch offene Lücke; das Frontend-Stück ist architektonisch so nicht baubar
   (Punkt 2 der echten Lücke), und der Brief-Endpunkt ist clientseitig bereits gebaut (`briefing`-Verb).
   **Kosten:** Die Story trägt jetzt einen schmaleren Titel, als der Name vermuten lässt — das steht explizit
   im Kasten oben, damit niemand die alten drei Punkte wieder aufwärmt.
2. **Neuer Verb `plan` im bestehenden `Pugling.Agent.Creator`, kein separates Supervisor-Agent-Projekt.**
   Begründung: Der Creator-Agent hat bereits ein Supervisor-fähiges Konto (Konto-Anforderung laut README),
   und `Pugling.Client.SupervisorApi` trägt `CreatePlanAsync`/`AddPositionAsync` schon fertig. Ein zweites
   Konsolenprojekt für exakt diese zwei Aufrufe wäre doppelte Infrastruktur (Auth, Konfiguration, Tests).
   **Kosten:** `Pugling.Agent.Creator` bekommt eine Zuständigkeit mehr (Fach: Übungen *und* Pläne erzeugen);
   [B-20](B-20-ki-supervisor-agent.md) bleibt für generisches Vater-seitiges Zuweisen/Bepunkten *bestehender*
   Übungen zuständig (kein KI-Bezug) und wird durch diese Entscheidung **nicht** vorweggenommen — beide
   Stories bleiben unabhängig auslieferbar.
3. **Ziel/Punkte/Stufe je Position aus derselben Intensitäts-Tabelle wie `VaterWizard.INTENSITY`** (Bestehen
   ab 70/80/90 %, 10/20/30 Punkte, 0/5/10 Münzen Malus), per `--intensity Locker|Normal|Intensiv` (Vorgabe
   `Normal`). Begründung: eine zweite, abweichende Heuristik für denselben fachlichen Sachverhalt wäre eine
   zweite Stelle zum Vergessen, und der Vater kann jede Position danach im Web nachjustieren (`PATCH
   …/positions/{id}` existiert bereits). **Kosten:** Die Konstante muss aus dem Frontend „portiert" (nicht
   geteilt) werden, da `Pugling.Agent.Creator` kein TypeScript importieren kann — eine Kommentar-Referenz
   hält beide Stellen als „dieselbe Regel, zwei Sprachen" erkennbar.
4. **Ein generierter Plan deckt genau ein Kapitel ab, mit einer Position je gewünschtem Übungstyp** (Analogie
   zum `ExamPlanner`: `--types Vocabulary,Cloze,Grammar --per-type 2` erzeugt z. B. 2 Vokabel-, 2 Cloze- und
   2 Grammatik-Übungen, jede als eigene Position). Begründung: mehrere Kapitel in einem Lauf bräuchten eine
   Reihenfolge-/Fälligkeits-Entscheidung, die nicht Teil der Notiz war; ein Kapitel ist die kleinste sinnvolle
   Einheit und deckt sich mit dem, was der Vater heute im Assistenten in einem Durchgang anlegt. **Kosten:**
   Ein Kind mit mehreren offenen Kapiteln braucht mehrere Läufe — akzeptabel, da jeder Lauf ohnehin geprüft
   werden soll, bevor er aktiv geschaltet wird (Entscheidung 5).
5. **Der generierte Plan entsteht mit `Active: false`.** Begründung: „genau ein aktiver+laufender Plan je
   Kind ist spielbar" — ein KI-Lauf, der den laufenden Plan eines Kindes stillschweigend ablöst, wäre ein
   Anti-Cheat- **und** ein Vertrauens-Problem (ungeprüfter KI-Inhalt würde sofort Pflicht). Der Vater sichtet
   den Plan im Web und aktiviert ihn bewusst (`PATCH …/study-plans/{id}` mit `Active: true` — bereits
   vorhanden). **Kosten:** ein Zusatzschritt für den Vater; das ist der Preis für „kein KI-Inhalt wird
   ungeprüft Pflicht".

## Nach B-106

Der in Entscheidung/Verlauf vom 2026-08-04 erwartete Parameter-Tausch ist **bereits erledigt** — als
Nebeneffekt von B-106s eigenem T-06, nicht durch diese Story. Nachgeprüft (2026-08-06):
`BriefingBuilder.BuildAsync` nimmt bereits `request.SeriesUnitId` entgegen, sucht die Unit darüber und
befüllt `CreatorBriefing.SeriesUnitId`
([BriefingBuilder.cs:26,36,50](../../backend/Pugling.Agent.Creator/Briefing/BriefingBuilder.cs)) — keine
Spur von `ChapterId` mehr im Agenten-Source (nur veraltete `bin/`-Build-Artefakte tragen noch die alte
Doku-Zeile, kein Quellcode). `ExamPlanner`/`CreatorPipeline` bauen auf demselben, bereits umgestellten
Weg auf.

**Was diese Story tatsächlich noch baut, ist davon unberührt:** ein neuer `PlanPlanner`
(`backend/Pugling.Agent.Creator/PlanPlanner.cs` existiert noch nicht) und der CLI-Verb `plan` — beides
unabhängig vom Parameter-Tausch, der schon feststeht.

**Empfehlung: bleibt gültig, Schätzung unverändert.** Der ursprünglich befürchtete Zusatzaufwand („nur
ein Parameter-Tausch nötig") ist bereits kostenlos erledigt statt zusätzlich anzufallen — wenn überhaupt,
sinkt der Aufwand leicht gegenüber der Schätzung vom 2026-08-03, nicht umgekehrt.

## Akzeptanzkriterien

1. `pugling-creator plan --child <id> --types a,b,c --per-type <n> [--intensity …] [--dry-run] [--strict]`
   erzeugt für jeden Typ die gewünschte Anzahl Übungen (dieselbe Pipeline wie `create`, inkl. Selbsttest je
   Übung) und legt anschließend einen `StudyPlan` mit einer `PlanPosition` je erzeugter Übung an.
2. Der Plan entsteht mit `Active: false`; er ist im Vater-Web unter `/vater/plaene` sofort sichtbar und
   manuell aktivierbar.
3. Ziel/Punkte/Malus je Position folgen der gewählten Intensität (Vorgabe `Normal`); der Vater kann jede
   Position danach unverändert über das bestehende Positions-Formular anpassen.
4. Ein fehlgeschlagener Übungstyp bricht den Lauf **nicht** ab (analog `ExamPlanner`), erscheint aber im
   Bericht und setzt Exit-Code 1 — kein halb-vollständiger Plan wird als vollständig gemeldet.
5. `--dry-run` plant und druckt ohne Schreibzugriff (weder Übungen noch Plan/Positionen entstehen).
6. Ohne `--child` lehnt der Befehl mit einer verständlichen Meldung ab — ein Plan ohne Kind ist sinnlos
   (anders als bei `create`, wo `--profile` allein für den geteilten Katalog reicht).
7. README und `docs/REST/` bzw. Skill `ki-creator` nennen den neuen Verb mit Beispiel.

## Schätzung

**Größe: M** — vergleichbar mit B-03 (neuer Batch-Pfad im `MediaSelector`): ein neuer Planner nach exakt
bestehendem Muster (`ExamPlanner`), ein neuer CLI-Verb, keine neuen Contracts, keine Migration.

- **`migration: nein`** — keine Schemaänderung; `CreatePlanDto`/`CreatePositionDto` existieren bereits mit
  allen benötigten Feldern.
- **`vertragsbruch: nein`** — es wird kein Contract geändert, nur ein bestehender Client-Aufruf
  (`SupervisorApi.CreatePlanAsync`/`AddPositionAsync`) erstmals vom Agenten statt nur vom Vater-Web genutzt.
- **`wo: backend`** — `Pugling.Agent.Creator` zählt als Backend-Tooling (kein Frontend-Anteil laut
  Entscheidung 1/2); die Fachbrille beim Review ist `csharp-senior-dev`/`pugling-reviewer`.
- **Risiken:**
  - Eine generierte Position kann Kapazität/Fälligkeit anders treffen als ein von Hand gebauter Plan — durch
    `Active: false` (Entscheidung 5) hat das keine Wirkung, bevor ein Mensch geprüft hat.
  - Mehrere Übungstypen mit `--use-weak` teilen sich denselben Wortschatz-Pool — zu prüfen, ob das zu
    Wiederholungen über Positionen hinweg führt (kein Blocker, ggf. Folge-Notiz).
- **Angriffsplan (Backend zuerst, hier ausschließlich Backend/Tooling):**
  1. `PlanRequest`/`PlanOutcome`-Records analog `ExamRequest`/`ExamOutcome` in einer neuen `PlanPlanner.cs`.
  2. `PlanPlanner.RunAsync`: pro Typ ein `CreatorPipeline.CreateAsync`-Lauf (wie `ExamPlanner`), danach
     `SupervisorApi.CreatePlanAsync` + je Übung `AddPositionAsync` mit der Intensitäts-Tabelle aus
     Entscheidung 3.
  3. Verb `plan` in `AgentCommands.cs`/`CommandLine.cs` verdrahten, Hilfetext ergänzen.
  4. `CreatorAgentTests` um Fälle analog den bestehenden `ExamPlanner`-Tests ergänzen (Zeilen 498-559 zeigen
     das Testmuster: `FakeChatClient`, In-Process-Server, echter `SupervisorApi`).
  5. README (`Pugling.Agent.Creator/README.md`) und Skill `ki-creator` um den neuen Verb ergänzen.
- **Testweg:** Integrationstest in `CreatorAgentTests.cs` nach dem Muster der bestehenden `ExamPlanner`-Tests
  (Zeilen 498 ff.): Plan mit zwei Typen → zwei Positionen mit erwarteten Zielen/Punkten, `Active: false`
  geprüft, ein erzwungener Fehlschlag eines Typs → Exit-Code 1 und trotzdem die anderen Positionen vorhanden.
  Kein `/smoke-test` nötig (der Agent ist ein separates Konsolenprojekt, keine API-Änderung); kein E2E, da
  kein Frontend-Anteil.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft), als Teilungsfall markiert.
- **2026-08-03** — ausformuliert gegen den Code: Datenfundament vollständig bestätigt (und damit als
  „offen" veraltet), `BriefingBuilder`/`ExamPlanner`/`SupervisorApi.CreatePlanAsync`/`AddPositionAsync` als
  bereits vorhandene Bausteine gefunden. Die vermutete XL-Dreiteilung erwies sich als eine echte Lücke plus
  zwei bereits erledigte bzw. architektonisch nicht passende Punkte.
- **2026-08-03** — gegrillt: autonom getroffen, Nutzerauftrag 2026-08-04 (explizite Autorisierung, die
  Stufen `gegrillt`/`geschaetzt` ohne Dialog-Runde zu entscheiden). Kein Split — Entscheidung 1 begründet,
  warum die vermutete XL-Teilung entfällt; vier weitere Entscheidungen legen Ort, Heuristik und Umfang der
  Plan-Erzeugung fest.
- **2026-08-03** — geschätzt: autonom getroffen, Nutzerauftrag 2026-08-04. **M**, `wo: backend`,
  `migration: nein`, `vertragsbruch: nein`. Nicht umgesetzt.
- **2026-08-04** — Querverweis: [B-106](B-106-lehrwerkgetriebener-katalog.md) verschmilzt `Exercise` mit
  `SeriesUnit`; diese Story bräuchte danach nur eine Parameter-Anpassung (`SeriesUnitId` statt
  `ChapterId`), keinen strukturellen Umbau — kein Status-Wechsel hier.
- **2026-08-06** — Nachtlauf, Prämissen-Nachprüfung nach B-106s Abnahme: der erwartete Parameter-Tausch
  ist bereits vollzogen (`BriefingBuilder` nutzt schon `SeriesUnitId`, als Nebeneffekt von B-106s
  eigenem T-06). Verbleibender Umfang (`PlanPlanner`, CLI-Verb `plan`) unverändert offen. `status`
  unverändert, Empfehlung „bleibt gültig" dokumentiert.
