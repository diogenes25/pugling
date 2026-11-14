---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student]
aliases: [Bonus-Runde ohne Item-Schnappschuss]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: beides
migration: ja
vertragsbruch: nein
quelle: "B-543, Entscheidung 6 (zurückgestellt), Bau im Backlog-Lauf 2026-11-13 (docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-533]
wartet_auf: ""
nachgeschaut: 2026-11-14
---

# B-545 · Eine Bonus-Runde wird gegen die aktuellen Items gewertet, nicht gegen die beim Start

`ExtraTrainingService.BuildRun` und `preview.CheckAsync` lesen die Items der Übung erst bei der Abgabe;
`ExtraTrainingRun.ItemCount` wird geschrieben, aber nirgends gelesen. Ändert der Autor die Übung zwischen Start und
Abgabe, zählen neue oder verschobene Items als falsch, und eine fortgesetzte Runde zeigt andere Karten.

Seit [B-543](B-543-bonus-deckel-verbrennt-einmal-uebung.md) kann eine Einmal-Runde über Nacht offen bleiben (die
Abgabe wartet bei vollem Deckel bis morgen) – das Fenster für diesen Fall ist dadurch länger geworden. Eine echte
Lösung braucht einen Item-Schnappschuss an der Runde (Migration, nur angehängt); ein Vergleich über `ItemCount`
erkennt eine Änderung bei gleicher Anzahl nicht.

## User Story

Als *Sohn* möchte ich, dass meine Bonus-Runde gegen genau die Karten gewertet wird, die ich gesehen habe, damit ich
keine Münzen verliere, weil jemand die Übung währenddessen geändert hat. Als *Vater/Creator* möchte ich eine Übung
jederzeit korrigieren können, ohne dass eine laufende Runde gegen einen veralteten Lösungsschlüssel zahlt.

## Ist-Stand am Code

1. **Die Runde merkt sich keine Inhalte.** `ExtraTrainingService.StartAsync` legt die Runde mit `Form`, `Stage` und
   `ItemCount` an (`ExtraTrainingService.cs:88-99`); `ItemCount` liest niemand (`GamificationEntities.cs:136`).
2. **Fortsetzen baut die Karten neu.** Eine offene Runde wird über `entry.Row.OpenRunId` wieder aufgenommen
   (`ExtraTrainingService.cs:84-86`) und mit den **aktuellen** Items gebaut (`:116`, `BuildRun` `:321-340`), Karten
   über `item.Index` adressiert.
3. **Die Abgabe wertet gegen die aktuellen Items.** `SubmitAsync` ruft `preview.CheckAsync(run.Exercise, …)`
   (`:151-152`), das die Items neu lädt (`ExercisePreviewService.cs:56`) und je `ItemIndex` vergleicht
   (`GradeByIndex`, `:77-92`). Ein eingefügtes Item verschiebt die Indizes – jede Antwort danach trifft das falsche
   Item.
4. **Die Antworten liegen bis zur Abgabe nur im Browser**, je `runId` (`frontend/src/sohn/SohnBonus.tsx:37-43`).
   Eine neue `runId` heißt im Frontend von selbst „von vorn“ (`:180`, gefiltert über `r.cards`).
5. **Seit B-543 bleibt eine Einmal-Runde bei vollem Deckel offen** und wird erst am nächsten Tag gewertet
   (`ExtraTrainingService.cs:146-149`) – das Fenster für eine Änderung ist eine Nacht lang.

## Die echte Lücke

Die Runde hat keinen Begriff davon, *welchen* Inhalt sie gestellt hat. Ein Index allein trägt das nicht, und
`ItemCount` erkennt keine Änderung bei gleicher Anzahl (ein korrigierter Lösungsschlüssel, ein getauschtes Wort).

## Offene Punkte

1. ~~Schnappschuss der Items oder Fingerabdruck?~~ → Entscheidung 1.
2. ~~Was geschieht mit einer veralteten Runde?~~ → Entscheidung 2 und 3.
3. ~~Bestandsrunden ohne Fingerabdruck?~~ → Entscheidung 4.
4. ~~Was geht in den Fingerabdruck ein?~~ → Entscheidung 5.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-14).

1. **Fingerabdruck statt vollem Schnappschuss.** Die Runde bekommt beim Anlegen `ContentHash` (SHA-256 über den
   gewerteten Inhalt, Entscheidung 5) als nullbare Textspalte. Begründung: Gegen einen eingefrorenen Schnappschuss
   zu werten hieße, Münzen für einen Lösungsschlüssel zu zahlen, den der Autor gerade *korrigiert* hat – und die
   Wertung lebte an zwei Orten (`CheckAsync` und ein Schnappschuss-Pfad). Ein Fingerabdruck ist eine kurze Spalte,
   keine JSON-Spalte (kein `ValueComparer`, Tor G7 unberührt). Kosten: Eine Runde, deren Übung sich ändert, ist
   verloren (Entscheidung 2) statt gerettet.
2. **Eine veraltete Runde wird verworfen, nie gewertet.** Weicht der Fingerabdruck beim **Fortsetzen** ab, löscht
   `StartAsync` die offene Runde und legt eine neue an (neue `runId` ⇒ das Frontend beginnt von selbst von vorn,
   Ist-Stand 4). Weicht er bei der **Abgabe** ab, löscht `SubmitAsync` die Runde unbezahlt und antwortet `409`
   mit neuem Code `extra_run_stale` („The exercise was changed since you started; start it again.“). Kosten: Das
   Kind verliert die Antworten einer laufenden Runde; das passiert nur, wenn ein Creator genau dann ändert.
3. **Eine verworfene Einmal-Runde verbraucht die Übung nicht.** Weil die Zeile gelöscht wird, steht die Übung wieder
   offen im Pool (`PoolAsync` zählt nur vorhandene Runden, `ExtraTrainingService.cs:262-266`). Anti-Cheat: das Kind
   hat bis dahin nur Fragen gesehen, keine Lösungen (die kommen erst mit dem Ergebnis), es gewinnt durch den Neustart
   nichts. Begründung: Fairness – das Kind hat die Änderung nicht verursacht. Kosten: keine nennenswerten.
4. **Bestandsrunden (`ContentHash = null`) verhalten sich wie bisher** – kein Abgleich, Wertung gegen die aktuellen
   Items. Begründung: Migration nur anhängen, keine Zeile umschreiben; offene Bestandsrunden gibt es höchstens wenige
   und für einen Tag. Kosten: Für sie bleibt der alte Fehler bis zur Abgabe bestehen.
5. **In den Fingerabdruck gehen nur gewertete Felder:** `ConfigJson` der Übung und je Item `Index`, `Prompt`,
   `Answer`, `AcceptedAnswers`, `GapIndex`, `ItemId` – nicht Bild, Audio oder Hinweis. Begründung: Ein neues Bild
   ändert keine Lösung und darf keine Runde kosten; Bilder können zudem je Kind gewählt sein. Kosten: Ein geänderter
   Hinweis wird in einer fortgesetzten Runde schon neu angezeigt – harmlos.
6. `ItemCount` bleibt (Spalte löschen wäre eine eigene Migration ohne Nutzen).

## Akzeptanzkriterien

1. Start einer Bonus-Runde, Creator ändert die Lösung eines Items, Abgabe ⇒ `409 extra_run_stale`, keine
   `ExtraTrainingCoins`-Buchung, die Runde ist weg und die Übung erscheint in `GET …/extra-training` wieder offen
   (bei einer Einmal-Übung `done: false`). Rot, wenn `SubmitAsync` den Fingerabdruck nicht vergleicht.
2. Start, Creator fügt ein Item ein, erneuter Start (Fortsetzen) ⇒ neue `runId`, Karten aus dem neuen Inhalt. Rot,
   wenn `StartAsync` die offene Runde ohne Abgleich fortsetzt.
3. Start, nur das Bild eines Items ändert sich, Abgabe ⇒ normal gewertet und bezahlt. Rot, wenn Bild-URLs in den
   Fingerabdruck eingehen.
4. Start, keine Änderung, Abgabe ⇒ wie bisher bezahlt (Regression; rot, wenn der Fingerabdruck nicht deterministisch
   ist, z. B. über Wörterbuch-Reihenfolge).
5. Eine Bestandsrunde mit `ContentHash = null` wird ohne Abgleich gewertet (Regression; rot, wenn `null` als
   „abweichend“ gilt).
6. Frontend: `extra_run_stale` steht in `PROBLEM_KEYS` (`frontend/src/lib/chromeText.tsx:50`) mit Text in `de.ts`,
   `en.ts` und `lib/api.ts`; die gespeicherten Antworten der Runde werden dabei geräumt (`clearAnswers`). Vitest in
   `SohnBonus.test.tsx`: rot, wenn der Code fehlt (dann stünde der rohe englische Text da).

## Schätzung

**Größe: S** – eine nullbare Spalte, eine Hilfsmethode für den Fingerabdruck, zwei Vergleiche, ein Fehlercode.
Vergleich: kleiner als B-543 (S/M, Deckel-Regel mit Frontend-Zeile), größer als ein reiner Regeltest.

- **`migration: ja`** – nullbare Spalte `ExtraTrainingRun.ContentHash` (`string?`), neue Migration an die Kette;
  Bestandszeilen bleiben `null` (Entscheidung 4). Kein Umschreiben.
- **`vertragsbruch: nein`** – kein DTO ändert sich; `extra_run_stale` ist ein zusätzlicher `409`-Code (in
  `ApiErrors` und in den `<response>`-Docs von Start/Abgabe). Clients, die ihn nicht kennen, zeigen den
  ProblemDetails-Text.

**Risiken:**

- Determinismus: der Fingerabdruck muss für denselben Inhalt immer gleich sein – feste Feldreihenfolge, eigene
  Serialisierung statt `JsonSerializer` über den ganzen `ContentItem` (der trägt Bild-/Audio-Felder).
- `ItemsOfAsync` ohne `childId` (`ExerciseContentResolver.cs:28`) wird schon heute für den Pool gerufen; dieselbe
  Item-Liste für Start und Abgabe nehmen, nicht einmal mit und einmal ohne Kind.
- Der Löschpfad bei der Abgabe läuft nach dem Laden des Kindes; das `ConcurrencyStamp`-Muster aus `SubmitAsync`
  (`:175`) bleibt die Serialisierung, damit eine doppelte Abgabe nicht einmal löscht und einmal bezahlt.

**Angriffsplan (Backend zuerst):**

1. Spalte + Migration; `SchemaGuardTests` grün.
2. `ContentFingerprint(Exercise, IReadOnlyList<ContentItem>)` (privat in `ExtraTrainingService`); beim Anlegen
   setzen.
3. `StartAsync`: offene Runde mit abweichendem Fingerabdruck löschen, neu anlegen. `SubmitAsync`: vor der Wertung
   vergleichen, bei Abweichung löschen + `ApiErrors.ExtraRunStale`. Dafür die Items einmal laden und an
   `CheckAsync` weiterreichen bzw. vorher vergleichen.
4. `ApiErrors.ExtraRunStale`, Controller-Doku, `contract.ts` neu erzeugen (nur Doku-Text ändert sich).
5. Frontend: Problem-Key, Texte, `clearAnswers` im Fehlerpfad.

**Testweg:**

- `ExtraTrainingTests`: AK 1–5 als Integrationstests (Creator ändert die Übung über die echte API zwischen Start und
  Abgabe); je Fall die rote Probe am richtigen Ort.
- Vitest `SohnBonus.test.tsx`: AK 6.
- Kein E2E: die Änderung durch einen Creator mitten in einer Kinder-Runde ist im Browser nur mit zwei Sitzungen
  nachzustellen; der Weg ist über die Integrationstests vollständig belegt (argumentierte Ausnahme vom Rollengang,
  der Bau-Sprint nennt sie im `## Verlauf`).

## Verlauf

- **2026-11-13** — angelegt aus B-543 (Entscheidung 6, zurückgestellt) beim Bau im Backlog-Lauf 2026-11-13.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt – `ExtraTrainingService.StartAsync` schreibt `ItemCount` (`ExtraTrainingService.cs:98`), gelesen wird er nirgends (Spalte `GamificationEntities.cs:136`); `SubmitAsync` wertet über `preview.CheckAsync` gegen die aktuellen Items (`:151`), `BuildRun` (`:116`, `:321`) baut die Karten ebenfalls aus den aktuellen Items. Kein Schnappschuss an der Runde.
- **2026-11-14** — Refinement Backlog-Lauf: ausformuliert, gegrillt (sechs Stakeholder-Entscheidungen im Auftrag des Nutzers: Fingerabdruck statt Schnappschuss, veraltete Runde wird verworfen statt gewertet und verbraucht keine Einmal-Übung, Bestandsrunden unverändert) und geschätzt: S, Migration ja (nullbare Spalte), Vertrag nein (neuer 409-Code), `wo` auf beides (Problem-Key im Frontend).
- **2026-11-14** — **abgenommen** (Sprint 73). volle Suite 1612/1612 (Release), Vitest 1000/1000, `tsc -b` grün, Rollengang Sohn als Playwright gegen frisch gestartete Server nach dem letzten Edit: 9 passed (`belohnung-trotz-wochenziel`, `bonus`, `bonus-deckel*`, `pfad`, `lektion`, `erstbegegnung`, `full-flow`); Reviews `pugling-reviewer` + `frontend-reviewer` (Funde im Sprint behoben); Commit `fa335075`. Mutanten: M16 Start ohne Abgleich (AK 2), M17 Abgabe ohne
  Abgleich (AK 1), M18 Hinweis im Fingerabdruck (AK 3), M19 `null` gilt als abweichend (AK 5), M22 Antworten nicht
  geräumt (AK 6, Vitest), M23 Code fehlt in `PROBLEM_KEYS` (AK 6) – alle rot. AK 4 hält der Bestandstest
  `Eine_Uebung_Ohne_Vokabeln_Ist_Nach_Dem_Start_Verbraucht`. Review-Fund behoben: Doppel-Abgabe einer veralteten Runde
  fing `DbUpdateConcurrencyException` nicht (500 statt 409) – ungemessen (zwei parallele Abgaben sind im Test nicht
  deterministisch zu erzwingen). Kein E2E (argumentierte Ausnahme laut Testweg: Creator-Änderung mitten in einer
  Kinder-Runde braucht zwei Sitzungen).
- **2026-11-14** — Nachschau Backlog-Lauf: Fingerabdruck, Verwerfen bei Start und Abgabe und das Räumen in der Arcade halten. Fund: die Endpunkt-Doku `ExtraTrainingController.cs:31-34/48-51` verspricht beim Start weiter das Fortsetzen mit denselben Karten und nennt `extra_run_stale` nicht (geplant war er in den Docs von Start/Abgabe) → **B-549**.
