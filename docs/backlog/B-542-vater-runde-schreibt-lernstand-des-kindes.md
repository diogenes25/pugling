---
tags: [typ/story, status/abgenommen, bereich/training, rolle/supervisor, rolle/student]
aliases: [Vater-Übungsrunde schreibt in den Lernstand des Kindes]
status: abgenommen
prio: P3
art: Frage
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "B-433 Entscheidung 3 (zurückgestellt) und pugling-reviewer zu Sprint 69 (Backlog-Lauf 2026-11-13, docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-542 · Soll eine Übungsrunde des Vaters überhaupt in Verlauf und Lernstand des Kindes schreiben?

[B-433](B-433-vorschau-des-vaters-verbraucht-die-einfuehrung.md) hat die Einführung abgeschirmt: nur die eigene
Begegnung des Kindes setzt `PositionItemProgress.IntroducedAt`. Bewusst zurückgestellt (B-433, Entscheidung 3) blieb
die größere Frage, und der Review von Sprint 69 hat sie belegt: Spielt der Vater eine Position durch, schreibt eine
gewertete Antwort weiterhin über `ItemProgressService` in den **plan-übergreifenden** Lernstand des Kindes
(`ItemProgress`, `ItemProgressService.cs:37-48`, `:60`, `:64-73`) und einen `ItemReviewEvent` in **dessen** Verlauf
(`PositionPracticeController.cs:510-514`, ohne Rollenprüfung) – auch ein „falsch“ für eine durchgeklickte Kennenlernkarte. Dazu bewegt
eine Leitner-Bewertung des Vaters Kasten und `DueOn`, während `IntroducedAt` leer bleibt.

Gegenstimme: Der **Nachtrag** (der Vater trägt eine Übung nach, die das Kind auf Papier gemacht hat) lebt genau
davon, dass seine Runde zählt. Zu klären: Vorschau und Nachtrag trennen (eigener Modus), oder die Vater-Runde schreibt
den Lernstand nur, wenn sie ausdrücklich als Nachtrag gestartet wird.

## User Story

Als *Vater* möchte ich eine Übung ausprobieren können, ohne den Lernstand meines Kindes zu verfälschen, **und** eine
Übung nachtragen können, die mein Kind auf Papier gemacht hat, damit sie zählt. Als *Sohn* möchte ich, dass nur
zählt, was ich oder mein Vater als meine Leistung eingetragen haben.

## Ist-Stand am Code

1. **Die Vater-Runde zählt voll.** `PositionPracticeController.Review` ruft `itemProgress.RecordAsync` ohne
   Rollenprüfung (`PositionPracticeController.cs:510-514`); `RecordAsync` schreibt immer den `ItemReviewEvent`
   (`ItemProgressService.cs:37-48`) und bei `countsForMastery` den `ItemProgress` (`:50-73`). Auch Leitner-Kasten und
   Punkte buchen auf das Kind (`PositionPracticeController.cs:520-545`, `ChildPointsEntries` mit `plan.ChildId`). Nur
   `IntroducedAt` ist abgeschirmt (`:449-453`, `User.IsStudent()`).
2. **Das ist der dokumentierte Nachtrag.** Nur der Supervisor darf beim Start einen fremden Tag setzen
   (`PositionPracticeController.cs:75-76`, `Forbid()` für das Kind; ebenso `PositionTestsController.cs:120`), und
   `docs/tutorial-supervisor.md:513-514` beschreibt genau das: „fremde Tage nachtragen“. Inaktive Pläne darf er
   ebenfalls spielen (`:79-81`, „preview/catch-up“).
3. **Das Ausprobieren hat schon einen eigenen, nebenwirkungsfreien Weg.** Der Vater probiert eine Übung über
   `creator/exercises/{id}/preview` (`ExercisePreviewController.cs:22`, `ExercisePreviewService.CheckAsync` „without
   any persistence“, `ExercisePreviewService.cs:49-52`) – im Vater-Web der Knopf „🧪 Ausprobieren“
   (`frontend/src/vater/PlanPositions.tsx:669`, `ExercisePreviewModal`).
4. **Im Frontend gibt es keinen Weg, als Vater die Position des Kindes zu spielen.** Die Arcade lässt nur die Rolle
   `Student` hinein (`frontend/src/sohn/SohnApp.tsx:66`); das Vater-Web ruft keinen `practice-sessions`-Endpunkt
   (nur `frontend/src/lib/api.ts:658-676`, gerufen aus `sohn/`). Eine Vater-Runde in der Position entsteht also nur
   über die API – mit Absicht (Nachtrag) oder über einen Agenten.

## Die echte Lücke

Keine im Produkt: Vorschau und Nachtrag **sind** schon getrennt – die Vorschau über den Preview-Endpunkt, der Nachtrag
über die Position. Was fehlt, ist die Festschreibung: der Code-Kommentar an `PositionPracticeController.cs:444-448`
nennt das Verhalten noch eine offene Frage (B-542), und kein Test hält fest, dass eine Vater-Runde in der Position
zählt. Ein späterer „Fix“ (Rollenprüfung um `RecordAsync`) würde den Nachtrag still brechen.

## Offene Punkte

1. ~~Soll die Vater-Runde in Verlauf und Lernstand des Kindes schreiben?~~ → Entscheidung 1.
2. ~~Eigener Vorschau-Modus in der Position?~~ → Entscheidung 2.
3. ~~Kasten bewegt sich, `IntroducedAt` bleibt leer – Widerspruch?~~ → Entscheidung 3.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-14, Rolle übertragen auch für `Frage`).

1. **Bleibt, wie es ist: eine Runde des Vaters in der Position des Kindes ist ein Nachtrag und zählt** – Verlauf,
   Lernstand, Kasten und Punkte. Begründung: Das ist der dokumentierte Zweck des Supervisor-Zugangs (Ist-Stand 2).
   Der Anti-Cheat richtet sich gegen das **Kind** (fremde Tage, inaktive Pläne, Stufe – alles am Server geklemmt);
   der Vater ist die Instanz, die ohnehin Münzen von Hand buchen darf (`Manual`), eine Wand gegen ihn schützt
   niemanden. Kosten: Wer als Vater (oder als Agent, B-20) die Position nur „anschauen“ will, verfälscht den
   Lernstand – dafür ist der Preview-Weg da, und das muss in Tutorial und Code-Kommentar stehen.
2. **Kein eigener Vorschau-Modus in der Position.** Begründung: Der nebenwirkungsfreie Weg existiert (Ist-Stand 3),
   ein zweiter wäre ein Parallel-Stack mit eigener Vertragsfläche (Modus-Feld, eigene Regeln). Kosten: Die Vorschau
   zeigt die Übung, nicht die Position (keine Tagesdosis, kein Kasten) – für „ausprobieren, bevor ich zuweise“ genügt
   das.
3. **`IntroducedAt` bleibt die Wand aus B-433** – Einführen ist das Erlebnis des Kindes; dass ein Nachtrag den Kasten
   bewegt, ohne einzuführen, wird hingenommen. Kosten: Ein nachgetragenes Wort zählt in der Tagesdosis weiter als
   neu, bis das Kind es selbst sieht (B-433, Kommentar `PositionPracticeController.cs:444-448`).

## Akzeptanzkriterien

1. **Regressionstest als Beleg:** Der Vater startet eine Übungsrunde in der Position des Kindes (mit `day` gestern)
   und beantwortet eine fällige, eingeführte Karte richtig getippt ⇒ beim Kind stehen ein `ItemReviewEvent`
   (`WasCorrect = true`), ein `ItemProgress` mit `CorrectCount = 1` und eine `Base`-Buchung im Ledger. Rot, wenn um
   den Aufruf `itemProgress.RecordAsync` oder um die Punktebuchung eine Rollenprüfung (`User.IsStudent()`) gelegt wird.
2. Derselbe Test ohne `day` (heute) zählt genauso – rot, wenn nur der zurückdatierte Start als Nachtrag gilt.
3. Der Kommentar an `PositionPracticeController.cs:444-448` nennt die Entscheidung statt der offenen Frage;
   `docs/tutorial-supervisor.md` (Abschnitt Nachtrag, `:513-514`) sagt in einem Satz: Ausprobieren über
   „🧪 Ausprobieren“ bzw. `creator/exercises/{id}/preview`, eine Runde in der Position zählt fürs Kind.

Ergebnis für den Bau-Sprint: Die Story wird mit diesem Regressionstest als Beleg **abgenommen** (es wird nichts am
Verhalten gebaut); wer den Test für überflüssig hält, setzt sie stattdessen als „gemessen“ auf `verworfen`. Die Stufe
setzt der Bau-Sprint.

## Schätzung

**Größe: XS** – ein Integrationstest, ein Kommentar, ein Satz im Tutorial; kein Verhalten ändert sich.

- **`migration: nein`** – kein Schema berührt.
- **`vertragsbruch: nein`** – kein DTO, kein Endpunkt.

**Risiken:**

- Der Test muss eine Karte treffen, die *gewertet* wird (eingeführt, fällig, getippt, heute noch nicht gewertet),
  sonst ist `countsForMastery` falsch und AK 1 prüft nur den Verlauf. Vorbild für den Aufbau:
  `ErstbegegnungTests.VaterRunde_FuehrtKeinWortEin_DasKindBekommtDieKennenlernkarteWeiterhin` (`:281`), aber mit
  `introduced: true`.
- Der KI-Supervisor-Agent (B-20) muss den Preview-Weg nehmen – als Hinweis in dessen Story, nicht hier.

**Angriffsplan (Backend zuerst):**

1. Integrationstest (AK 1, AK 2) in `Pugling.Api.Tests/ErstbegegnungTests.cs` neben dem B-433-Test oder in
   `PositionPracticeFlowTests.cs`; rote Probe: `if (User.IsStudent())` um `RecordAsync` legen – der Test muss **an
   der Zusicherung auf `ItemReviewEvent`** fallen, nicht früher.
2. Kommentar in `PositionPracticeController.cs:444-448` umschreiben (englisch, Entscheidung statt offener Frage).
3. Satz in `docs/tutorial-supervisor.md`.

**Testweg:**

- Integrationstest wie oben; kein E2E (kein UI-Weg betroffen, Ist-Stand 4), kein `/smoke-test` nötig.

## Verlauf

- **2026-11-13** — angelegt im Backlog-Lauf 2026-11-13 aus B-433 (E3) und dem `pugling-reviewer`-Befund zu Sprint 69.
- **2026-11-14** — Abgleich Backlog-Lauf: Kern gilt – der Aufruf `itemProgress.RecordAsync` (`PositionPracticeController.cs:510-514`) prüft keine Rolle und schreibt `ItemReviewEvent` (`ItemProgressService.cs:37-48`) und `ItemProgress` (`:56-73`) auch für eine Vater-Runde; nur `PositionItemProgress.IntroducedAt` ist seit B-433 abgeschirmt (`PositionPracticeController.cs:449-451`). Zeilenangaben berichtigt.
- **2026-11-14** — Refinement Backlog-Lauf: ausformuliert, gegrillt (drei Stakeholder-Entscheidungen im Auftrag des Nutzers: bleibt wie es ist – die Vater-Runde ist der dokumentierte Nachtrag, das Ausprobieren hat mit dem Preview-Endpunkt schon einen nebenwirkungsfreien Weg) und geschätzt: XS, ein Regressionstest als Beleg, kein Verhalten geändert.
- **2026-11-14** — **abgenommen** (Sprint 73) mit Regressionstest als Beleg (Entscheidung: bleibt wie es ist).
  volle Suite 1612/1612 (Release), Vitest 1000/1000, `tsc -b` grün, Rollengang Sohn als Playwright gegen frisch gestartete Server nach dem letzten Edit: 9 passed (`belohnung-trotz-wochenziel`, `bonus`, `bonus-deckel*`, `pfad`, `lektion`, `erstbegegnung`, `full-flow`); Reviews `pugling-reviewer` + `frontend-reviewer` (Funde im Sprint behoben); Commit `fa335075`. Mutanten M20 (`IsStudent` um `RecordAsync`) und M21 (um die Punktebuchung) → rot an
  `Gewertete_Vaterrunde_Schreibt_Verlauf_Lernstand_Und_Punkte_Des_Kindes` (beide Tage). Kommentar und Tutorial-Satz
  nachgezogen. Kein Rollengang (kein UI-Weg betroffen).
