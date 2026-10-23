---
tags: [typ/story, status/abgenommen, bereich/doku, rolle/creator]
aliases: [Creator.http chapters, REST-Tutorial 404]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: doku
migration: nein
vertragsbruch: nein
quelle: Nachtlauf 2026-10-23, Sprint 2 (Sweep zu B-166)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-10-23 (zweiter Lauf, Sprint 5, frischer Agent, gegengelesen): alle 23 @name-Requests gegen Route/Methode/DTO (publishers :22/:85, textbook-series :128, units :57 mit Topics List, ExerciseRoutes.Base, ExercisePayload inkl. executePublic, items :136, exercises?type, preview, tags, grants GET/POST/DELETE, profiles 12 Felder, textbooks inkl. seriesId/currentUnitId, match); Variablenkette vollstaendig; Seed hat >= 3 en->de fuer skip=0&take=2 (Seed.cs:772-808); Slug-Kommentare gegen :107-110/:153-156; verifiziert-am im Kopf, Statuscodes im Verlauf; kein Defekt. Nit: Verlauf sagte 26 Requests, es sind 23 (berichtigt); Kopf :7-8 nennt matchProfiles nicht als dritte Betreuungs-Bedingung"
---

# B-238 · Das ausführbare REST-Tutorial des Creators fährt die Kapitel-Route, die es seit B-106 nicht gibt

## User Story

Als **Creator mit dem REST-Client** möchte ich `docs/REST/Creator.http` Schritt für Schritt ausführen können,
damit das Tutorial zeigt, wie Fach → Reihe → Unit → Übung heute entsteht – statt ab Schritt 4 nur 404 zu liefern.

## Ist-Stand am Code

`docs/REST/Creator.http` (Stand `6af2c62`, 2026-08-10, 271 Zeilen) baut ab `:60` auf der entfernten Ebene:

| Zeile | Request | Lebende Route |
| --- | --- | --- |
| `:62` | `POST …/creator/subjects/{{subjectId}}/chapters` | gibt es nicht – eine Übung hängt an `textbook-series/{seriesId}/units/{seriesUnitId}` |
| `:72` | `@chapterId = {{createChapter.response.body.id}}` | Variable ohne Quelle, sobald `:62` 404 liefert |
| `:85` | `POST …/subjects/{{subjectId}}/chapters/{{chapterId}}/vocabulary` | `POST …/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary` |
| `:109` | `GET …/chapters/{{chapterId}}/vocabulary/{{exerciseId}}/items` | `GET …/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}/items` |

Alles ab `:112` (Katalogfilter, Preview, Tags, Grants, Sharing) hängt an `@exerciseId` aus `:105` und läuft
damit ebenfalls ins Leere. `Supervisor.http` und `Student.http` tragen keine Kapitel-Route (gegrept: 0).
`docs/REST/README.md:15` beschreibt die Datei als „Fach → Kapitel → Vokabelübung → Items → Preview → Tags".

## Die echte Lücke

Nicht ein Wort, sondern **drei fehlende Schritte**: Das Tutorial braucht vor der Übung eine Reihe
(`POST creator/textbook-series`, mit `subjectId`) und eine Unit (`POST …/{seriesId}/units`), deren Ids es
dann weiterreicht – genau die Umschreib-Arbeit, die B-166 für sein Wiki-Beispiel als „nicht bloß Pfad
ersetzen" benannt hat. Die Datei ist ausführbar und damit die einzige Doku, die beim Lesen *nicht* auffällt
und beim Ausführen sofort.

## Akzeptanzkriterien

1. `docs/REST/Creator.http` enthält keine Route mit `chapters`; vor der Übung stehen die Requests für Reihe und
   Unit, und `@seriesId`/`@seriesUnitId` reisen in die Übungs-Requests weiter.
2. Die Datei läuft im REST-Client von oben bis unten ohne 404 durch (Datum des Durchlaufs in der Datei, wie
   `docs/tutorial-creator.md` es hält); ersatzweise ein `curl`-Durchlauf derselben Requests gegen die laufende
   API, im `## Verlauf` benannt.
3. `docs/REST/README.md:15` beschreibt den neuen Weg (Fach → Reihe → Unit → Vokabelübung → Items → Preview → Tags).

## Offene Punkte

1. Welche Felder verlangt `POST creator/textbook-series` minimal (Name, Slug?, `subjectId`, Sprachen)? Aus
   `TextbookSeriesDtos.cs` und `docs/tutorial-creator.md` abschreiben, nicht raten.
2. Soll das Tutorial die geseedete Reihe „Green Line 1" nutzen (kürzer, aber abhängig vom Seed) oder eine
   eigene anlegen (länger, aber idempotent gegen Wiederholung über den Slug)? *Empfehlung: eigene, mit
   Zeitstempel im Namen wie bisher beim Fach.*
3. Gibt es einen maschinellen Beleg? Die `.http`-Dateien laufen nur im REST-Client (httpYac inkompatibel,
   siehe Memory); ein Test dafür existiert nicht. *Empfehlung: einmal von Hand durchfahren und das Datum in
   die Datei schreiben – wie `docs/tutorial-creator.md` es mit „verifiziert am" tut.*

## Entscheidungen

Gegrillt im zweiten Nachtlauf 2026-10-23 (Freigabe 1, `art: Defekt`).

1. **Minimale Felder, aus den DTOs abgeschrieben** (Punkt 1): `CreateTextbookSeriesDto(Name, PublisherId?,
   SubjectName?, SubjectId?, SchoolTypes?, SourceLanguage?, TargetLanguage?, Notes?)` und
   `CreateSeriesUnitDto(Label, Grade?, OrderIndex?, BookType?, Topics? als Liste, Grammar?, VocabularyNotes?)`
   (`TextbookSeriesDtos.cs:23-24, :54-55`). **Damit ist die zweite Hälfte der Datei ebenfalls tot:** der
   bestehende `createSeries` (`:189-201`) schickt `"publisher": "Cornelsen"` (kein Feld des Vertrags →
   `400 unknown_field`, `UnmappedMemberHandling.Disallow`), `createUnit` (`:208-219`) schickt `topics` als
   String statt Liste. Beide werden mit umgeschrieben. **Kosten:** die Story wächst von drei auf fünf
   Requests, die Größe von XS auf S.
2. **Eigene Reihe mit Zeitstempel** (Punkt 2): `"name": "REST-Demo {{$timestamp}}"` wie bisher beim Tag –
   kein Seed-Bezug, und der Slug-Idempotenz-Fall („200 statt 201") bleibt im Kommentar erklärt. Das Fach
   bekommt denselben Zeitstempel, damit ein zweiter Durchlauf nicht an einem Namens-Duplikat hängt.
   **Kosten:** jeder Durchlauf hinterlässt ein Fach und eine Reihe in der Dev-DB – das tut die Datei heute
   auch schon.
3. **Umbau statt Flicken:** Reihe und Unit wandern **vor** die Übung; der spätere Block „Unterrichtsmaterial &
   Creator-Profile" legt nur noch Profil und Lehrbuch-Verknüpfung an und benutzt `@seriesId`/`@seriesUnitId` von
   oben. Der `restrictExecute`-PUT geht auf die Unit-Route. **Kosten:** der Lesefluss der Datei ändert sich;
   dafür zeigt sie den Weg, den `tutorial-creator.md` seit B-106 erzählt.
4. **Beleg: `curl`-Durchlauf gegen die Wegwerf-Instanz `:5280`** (Punkt 3, AK 2 zweiter Fall): dieselben
   Requests in derselben Reihenfolge, mit Statuscode je Schritt im Verlauf, und das Datum als „verifiziert
   am" im Kopf der Datei. Der REST-Client selbst läuft nur im Editor – der Nutzer fährt ihn einmal (steht
   schon unter „Für den Morgen"). **Kosten:** der `curl`-Lauf prüft Routen und Nutzlasten, nicht die
   REST-Client-Variablensyntax (`{{login.response.body.token}}`); die bleibt beim Durchlauf von Hand.

## Schätzung

**Größe S** (Anker: eine Datei, ~270 Zeilen, fünf Requests umgeschrieben und zwei neu geordnet, dazu ein
Live-Durchlauf). `wo: doku`, keine Migration, kein Vertrag.

**Angriffsplan:** Wegwerf-Instanz starten (`/smoke-test`-Rezept, Port 5280) → heutigen Stand der Datei per
`curl` fahren (rote Zahl: welcher Schritt 404/400) → Datei umbauen → denselben Lauf erneut (alle 2xx) →
„verifiziert am" + README-Zeile → Reviewer → Commit.

**Risiko:** `{{$timestamp}}` im Fach-Namen ändert den Slug-Hinweis bei der Reihe („Name schon vergeben ⇒
200") – der Kommentar muss sagen, dass das nur ohne Zeitstempel eintritt.

**Testweg:** `curl`-Sequenz mit Statuscodes (vorher/nachher) im Verlauf.

## Verlauf

- **2026-10-23** — **abgenommen** (zweiter Nachtlauf, Sprint 4). Reviewer (allgemeiner Agent): alle 23
  Requests statisch gegen Route/Methode/DTO geprüft (`CreatePublisherDto`, `CreateTextbookSeriesDto`,
  `CreateSeriesUnitDto` mit `Topics: List<string>`, `ExercisePayload<VocabularyConfig>`, `AddGrantDto`,
  `CreateTagDto`, `CreateCreatorProfileDto`, `CreateTextbookDto`), Variablenkette (jede `@variable` aus einem
  existierenden `# @name`), Slug-Kommentar gegen `PublishersController.cs:106-109` und
  `TextbookSeriesController.cs:152-155` (gemessen 201 → 200), PUT-Body vollständig (Items danach 2 → 2); und
  **eigener Live-Lauf** gegen eine frische Wegwerf-Instanz: alle 2xx. **Ohne Blocker;** ein Fund mit
  Doku-Wirkung (dieser Verlauf nannte den curl-Lauf noch nicht – hier), Nits übernommen (`skip=0` statt
  `skip=10`, damit auch ein Seed mit weniger als zwölf en→de-Einträgen zwei Treffer liefert; `@seriesUnitId`
  im Story-Text). **Rollengang Creator:** der curl-Durchlauf *ist* der Flow der Datei, Request für Request in
  ihrer Reihenfolge – im REST-Client selbst fährt ihn der Nutzer (Datum im Kopf der Datei dafür offen).
  Commit `7c33765` (Protokoll `docs/pm-sitzung-2026-10-23-nacht.md`, „Sprint 4").
- **2026-10-23** — **gebaut** (zweiter Nachtlauf, Sprint 4). **Rote Probe:** die Requests der alten Datei
  (`6af2c62`) per curl gegen eine Wegwerf-Instanz (`:5280`, frische Seed-DB, Harness
  `scratchpad/creator-http-alt.sh`) – erwartet 2xx durchgehend, gemessen: login 200 · me 200 · listSubjects 200
  · createSubject 201 · getSubject 200 · **createChapter 405** · getVocabulary 200 · **createExercise 405 ·
  getItems 404** · listExercises 200 · **preview 404** · createTag 201 · exerciseTypes 200 · **listGrants 404 ·
  addGrant 404 · restrictExecute 405 · removeGrant 404 · createSeries 400 `unknown_field` (`publisher`) ·
  createUnit 400 `validation_error` (`topics` kein String) · listUnits 404** · createProfile 400 (Artefakt der
  Git-Bash-Kommandozeile: Umlaute nicht als UTF-8 gesendet – mit ASCII 201) · **linkTextbook 400** (keine
  `seriesId`) · matchProfiles 200 – **13 von 24 Schritten rot**, alles ab Schritt 6. **Nach dem Umbau**
  (Harness `scratchpad/creator-http-neu.sh`, Umlaute als `\u`-Escapes): login 200 · me 200 · listSubjects 200 ·
  createSubject 201 · getSubject 200 · createPublisher 201 · createSeries 201 · createUnit 201 · listUnits 200 ·
  getVocabulary 200 · createExercise 201 · getItems 200 · listExercises 200 · preview 200 · createTag 201 ·
  exerciseTypes 200 · listGrants 200 · addGrant 201 · restrictExecute 200 · removeGrant 204 · createProfile 201
  · linkTextbook 201 · matchProfiles 200 (liefert das Profil) – **23 von 23 Schritten 2xx**. „Verifiziert am
  2026-10-23" steht im Kopf der Datei; `docs/REST/README.md:15` beschreibt den neuen Weg.

- **2026-10-23** — gegrillt und geschätzt im zweiten Nachtlauf desselben Tages (Freigabe 1); beim Grillen gegen die DTOs zwei weitere tote Requests gefunden (Entscheidung 1), Größe S.

- **2026-10-23** — angelegt im Nachtlauf (Sprint 2) beim Grep-Sweep zu B-166, direkt `ausformuliert`: die
  Fundstellen stehen mit Zeile, die lebenden Routen sind gegen `ExerciseControllers.cs` geprüft. **Bewusst
  nicht in B-166 mitgenommen** (README: ein Fund beim Bauen wird eine eigene Story): dort sind es
  Wortersetzungen, hier ein Umbau mit drei neuen Requests, und der Beleg ist ein anderer (Ausführen, nicht
  Grep). `entgangen_bei` bleibt leer – die Datei wurde nie als Story abgenommen.
