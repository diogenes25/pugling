# T-01 · Welche Rolle behalten `Subject`/`ExerciseCategory` nach der Verschmelzung?

Status: entschieden     <!-- offen | beansprucht | entschieden -->
Typ: grilling           <!-- research | prototype | grilling | task -->
Blockiert durch:

## Frage

Wenn `Exercise.ChapterId` durch `Exercise.SeriesUnitId` ersetzt wird, entfällt `Chapter`. `Subject`
(fachliche Gruppierung, `LearnEntities.cs:8-18`) und `ExerciseCategory` (Fach-skopierte kontrollierte
Liste, `LearnEntities.cs:25-32`, referenziert von `Exercise.CategoryId`) hängen heute an `Chapter` bzw.
an `Subject` — nicht an `TextbookSeries`/`SeriesUnit`. Bleibt `Subject` als eigenständige fachliche Klammer
bestehen (z. B. für `ExerciseCategory`, Fachlehrer-Matching, Klassenarbeiten), oder wird auch `Subject`
durch `TextbookSeries.SubjectId` ersetzt? Bleibt `ExerciseCategory` fach-skopiert oder wird sie
reihen-/unit-skopiert?

## Antwort

**`Subject` bleibt bestehen — nur `Chapter` entfällt.** Begründung: `ExerciseCategory` und
`Klassenarbeit.SubjectId` hängen beide an `Subject`, nicht an `Chapter` — ihr Gegenstand entfällt nicht,
wenn `Chapter` verschwindet. `Chapter` selbst (nur `Name`+`OrderIndex`+`Exercises`,
`LearnEntities.cs:35-44`) wird ersatzlos gestrichen; seine einzige Aufgabe (Übungen gruppieren)
übernimmt `SeriesUnit`.

Der Bezug `Exercise → Subject` wird künftig **transitiv** über
`Exercise.SeriesUnitId → SeriesUnit.SeriesId → TextbookSeries.SubjectId` hergestellt statt direkt über
`Chapter.SubjectId`. Das deckt die `ExerciseCategory`-Validierung (`CategoryValid` prüft die Kategorie
gegen das Fach der Übung) und die `Klassenarbeit`-Fach-Filterung weiterhin ab — vorausgesetzt,
`TextbookSeries.SubjectId` ist bei jeder Reihe gesetzt, die Übungen tragen soll. Das ist heute **nicht**
erzwungen (`SubjectId` ist nullable, `CurriculumEntities.cs:26`; B-63 behandelt es als optionale
Dimension) — diese Karte macht `SubjectId` faktisch zur Pflicht für jede Reihe mit katalogisierten
Übungen, aber **nicht** schema-technisch `NOT NULL` (eine Reihe ganz ohne Übung bleibt weiterhin ohne
Fach denkbar), sondern als **Validierungsregel beim Anlegen der ersten Übung** einer ihrer Units
(neuer additiver `ApiErrors`-Code, z. B. `series_without_subject`).

`ExerciseCategory` bleibt **fach-skopiert** (unverändert an `Subject`), **nicht** reihen-/
unit-skopiert. Begründung: Grammatik-/Vokabel-Kategorien sind laut `backend/Pugling.Api/CLAUDE.md`
sprachübergreifend wiederverwendbar (z. B. „Zeiten" für jedes Englisch-Lehrwerk); eine Reihen-Bindung
zerschlüge genau die Wiederverwendung, die B-63 Entscheidung 3 für Grammatik-Themen als wünschenswert
identifiziert (dort zurückgestellt, hier bestätigt statt widerlegt).

**Kosten:** `Subject` bleibt eine dritte, jetzt chapter-lose Entität im Modell — kein Aufräumen dort in
dieser Karte. Neue Validierungsregel ist eine zusätzliche Guard Clause in `ExerciseControllerBase.Create`
plus ein additiver Fehlercode. `TextbookSeriesController` lässt `SubjectId` weiterhin optional (Reihen
ohne Übungen bleiben denkbar) — nur der Übungs-Anlage-Pfad wird strenger, nicht das Reihen-Anlegen
selbst.

**Verlauf:** 2026-08-04 — gegrillt, autonom entschieden (Nutzerauftrag 2026-08-04, PM-Loop Runde
„Lehrwerkgetriebener Katalog"), grundiert durch die Live-Prüfung derselben Runde
(`docs/pm-sitzung-2026-08-04.md`): `creator/textbook-series` und `creator/profiles` liefern über die
gesamte Seed-Landschaft `[]`.

