---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Fokus im Abschlusstest nach Aufdecken]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "B-531, Entscheidung 3 und Bau-Befund (Backlog-Lauf 2026-11-13, docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: 2026-11-14
---

# B-541 · Im Abschlusstest fällt der Fokus nach dem Aufdecken auf `body`

[B-531](B-531-fokus-faellt-nach-aufdecken-auf-body.md) hat den Fokus beim Kartenwechsel im Üben
(`SohnPractice.tsx`) repariert und den Test-Bildschirm bewusst ausgenommen (Entscheidung 3). Beim Bauen fiel auf:
`SohnTest.tsx` hat gar keinen Fokus-Effekt, `onReveal` setzt dort nur `revealed` (`SohnTest.tsx:241`). Nach
„Aufdecken“ und nach dem Urteil verschwindet der fokussierte Knopf also sehr wahrscheinlich und der Fokus fällt auf
`body` – wer mit Tastatur oder Screenreader prüft, verliert den Ort. Gleiche Regel wie B-531: jeder Kartenwechsel
durch eine Handlung des Kindes fokussiert das erste Bedienbare der neuen Antwortzone.

**Zweiter Fall derselben Klasse** (frontend-reviewer zu B-531/B-532): Beendet `plan_inactive` die Runde mitten im
Üben, greift `landingRef` nicht (`SohnPractice.tsx:180`, nur bei `focusAfterLoad`), und der Fokus fällt ebenfalls
auf `body`. Die `role="alert"`-Meldung wird angesagt, der Ort aber geht verloren. Der Fall gehört hierher, nicht in
eine eigene Story: dieselbe Regel, derselbe Testweg.

## User Story

Als **Sohn**, der den Abschlusstest an der Tastatur (oder mit Screenreader) schreibt, möchte ich nach „Aufdecken“,
nach jeder Antwort, nach dem letzten Urteil und nach einer Fehlermeldung mit dem Fokus dort stehen, wo ich als
Nächstes handle, **damit** ich die Klausur ohne Maus durchschreiben kann und die Stelle nicht verliere – im Üben
ebenso, wenn der Plan mitten in der Runde endet.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Worktree `backlog-lauf`, Stand nach Sprint 69, `afadf18d`):

- **Kein Fokus-Effekt im Test.** `frontend/src/sohn/SohnTest.tsx` kennt weder `focus()` noch einen Ref auf die
  Antwortzone; der einzige Fokus-Schutz ist, dass `Antwortflaeche` nicht neu eingehängt wird (`Lektion.tsx:120-124`),
  belegt von `SohnTest.test.tsx:93-111` (getipptes Feld bleibt derselbe Knoten).
- **Aufdecken:** `onReveal={() => setRevealed(true)}` (`SohnTest.tsx:241`). `SelfAssessAnswer` rendert vor dem
  Aufdecken nur „Aufdecken“ (`:278-283`), danach „Nicht gewusst“/„Gewusst“ (`:292-295`) – der fokussierte Knopf
  verschwindet.
- **Urteil / Auswahl → nächste Frage:** `answerAndAdvance` (`:142-160`) setzt `item`, `cursor`, `revealed=false`
  (`:153`). Danach steht wieder „Aufdecken“ (neuer Knoten) bzw. neue Optionsknöpfe (`Lektion.tsx`, Schlüssel
  `${i}-${c}`); während der Bewertung sind die Optionen `disabled={gesperrt}` (`action.busy`, `SohnTest.tsx:229`).
- **Letzte Antwort → Ergebnis:** `finish` setzt `result` und `item=null` (`:80-81`); `TestResult` (`:305-390`) hat
  keinen fokussierbaren Landeplatz. Dieselbe Lücke, die B-439 im Üben für das Rundenende geschlossen hat.
- **Antwort scheitert:** `catch` setzt `error` (`:154-157`); die Fehleransicht (`:179-181`) hat weder Ref noch
  `tabIndex` – der Knopf, der die Antwort schickte, ist weg.
- **„Weiter versuchen“ / „Nochmal versuchen“:** `resume` (`:129-139`) und `start` über `onRetry` (`:184`) räumen die
  Ansicht über einen Ladezustand (`:189-190`) – der gedrückte Knopf verschwindet, die neue Frage bekommt keinen Fokus.
- **Erster Aufbau:** `start()` aus dem Mount-Effekt (`:119-126`) – hier soll, wie im Üben (B-443), nichts ungefragt
  fokussiert werden.
- **Üben, `plan_inactive`:** `judge` fängt den Code und setzt `phase="error"` (`SohnPractice.tsx:273-275`). Der
  Effekt fokussiert `landingRef` nur, wenn `prevPhase` `loading` war **und** `focusAfterLoad` gesetzt ist
  (`:181-182`); vom Kartenzustand (`front`/`back`) aus nach `error` gibt es keinen Zweig. Die Fehlerbox trägt
  `ref={landingRef}`, `tabIndex={-1}`, `role="alert"` schon (`:328`). Der Test `SohnPractice.test.tsx:228-245`
  prüft nur den Text, nicht den Fokus.
- **Vorbild** ist der Effekt aus B-531 (`SohnPractice.tsx:166-188`): „erstes Bedienbares der Antwortzone“ per
  `querySelector("input, button:not([disabled])")`, ausgelöst nur durch Handlungen des Kindes, erster Aufbau
  ausgenommen.

## Die echte Lücke

Der Test kennt die Regel aus B-531 gar nicht: **verschwindet das fokussierte Element durch eine Handlung des Kindes,
landet der Fokus am nächsten Ort, an dem es handelt** – erstes Bedienbares der Antwortzone, sonst die Überschrift des
Ergebnisses oder die Fehlermeldung. Sechs Wege im Test und einer im Üben verletzen sie.

## Offene Punkte

1. ~~Wohin nach „Aufdecken“ im Test – „Nicht gewusst“ oder „Gewusst“?~~ → Entscheidung 1.
2. ~~Nur Aufdecken und Urteil, oder auch Ergebnis, Fehler und „Weiter versuchen“?~~ → Entscheidung 2.
3. ~~Wie verhindert der Effekt, dass er gesperrte Optionen vorfindet und den Fokus verliert?~~ → Entscheidung 3.
4. ~~Wohin bei `plan_inactive` im Üben?~~ → Entscheidung 4.
5. ~~Fokus beim ersten Laden des Tests?~~ → Entscheidung 5.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Nach „Aufdecken“ steht der Fokus auf dem ersten freigegebenen Knopf („Nicht gewusst“)** – dieselbe Regel wie
   B-531 Entscheidung 1, keine Sonderregel für den Test. *Begründung:* Enter darf nicht der bequeme Weg zur
   Selbstbelohnung sein; in der Klausur gilt das erst recht. *Kosten:* wer „Gewusst“ meint, braucht einmal Tab.
2. **Die ganze Regel, nicht nur das Aufdecken:** Aufdecken, Antwort → nächste Frage, letzte Antwort → Ergebnis
   (Überschrift `vtitle`, analog B-439), gescheiterte Antwort → Fehlermeldung, „Weiter versuchen“/„Nochmal
   versuchen“ → erste Bedienbare der neuen Frage. *Begründung:* dieselbe Fehlerklasse; eine Teil-Reparatur hinterließe
   vier bekannte Lücken, die der nächste Review wieder findet (so entstand diese Story aus B-531). *Kosten:* Größe S
   statt XS, fünf statt zwei Testfälle mehr.
3. **Ein „Absicht“-Ref wie `focusAfterLoad`, verbraucht erst, wenn ein Ziel gefunden ist.** Jede Handlung des Kindes
   (Aufdecken, Antwort, Weiter versuchen, Nochmal versuchen) setzt ihn; der Effekt hängt an Frage, `revealed`,
   `error`, `result` **und** `action.busy` und landet erst, wenn das Ziel bedienbar ist. *Begründung:* `setItem`
   läuft innerhalb von `action.run`, die Optionen sind dann noch `disabled` – ein Effekt nur auf `item` fände kein
   Ziel und ließe den Fokus auf `body`. *Kosten:* ein Ref und eine Abhängigkeit mehr als im Üben.
4. **`plan_inactive` im Üben landet auf der Fehlermeldung** (`landingRef`), sobald die Phase von einer Karte
   (`front`/`back`/`blatt`) nach `error` wechselt – nicht nur nach dem Laden. *Begründung:* dieselbe Regel; die Box
   ist schon fokussierbar. *Kosten:* ein Zweig im Effekt aus B-531.
5. **Der erste Aufbau des Tests fokussiert nichts** (B-443). Der Mount-`start()` setzt den Absicht-Ref nicht.
   *Kosten:* wer per Tastatur einsteigt, tabbt einmal in die Karte – wie im Üben.

## Akzeptanzkriterien

Alle als Vitest-Fälle; je Fall die Änderung, die ihn rot macht.

1. **Aufdecken:** nach „Aufdecken“ ist `document.activeElement` der Knopf „Nicht gewusst“. *Rot, wenn* `onReveal`
   den Absicht-Ref nicht setzt oder der Effekt nicht an `revealed` hängt.
2. **Urteil → nächste Selbsteinschätzung:** nach „Gewusst“ ist der Fokus auf „Aufdecken“ der nächsten Frage.
   *Rot, wenn* der Effekt nicht an der Frage (`item`/`cursor`) hängt.
3. **Auswahl → nächste Auswahl:** nach dem Antworten steht der Fokus auf der ersten **freigegebenen** Option der
   neuen Frage. *Rot, wenn* der Effekt nicht an `action.busy` hängt oder den Ref verbraucht, obwohl er kein Ziel fand
   (dann sind die Optionen beim Lauf noch gesperrt).
4. **Getippt:** der bestehende Fall `SohnTest.test.tsx:93-111` bleibt grün (derselbe Feldknoten behält den Fokus).
   *Rot, wenn* der Umbau die `Antwortflaeche` je Frage neu einhängt.
5. **Letzte Antwort → Ergebnis:** der Fokus steht auf der Überschrift des Ergebnisses („SIEG!“/„FAST!“).
   *Rot, wenn* das Ergebnis keinen fokussierbaren Landeplatz bekommt.
6. **Antwort scheitert:** der Fokus steht auf der Fehlermeldung. *Rot, wenn* die Fehlerbox ohne Ref/`tabIndex={-1}`
   bleibt.
7. **„Weiter versuchen“:** nach dem Fortsetzen steht der Fokus im ersten Bedienbaren der geholten Frage.
   *Rot, wenn* `resume` den Absicht-Ref nicht setzt.
8. **Erster Aufbau:** nach dem Laden der ersten Frage liegt der Fokus nicht in der Karte. *Rot, wenn* der Mount-`start`
   den Ref setzt.
9. **Üben, `plan_inactive`:** nach dem Urteil, das `plan_inactive` beantwortet, ist der Fokus auf der Fehlermeldung.
   *Rot, wenn* der `landingRef`-Zweig weiter nur nach dem Laden greift.

## Schätzung

**Größe: S** – ein Fokus-Effekt mit Absicht-Ref in `SohnTest.tsx` (Refs auf Antwortzone, Fehlerbox, Ergebnis-
Überschrift; `TestResult` bekommt dafür einen Ref oder einen `autoFocus`-fähigen Landeplatz), ein Zweig im Effekt von
`SohnPractice.tsx`, acht neue Fälle in `SohnTest.test.tsx`/`TestResult.test.tsx` und die Erweiterung eines Falls in
`SohnPractice.test.tsx`. Anker: zwischen B-531 (XS, ein Effekt) und B-01. `wo: frontend`, `migration: nein`,
`vertragsbruch: nein` (keine API berührt).

**Risiken:**

- **Gesperrte Ziele** (Entscheidung 3): das ist die wahrscheinlichste Falle; AK 3 hält sie.
- **`TestResult` ist eine exportierte Props-Komponente** (`TestResult.test.tsx`): ein Fokus beim Einhängen dort
  fokussierte auch ein Ergebnis, das ohne Handlung erscheint (`start` → `first.done`). Darum fokussiert `SohnTest`
  den Landeplatz über den Absicht-Ref, `TestResult` reicht nur den Ref durch.
- **Live-Region:** `TaskAnnouncer` sagt die neue Aufgabe an (`SohnTest.tsx:168-171`); der B-286-Knoten bleibt
  unverändert. Ob der Fokuswechsel die Ansage übertönt, beim Bau mit dem Screenreader-Verhalten aus B-420 abgleichen
  (dort steht der Fokus während der Ansage auf „Weiter“).
- **Handy:** Fokus ins Feld kann die Tastatur öffnen – das tut der getippte Weg heute schon (derselbe Knoten).

**Angriffsplan** (kein Backend-Anteil, die API bleibt unberührt):

1. Fälle zu AK 1–3 und 5–9 schreiben und gegen den Ist-Code rot sehen (AK 8 ist dort grün und als Absicherung des
   neuen Zweigs beschriftet, wie AK 4 in B-531).
2. `SohnTest.tsx`: Absicht-Ref, Refs, Effekt; Fehlerbox mit `ref`/`tabIndex={-1}`; Ref durch `TestResult`.
3. `SohnPractice.tsx`: Zweig Karte → `error` auf `landingRef`.
4. Grün; rote Probe je Fall an der benannten Stelle (`mutant-probe.py`, Vitest der Datei).
5. E2E als Rollengang-Zeugen.

**Testweg:** Vitest `frontend/src/sohn/SohnTest.test.tsx` (AK 1–8, AK 5 ggf. zusätzlich `TestResult.test.tsx` für
das Durchreichen des Refs), `frontend/src/sohn/SohnPractice.test.tsx` (AK 9, Erweiterung des Falls „beendet die
Runde mit dem Satz der Arcade“). E2E-Zeugen des Sohns: `frontend/e2e/full-flow.spec.ts`,
`frontend/e2e/test-durchgefallen-ueben.spec.ts`, `frontend/e2e/karten-umdrehen.spec.ts`.

## Verlauf

- **2026-11-13** — angelegt im Backlog-Lauf 2026-11-13 aus dem Bau von B-531.
- **2026-11-13** — um den `plan_inactive`-Fall aus dem Review von B-531/B-532 erweitert.
- **2026-11-13** — `ausformuliert`: am Code belegt (Stand nach Sprint 69); außer Aufdecken und Urteil verlieren auch
  Ergebnis, gescheiterte Antwort und „Weiter/Nochmal versuchen“ den Fokus; die Optionen sind beim Setzen der neuen
  Frage noch gesperrt.
- **2026-11-13** — `gegrillt`: Entscheidungen 1–5; Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf
  2026-11-13), Regel aus B-531 übernommen.
- **2026-11-13** — `geschaetzt`: S, frontend, ohne Migration und Vertragsbruch.
- **2026-11-13** — `in-arbeit` (Sprint 71, Frontend): `SohnTest.tsx` bekommt den Absicht-Ref `focusIntent`
  (gesetzt von Aufdecken, `answerAndAdvance`, `resume`, „Nochmal versuchen“ – nicht vom Mount-`start`), Refs auf
  Antwortzone, Fehlerbox (`tabIndex={-1}`) und Ergebnis-Überschrift (`TestResult` reicht `titleRef` nur durch,
  `vtitle` trägt `tabIndex={-1}`); der Effekt hängt an Frage, `revealed`, `error`, `result` und `action.busy` und
  verbraucht die Absicht erst bei gefundenem Ziel. `SohnPractice.tsx`: Zweig Karte → `error` auf `landingRef`.
  Tests: acht neue Fälle in `SohnTest.test.tsx` (AK 1–3, 5–8 inkl. „Nochmal versuchen“), AK 4 ist der bestehende Fall,
  AK 9 erweitert „beendet die Runde mit dem Satz der Arcade“ in `SohnPractice.test.tsx`. Befund zu AK 2: die
  Abhängigkeit an `item`/`cursor` allein trägt nichts – mit der Frage wechseln immer auch `revealed` bzw.
  `action.busy`, der Effekt liefe also auch ohne sie. Gemessen wurde AK 2 darum an der Stelle, die ihn wirklich trägt:
  `answerAndAdvance` setzt die Absicht. Rote Probe (`mutant-probe.py`, Vitest der Datei):

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | AK1 | frontend/src/sohn/SohnTest.tsx:266 | red | red | Aufdecken setzt keine Absicht |
  | AK1b | frontend/src/sohn/SohnTest.tsx:94 | red | red | Effekt ohne `revealed` |
  | AK2 | frontend/src/sohn/SohnTest.tsx:167 | red | red | `answerAndAdvance` ohne Absicht |
  | AK3 | frontend/src/sohn/SohnTest.tsx:91 | red | red | Absicht verbraucht ohne Ziel |
  | AK4 | frontend/src/sohn/SohnTest.tsx:253 | red | red | `Antwortflaeche` mit `key={cursor}` |
  | AK5 | frontend/src/sohn/SohnTest.tsx:88 | red | red | Ergebnis ohne Landeplatz |
  | AK6 | frontend/src/sohn/SohnTest.tsx:203 | red | red | Fehlerbox ohne Ref/`tabIndex` |
  | AK7 | frontend/src/sohn/SohnTest.tsx:152 | red | red | `resume` ohne Absicht |
  | AK7b | frontend/src/sohn/SohnTest.tsx:209 | red | red | „Nochmal versuchen“ ohne Absicht |
  | AK8 | frontend/src/sohn/SohnTest.tsx:112 | red | red | Mount-`start` setzt die Absicht |
  | AK9 | frontend/src/sohn/SohnPractice.tsx:185 | red | red | `landingRef` nur nach dem Laden |

  Gemessen an `afadf18d + Arbeitsbaum geändert`, 2026-11-13 22:06; Grundlinie nach dem Zurückspielen grün (11/11).
  E2E-Zeugen für den Rollengang: `full-flow.spec.ts`, `test-durchgefallen-ueben.spec.ts`, `karten-umdrehen.spec.ts`.
- **2026-11-13** — Review-Nachtrag (`frontend-reviewer`, ohne Blocker): die Fehlerbox im Test trägt jetzt
  `role="alert"` wie im Üben (Fall „sagt die Fehlermeldung als Alarm an"); AK 8 bekommt den zweiten Weg „Mount trifft
  einen fertigen Versuch" (Ergebnis ohne Handlung, Überschrift nicht fokussiert), damit der `titleRef`-Pfad gedeckt ist.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | B541-alert | frontend/src/sohn/SohnTest.tsx:203 | red | red | Fehlerbox ohne `role="alert"` |
  | B541-AK8b | frontend/src/sohn/SohnTest.tsx:86 | red | red | Ergebnis ohne Absicht fokussiert |

  Gemessen an `afadf18d + Arbeitsbaum geändert`, 2026-11-13 22:27; Grundlinie danach grün.
- **2026-11-13** — **abgenommen** mit Commit `45365c66` (Sprint 71, Backlog-Lauf 2026-11-13). Verifikation: Backend 1577/1577 grün (dotnet test Pugling.sln -c Release), Vitest 960/960, tsc -b sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: `frontend-reviewer` ohne Blocker; drei kleine Punkte (fehlendes `role="alert"` an der Fehlerbox, fehlender Fall „Aufbau trifft fertigen Versuch“) im selben Sprint behoben. Rollengang: Playwright gegen frisch gestartete Server auf Ausweichports (API 5214, Web 5187) nach dem letzten Edit – bonus, bonus-deckel, full-flow, test-durchgefallen-ueben, karten-umdrehen, assistent, assistent-bewertung, lektion, erstbegegnung: **13 passed**. Den vollen Deckel in der Oberfläche fährt keine Spec; er ist über Vitest (SohnBonus.test.tsx) und Integrationstests belegt – ein Handblick am Gerät (30/30 Münzen, Einmal-Übung zeigt den Satz statt „Spielen“) bleibt für den Menschen.
