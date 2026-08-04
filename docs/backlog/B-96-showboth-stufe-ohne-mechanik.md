---
tags: [typ/story, status/ausformuliert, bereich/uebungen, lerntechnik/vokabeln, rolle/supervisor]
aliases: [Beide zeigen ohne Mechanik, ShowBoth-Stufe]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
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

## Offene Punkte

1. **Bauen oder zurückziehen?** Empfehlung: **bauen** — die Stufe hat einen echten didaktischen Platz (erste
   Begegnung mit neuem Wortschatz, siehe Leitner/Birkenbihl) und der Seed benutzt sie als ersten Schritt
   seines Fahrplans. Zurückziehen wäre billiger, nähme dem Fahrplan aber seinen Einstieg.
2. **Wenn gebaut: zählt eine Kennenlern-Runde für die Pflicht?** Empfehlung: sie zählt als *geübt* (Minuten,
   Sitzung, Missionen), aber **nicht** als Treffer und **nicht** für die Leitner-Kastenbewegung — sonst ist
   „Kennenlernen" der billigste Weg zu Münzen, und das Punktesystem belohnt Nichtstun.
3. **Braucht die Klausur die Stufe?** Empfehlung: **nein** — eine Prüfung ohne Frage ist keine. Dann muss der
   Abschlusstest sie ablehnen (heute nimmt er sie an, siehe `AntiCheatTests.cs:61`, wo genau das als
   Anti-Cheat-Fall geprüft wird — der Fall gehört überprüft, nicht übernommen).

## Akzeptanzkriterien

1. Wer eine Position auf „Beide zeigen (Kennenlernen)" stellt und sie als Kind spielt, sieht Wort **und**
   Übersetzung ohne Umdrehen.
2. Auf dieser Stufe gibt es keine Selbstbewertung („Gewusst / Nicht gewusst") — oder, falls Punkt 1 der
   offenen Punkte anders entschieden wird: die Stufe steht nicht mehr im Manifest, und ein Schreibversuch mit
   ihr wird von der B-79-Validierung abgelehnt.
3. Der Leitner-Kasten bewegt sich durch eine Kennenlern-Runde nicht (entsprechend der Entscheidung zu Punkt 2).
4. Ein Test hält die Entscheidung fest — kein „grün wie vorher": vorher ist die Stufe von `SelfAssess` nicht
   unterscheidbar, ein Test darauf wäre also heute schon grün.

## Verlauf

- **2026-08-04** — angelegt aus dem Code-Review der B-70/B-78/B-79-Commits; Ist-Stand direkt am Code
  belegt (Manifest, `StageMechanics`, Volltext-Suche, Seed), darum gleich `ausformuliert`. Die Id ist `B-96`,
  weil `B-93` beim Anlegen schon vergeben war (der committete Index war stale) — `B-94` bleibt darum
  unbenutzt: die Nummer trug kurzzeitig eine Dublette zu [B-93](B-93-birkenbihl-einstellungen-ohne-wirkung.md)
  und wird nicht neu vergeben.
