---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/ki, rolle/creator]
aliases: [Agent gibt 10 statt 20, RewardPoints-Bedeutung nachgezogen, halbierte Zielbelohnung]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-09-02 zu den 19 Commits vor origin/main
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-45]
wartet_auf: ""
---

# B-202 · Der KI-Creator halbiert die Zielbelohnung jeder erzeugten Übung

B-45 hat die **Bedeutung** von `RewardPoints` geändert: aus „was die Übung wert ist" wurde „die
Zielbelohnung, die eine neue Position **erbt**, wenn der Supervisor nichts sagt". Die Vorgabe dieser
Zielbelohnung ist 20 (`GoalPointsPolicy.Fallback`). Der KI-Creator gab weiter 10 mit — jede vom Agenten
erzeugte Übung setzte damit ein halbiertes Münzziel für jede Position, die aus ihr entsteht.

**Dieselbe Regression war im selben Diff schon behoben** — im Frontend
(`VaterExerciseCreate.tsx:66`, 10 → 20). Eine Tür weiter blieb sie stehen.

## User Story

Als *Vater* möchte ich, dass eine vom KI-Creator erzeugte Übung dieselbe Zielbelohnung empfiehlt wie eine
von Hand angelegte, damit ich nicht bei jeder Position nachrechnen muss, ob der Agent oder das Formular sie
angelegt hat.

## Ist-Stand am Code (vor der Reparatur)

| Stelle | Wert | Fundort |
|---|---|---|
| Vorgabe des Agenten | **10** | `AgentCommands.cs:16` |
| Vorgabe des Formulars | 20 | `VaterExerciseCreate.tsx:66` |
| Vorgabe des Servers beim Erben | 20 | `GoalPointsPolicy.Fallback` |
| Doku am Vertrag | „Points the exercise is worth." — die **alte** Bedeutung | `GenerationRequest.cs:38` |
| Hilfetext der Konsole | „Punkte der Übung (Standard 10)" | `AgentCommands.cs:387` |

Ein zweiter Weg existiert nicht: `create` **und** `exam` bauen ihre Anfrage über `BuildRequestAsync`
(`AgentCommands.cs:250`), eine Konstante deckt also beide.

## Die echte Lücke

Nicht die Zahl, sondern **die Bedeutung, die an drei Stellen nicht nachgezogen wurde**. Die Zahl 10 war
richtig, solange das Feld „was die Übung wert ist" hieß; sie wurde erst durch B-45 falsch. Die beiden
Textstellen (Vertragsdoku, Hilfetext) erklären dem nächsten Leser weiter die alte Welt — und der
Hilfetext ist die einzige Stelle, an der ein Nutzer die Regel überhaupt erfährt.

Der Review hat nur die Konstante und die Vertragsdoku genannt; der Hilfetext trug denselben Fehler und ist
mitgezogen.

~~und der Hilfetext ist die einzige Stelle, an der ein Nutzer die Regel überhaupt erfährt~~ — **falsch, vom
`pugling-reviewer` widerlegt:** `backend/Pugling.Agent.Creator/README.md:152` trug die Option in einer
Tabelle mit derselben alten Bedeutung **und** der alten Zahl, und `backend/Pugling.Agent.Creator/CLAUDE.md`
verweist für die Bedienung ausdrücklich auf dieses README. Es waren also **vier** Stellen, nicht drei — die
Behauptung „die einzige" war schon beim Schreiben nicht geprüft.

## Akzeptanzkriterien

1. `--points` ohne Wert ergibt eine Übung mit `RewardPoints == GoalPointsPolicy.Fallback`, geprüft **über
   die echte Kommandozeile** — dort wohnt der Vorgabewert, nicht im Anfrage-Helfer der Tests.
2. Der Wert, den eine Position tatsächlich erben würde (`SuggestedGoalPoints`), ist derselbe — die Vorgabe
   ist also keine Zahl, die die Policy auf dem Weg nach draußen erst reparieren muss.
3. Vertragsdoku und Hilfetext nennen die **heutige** Bedeutung samt der Schranke `(0, 200]`.
4. Die rote Probe fällt an der Punktezusicherung, nicht am Aufsetzen des Tests.

## Entscheidungen

1. **Die Konstante verweist auf `GoalPointsPolicy.Fallback`, statt 20 zu wiederholen** — als Kommentar, nicht
   als Code-Abhängigkeit. Begründung: Das Agenten-Projekt hängt an `Pugling.Contracts`/`Client`, nicht an
   `Pugling.Api`; ein `using` auf die Policy wäre eine neue Projektkante für eine Zahl. Der **Test** stellt
   die Verbindung her (er läuft in `Pugling.Api.Tests` und darf beides sehen) — dort fällt er auf, wenn die
   Policy ihre Vorgabe je ändert. *Kosten:* Zwei Zahlen an zwei Orten, verbunden durch einen Test statt
   durch den Compiler. Genau das, was die Testzeile leistet.
2. **Der Hilfetext bleibt deutsch.** Begründung: `CLAUDE.md` nennt Laufzeit-Ausgabe mit Produktinhalt
   ausdrücklich als deutsche Fläche; die Konsolen-Hilfe ist durchgehend deutsch. *Kosten:* keine — der
   Nachbarsatz war schon deutsch.

## Schätzung

**Größe: XS** — eine Konstante, zwei Textstellen, ein Test.

- **`wo: backend`** — `Pugling.Agent.Creator` plus ein Test in `Pugling.Api.Tests`.
- **`migration: nein`** — kein Schema berührt. **`vertragsbruch: nein`** — `GenerationRequest` ist ein
  internes Record des Agenten, nicht `Pugling.Contracts`; nur seine Doku ändert sich.

**Testweg**: `backend/Pugling.Api.Tests/CreatorAgentTests.cs`
(`Ohne_points_erbt_die_Position_die_Vorgabe_des_Produkts_und_nicht_die_Haelfte`) — treibt
`AgentCommands.RunAsync` mit `create` und liest die Übung aus dem Katalog zurück.

## Verlauf

- 2026-09-02 · Aufgenommen aus dem **Code-Review** der 19 Commits vor `origin/main` und im selben Zug
  **gebaut**. Zuordnung aus dem Git belegt, nicht geraten: `git log -S DefaultRewardPoints` nennt `8b1c767`
  (die Einführung des Agenten, damals korrekt), falsch wurde der Wert erst mit B-45 — darum
  `entgangen_bei: [B-45]`.
  **Verifikation:** 839/839 Backend grün (+1 neu), `dotnet format` auf den geänderten Dateien sauber.
  Commit `81eb60c`.
  **Rote Probe mit Zahl:** Vorgabe zurück auf 10 → `Expected: 20, Actual: 10` (CreatorAgentTests.cs:590).
  Der **erste** Anlauf der Probe fiel am **falschen Ort** und ist der lehrreichste Teil: ohne `--count 3`
  verlangt der Agent zehn Aufgaben, der Fake-Entwurf liefert drei, der Entwurf fällt durch die Regeln — der
  Test lieferte Exit-Code 1 statt der Punktezahl und wäre grün wie rot nutzlos gewesen. Die Begründung steht
  jetzt im Test, damit sie nicht wieder verloren geht.
- 2026-09-02 · Bleibt auf `in-arbeit`: Der `pugling-reviewer` ist **nicht gelaufen**, weil diese Sitzung die
  Regel trägt, keine Agenten unaufgefordert zu starten. Alles andere ist belegt. `wartet_auf` benennt es.
  Ein **Rollengang** an der laufenden App fehlt ebenfalls und ist hier auch nicht der passende Beleg: die
  Fläche ist eine Konsolen-Vorgabe, und der Integrationstest fährt sie über die echte Kommandozeile.
- 2026-09-02 · **`pugling-reviewer` gelaufen** (Freigabe des Nutzers). **Kein Blocker**, aber ein 🟡 und drei
  🟢, und der 🟡 hielt Akzeptanzkriterium 3 offen. Alles eingearbeitet:
  1. **`README.md:152`** trug die vierte Textstelle mit alter Bedeutung und alter Zahl. Damit ist die
     Behauptung „der Hilfetext ist die einzige Stelle" im Abschnitt „Die echte Lücke" widerlegt und dort
     durchgestrichen statt stillschweigend korrigiert.
  2. **`CreatorAgentTests.cs:593`** war eine **Zusicherung, die nicht fallen kann** — genau der Bruch der
     Regel aus der Root-`CLAUDE.md`. `Assert.Equal(Fallback, Effective(Fallback))` ist für *jeden* Wert von
     `Fallback` wahr, weil beide Zweige von `Effective` ihn zurückgeben. Selbst nachgerechnet und bestätigt
     (`GoalPointsPolicy.cs:49-50`): auch mit `Fallback = 300` hielte sie. Ersetzt durch
     `Assert.Equal(exercise.RewardPoints, exercise.SuggestedGoalPoints)` — das prüft, was sie noch tragen
     kann (das Feld wird überhaupt aus der Policy befüllt), und der Kommentar sagt das ausdrücklich. **Die
     Schranke wird nicht nachgebaut:** sie ist längst gepinnt, wo ein Wert wirklich repariert wird
     (`PlanPositionCrudTests.cs:336` und `:372-388`) — nachgesehen, nicht vermutet.
  3. **Hilfetext** nennt jetzt die Schranke (`Standard 20, max 200`) und sitzt wieder auf Spalte 81 wie
     `--count`/`--per-type`/`--date`. Kriterium 3 verlangt beides — Bedeutung **und** Schranke.
  4. **Der Gruppen-Kommentar** über den Konstanten dokumentierte durch meine zwei Leerzeilen nur noch
     `DefaultItemCount`; `DefaultExamPerType` stand undokumentiert da (still, weil CS1591 bei `private`
     nicht greift). Gruppe wieder zusammengeführt, `DefaultRewardPoints` mit eigener Doku und der
     Begründung, warum sie sie braucht.
  **Bewusst nicht geändert:** `CreatorAgentTests.cs:67` lässt der `Request(…)`-Helfer weiter `RewardPoints: 10`
  stehen. Der Reviewer nennt es „verteidigbar, aber eine Kopiervorlage". Es bleibt — ein **ausdrücklicher**
  Wert im Helfer macht die übrigen Tests unabhängig vom Vorgabewert, und 20 würde ihn versehentlich
  spiegeln: dann wäre nicht mehr erkennbar, ob ein Test die Vorgabe prüft oder nur zufällig trifft.
  **Vom Reviewer unabhängig nachgefahren:** Build ohne Warnung, **839/839** grün. Zusätzlich von ihm
  bestätigt: `RewardPoints` wird an genau einer Stelle gesetzt und einer gelesen, und **drei** Verben
  (`create`, `exam`, `briefing`) bauen über `BuildRequestAsync` — die eine Konstante deckt also mehr Wege
  als die Story behauptete. Entscheidung 1 (Kommentar statt `using`) hat er an den csproj nachgeprüft: sie
  trägt.
- 2026-09-02 · `in-arbeit` → **`abgenommen`**. Reviewer gelaufen und eingearbeitet, Kriterium 3 jetzt
  vollständig erfüllt, **839/839** Backend grün, `dotnet format` auf allen geänderten Dateien sauber.
  Commits `81eb60c` (Reparatur) und der Nachtrag dieser Runde. Ein **Rollengang** entfällt begründet: die
  Fläche ist eine Konsolen-Vorgabe, und der Integrationstest fährt sie über die echte Kommandozeile —
  dieselbe Ausnahme, die das Bereichs-README für nicht sinnlich beurteilbare Flächen zulässt.
