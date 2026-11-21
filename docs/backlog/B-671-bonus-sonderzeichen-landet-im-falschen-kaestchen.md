---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Bonusrunde Sonderzeichen falsches Kästchen]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Nachtlauf 2026-11-18, Nachschau von B-661 (96a3491d, frontend-reviewer) – docs/pm-sitzung-2026-11-18.md, Runde 5"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-661]
nachgeschaut: "2026-11-21"
---

# B-671 · In der Bonusrunde schreibt die Sonderzeichen-Leiste ins Kästchen der vorigen Karte

Aus der Nachschau von B-661, am Code gelesen, nicht im Browser nachgestellt: zwei Kästchen-Karten nacheinander, auf der
ersten Kästchen 5 gefüllt und abgegeben ⇒ das erste „é" der nächsten Karte landet in Kästchen 5 statt 1.

## User Story

Als Schüler möchte ich in der Bonusrunde nach dem Weiter in der ersten Lücke der neuen Karte schreiben – mit der
Tastatur wie mit der Sonderzeichen-Leiste –, damit ein Akzent nicht in einem Kästchen der vorigen Karte landet.

## Ist-Stand am Code

1. **Gemerktes Kästchen:** `LetterBoxes` hält `zuletzt` (`frontend/src/components/LetterBoxes.tsx:35`), setzt es bei
   jedem Fokus (`:116`) und nutzt es für ein Leisten-Zeichen, solange es kleiner als die Länge und nicht fest ist
   (`:83-84`); sonst das erste leere. Zurückgesetzt wird es nirgends.
2. **Die Kästchen überleben den Kartenwechsel:** `Antwortflaeche` hängt sie bewusst nicht neu ein, ein `key` von
   außen risse den Fokus weg (`frontend/src/sohn/Lektion.tsx:144-146`); die Kästchen tragen Index-Schlüssel
   (`LetterBoxes.tsx:102`).
3. **Üben und Test fokussieren bei jedem Kartenwechsel das erste Bedienbare der Antwortzone** – damit auch Kästchen 1
   und `zuletzt = 0` (`frontend/src/sohn/SohnPractice.tsx:196-203`, B-531; `frontend/src/sohn/SohnTest.tsx:89-102`,
   B-541).
4. **Die Bonusrunde tut das nicht:** `SohnBonus.tsx` ruft `.focus()` nur für die Fehlermeldung auf (`:181`); der
   Kartenwechsel (`answer`, `:222-229`) leert den Wert und rückt den Zeiger vor, mehr nicht. Die Fläche bekommt
   dieselbe Karten-Id wie im Test (`kartenId={card.itemIndex}`, `:319`).
5. **Test:** der vorhandene Fall deckt nur den Wechsel auf eine *kürzere* Karte (`LetterBoxes.test.tsx:105-112`). Ein
   Fall mit gleich langer Folgekarte: nicht gefunden.

## Die echte Lücke

Schmaler und breiter als die Notiz: nicht das gemerkte Kästchen ist die Ursache, sondern dass die Bonusrunde die
Fokusregel von Üben und Test nicht kennt. Wird die Karte per Enter abgegeben, **bleibt der Fokus im Kästchen 5 der
alten Karte stehen** – die Kästchen werden nicht ausgetauscht –, und dann landet nicht nur das Leisten-Zeichen dort,
sondern auch jede getippte Taste (`setChar(i)` über `onChange`, `LetterBoxes.tsx:60-70`). Wird per „Weiter"-Knopf
abgegeben, steht der Fokus auf dem Knopf und nur das gemerkte Kästchen irrt. Beides am Code gelesen; im Browser nicht
nachgestellt.

## Offene Punkte

1. ~~**Wo wird repariert?**~~ Entschieden: Entscheidung 1 (anders als der Vorschlag der Ausformulierung).
2. ~~**Wann setzt `LetterBoxes` zurück?**~~ Gegenstandslos durch Entscheidung 1: `LetterBoxes` bleibt unverändert.
3. ~~**Bonus-Runde: Fokus nach Laden?**~~ Entschieden: Entscheidung 2.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Repariert wird nur in `SohnBonusRun`, mit der Fokusregel von Üben und Test; `LetterBoxes` bleibt unangetastet.**
   `answer()` (`SohnBonus.tsx:222-229`) setzt eine Absicht (`useRef`), ein Effekt auf `cursor` fokussiert danach das
   erste Bedienbare der Antwortzone (`answerRef.current?.querySelector("input, button:not([disabled])")`, die
   Antwortzone `div.lektion-antwort` bekommt den Ref) – Muster `SohnTest.tsx:89-102` und `SohnPractice.tsx:196-203`.
   Begründung am Code: der Fokus auf Kästchen 1 ruft `onFocus` (`LetterBoxes.tsx:116`) und setzt `zuletzt` auf 0 –
   damit heilt **ein** Eingriff beide Wege, den Enter-Weg (Fokus bliebe sonst in Kästchen 5, jede Taste dorthin) und
   den Knopf-Weg (nur das gemerkte Kästchen irrte). Ein stehengebliebenes `zuletzt` überlebt ohnehin nur zwischen
   zwei Kästchen-Karten, weil die Kästchen sonst neu eingehängt werden (`Lektion.tsx:144-146`, B-526); jeder Wechsel
   auf eine solche Karte landet jetzt in Kästchen 1. Verworfen: (a) `zuletzt` beim Verlassen der Gruppe
   zurücksetzen und (c) beides – die Ausformulierung empfahl (c), weil „(b) allein den Knopf-Weg offen lasse, wenn der
   Fokus nicht an die Kästchen geht"; ein solcher Weg ist am Code nicht zu finden (im Bonus gibt es keine Sperre
   zwischen den Karten, `action.busy` gilt nur für die Abgabe am Ende). (a) kostet dagegen sicher etwas: eine geteilte
   Komponente, die Üben und Test auch tragen, bekäme eine `onBlur`-Logik, deren Verhalten am Handy (Tastatur klappt
   zu, Ziel unklar) kein jsdom-Test sieht (`LetterBoxes.test.tsx:118-123` prüft nur die Ereignisse). Kosten der
   Entscheidung: ein künftiger Bildschirm mit Kästchen und ohne Fokuslandung kann `zuletzt` wieder falsch stehen
   lassen – die Ursache wäre dann dieselbe, die Reparatur dieselbe. Ausgeschlossen wird der Fall der User Story: der
   Akzent aus der Leiste oder getippt landet nach dem Weiter in der ersten Lücke der neuen Karte, auf beiden Wegen.
2. **Der Fokus wird nur nach einer Handlung des Kindes gesetzt, nicht beim Laden der Runde.** Begründung: B-443,
   Üben setzt ihn bewusst nicht beim ersten Aufbau; am Handy klappte sonst die Tastatur ungefragt auf. Verworfen:
   Fokus auch auf Karte 1. Kosten: auf der ersten Karte einer Runde braucht es einen Tipp ins Feld, bevor getippt
   wird; dort ist `zuletzt` `null`, das Leisten-Zeichen geht ins erste leere Kästchen – richtig.
3. **Der Test sitzt in Vitest (`SohnBonus.test.tsx`), nicht im E2E.** Die Ausformulierung nannte das E2E, weil
   die „Fläche mit Server" laufe; die Bonus-Tests rendern die Runde aber mit gemockter API
   (`SohnBonus.test.tsx:130`, `startExtraRun.mockResolvedValue(…)`) und können `document.activeElement` lesen.
   Verworfen: ein neuer E2E in `e2e/bonus.spec.ts` – er wäre langsamer und sähe gegenüber Vitest nichts Zusätzliches
   (ob am echten Handy die Tastatur offen bleibt, sieht keines von beiden). Kosten: jsdom-Fokus ist nicht der
   Browser-Fokus; die Rollenprobe an der laufenden App bleibt nötig.
4. **Karten mit Auswahl oder Karteikarte bekommen denselben Fokus wie in Üben.** Das erste Bedienbare ist dort die
   erste Option bzw. „Aufdecken" – gleiches Verhalten wie `SohnPractice.tsx:201`. Verworfen: den Fokus auf
   Kästchen-Karten beschränken (zwei Regeln statt einer, und Tastaturkinder profitieren überall). Kosten: ein
   Tastaturkind, das auf einer Auswahlkarte Enter drückt, trifft die erste Option; wie im Üben.

## Akzeptanzkriterien

Final; je Kriterium die Änderung, die es rot macht. Alle in `SohnBonus.test.tsx`, Fixture: zwei gleich lange
Kästchen-Karten (`answerLength: 6`, `keyboardLang: "fr"`).

1. Enter-Weg: Kästchen 5 der ersten Karte fokussiert, ein Zeichen getippt, Enter. Danach ist das fokussierte Element
   „Buchstabe 1 von 6" der zweiten Karte. Rot macht es: der Effekt in `SohnBonusRun` fehlt (heute so, der Fokus bleibt
   im Kästchen 5) oder die Absicht wird nicht in `answer()` gesetzt.
2. Leisten-Zeichen nach dem Kartenwechsel: nach AK 1 „î einsetzen" gedrückt – das Zeichen steht in Kästchen 1, nicht
   in 5. Rot macht es: derselbe fehlende Effekt (`zuletzt` bliebe 4); ein Fokus, der `onFocus` nicht auslöst (zum
   Beispiel das Setzen des Werts ohne `.focus()`), lässt dieses Kriterium fallen, AK 1 nicht.
3. Knopf-Weg: Kästchen 5 gefüllt, Abgabe per „Weiter"-Knopf. Der Fokus steht danach in Kästchen 1 der nächsten Karte.
   Rot macht es: die Absicht hängt am Enter-Handler der Kästchen statt in `answer()`.
4. Festschreibung (besteht im Ausgangszustand): nach dem Laden der Runde steht der Fokus nicht in den Kästchen. Rot
   macht es: ein Fokus beim ersten Aufbau (Entscheidung 2).
5. Festschreibung: `LetterBoxes.test.tsx` und die Fokus-Fälle von Üben und Test bleiben unverändert grün; die Datei
   `LetterBoxes.tsx` wird nicht angefasst. Rot macht es: eine `onBlur`-Logik in `LetterBoxes`, die den Fall
   „Kästchen 3 antippen, dann Zeichen wählen" (`LetterBoxes.test.tsx:95-101`) bricht.

## Schätzung

*Geschätzt im Vorbereitungslauf 2026-11-19 (Sonnet), am Code nachgesehen.*

**Größe: S** — wie `childId` aus dem Test-Pfad ziehen (B-01): ein Eingriff an einer Stelle (`SohnBonusRun`, ein Ref und
ein Effekt nach dem Muster von `SohnTest.tsx:89-102`), dazu fünf Vitest-Fälle.

- **`wo: frontend`** — die Ursache liegt allein in `SohnBonus.tsx`; das Backend schickt die Karten richtig.
- **`migration: nein`** — kein Schema.
- **`vertragsbruch: nein`** — `Pugling.Contracts` unberührt; `LetterBoxes.tsx` bleibt unangetastet (Entscheidung 1).

### Risiken

1. **Absicht nach der letzten Karte:** nach der letzten Antwort ist `card` `null` (`SohnBonus.tsx`, `const card = … cursor <
   run.cards.length`), die Antwortzone steht nicht mehr da. Findet der Effekt kein Ziel und lässt die Absicht stehen
   (wie `SohnTest.tsx:96-98`, wo `return` vor dem Zurücksetzen liegt), feuert sie später am Ergebnisbild. Die Absicht
   gehört in dem Fall verbraucht; ein Fall in `SohnBonus.test.tsx` sichert es.
2. **jsdom-Fokus ≠ Browser-Fokus:** `fireEvent.focus` setzt `document.activeElement` nicht. Die Fälle müssen
   `element.focus()` (in `act`) nutzen, sonst ist AK 1 auf `document.activeElement` blind und AK 2 liest ein
   unveränderliches `zuletzt`. Das ist die Stelle, an der eine leere Zusicherung entstehen kann (CLAUDE.md, „Zusicherung,
   die den Ausgangszustand prüft"): je Fall vor dem Bau den roten Lauf sehen.
3. **Die Leiste braucht einen Fall, der `onFocus` auslöst:** AK 2 fällt nur, wenn der Fokus über `.focus()` kommt
   (Entscheidung 1); das Setzen des Werts allein lässt `zuletzt` bei 4.
4. **Fremde Arbeit im Baum:** `SohnBonus.tsx` und `SohnBonus.test.tsx` sind sauber, aber `sohn/Lektion.tsx` (dort
   `Antwortflaeche`, auf der die Reparatur aufsetzt) trägt ungecommittete LearningDad-Änderungen (6 Zeilen). Vor dem
   Bau gegen den Stand von `HEAD` oder den dann gemergten Stand lesen; Zeilennummern in `sohn/*` können driften.
5. **E2E sieht den Mobilfall nicht:** ob am Handy die Tastatur offen bleibt, entscheidet kein Test (Entscheidung 3).

### Angriffsplan

1. Rote Probe zuerst: den Vitest-Fall mit zwei gleich langen Kästchen-Karten (`answerLength: 6`, `keyboardLang: "fr"`)
   schreiben und im Ausgangszustand rot sehen (AK 1–3).
2. `SohnBonusRun`: Absicht (`useRef`) in `answer()` setzen, `answerRef` auf `div.lektion-antwort`, Effekt auf `cursor`
   fokussiert das erste Bedienbare und verbraucht die Absicht auch ohne Ziel.
3. Festschreibungen AK 4 und 5 laufen lassen; `dotnet`-Teile entfallen (kein Backend).

### Testweg

- **AK 1–4:** `frontend/src/sohn/SohnBonus.test.tsx` (mockt `startExtraRun`, liest `document.activeElement`); neuer
  `describe` für den Kartenwechsel. AK 1–3 sind **Regressionstests**, vorher rot: heute bleibt der Fokus in Kästchen 5
  bzw. auf dem Knopf, und `zuletzt` steht auf 4. Rot macht sie: der Effekt oder die Absicht in `answer()` fehlt. AK 4
  besteht im Ausgangszustand (Festschreibung).
- **AK 5:** `frontend/src/components/LetterBoxes.test.tsx` und die Fokusfälle von `SohnPractice` und `SohnTest` bleiben
  unverändert grün; `git diff` zeigt `LetterBoxes.tsx` unberührt.
- Vor der Abnahme: `frontend-reviewer`; Rollengang als Sohn in einer Bonusrunde über eine Kästchen-Liste mit Akzent
  (Handy-Tastatur bleibt hier ungeprüft, siehe Risiko 5).

## Verlauf

- **2026-11-18** — angelegt aus der Nachschau von B-661 (`frontend-reviewer`), `entgangen_bei: [B-661]`.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): am Code gelesen (nicht im Browser nachgestellt); die Ursache
  liegt in der Bonusrunde (kein Fokus beim Kartenwechsel), und der Enter-Weg betrifft auch normale Tasten.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): vier Entscheidungen; Reparatur nur in `SohnBonusRun` (Fokusregel von Üben/Test), `LetterBoxes` bleibt; weicht von der Empfehlung (c) der Ausformulierung ab, weil Kästchen-1-Fokus `zuletzt` über `onFocus` ohnehin setzt; Test in Vitest statt E2E.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): S, frontend, keine Migration, kein Vertragsbruch; Risiken: Absicht nach der letzten Karte, jsdom-Fokus, `Lektion.tsx` mit fremder Arbeit.
- **2026-11-21** — Nachtlauf, Sprint 99: `in-arbeit` und gebaut. Die stellvertretenden Entscheidungen 1–4 trägt der Lauf
  als Defekt (Freigabe 1). `SohnBonusRun` hat eine Fokus-Absicht in `answer()` und einen Effekt auf `cursor`, der das
  erste Bedienbare der Antwortzone fokussiert; `LetterBoxes.tsx` bleibt unberührt (AK 5).
  - **Tests** in `SohnBonus.test.tsx`, Block „Fokus nach dem Kartenwechsel (B-671)“:
    - AK 1+2 → „Enter-Weg …“;
    - AK 3 → „Knopf-Weg …“;
    - AK 4 → „… beim Laden …“ (Festschreibung) und „… beim Fortsetzen …“.
  - **Rote Probe** gegen den Ausgangsstand von `SohnBonus.tsx`: erwartet 2 failed, gemessen 2 failed (Enter-Weg,
    Knopf-Weg). Mit Fix 17/17.
  - **Review `frontend-reviewer`:**
    - 🟡 Die Absicht wurde nach der letzten Karte nicht verbraucht (Risiko 1 der Schätzung) → jetzt vor dem Fokus
      verbraucht (gezählt, Sprint 99: 2). Ohne eigenen roten Test: Kein heutiger Weg benutzt die Instanz nach der letzten
      Karte weiter.
    - 🟡 Der Lade-Fall trug fast nichts → Fortsetzungsfall ergänzt (gezählt, Sprint 99: 3). Mutante „Wächter
      `if (!focusIntent.current)` gestrichen“: erwartet 1 failed, gemessen 1 failed („… beim Fortsetzen …“).
    - 🟢 Kein Fokus nach „Aufdecken“ und auf dem Ergebnisblatt (schon vorher so) → B-778 (ohne Zählung).
  - AK 2 hat keine eigene Mutante: Ein Fokus ohne `onFocus` lässt sich in happy-dom nicht herstellen, der Fall hängt an
    derselben Ursache wie AK 1.
  - Vitest 1301/1301.
- **2026-11-21** — `abgenommen` mit `1793475e`. Rollengang: Vitest mit gemockter API (Entscheidung 3) und volle E2E
  **179/179** als Regressionszeuge (`bonus*.spec.ts` grün). **Geliefert, Geräte-Prüfung offen:** Ob am echten Handy die
  Tastatur nach dem Weiter offen bleibt und das „î“ aus der Leiste in der neuen Karte landet, sieht kein jsdom-Test. Einmal
  in einer französischen Bonusrunde am Handy durchtippen.
- **2026-11-21** — nachgeschaut (Nachtlauf, Retro Sprint 100), Diff `1793475e` in `SohnBonus.tsx:222-240` gelesen:
  - Die Absicht setzt nur `answer()`.
  - Der Effekt verbraucht sie auch ohne Ziel, also nach der letzten Karte, so dass kein späterer Cursor-Wechsel beim
    Laden fokussiert.
  - Gesucht wird `input, button:not([disabled])` nur im `answerRef`.
  Kein Befund. Der Fokus nach „Aufdecken“ liegt schon als B-778 vor.
