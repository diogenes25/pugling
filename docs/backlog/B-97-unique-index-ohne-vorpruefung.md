---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/api]
aliases: [Unique-Index ohne Vorprüfung, 500 statt 409, duplicate_achievement]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/api-design-bewertung.md (Vorschlag A1) — Arbeitsrunde PM/API-Designer/Entwickler am 2026-08-04
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-08-07"
---

# B-97 · Zwei Schreibpfade laufen ungeprüft in einen Unique-Index und antworten mit 500

Ein Kapitel umbenennen und ein Ziel-Abzeichen anlegen laufen ohne Vorprüfung gegen einen Unique-Index der
Datenbank. Ergebnis ist kein `409` mit lesbarem Grund, sondern ein **500 mit halb gespeichertem Zustand** —
und für den Abzeichen-Fall gibt es nicht einmal einen Fehlercode. Beide Stellen verletzen eine Regel, die
dieses Repo sich selbst aufgeschrieben hat (Vorprüfung vor dem Schreiben, damit der Konflikt einen `code`
bekommt).

## User Story

Als **Vater** möchte ich beim Umbenennen eines Kapitels oder beim Anlegen eines Abzeichens eine Meldung
bekommen, die den Grund nennt, damit ich den Namen ändern kann — statt eines Serverfehlers, nach dem ich
nicht weiß, was gespeichert wurde.

## Ist-Stand am Code

- `Controllers/Creator/ChaptersController.cs:80` setzt `chapter.Name` ohne jede Prüfung; der Unique-Index
  `(SubjectId, Name)` steht in `Data/PuglingDbContext.cs:707`. Der Code `duplicate_chapter_name` **existiert**
  und wird auf dem POST-Pfad benutzt — über PATCH ist er damit unerreichbar.
- `Controllers/Supervisor/MissionsController.cs:119` (POST) und `:141` (PATCH) schreiben gegen
  `(ChildId, Metric, Threshold)` (`PuglingDbContext.cs:716`). Für diesen Konflikt gibt es **keinen**
  Fehlercode in `Errors/ApiErrors.cs`.
- Das Muster für die Reparatur steht wenige Zeilen entfernt im Nachbarcontroller:
  `Controllers/Creator/VocabularyTagsController.cs:65` (`AnyAsync`-Vorlauf, dann `ProblemWithCode`).
- Ein globaler `DbUpdateException` → `409`-Handler existiert nicht (geprüft in `Program.cs` und
  `Errors/`); der Fall endet darum im allgemeinen 500-Pfad.
- **Reichweite präzisiert** (Runde, gegen die erste Fassung des Berichts): Über PATCH ist der
  Abzeichen-Konflikt **schmaler** als über POST — `UpdateAchievementDto` (`Contracts/…/GamificationDtos.cs:24`)
  trägt **kein** `Metric`. Erreichbar ist er nur, indem `Threshold` auf den Wert eines anderen Abzeichens
  *derselben* Metrik gesetzt wird. Der POST-Pfad ist der breite.

## Die echte Lücke

Nicht „die API kennt keine Konflikte" — der Katalog ist stark (57 Codes, keiner tot). Die Lücke sind **zwei
Nachzügler**: an zwei von vielen Schreibpfaden fehlt die Vorprüfung, die überall sonst steht. Die Reparatur
ist je Stelle ein `AnyAsync` plus eine `ProblemWithCode`-Zeile; teuer wäre nur der Versuch, daraus eine
mechanische Regel über *alle* Indizes zu machen — siehe Entscheidung 3.

## Entscheidungen

Erarbeitet in der Arbeitsrunde PM/API-Designer/Entwickler am 2026-08-04, übernommen auf Nutzerauftrag
(„fahre gemäß deiner eigenen Einschätzung fort").

1. **Beide Vorprüfungen bauen** — einig. Der neue Code `duplicate_achievement` ist **additiv** in
   `ApiErrors`. Kosten: zwei zusätzliche `AnyAsync`-Abfragen auf Schreibpfaden, die ohnehin schreiben —
   messbar irrelevant, und sie sparen den 500.
2. **Kein globaler `DbUpdateException` → 409-Handler.** Er würde jeden künftigen Index stumm in ein `409`
   ohne fachlichen `code` verwandeln und damit genau die Aussagekraft nehmen, für die der Katalog existiert.
3. **Das im Bericht vorgeschlagene Tor über alle Unique-Indizes ist zurückgezogen** — von beiden Seiten.
   Es sind **47** `IsUnique` (davon 18 mit `HasFilter`; doppelt belegt: `PuglingDbContext.cs` und die eine
   Migration nennen dieselbe Zahl — die „41" im Bericht war falsch). Die Zuordnung Index → Schreibpfad ist
   **nicht reflexiv ableitbar**: viele Indizes liegen auf Tabellen ohne jeden Schreib-Endpunkt
   (`PositionGoalReward`, `MissionAward`, `ItemProgress` …), andere auf Join-Tabellen, deren Schreibpfad
   bewusst ein Link-Insert ohne 409 ist. Ein Tor müsste eine Liste von Paaren pflegen, und die pflegt
   niemand. Die Regel bleibt, wo sie steht: in `backend/Pugling.Api/CLAUDE.md`.

## Schätzung

**Größe: XS** — zwei `AnyAsync`-Vorprüfungen nach dem Muster `VocabularyTagsController.cs:65`, ein additiver
Eintrag in `ApiErrors`, drei Testfälle. Kein Schema, keine Migration, kein Vertragsbruch (ein `code` wird
*spezifischer*; wer bisher `500` bekam, bekommt jetzt `409` — für einen Client ist das die Reparatur, nicht
der Bruch).

**Risiken.** Zwei, beide klein: (a) Der Kapitel-Vergleich muss dieselbe Groß-/Kleinschreib-Semantik haben wie
der Index. **Korrigiert nach dem Review:** `Chapter.Name` trägt **keine** Collation
(`Data/Migrations/20260803223259_InitialCreate.cs:496` — `TEXT`, `maxLength: 200`, ohne `collation:`);
`NOCASE` haben ausschließlich `Vocabulary.Word`/`.Translation` (`PuglingDbContext.cs:257-258`). Index und
`==`-Vergleich sind damit **beide BINARY** und deckungsgleich — „unit 1" neben „Unit 1" ist im Fach erlaubt
und erzeugt keinen 500. Wer diesen Absatz in seiner ersten Fassung („die Namensspalten sind `NOCASE`") gelesen
und die Prüfung case-insensitiv „reparieren" wollte, hätte die Divergenz erst erzeugt: ein 409 für einen
Namen, den der Index zulässt. (b) Der Abzeichen-Vergleich muss auf das **Tripel**
`(ChildId, Metric, Threshold)` gehen und beim PATCH die Zeile selbst ausschließen, sonst kollidiert eine
Zeile mit sich.

**Bewusst nicht gelöst: die Vorprüfung ist nicht atomar.** Zwei genau parallele Schreibzugriffe können beide
durch die Prüfung kommen, und einer bekommt weiter den 500 aus dem Index. Das ist konsistent mit **allen**
bestehenden Vorprüfungen im Projekt und die Folge von Entscheidung 2 (kein globaler
`DbUpdateException`-Handler). Es steht hier, damit es nicht in einem halben Jahr als frischer Defekt gemeldet
wird: der Fall braucht zwei Erwachsene, die im selben Moment denselben Namen vergeben.

**Angriffsplan** (Backend zuerst, es gibt keinen Frontend-Anteil):

1. `ApiErrors.DuplicateAchievement` additiv ergänzen.
2. Testfälle **zuerst** schreiben und rot laufen lassen (Abnahmeform `Defekt`).
3. `ChaptersController.Update` und die beiden Abzeichen-Pfade um die Vorprüfung ergänzen.
4. Voller Suite-Lauf; `docs/api-examples` nur, wenn `DocsCaptureTests` den neuen Code mitschneidet.

**Testweg:** `Pugling.Api.Tests` — Kapitel-Fall zu den bestehenden Katalog-Tests, Abzeichen-Fälle zu den
Gamification-Tests. Kein E2E (keine Oberfläche betroffen), und statt des vollen `/smoke-test`-Flusses eine
gezielte Live-Probe als Gegenprobe zum Testdoppel — gegen einen **isolierten** Server mit Wegwerf-DB
(Port 5280 nach dem Muster des `/smoke-test`-Kommandos, damit `pugling.db` unangetastet bleibt), nicht gegen
eine laufende Dev-Instanz auf 5200.

## Akzeptanzkriterien

1. `PATCH creator/subjects/{subjectId}/chapters/{chapterId}` mit einem im Fach bereits vergebenen Namen
   antwortet `409` mit `code: duplicate_chapter_name` — nicht `500`.
2. `POST` **und** `PATCH` auf `supervisor/children/{childId}/achievements` antworten bei einem Konflikt auf
   `(ChildId, Metric, Threshold)` mit einem `ProblemDetails` mit `code: duplicate_achievement`.
3. Nach einem abgelehnten PATCH steht in der Datenbank der **alte** Wert (kein halb geschriebener Zustand).
4. Je Fall ein Integrationstest, der **vor** der Änderung rot war (Abnahmeform für `art: Defekt`).

## Verlauf

- **2026-08-04** — angelegt aus `docs/api-design-bewertung.md` (A1) und der Arbeitsrunde
  PM/API-Designer/Entwickler. Beide Befunde von beiden Seiten unabhängig am Code bestätigt; die
  Tor-Ausweitung und der globale Exception-Handler wurden in der Runde verworfen, die Reichweite des
  PATCH-Falls korrigiert.
- **2026-08-04** — **gegrillt und geschätzt** (autonom getroffen, Nutzerauftrag „fahre gemäß deiner eigenen
  Einschätzung fort"): die drei Ergebnisse der Arbeitsrunde als nummerierte Entscheidungen mit Begründung und
  Kosten übernommen; Größe **XS**, `wo: backend`, `migration: nein`, `vertragsbruch: nein`, Risiken,
  Angriffsplan und Testweg ergänzt.
- **2026-08-04** — **gebaut.** Reihenfolge wie im Angriffsplan: **rote Probe zuerst.** Beide neuen Tests
  scheiterten gegen `HEAD` mit `Expected: Conflict / Actual: InternalServerError` — der Defekt ist damit
  reproduziert, nicht behauptet.
  - `ApiErrors.DuplicateAchievement` additiv ergänzt.
  - `ChaptersController.Update`: Vorprüfung vor der ersten Zuweisung, Selbstausschluss über die **Id** (nicht
    über den Namen — das hält auch, falls die Spalte je eine Collation bekommt).
  - `AchievementsController`: ein geteilter Helfer `ThresholdTakenAsync` für **beide** Schreibpfade statt
    zweier Kopien — dieselbe Lehre wie [B-95](B-95-stufenwaechter-haengt-am-include.md).
  - `[ProducesResponseType(409)]` an allen drei Actions; das Dokument (`docs/openapi/v1.json`) und
    `docs/api-examples/index.md` sind vom Testlauf regeneriert (drei 409-Antworten, 58 statt 57 Codes).
  - **Tests:** `CatalogManagementTests.Kapitel_MitVorhandenemNamen_Liefert409` um PATCH-Konflikt,
    „Kapitel unverändert" und Selbst-Umbenennung erweitert;
    `GamificationTests.Auszeichnung_MitVorhandenerSchwelle_Liefert409_AufBeidenSchreibwegen` neu.
- **2026-08-04** — **Review (`pugling-reviewer`), zwei Befunde, beide eingearbeitet:**
  - **Eine Lücke im gerade reparierten Pfad.** Meine erste Fassung prüfte `dto.Name?.Trim() is { Length: > 0 }`
    — ein Name aus Leerzeichen fiel damit **durch die Vorprüfung** und wurde darunter als `""` geschrieben
    (was `Create` verbietet); der **zweite** leere Name traf dann wieder den Index als 500. Die Dublettenprüfung
    allein schloss den Pfad also nicht, dem sie hinzugefügt wurde. Jetzt in der Form des Schwester-Controllers
    (`ExerciseCategoriesController.Update`): erst `validation_error` bei Leername, dann die Dublette, ein
    einziges `Trim()`, `[ProducesResponseType(400)]` an der Action. Mit eigenem Testfall.
  - **Der Ist-Stand dieser Story enthielt eine falsche Behauptung**, und zwar eine gefährliche: `Chapter.Name`
    trägt **keine** `NOCASE`-Collation (nur `Vocabulary.Word`/`.Translation` tun das). Korrigiert im Abschnitt
    „Schätzung" — wer den alten Satz gelesen und die Prüfung case-insensitiv „reparierte", hätte die Divergenz
    zum Index erst erzeugt.
  - Ohne Befund geprüft und hier festgehalten: **keine weiteren Schreibpfade** auf die beiden Indizes (der
    Seed ist inhaltsverankert idempotent, `PATCH` kann die Metrik nicht ändern), Konventionen erfüllt,
    `exceptId: 0` auf dem Create-Pfad ist sauber (Ids sind positiv) — im `<summary>` jetzt ausdrücklich
    genannt.
  - **Der dritte Fundort derselben Fehlerklasse** liegt außerhalb dieser Story und ist als
    [B-104](B-104-keyresult-dublette-zahlt-doppelt.md) abgelegt: drei ungeprüfte Schreibpfade auf die
    `KeyResult`-Eindeutigkeiten — und dort hängt eine Prämie dran.
- **2026-08-04** — **abgenommen.** Verifikation belegt: `dotnet test Pugling.sln -c Release` →
  **710/710 grün**, 0 Warnungen (Doku-Regeneration byte-stabil, vom Reviewer unabhängig nachgelaufen);
  `pugling-reviewer` ohne Blocker, beide Befunde eingearbeitet statt offengelassen; statt `/smoke-test` die
  **Live-Probe** gegen einen isolierten Server auf Port 5280 (Wegwerf-DB, `pugling.db` unangetastet), alle
  vier Wege bestätigt: `PATCH chapters/{id} {"name":"Unit 1"}` → **409 `duplicate_chapter_name`**,
  `{"name":"   "}` → **400 `validation_error`** und der Name danach unverändert `Unit 2`, Selbst-Umbenennung
  → **200**; `POST achievements` mit vergebener Schwelle → **409 `duplicate_achievement`**, `PATCH` auf eine
  vergebene Schwelle → **409** und die Schwelle danach unverändert. Alle vier Akzeptanzkriterien erfüllt.
  Commit `1b59eb5`; die Abnahme-Zeile selbst in `HEAD`.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob die Achievement-Vorprüfung weiterhin steht. **Fund
  ohne Regression:** `ChaptersController` existiert nicht mehr — die `Chapter`-Entität wurde seit B-97
  durch `SeriesUnit` ersetzt (B-106). Die Achievement-Hälfte hält unverändert
  (`MissionsController.cs:106-168`, geteilter Helfer `ThresholdTakenAsync`); die Chapter-Hälfte ist
  gegenstandslos, weil ihr Gegenstand entfernt wurde. Geprüft, ob dadurch ein neuer ungeschützter
  Unique-Index-Schreibpfad entstanden ist: `SeriesUnitsController` trägt auf `(SeriesId, Grade,
  OrderIndex)` **keinen** Unique-Index, und `TextbookSeriesController` behandelt seinen einzigen
  Unique-Index (`Slug`) bereits idempotent. Kein neuer Fund, kein Fix nötig.
