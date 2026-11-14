---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Beinahe-Treffer, Tippfehler-Toleranz]
status: abgenommen
prio: P3
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-421
grund: ""
ersetzt_durch: []
wartet_auf: ""
nachgeschaut: 2026-11-14
---

# B-422 · Prüfauftrag: soll ein Tippfehler („Beinahe-Treffer") verziehen werden – und wie?

Abgetrennt von [B-421](B-421-antwort-toleranz-artikel.md), das nur den Artikel verzeiht. Offen bleibt die
zweite Fairness-Frage: „Strase" statt „Straße", „Geschaft" statt „Geschäft". Duolingo wertet einen Buchstaben
Abstand als richtig mit Hinweis („Achte auf die Akzente"). **Produktfrage:** ein Beinahe-Treffer ist bei Vokabeln für
ein Kind in Klasse 5 Fairness, bei Rechtschreibübungen aber genau das, was geprüft werden soll. Empfehlung des PM
(2026-11-08): Levenshtein-Abstand 1 ab fünf Zeichen Lösungslänge, **nur im Üben** als „fast richtig" gewertet (voller
Treffer, aber Reveal mit Markierung), **im Test** weiterhin exakt – und als Opt-out je Übung wie `articleRequired`. Zu
klären war außerdem, ob Akzente/Umlaute (é/e, ä/ae) eine eigene, immer erlaubte Normalisierung bekommen.

## User Story

Als **Sohn** möchte ich beim Üben erkennen, ob ich ein Wort nicht wusste oder nur falsch geschrieben habe, **damit** ein
Tippfehler sich nicht anfühlt wie „keine Ahnung“ und ich weiß, woran ich arbeiten muss. Als **Vater** möchte ich, dass
das Üben auf denselben strengen Test vorbereitet, den mein Sohn bestehen muss, **damit** der Kasten nur steigt, wenn das
Wort wirklich sitzt.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Branch `auto/backlog-lauf-2026-11-13`, Stand `b0653edb`):

- **Wertung ist exakt und für Üben und Test dieselbe:** `backend/Pugling.Api/Services/Shared/AnswerGrader.cs:17-18`
  (`Matches(given, item)`, „so the rule cannot differ between practice and test“) und `:26-33` (Vergleich nach
  `StageMechanics.Normalize`, Artikel-Toleranz B-421). Der frühere Verweis `:14-15` zeigt heute auf `:17-33`.
- **Erkennen ist gebaut (B-524, Teil C):** `AnswerGrader.IsMisspelled` (`AnswerGrader.cs:47-61`, Abstand mit
  Vertauschungen ≤ 1 bis fünf Buchstaben, ≤ 2 darüber; keine Nachbarlösung gleich nah). Nur Anzeige: genutzt im
  Testergebnis (`backend/Pugling.Contracts/Student/TestDtos.cs:74-79`, Feld `Misspelled`) und in der Vorschau des
  Vaters (`backend/Pugling.Api/Services/Creator/ExercisePreviewService.cs:89`). Frontend: `frontend/src/sohn/SohnTest.tsx:340`
  (✏️), `frontend/src/vater/ExercisePreviewModal.tsx:318-326`, Text `test.misspelled` (`frontend/src/i18n/de.ts:382`).
- **Im Üben fehlt es:** `PositionPracticeController.Review` wertet über `grader.Matches`
  (`backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:382-384`) und antwortet mit `ReviewOutcome`
  (`:513`; Vertrag `backend/Pugling.Contracts/Student/PracticeDtos.cs:102-103`) – ohne Kennzeichen. Das Blatt
  (`frontend/src/sohn/Lektion.tsx:206-232`) zeigt bei falsch nur „Lösung: …“.
- **Frühere Entscheidung des Nutzers (B-524, Entscheidung 2, Grill-Runde 2026-11-13):** „Die Wertung bleibt binär,
  Schreibfehler werden nur gesondert ausgewiesen … wertet falsch geschrieben als falsch“. Die User Story von B-524 will,
  dass „seine eigentliche Schwäche, die Rechtschreibung, sichtbar wird und nicht untergeht“; der Ist-Stand dort nennt die
  exakte Prüfung „das gewünschte Strenge-Niveau“.
- Seit B-476 (Leiter) verlangt der Test, was das Üben zuletzt verlangte: ein Wort wird ab Tag 7 auch im Üben getippt
  (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs`, `CardStage`/`LadderDays`).

## Die echte Lücke

Nicht das Verzeihen fehlt, sondern das **Benennen im Üben**: das Kind sieht beim Üben nicht, dass es „fast richtig“ war.
Das Erkennen gibt es seit B-524, es kommt nur im Test und in der Vorschau an.

## Offene Punkte

1. ~~Wird ein Beinahe-Treffer im Üben als Treffer gewertet (PM-Empfehlung) oder nur gekennzeichnet?~~ → Entscheidung 1.
2. ~~Welches Abstandsmaß?~~ → Entscheidung 2.
3. ~~Opt-out je Übung wie `articleRequired`?~~ → Entscheidung 3.
4. ~~Eigene, immer erlaubte Normalisierung für Akzente/Umlaute (é/e, ä/ae)?~~ → Entscheidung 4.
5. ~~Test?~~ → Entscheidung 5.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Kein Verzeihen im Üben – nur das Kennzeichen.** Ein Beinahe-Treffer bleibt **falsch** (Kasten, Punkte, Combo wie
   bisher); das Blatt zeigt aber „Falsch geschrieben“ mit der Lösung statt des schlichten „Lösung: …“. **Abweichend von
   der PM-Empfehlung dieser Story**, weil sie mit einer späteren ausdrücklichen Entscheidung des Nutzers kollidiert:
   B-524/2 wertet „falsch geschrieben als falsch“, und dessen User Story will die Rechtschreibung sichtbar halten. Ein
   Treffer im Üben ließe genau Friedrichs Schwäche den Kasten hochsteigen – und ihn dann am strengen Test scheitern.
   `AnswerGrader` hält die Regel für Üben und Test bewusst gleich (`AnswerGrader.cs:15`). Die Fairness kommt aus dem
   Benennen: der Sohn sieht, dass er das Wort wusste. *Kosten:* ein additives Feld `Misspelled` an `ReviewOutcome`,
   der Aufruf von `IsMisspelled` im `Review`, eine Zeile im Blatt.
2. **Das Maß ist das von B-524** (`AnswerGrader.IsMisspelled`), kein zweites. *Begründung:* eine Regel für Test,
   Vorschau und Üben; die PM-Empfehlung „Levenshtein 1 ab fünf Zeichen“ ist damit überholt. *Kosten:* keine.
3. **Kein Opt-out je Übung.** Ohne Verzeihen gibt es nichts abzuschalten; das Kennzeichen kostet nichts.
   *Kosten:* keine.
4. **Keine eigene Normalisierung für Akzente/Umlaute.** „é“ für „e“ ist im Französischen ein Rechtschreibfehler, den die
   Schule zählt; „ae“ für „ä“ ist auf den Geräten der Söhne (deutsche Tastatur) kein Zwang. Ein Abstand von eins wird
   ohnehin als „falsch geschrieben“ gekennzeichnet. *Kosten:* „Strasse“ für „Straße“ (Abstand 2, sieben Buchstaben)
   wird gekennzeichnet, aber nicht verziehen – gewollt.
5. **Im Test ändert sich nichts** (B-524 hat das Kennzeichen dort schon). *Kosten:* keine.

## Akzeptanzkriterien

1. Üben, getippte Karte „clinic“, Antwort „clinik“: `wasCorrect == false`, `misspelled == true`, Kasten fällt wie bei
   jeder falschen Antwort. *Rot, wenn* `Review` das Feld nicht setzt (Wert `false`) – oder wenn jemand den Beinahe-Treffer
   doch als Treffer wertet (dann `wasCorrect == true`).
2. Antwort „racist“ für „racism“ mit „racist“ als Nachbar-Atom derselben Übung: `misspelled == false`. *Rot, wenn* das
   Üben `IsMisspelled` ohne die Geschwister-Atome aufruft.
3. Mengen-Modus (ungeordnete Liste) und Selbsteinschätzung: nie `misspelled`. *Rot, wenn* die Bedingung `typed &&
   !setMode` fehlt.
4. Frontend: das Blatt einer falschen, als falsch geschrieben gekennzeichneten Antwort zeigt „Falsch geschrieben“ und die
   Lösung; ohne Kennzeichen bleibt es bei „Lösung: …“. *Rot, wenn* das Blatt das Feld ignoriert.

## Schätzung

**Größe: S** – Backend: Feld `Misspelled = false` an `ReviewOutcome` (additiv, Standardwert), ein Aufruf von
`grader.IsMisspelled(dto.GivenAnswer, item, items)` im `Review` nach `:382-384`, OpenAPI/`contract.ts` neu erzeugen;
Frontend: `reviewFeedback` (`frontend/src/sohn/SohnPractice.tsx:39`) reicht das Feld durch, `Rueckmeldung` und
`RueckmeldungsBlatt` (`Lektion.tsx:179-232`) bekommen eine Zeile, Text wiederverwendet (`test.misspelled`).
`wo: beides`, `migration: nein`, `vertragsbruch: nein` (additives Feld mit Standardwert).

**Risiken:**

- `DocsCaptureTests` schreiben `docs/api-examples` neu, wenn `ReviewOutcome` ein Feld mehr trägt – erwartete Änderung,
  mitcommitten.
- `UnmappedMemberHandling.Disallow` betrifft nur Eingaben; ein neues Ausgabefeld bricht keinen Client.
- Der Abstand läuft je getippter Bewertung über alle Atome der Übung (`IsMisspelled` vergleicht mit den Geschwistern) –
  bei Wortlisten mit einigen Hundert Einträgen vernachlässigbar, aber nur bei falscher Antwort aufrufen.

**Angriffsplan:** (1) Integrationstest `backend/Pugling.Api.Tests/UebenFalschGeschriebenTests.cs` (AK 1–3), rot gegen
den Ist-Code; (2) Vertragsfeld, Aufruf im `Review`, Vertrag neu erzeugen; (3) rote Probe je AK; (4) Frontend: Fälle in
`frontend/src/sohn/reviewFeedback.test.ts` und `SohnPractice.test.tsx` (AK 4), dann Blatt; (5) `pugling-reviewer` und
`frontend-reviewer`.

**Testweg:** `backend/Pugling.Api.Tests/UebenFalschGeschriebenTests.cs` (neu, AK 1–3); Vitest `reviewFeedback.test.ts`,
`SohnPractice.test.tsx` (AK 4); E2E `frontend/e2e/lektion.spec.ts` als Regressionszeuge, Rollengang als Sohn mit einem
absichtlichen Tippfehler.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Sohn ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  abgetrennt von B-421 als eigene Wertentscheidung.
- **2026-11-13** — Backlog-Abgleich gegen den Code: [B-524](B-524-vokabeltest-wie-in-der-schule.md) hat das **Erkennen** schon gebaut (`AnswerGrader.IsMisspelled`, `AnswerGrader.cs:47`, Kennzeichen `misspelled` – nur Anzeige, Wertung bleibt binär) und für den Schultest die Strenge ohne Tippfehler-Abstand ausdrücklich entschieden. Offen ist damit nur noch das **Verzeihen beim Üben**; der Verweis `AnswerGrader.cs:14-15` oben zeigt heute auf `:28-32`.
- **2026-11-13** — `ausformuliert`: am Code belegt; das Kennzeichen fehlt im Üben (`ReviewOutcome`), die Wertung ist für
  Üben und Test bewusst dieselbe.
- **2026-11-13** — `gegrillt`: Entscheidungen 1–5; Entscheidung 1 **gegen die PM-Empfehlung** dieser Story, weil sie
  B-524/2 (Nutzer) widerspräche; `art` von `Frage` auf `Wunsch`. Stakeholder-Entscheidung im Auftrag des Nutzers
  (Backlog-Lauf 2026-11-13).
- **2026-11-13** — `geschaetzt`: S, beides, ohne Migration und Vertragsbruch.
- **2026-11-13** — `in-arbeit`: Backend gebaut. `ReviewOutcome` trägt additiv `Misspelled` (Standard `false`), gesetzt in `PositionPracticeController.Review` über `AnswerGrader.IsMisspelled(given, item, items)` nur bei falscher getippter Antwort außerhalb des Mengen-Modus; die Wertung bleibt falsch. `docs/openapi/v1.json` und `docs/api-examples/study-plans.md` neu erzeugt (Feld `misspelled`). Test `backend/Pugling.Api.Tests/UebenFalschGeschriebenTests.cs` (AK 1–3). **Offen:** AK 4 (Frontend: Blatt zeigt „Falsch geschrieben“ + Lösung, Text `test.misspelled`) – `contract.ts` entsteht beim Build aus `v1.json`.
  Rote Probe, gemessen an `cf07416f + Arbeitsbaum geändert`, 2026-11-13 21:38 (`mutant-probe.py`, Grundlinie nach dem Zurückspielen grün):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | B422-AK1a | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:388 | red | red |
  | B422-AK1b | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:383 | red | red |
  | B422-AK2 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:388 | red | red |
  | B422-AK3a | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:388 | red | red |
  | B422-AK3b | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:388 | red | red |

- **2026-11-13** — `in-arbeit`: AK 4 (Frontend) gebaut. `contract.ts` per `npm run gen:contract` neu erzeugt;
  `reviewFeedback` (`frontend/src/sohn/SohnPractice.tsx`) reicht `misspelled` im `wrong`-Fall durch, `Rueckmeldung` trägt
  `falschGeschrieben?`, und `RueckmeldungsBlatt` (`Lektion.tsx`) titelt dann „Falsch geschrieben“ (`test.misspelled`, wie
  in `SohnTest`) statt „Fast.“ – die Lösung bleibt darunter, das Blatt bleibt rot, die Wertung falsch. Die Ansage der
  Live-Region nennt das Etikett mit („Falsch geschrieben. Lösung: …“). Tests: `reviewFeedback.test.ts` (Durchreichen;
  zwei bestehende Fälle um `misspelled: false` ergänzt), `SohnPractice.test.tsx` (Blatt mit und ohne Kennzeichen).
  Rote Probe (`mutant-probe.py`, Build `echo ok`, Test Vitest beider Dateien):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | Blatt ignoriert das Kennzeichen | frontend/src/sohn/Lektion.tsx:230 | red | red |
  | Etikett auch ohne Kennzeichen | frontend/src/sohn/Lektion.tsx:230 | red | red |
  | `reviewFeedback` reicht nicht durch | frontend/src/sohn/SohnPractice.tsx:51 | red | red |
  | Blatt bekommt das Feld nicht | frontend/src/sohn/SohnPractice.tsx:265 | red | red |
  | Ansage ohne Etikett | frontend/src/sohn/SohnPractice.tsx:268 | red | red |

  Gemessen an `cf07416f + Arbeitsbaum geändert`, 2026-11-13 21:44; Grundlinie danach grün. Vitest 937/937, `tsc -b` 0.
  E2E-Zeuge `frontend/e2e/lektion.spec.ts` noch nicht gelaufen.
- **2026-11-13** — **abgenommen** mit Commit `699b02a7` (Sprint 69, Backlog-Lauf 2026-11-13). Verifikation: Backend 1555/1555 grün (dotnet test Pugling.sln -c Release), Vitest 937/937, tsc -b sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: `pugling-reviewer` ohne Befund (Feld additiv, Mengen-Modus und SelbsteinschÃ¤tzung ausgeschlossen); die Anzeige im Blatt ist mit RTL-Tests und Mutanten belegt. Rollengang: Playwright gegen frisch gestartete Server auf Ausweichports (API 5211, Web 5184) nach dem letzten Edit – lektion, erstbegegnung, karten-umdrehen, full-flow, test-durchgefallen-ueben, uebung-abbruch, pfad, bonus, artikel-pflicht: **10 passed**. Vater und Creator sind Regressionszeugen (Suite grün, kein Pfad ihrer Ebene im Diff außer der Vater-Wand von B-433).
