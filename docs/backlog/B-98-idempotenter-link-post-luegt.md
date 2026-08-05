---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/api]
aliases: [201 ohne Insert, erfundene Antwortwerte, idempotenter Link-POST]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: ja
quelle: docs/api-design-bewertung.md (Vorschlag A2) — Arbeitsrunde PM/API-Designer/Entwickler am 2026-08-04
grund: ""
ersetzt_durch: []
---

# B-98 · Drei idempotente Schreibpfade antworten mit erfundenen Werten, zwei davon mit `201 Created`

Wer eine Verlinkung anlegt, die es schon gibt, bekommt eine Antwort, die **nicht aus der Datenbank kommt**:
einmal einen Zähler, der immer `0` ist, zweimal einen erfundenen Zeitstempel — und dazu ein
`201 Created`, obwohl nichts erzeugt wurde. Das Repo hat diese Frage an drei anderen Stellen schon
entschieden; diese drei sind Nachzügler hinter der eigenen Hausregel.

## User Story

Als **Konsument der API** (Frontend, `Pugling.Client`, KI-Agent) möchte ich, dass eine Antwort den
gespeicherten Zustand beschreibt und der Statuscode sagt, ob etwas entstanden ist — damit ich der Antwort
glauben kann, ohne nachzuladen.

## Ist-Stand am Code

Drei Stellen, alle in der „existiert schon"-Verzweigung:

1. `Controllers/Creator/VocabularyTagsController.cs:44` liefert `existing.Links.Count`. Die Navigation wird
   nie geladen und ist leer initialisiert (`Models/VocabEntities.cs:76`), Lazy Loading ist nirgends aktiv
   (0 Treffer für `UseLazyLoadingProxies`) — der Zähler ist **immer 0**. Dieselbe Datei rechnet ihn 28 Zeilen
   tiefer korrekt (`:72`, eigenes `CountAsync`), und `List` (`:29`) tut es per SQL.
2. `Controllers/Creator/ExerciseGrantsController.cs:78-79` antwortet mit `User.AdultId()` und
   `DateTime.UtcNow`, auch wenn der Grant von jemand anderem und früher angelegt wurde.
3. `Controllers/Supervisor/ChildrenController.cs:172-173` antwortet mit `dto.Relation` und `DateTime.UtcNow`,
   auch wenn die bestehende Betreuung eine **andere** Verwandtschaft trägt.

Dazu der Statuscode: bei (2) und (3) wird `201 Created` gemeldet, obwohl der `if`-Block übersprungen wurde.

**Die Hausregel existiert schon dreifach** — `creator/interest-tags`, `creator/textbook-series` und
`creator/vocabulary/tags` (`VocabularyTagsController.cs:44` gibt `Ok`) antworten 200-oder-201. Es ist also
keine Design-Frage, sondern eine Abweichung.

**Wer liest die Werte?** Gemessen in der Runde: `addExerciseGrant`/`addChildSupervisor` haben je **genau
einen** Aufrufer (`frontend/src/vater/ExerciseSharingPanels.tsx:36`,
`frontend/src/vater/ChildCarePanels.tsx:36`), beide in der Form `if (!await action.run(…)) return;` — der
Antwortrumpf wird **verworfen**, danach wird die Liste neu geladen. `Pugling.Client` gibt ihn durch
(`CreatorApi.cs:323`, `SupervisorApi.cs:61`), kein Agent liest ihn. Der erfundene Wert erreicht heute also
**niemanden** — außer bei (1), wo der Zähler angezeigt wird.

**Ein Test schreibt die Lüge als Zusicherung fest:** `Pugling.Api.Tests/ExerciseGrantsTests.cs:120-122`
prüft `Assert.Equal(HttpStatusCode.Created, …)` für **beide** POSTs, also auch für den, der nichts erzeugt.
(`MultiSupervisorTests.cs:37,91` nutzt `EnsureSuccessStatusCode()` und überlebt eine Umstellung.)

## Die echte Lücke

Bei (1) ein sichtbarer Defekt: der Vater liest „0 Vokabeln" an einem Tag, an dem 200 daran hängen. Bei (2)
und (3) **kein** heutiger Schaden — aber ein `201` ohne Erzeugung ist eine Falschaussage im Vertrag,
unabhängig davon, ob sie gerade jemand liest, und ein Test hält sie fest. Genau darüber ging der Streit in
der Runde: „niemand liest es" begründet, warum es nicht **drängt**, nicht, warum es richtig ist.

## Ergebnis der Arbeitsrunde vom 2026-08-04 (gegrillt)

1. **Alle drei Stellen, als eine Regel.** Formulierung für `backend/Pugling.Api/CLAUDE.md`: *„Ein
   idempotenter Link-POST antwortet `201` nur bei tatsächlichem Insert, sonst `200` mit der **gelesenen**
   Zeile."* Vorschätzung **S** (nach der Messung eher XS), `wo: backend`, keine Migration.
   **Vertragsbruch: ja** (201 → 200), in v1 zulässig; betroffen sind die Tests, nicht das Frontend (siehe
   Messung oben).
2. **Der PUT-Umbau ist verworfen** — von beiden Seiten, mit demselben Argument: `PUT
   …/grants/{creatorId}/{permission}` schiebt einen *Wert* als Schlüssel in ein Pfadsegment und vertieft
   genau die Verschachtelung, die der Bericht selbst als teuerste Kante des Vertrags rügt.
3. **Kein Tor.** Der Unterschied „hat der `if`-Block gelaufen?" steht im Methodenrumpf und ist reflexiv
   nicht entscheidbar. Die Regel bleibt ein Satz im Bereichs-Kontext.
4. **`ExerciseGrantsTests.cs:120-122` gehört zur Story**, nicht in eine Nacharbeit: ein Test, der falsches
   Verhalten schützt, ist teurer als der Fix.

## Akzeptanzkriterien

1. `POST creator/vocabulary/tags` mit einem existierenden Namen liefert den **echten** Verlinkungszähler
   (gleich dem, den `GET …/tags` für denselben Tag ausgibt).
2. `POST creator/exercises/{exerciseId}/grants` und `POST supervisor/children/{childId}/supervisors`
   antworten `200`, wenn die Verlinkung schon bestand, und `201` nur bei einem echten Insert.
3. Die Antwort trägt in beiden Fällen die **gespeicherten** Werte (Ersteller, Zeitstempel, Verwandtschaft),
   nicht die des Aufrufers.
4. `[ProducesResponseType(Status200OK)]` steht an beiden Actions, und `docs/openapi/v1.json` weist beide
   Status aus.
5. Ein Integrationstest je Fall, der vorher rot war; `ExerciseGrantsTests` prüft die neue Semantik statt der
   alten.

## Schätzung

`groesse: S`, `wo: backend`, `migration: nein`, `vertragsbruch: ja` (201 → 200 im idempotenten Zweig, in
v1 zulässig — betrifft laut Leser-Messung nur Tests, kein Frontend/Client-Konsument). Angriffsplan: alle
drei Stellen als eine Regel („Insert → 201, sonst 200 mit der gelesenen Zeile"), reihum
`VocabularyTagsController` (nur Zähler, Statuscode war schon richtig) →
`ExerciseGrantsController` → `ChildrenController.AddSupervisor`. Testweg: ein Integrationstest je Stelle,
rot gegen den Vorzustand (`git stash` der drei Controller), dazu der bestehende
`ExerciseGrantsTests.GrantIstIdempotent_UndListeZeigtOwner`-Test korrigiert (er schrieb die falsche
Erwartung fest, siehe „Ergebnis der Arbeitsrunde" Punkt 4).

## Verlauf

- **2026-08-04** — angelegt aus `docs/api-design-bewertung.md` (A2) und der Arbeitsrunde. Der API-Designer
  hatte (2)/(3) zunächst zurückgezogen („kein Leser"), nach dem Vertragsargument des Entwicklers wieder
  aufgenommen; die Leser-Messung und der Test-Fund stammen aus Runde 2.
- **2026-08-05** — im Autonomen Modus gegrillt (Arbeitsrunden-Ergebnis vom 2026-08-04 als Entscheidung
  übernommen), geschätzt und gebaut. Rote Probe zuerst: alle drei neuen Tests scheiterten gegen den
  Vorzustand (`vocabCount` 0 statt 2, `201` statt `200`). `pugling-reviewer` fand keinen Blocker; ein
  Politur-Hinweis (`AsNoTracking()` im reinen Lese-Zweig von zwei Controllern) direkt übernommen und erneut
  grün getestet — der TOCTOU-Hinweis des Reviewers ist ausdrücklich derselbe vorbestehende, bewusst
  nicht adressierte Zustand wie vor dem Fix (Entscheidung 3: „kein Tor"). `dotnet test Pugling.sln -c
  Release` → **715/715 grün** (713 + 2 neue Fakten in bestehenden Testklassen). Frontend: `npm run build`
  (Contract-Regenerierung, Typecheck) und `npm test` (127/127) unverändert grün — kein Quellcode-Zweig
  betroffen. Commit: siehe Repo-Verlauf (B-98-Commit). Status → `abgenommen`.
