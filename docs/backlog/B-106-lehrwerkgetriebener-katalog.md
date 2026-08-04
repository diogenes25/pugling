---
tags: [typ/story, status/in-arbeit, bereich/katalog, bereich/training, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Lehrwerk-getriebener Katalog, Exercise an SeriesUnit, Kapitel-Verschmelzung]
status: in-arbeit
prio: P1
art: Wunsch
groesse: L
wo: beides
migration: Kette neu gefaltet (20260804214041_InitialCreate)
vertragsbruch: ja — ChaptersController entfernt, Exercise-Routen auf textbook-series/{}/units/{} verlegt, ExerciseSummary/Detail um SeriesId erweitert, KeyResult-Scope chapterId→seriesUnitId
quelle: Testsitzung 2026-08-04 + PM-Loop-Zyklus (docs/pm-sitzung-2026-08-04.md ff.)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-106 · Übungen hängen künftig am Lehrwerk, nicht am Kapitel

Heute sind Lehrwerk (`TextbookSeries → SeriesUnit`) und Übungskatalog (`Subject → Chapter → Exercise`)
strukturell unverbunden: eine Unit trägt Stoff (Themen/Grammatik/Vokabelnotizen), aber keine einzige
Übung; eine Übung hängt an einem Kapitel, das vom Lehrwerk nichts weiß. Wer als Lehrer zum Stoff einer
konkreten Unit passende Übungen anlegen will, hat dafür keinen strukturellen Anker — nur den Prompt-Text
des KI-Creators, der beides lose zusammenfasst.

## User Story

Als *Creator/Lehrer* möchte ich Übungen direkt aus dem Stoff einer Lehrwerk-Unit erstellen, damit jedes
Kind Übungen zu genau dem Kapitel bekommt, das es im Lehrwerk gerade lernt.

## Ist-Stand am Code

- `Exercise.ChapterId` ist **nicht nullable**
  ([LearnEntities.cs:54](../../backend/Pugling.Api/Models/LearnEntities.cs)); zwölf typisierte
  Übungs-Controller (`VocabularyController`, `ReadingController`, `ClozeController`, `EssaysController`,
  `ListeningController`, `GrammarController`, `MatchingController`, `TranslationController`,
  `BirkenbihlController`, `ArithmeticController`, `ArithmeticDrillController`, `ListController`, alle in
  [ExerciseControllers.cs](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs)) erben
  von `ExerciseControllerBase<TConfig>` und binden `subjectId`/`chapterId` über die geteilte Routenvorlage
  `ExerciseRoutes.Base` (`ExerciseControllers.cs:17`) als Pflicht-Routenparameter
  ([ExerciseControllerBase.cs:92-93](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs)
  `ChapterExists`, `:122-125` `FindAsync`).
- `SeriesUnit` ([CurriculumEntities.cs:51-69](../../backend/Pugling.Api/Models/CurriculumEntities.cs))
  trägt `Topics` (`:63`), `Grammar` (`:65`), `VocabularyNotes` (`:67`) — aber **keine** Exercise-Referenz;
  `SeriesId` ist Pflicht (`:54`), `TextbookSeries.SubjectId` ist optional (`:26`).
- `Textbook` ([AdminEntities.cs:146-182](../../backend/Pugling.Api/Models/AdminEntities.cs)):
  `SeriesId` (`:171`) und `CurrentUnitId` (`:178`) sind beide optional, Freitext (`Title` `:153`,
  `SubjectName` `:155`, `CurrentChapter` `:164`) bleibt ausdrücklich Rückfallebene für unkatalogisierte
  Werke.
- `PlanPosition` referenziert nur `ExerciseId`
  ([PlanPositionEntities.cs:17-129](../../backend/Pugling.Api/Models/PlanPositionEntities.cs), FK `:24-25`)
  — bereits vollständig entkoppelt von Chapter/Subject, kein Zusatzaufwand an dieser Stelle.
- `Klassenarbeit.SubjectId` ist optional
  ([KlassenarbeitEntities.cs:73](../../backend/Pugling.Api/Models/KlassenarbeitEntities.cs)), kein
  Chapter- oder Lehrwerk-Bezug.
- Einziger heutiger Bezug zwischen beiden Welten: `BriefingBuilder.ResolveMaterialAsync`
  ([BriefingBuilder.cs:90-118](../../backend/Pugling.Agent.Creator/Briefing/BriefingBuilder.cs)) fasst
  Chapter und SeriesUnit nur **im Prompt-Text** zusammen (Chapter wird separat in `BuildAsync:24-27`
  aufgelöst) — keine FK, keine strukturelle Verbindung.
- Ownership-Muster: `Exercise` trägt eigene Autorschaft/RWX-Rechte (`AuthorAdultId` + `ExerciseGrant`),
  `TextbookSeries` ein einfaches `OwnerAdultId`
  ([CurriculumEntities.cs:37](../../backend/Pugling.Api/Models/CurriculumEntities.cs)) — **nur auf
  Reihen-Ebene**, `SeriesUnit` selbst trägt kein eigenes Ownership-Feld. Für diese Story unkritisch, weil
  `Exercise` seine Autorschaft unabhängig vom Elternobjekt trägt, aber wichtig für die spätere
  Formulierung: die beiden Muster sind **nicht identisch**, nur auf Reihen-Ebene vergleichbar.
- Frontend: `/vater/katalog` und `/vater/lehrwerke` sind heute getrennte, unverlinkte Bereiche.

Alle Datei:Zeile-Belege sind am 2026-08-04 gegen den aktuellen Code nachgeprüft (drei parallele
Explore-Durchgänge: Katalog-Modell, Lehrwerk-Modell, StudyPlan/Backlog-Struktur).

## Die echte Lücke

Zwei parallele, unverbundene Stoffgliederungen für denselben fachlichen Sachverhalt: der Übungskatalog
(`Subject → Chapter`) und das Lehrwerk (`TextbookSeries → SeriesUnit`). [B-64](B-64-textbook-vs-series.md)
dokumentiert die Trennung heute noch als *bewusste* Entscheidung ohne Bedarf an Verschmelzung — diese
Karte hebt das auf.

## Karte

### Ziel

`Exercise` hängt strukturell an `SeriesUnit` statt an `Chapter`; jede Übung gehört zu genau einer Unit
einer konkreten Reihe (lehrwerk-spezifisch, nicht reihen-übergreifend geteilt). Lernbetrieb setzt ein
katalogisiertes Lehrwerk voraus — Freitext bleibt nur noch Anzeige-Metadatum am `Textbook`, keine
gültige Grundlage für neue Übungen.

### Notizen

Konfliktlage mit Bestandsstorys, damit niemand von einer veralteten Prämisse weiterarbeitet:

- **B-64** widerlegt sich selbst durch diese Karte — „gegenstandslos" heißt hier **verworfen**, aber erst
  nach dem Bau dieser Karte, nicht schon jetzt (B-64 bleibt bis dahin gültig und `geschaetzt`).
- **B-63** bleibt wahrscheinlich unabhängig: sie baut die innere Struktur der Lehrwerk-Reihe um (Verlag,
  Grammatik-Taxonomie, Themen-Liste, Buchtyp, Filter) — nicht die Kapitel-Zuordnung von Übungen. Bei
  Überschneidung (z. B. `SeriesUnit`-Feldtypwechsel) beim jeweiligen Bau gegenprüfen.
- **B-13** (Fach-/Kapitel-Eigentum) verliert ihren Gegenstand, sobald `Chapter` entfällt — ihr
  Owner-Muster (`OwnerAdultId` nach `TextbookSeries`-Vorbild) wandert dann sinngemäß auf `SeriesUnit`.
- **B-19** (KI-Lehrplan-Generator) bräuchte nur eine Parameter-Anpassung (`SeriesUnitId` statt
  `ChapterId`), keinen strukturellen Umbau.

### Außerhalb des Ziels

Der KI-Lehrplan-Generator selbst (B-19), die innere Verlag/Reihen-Struktur (B-63), Frontend-Detailarbeit
über das Zusammenführen von Katalog/Lehrwerke-Bereichen hinaus.

## Offene Punkte

Alle sechs Klärungs-Tickets sind entschieden — Details je Ticket, hier nur das Ergebnis:

1. ~~Welche Rolle behalten `Subject`/`ExerciseCategory` nach der Verschmelzung?~~ → siehe
   Entscheidung 4 ([T-01](karten/B-106/T-01-subject-exercisecategory-rolle.md), entschieden).
2. ~~Grade/SchoolTypes-Dopplung zwischen `Exercise`-Metadaten und Lehrwerk?~~ → keine Dopplung, zwei
   verschiedene Fragen (Spanne vs. Einzelwert), kein Schema-Umbau
   ([T-02](karten/B-106/T-02-grade-schooltypes-dopplung.md), entschieden).
3. ~~Wie wird der Chapter-Altbestand (Übungen, Testdaten, Seed) migriert?~~ → siehe Entscheidung 5
   ([T-03](karten/B-106/T-03-altdaten-migration.md), entschieden).
4. ~~Bekommt `Klassenarbeit` einen `SeriesUnit`-Bezug?~~ → nein, bewusst lehrwerk-agnostisch wie schon
   heute ([T-04](karten/B-106/T-04-klassenarbeiten-bezug.md), entschieden).
5. ~~Wie verschmelzen `/vater/katalog` und `/vater/lehrwerke` im Frontend?~~ → zwei verlinkte, getrennte
   Ansichten (bereits umgesetzt im Notfall-Fix, siehe Verlauf) — deckt sich mit „Außerhalb des Ziels"
   unten ([T-05](karten/B-106/T-05-frontend-konsolidierung.md), entschieden).
6. ~~Wie muss der KI-Creator-Agent (`BriefingBuilder`, `CreatorPipeline`) angepasst werden?~~ →
   entschieden ([T-06](karten/B-106/T-06-ki-creator-agent-anpassung.md)): reiner Parameter-Tausch,
   erzwungen durch den Wegfall von `Chapter`, keine gestalterische Änderung.

## Entscheidungen

1. **Verschmelzen: `SeriesUnit` ersetzt `Chapter` als Pflicht-Anker der `Exercise`.** Begründung: trifft
   die Nutzer-Absicht direkt; ein Nebeneinander zementierte die von B-64 kritisierte Doppelstruktur.
   Kosten: größter Schema-Bruch dieser Karte, alle zwölf Typ-Controller/Routen betroffen,
   `Subject`/`Chapter`/`ExerciseCategory` müssen neu verortet werden ([T-01](karten/B-106/T-01-subject-exercisecategory-rolle.md)).
2. **Lehrwerk-spezifisch: eine Übung gehört zu genau einer Unit einer konkreten Reihe.** Begründung:
   passt zur Kernidee „Stoff aus DEM Lehrwerk des Kindes". Kosten: die heute reihen-übergreifend geteilte
   Übungs-Bibliothek entfällt für neue Übungen.
3. **Katalogisierung wird Pflicht — Freitext bleibt nur noch Anzeige-Metadatum.** Begründung: sonst
   widerspräche sich „Übung hängt immer am Lehrwerk" mit dem heutigen Freitext-Fallback am `Textbook`.
   Kosten: B-64 Akzeptanzkriterium 3 („ein unkatalogisiertes Werk bleibt vollständig eintragbar") gilt
   nach dieser Karte nicht mehr uneingeschränkt für den *Lernbetrieb* — abgefedert durch B-64s eigene
   Entscheidung 2 (leichte Inline-Erzeugung einer Reihe direkt aus dem Anlege-Formular).
4. **`Subject` bleibt bestehen, nur `Chapter` entfällt** ([T-01](karten/B-106/T-01-subject-exercisecategory-rolle.md)).
   `ExerciseCategory` bleibt fach-skopiert; der Bezug `Exercise → Subject` wird transitiv über
   `SeriesUnit.SeriesId → TextbookSeries.SubjectId`. Begründung/Kosten: siehe Ticket-Antwort. Kosten in
   Kurzform: neue Guard Clause „Reihe ohne Fach kann keine Übung tragen", `Subject` bleibt eine dritte,
   chapter-lose Entität.
5. **Seed-Migration mit echten Reihen/Units statt synthetischer Stubs**
   ([T-03](karten/B-106/T-03-altdaten-migration.md)). Englisch bekommt eine echte `TextbookSeries`
   „Green Line 1" (Klett) mit Units passend zu den heutigen Kapiteln; die übrigen drei Fächer je eine
   pauschale Reihe/Unit. Laufende `PlanPosition`s bleiben strukturell unberührt (referenzieren nur
   `ExerciseId`). Begründung/Kosten: siehe Ticket-Antwort.

## Akzeptanzkriterien

Vorläufig — die Karte ist Planung, wird pro Sprint geschärft:

1. Eine neu erstellte Übung verlangt `SeriesUnitId`, keine `ChapterId` mehr.
2. Fachlehrer-Matching (`CreatorProfileService`) funktioniert unverändert ohne `ChapterId`.
3. Ein Kind ohne katalogisiertes Lehrwerk bekommt eine handlungsfähige Fehlermeldung statt eines
   stillen 500ers.

## Verlauf

- **2026-08-04** — angelegt aus der PM-Loop-Recherche (Katalog-Modell, Lehrwerk-Modell,
  StudyPlan/Backlog-Struktur, drei parallele Explore-Durchgänge); drei Grundentscheidungen bereits im
  Dialog mit dem Nutzer gefallen (Verschmelzen, Lehrwerk-spezifisch, Katalogisierung Pflicht). Ab hier
  treibt der Skill `pm-loop` den Prozess in wiederholten, **autonom** durchlaufenen Sprints (Grillen und
  Schätzen ohne Dialog-Runde je Ticket — explizite Nutzerautorisierung 2026-08-04, analog zum bereits
  gelebten Muster bei B-19/B-13).
- **2026-08-04** — PM-Loop-Runde 1: alle drei Stakeholder-Rollen (Creator/Vater/Sohn) live gegen die
  frisch geseedete App befragt (`docs/pm-sitzung-2026-08-04.md`, Abschnitt „Runde:
  Lehrwerkgetriebener Katalog"); zentraler Live-Befund: `creator/textbook-series` und
  `creator/profiles` liefern über die gesamte Seed-Landschaft `[]` — kompletter Kaltstart. T-01 und
  T-03 dadurch gegrillt (Entscheidungen 4–5), da alle drei Rollen unabhängig auf dieselbe Naht
  (Altdaten-Migration) zeigten. Autonom entschieden, Nutzerauftrag 2026-08-04.
- **2026-08-05** — Sprint 1 gebaut: der komplette Schema-Slice (`Exercise.ChapterId`→`SeriesUnitId`,
  `ChaptersController` gelöscht, alle zwölf Typ-Controller auf `textbook-series/{}/units/{}` verlegt,
  Migration neu gefaltet). `pugling-reviewer` fand keinen Blocker, aber einen toten `"chapter"`-String
  in `ObjectiveService.KrScope`, zwei veraltete Kommentare und eine falsche XML-Doku — alle behoben.
  Backend **706/706 grün** (Endpunkt-Abdeckung 258/258), Build sauber. T-06 nebenbei geklärt (siehe
  Ticket): reiner, erzwungener Parameter-Tausch im KI-Creator-Agenten, keine inhaltliche Briefing-Änderung.
- **2026-08-05** — Re-Review gegen die echte laufende App (drei Rollen, live getestet statt vermutet):
  **Creator und Vater fanden das Frontend komplett tot** — die Übungs-Anlage verlangte weiter ein
  „Kapitel" aus einer 404-Route (leeres Pulldown, kein Fehlertext), die Ziel-Etappen-Erstellung schickte
  weiter `chapterId` und scheiterte an jeder Speicherung; Sohn-Seite unberührt (per API + Code-Inspektion
  bestätigt). Da beides ein echter **Funktionsverlust** war (nicht nur ein fehlendes Feature), ist der
  Fix trotz „Backend-only"-Zuschnitt dieses Sprints noch in derselben Runde nachgezogen worden: Vertrag
  neu generiert, Fach→Reihe→Unit-Kaskaden-Picker in `CatalogAdmin`/`VaterExerciseCreate`/`VaterExercises`/
  `ExerciseFilterBar`/`VaterZiele`, Drilldown-Umbenennung in `VaterLernstand`, drei E2E-Specs
  (`freigabe`/`uebungstypen`/`vater-von-null`) nachgezogen, `smoke-checks.sh` repariert (nutzte ebenfalls
  die alte Route). `frontend-reviewer` fand keinen Blocker, vier kleinere Stellen (drei stale
  „Kapitel"-Texte außerhalb des Diffs, ein falsch getippter `chapterId` in `uiTypes.ts`) behoben.
  Endstand: Backend 706/706, Frontend-Build sauber, Vitest 122/122, E2E **24/25** (der eine Rest,
  `full-flow.spec.ts`, ist ein bestätigter Alt-Flake in der Sohn-seitigen Klausur-Animation, null
  Dateiüberschneidung mit diesem Diff, von CI ohnehin nicht gegatet).
- **2026-08-05** — Abnahme dieses Sprints: **Creator signiert** (Übung anlegen funktioniert wieder,
  Fach→Reihe→Unit statt Fach→Kapitel, per API+Code-Inspektion geprüft). **Vater signiert** (Ziel-Etappe
  mit `seriesUnitId`-Scope legt sich an, Antwort trägt `"scope":"seriesUnit"`). **Sohn signiert**
  (Spielweg vollkommen unberührt, Punkte/Combo/Daily-Box korrekt verbucht). **Benannte menschliche
  Prüfung offen:** kein Reviewer hatte in dieser Runde eine echte Browser-Verbindung (Chrome-Anbindung
  war nicht verfügbar) — die neuen Kaskaden-Picker sind nur per direktem HTTP + Code-Lesen geprüft, nicht
  in einem echten Browser geklickt. Empfehlung: einmal `/vater/exercises/neu` und `/vater/kind/{id}/ziele`
  im Browser durchklicken, bevor diese Sprint-Abnahme als vollständig gilt. T-02, T-04 und die tiefere
  Frage von T-05 (echte Verschmelzung von `/vater/katalog` und `/vater/lehrwerke`, über den Notfall-Fix
  hinaus) bleiben offen für den nächsten Sprint.
- **2026-08-05** — Sprint 2 (autonom, ohne Dialog-Gate): die drei verbliebenen Tickets geprüft und
  gegrillt. **T-02** (Grade/SchoolTypes): keine Dopplung — `Exercise.GradeMin/Max`/`SchoolTypes` sind
  Such-Metadaten der geteilten Bibliothek, `SeriesUnit.Grade`/`TextbookSeries.SchoolTypes` beschreiben
  das reale Lehrwerk; unterschiedliche Form (Spanne vs. Einzelwert) bestätigt die unterschiedliche
  Aussage. Kein Code. **T-04** (Klassenarbeit-Bezug): bleibt bewusst lehrwerk-agnostisch — war schon vor
  dieser Karte schon fach-/kapitelübergreifend zulässig, kein Live-Befund verlangt eine Einschränkung.
  Kein Code. **T-05** (tiefere Frage): der Notfall-Fix aus Sprint 1 *ist* bereits die Antwort — „zwei
  verlinkte, getrennte Ansichten" deckt sich mit B-106s eigener Abgrenzung („Frontend-Detailarbeit über
  das Zusammenführen … hinaus" liegt außerhalb des Ziels). Formal entschieden, kein weiterer Code.
  **Damit sind alle sechs Tickets entschieden und alle daraus entstandenen Code-Änderungen liegen im
  Sprint-1-Commit** — kein dritter Sprint mit neuem Code nötig. Einzig offen: die in Sprint 1 benannte
  menschliche Browser-Prüfung der neuen Kaskaden-Picker (Chrome-Anbindung war beide Male nicht
  verfügbar). Status bleibt bewusst `in-arbeit`, nicht `abgenommen`, bis diese Prüfung erfolgt ist.
