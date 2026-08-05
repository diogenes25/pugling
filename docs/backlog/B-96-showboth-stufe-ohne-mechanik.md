---
tags: [typ/story, status/abgenommen, bereich/uebungen, lerntechnik/vokabeln, rolle/supervisor]
aliases: [Beide zeigen ohne Mechanik, ShowBoth-Stufe]
status: abgenommen
prio: P2
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-08-04 der Commits 3be7409…f8b0c99 (B-70/B-78/B-79)
grund: ""
ersetzt_durch: []
---

# B-96 · „Beide zeigen (Kennenlernen)" ist eine Beschriftung ohne eigene Stufe

Der Vater kann einer Position seit B-79 die Stufe **„Beide zeigen (Kennenlernen)"** zuweisen — der Name
verspricht eine Stufe zum bloßen Anschauen, ohne Prüfung. Im Spielpfad existiert sie nicht: das Kind bekommt
dieselbe Umdrehkarte wie bei der Selbsteinschätzung, wird also gefragt, ob es das Wort *gewusst* hat, und die
Antwort zählt wie eine normale Selbstbewertung. Dieselbe Fehlerklasse wie B-73 („Das Auswahl-Feld verspricht
Multiple-Choice, das Kind bekommt Freitext"): die Beschriftung stellt eine Lernform in Aussicht, die es nicht
gibt.

## User Story

Als **Vater** möchte ich beim Zuweisen einer Position darauf vertrauen können, dass eine angebotene Stufe die
Lernform bringt, die ihr Name verspricht — damit ich „Kennenlernen" nicht als erste, bewertete Prüfung
verabreiche.

## Ist-Stand am Code

- Angeboten wird die Stufe für Vokabel-Übungen: `backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:84`
  → `new((int)TestStage.ShowBoth, "Beide zeigen (Kennenlernen)")`. Über dieses Manifest füllt das Frontend
  seine Auswahl, und seit B-79 **validiert** dieselbe Liste die Stufe beim Schreiben.
- Der Wert selbst: `backend/Pugling.Api/Models/StudyPlanEntities.cs:40` → `ShowBoth = 1`.
- Im Spielpfad taucht er **nirgends** auf. Die stufenabhängigen Zweige der Vokabel-Übung sind
  `VocabularyExerciseType.cs:50` (Distraktoren nur bei `MultipleChoice`), `:65` (Buchstabenzahl nur bei
  `LetterBoxes`), `:66` (Audio nur bei `Audio`) und `:73` (`AudioReplacesPrompt`) — keiner nennt `ShowBoth`.
- Damit fällt die Stufe in den Nicht-Getippt-Zweig: `Services/Shared/StageMechanics.cs:17` zählt nur
  `LetterBoxes`, `FreeText`, `Audio` und `MultipleChoice` als getippt. Für alles andere gilt der
  Selbsteinschätzungs-Pfad — Lösung aufdecken, „Gewusst / Nicht gewusst", Punkte und (bei
  `UseLeitner`) Kastenbewegung.
- Ein Volltext-Treffer über `ShowBoth` findet außerhalb von Enum, Manifest, Seed und Tests keine Zeile
  (geprüft am 2026-08-04 über `backend/` und `frontend/src`).
- Der Seed umgeht das Problem, statt es zu lösen: `Data/Seed.cs:309` legt die Stufe mit `leitner: false` und
  `GoalCadence.None` an (Kommentar: „getting acquainted (front/back visible)"), `Seed.cs:339` setzt sie als
  ersten Schritt eines Stufenfahrplans. Das ist ein Indiz, dass die Stufe als **nicht bewertend** gedacht war.

## Die echte Lücke

Nicht „ShowBoth ist kaputt" — die Stufe verhält sich vollständig wie `SelfAssess`, also korrekt für einen
Selbstcheck. Die Lücke ist der **Widerspruch zwischen Beschriftung und Mechanik**: „Beide zeigen" sagt, dass
Vorder- **und** Rückseite von Anfang an sichtbar sind, und „Kennenlernen" sagt, dass hier nicht bewertet wird.
Geliefert wird eine verdeckte Karte mit Selbstbewertung. Der Vater kann das nicht sehen, ohne die Position
selbst durchzuspielen.

Zwei Wege stehen offen, und sie unterscheiden sich in den Kosten deutlich: die Stufe **bauen** (Karte zeigt
beide Seiten, keine Bewertung, kein Leitner-Fortschritt) oder sie **nicht mehr anbieten** (aus dem Manifest
nehmen; der Enum-Wert bleibt, weil Seed und Bestandsdaten ihn tragen).

## Offene Punkte (gegrillt)

1. **Bauen oder zurückziehen?** Entscheidung: **bauen** — die Stufe hat einen echten didaktischen Platz
   (erste Begegnung mit neuem Wortschatz, siehe Leitner/Birkenbihl) und der Seed benutzt sie als ersten
   Schritt seines Fahrplans. Kosten: ein neues `IExerciseType`-Interface-Mitglied (`IsDisplayOnlyStage`,
   additiv mit Default `false`), ein Override, ein neues Contract-Feld (`PracticeCard.DisplayOnly`,
   additiv), Anpassungen in `PositionPracticeController`/`PositionTestsController`, ein Frontend-Zweig.
2. **Zählt eine Kennenlern-Runde für die Pflicht?** Entscheidung: sie zählt als *geübt* (Minuten, Sitzung,
   Missionen: `IntroducedAt`/`DueOn` werden gestempelt), aber **nicht** als Treffer und **nicht** für die
   Leitner-Kastenbewegung — sonst ist „Kennenlernen" der billigste Weg zu Münzen. Kosten: `scored` in
   `PositionPracticeController.Review` schließt `IsDisplayOnlyStage` zusätzlich aus; ein Nachtrag aus dem
   `pugling-reviewer`-Befund beim Bau schließt zusätzlich die reine Verlaufszeile (`ItemReviewEvent`) ein —
   sie darf auch bei gespooftem `wasKnown: true` kein „richtig" tragen, sonst zeigt der Vater-Verlauf ein
   Urteil, das die Stufe nie gefällt hat.
3. **Braucht die Klausur die Stufe?** Entscheidung: **nein** — eine Prüfung ohne Frage ist keine. Der
   Abschlusstest lehnt sie jetzt für Kind **und** Vater-Vorschau ab (`ApiErrors.StageNotTestable`, additiv,
   400). Der in der Story benannte `AntiCheatTests.cs:61` blieb beim Überprüfen unverändert richtig: er
   testet, dass das kindliche `dto.Stage` überhaupt ignoriert wird (nicht den ShowBoth-Fall selbst) und
   bleibt aussagekräftig. Ein anderer Bestandstest (`PositionPlayModesTests`, Vater-Vorschau) nutzte
   `ShowBoth` nur zufällig als „irgendeine vom Vater gewählte Stufe" und wurde auf `SelfAssess`
   umgestellt (legitime Anpassung, keine Verwässerung — geprüft vom `pugling-reviewer`).

## Akzeptanzkriterien

1. Wer eine Position auf „Beide zeigen (Kennenlernen)" stellt und sie als Kind spielt, sieht Wort **und**
   Übersetzung ohne Umdrehen.
2. Auf dieser Stufe gibt es keine Selbstbewertung („Gewusst / Nicht gewusst") — oder, falls Punkt 1 der
   offenen Punkte anders entschieden wird: die Stufe steht nicht mehr im Manifest, und ein Schreibversuch mit
   ihr wird von der B-79-Validierung abgelehnt.
3. Der Leitner-Kasten bewegt sich durch eine Kennenlern-Runde nicht (entsprechend der Entscheidung zu Punkt 2).
4. Ein Test hält die Entscheidung fest — kein „grün wie vorher": vorher ist die Stufe von `SelfAssess` nicht
   unterscheidbar, ein Test darauf wäre also heute schon grün.

## Schätzung

`groesse: M`, `wo: beides` (Backend zuerst), `migration: nein`, `vertragsbruch: nein` (additiv: neues
Interface-Mitglied mit Default, neues Contract-Feld, neuer Fehlercode). Angriffsplan: Plugin-Erweiterung
(`IExerciseType.IsDisplayOnlyStage`, Override in `VocabularyExerciseType`) → Übungspfad
(`PositionPracticeController`: Karte flaggen, `scored`/Verlaufszeile ausschließen) → Klausurpfad
(`PositionTestsController`: `stage_not_testable` vor der ersten Zuweisung) → Frontend (`SohnPractice.tsx`:
Karte sofort beidseitig zeigen, „Weiter"-Knopf statt Urteil). Testweg: vier neue Backend-Tests in
`PositionPracticeFlowTests.cs` (Karte+Flag, Nicht-Wertung inkl. Verlaufszeile, Klausur-Ablehnung), rot
verifiziert per `git stash` der Implementierung; ein Bestandstest angepasst (`PositionPlayModesTests`);
Frontend-Logik als reine Funktion (`reviewFeedback`) ausgelagert und mit 5 Vitest-Fällen abgedeckt (Muster
`SelfAssessAnswer.test.tsx`) — kein Playwright nötig, `SohnPractice.tsx` bleibt sonst ein
fetch-getriebener Bildschirm (Grenze aus `frontend/CLAUDE.md`).

## Verlauf

- **2026-08-04** — angelegt aus dem Code-Review der B-70/B-78/B-79-Commits; Ist-Stand direkt am Code
  belegt (Manifest, `StageMechanics`, Volltext-Suche, Seed), darum gleich `ausformuliert`. Die Id ist `B-96`,
  weil `B-93` beim Anlegen schon vergeben war (der committete Index war stale) — `B-94` bleibt darum
  unbenutzt: die Nummer trug kurzzeitig eine Dublette zu [B-93](B-93-birkenbihl-einstellungen-ohne-wirkung.md)
  und wird nicht neu vergeben.
- **2026-08-05** — im Autonomen Modus gegrillt, geschätzt und gebaut. Rote Probe zuerst: alle neuen Tests
  scheiterten gegen den Vorzustand (fehlendes `displayOnly`-Feld, gewertete Punkte/Box, 201 statt 400 bei
  der Klausur). `dotnet test Pugling.sln -c Release` → **713/713 grün**. `pugling-reviewer` fand einen
  nicht-blockierenden Zusatzbefund (rohe `WasCorrect` in der `ItemReviewEvent`-Verlaufszeile trotz
  `displayOnly`) — direkt behoben und mit einer zusätzlichen Assertion belegt, danach erneut 713/713.
  `frontend-reviewer` fand einen **Blocker** (jede Kennenlern-Karte zeigte fälschlich „Leider nicht.", weil
  der Client `wasCorrect` ungeprüft auswertete) — behoben durch Auslagern der Feedback-Regel in die reine
  Funktion `reviewFeedback` (Muster `SelfAssessAnswer`), mit 5 neuen Vitest-Fällen belegt; danach ohne
  Blocker. Frontend: `npm run build` (Typecheck) grün, `npm test` → **127/127** (122 + 5 neu). Commit:
  siehe Repo-Verlauf (B-96-Commit). Status → `abgenommen`.
