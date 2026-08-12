---
tags: [typ/story, status/gegrillt, bereich/katalog, rolle/creator, rolle/supervisor]
aliases: [Grammatik-Themen als Tags, Grammatik-Taxonomie, Grammatik übungsübergreifend suchen]
status: gegrillt
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nutzer-Dialog 2026-08-12 (Sitzung zum Lehrwerk-Weg, Commit 4876e5a)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-155 · Grammatik als Thema, nicht als Freitext

Die Grammatik einer Unit ist heute ein Freitextfeld — gut für den KI-Creator, der es liest, aber blind für
jede Frage, die über *diese* Unit hinausgeht: „Present perfect vs. simple past" und „present perfect" sind
für jede Suche zwei Dinge.

## User Story

Als **Supervisor** möchte ich Übungen nach einem Grammatik-Thema suchen können, **quer über Lehrwerke und
Klassenstufen**, damit ich einen Lehrplan zu einem Thema füllen kann, ohne zu wissen, in welcher Unit
welchen Werks es steckt.

Als **Creator** möchte ich sehen, **welches Lehrwerk dasselbe Grammatik-Thema wann bringt**, damit ich
Reihenfolge und Klassenstufe meines Materials mit anderen Werken vergleichen kann.

## Ist-Stand am Code

**Der Stoff der Unit ist reiner Freitext, und genau ein Verbraucher liest ihn.**

- `SeriesUnit.Grammar` ist `string?` — [CurriculumEntities.cs:83](../../backend/Pugling.Api/Models/CurriculumEntities.cs),
  Nachbarn `Topics` (`List<string>`, :81) und `VocabularyNotes` (:85). CRUD über
  [SeriesUnitsController.cs:62](../../backend/Pugling.Api/Controllers/Creator/SeriesUnitsController.cs)
  (Anlegen, Eigentumsprüfung :66).
- Gelesen wird `Grammar` außerhalb von CRUD **an einer einzigen Stelle**: dem Briefing des KI-Creators,
  [ProfileFacts.cs:63](../../backend/Pugling.Agent.Creator/Briefing/ProfileFacts.cs)
  (`- Grammatik der Unit: {Unit.Grammar}`; die Zeile darüber tut dasselbe für `Topics`). Kein Suchpfad,
  kein Filter, keine Auswertung.
- Im Vater-Web ist es ein `<input>` im Unit-Formular,
  [VaterLehrwerke.tsx:603](../../frontend/src/vater/VaterLehrwerke.tsx) („Grammatik der Unit"), angezeigt
  als Zeile „Grammatik: …" in der Unit-Tabelle (:434).

**Die Übungssuche kennt sieben Facetten — keine davon ist ein Thema.**

- [ExerciseCatalogController.cs:48-53](../../backend/Pugling.Api/Controllers/Creator/ExerciseCatalogController.cs):
  `subjectId`, `seriesUnitId`, `grade`, `schoolType`, `categoryId`, `type`, `search`, `source`, `mineOnly`
  plus Sortierung/Paging. Der Freitext `search` läuft per LIKE **nur über `Title` und `Description`**
  (:89-90), `source` über `Exercise.Source` (:98). `SeriesUnit.Grammar` ist in **keiner** dieser Bedingungen.
- Dasselbe im Frontend als Facetten-Leiste:
  [ExerciseFilterBar.tsx:59-112](../../frontend/src/vater/ExerciseFilterBar.tsx) (Fach, Reihe, Unit, Klasse,
  Schulart, Typ, Art, Freitext) — alle Facetten sind **einwertig**.

**Übungen *sind* taggbar — aber die Tags sind kind-gebunden.**

- `Tag` trägt eine **nicht-nullable** `ChildId` — [KlassenarbeitEntities.cs:18-21](../../backend/Pugling.Api/Models/KlassenarbeitEntities.cs),
  Name je Kind eindeutig; `ExerciseTag` verbindet ihn mit einer Übung (:35). Verwendet wird das heute für
  die Relevanz einer Klassenarbeit (`ExerciseCatalogController.cs:227`).
- Damit kann ein solcher Tag ein Thema **nicht** ausdrücken: „present perfect" wäre je Kind ein eigener
  Datensatz, und ein Vergleich über Kinder, Bücher oder Creator hinweg ist nicht formulierbar.
- Im Frontend hängen diese Tags nur am **Vokabelspeicher** ([VaterVocab.tsx:111](../../frontend/src/vater/VaterVocab.tsx)
  über `api.childTags`); für Übungen gibt es keinen Bildschirm, nur die Endpunkte.

**Ein kindneutrales, geteiltes Tag-Vokabular existiert schon — aber nur für Vokabeln.**

- `VocabTag` (global eindeutiger Name) + `VocabTagLink` — [VocabEntities.cs:67-87](../../backend/Pugling.Api/Models/VocabEntities.cs).
  Der Link zeigt auf `Vocabulary`, nicht auf `Exercise` oder `SeriesUnit`.
- `InterestTag` ([InterestEntities.cs:16-38](../../backend/Pugling.Api/Models/InterestEntities.cs)) ist das
  reifere Vorbild: `Slug` (stabil, global eindeutig) + `Label` + **`Synonyms`** als JSON-Liste, ausdrücklich
  damit „dasselbe Interesse nicht als mehrere Tags endet". Gepflegt wird es von **jedem** Creator ohne
  Eigentümer-Prüfung ([InterestTagsController.cs:23](../../backend/Pugling.Api/Controllers/Creator/InterestTagsController.cs)),
  POST ist idempotent über den Slug (:88-91), der Slug selbst ist unveränderlich (:110).
- Die Normalisierung liegt in `InterestSlug.From` (`SlugExtensions.cs:26`): Groß-/Kleinschreibung und
  Diakritika fallen zusammen — Schreibweisen-Drift ist damit gefangen, echte Synonymie nicht.

**Eine grobe Grammatik-Achse gibt es bereits — eine Ebene zu grob.**

- `ExerciseCategory` („Art") hängt am **Fach**, `SubjectId` ist **nicht** nullable
  ([LearnEntities.cs:40-46](../../backend/Pugling.Api/Models/LearnEntities.cs)) und trägt im Seed genau die
  Werte `Vokabeln` / `Grammatik` / `Leseverstehen`
  ([Seed.cs:997-999](../../backend/Pugling.Api/Data/Seed.cs)). Filterbar ist sie schon (`categoryId`,
  UI „Art-Filter", `ExerciseFilterBar.tsx:104`).
- Geseedete Fächer: Französisch, Englisch, Mathe, Erdkunde (`Seed.cs:607/1000/1004/1006`).
- Eine Übung kann ohnehin nur in einer Reihe **mit** Fach entstehen (`ExerciseControllerBase.cs:245-247`,
  `series_without_subject`) — am Ort der Benutzung gibt es also immer ein Fach.
- Auf Aufgaben-Ebene trägt außerdem jede Grammatik-Aufgabe einen optionalen Regelhinweis als Freitext:
  `GrammarTask(Prompt, Answer, RuleHint)` — [ExerciseConfigs.cs:124](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs).
- Angelegt wird über `ExercisePayload<TConfig>`
  ([ExerciseAuthoringDtos.cs:12](../../backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs)) — derselbe
  Vertrag für POST **und** PUT, und PUT ist Vollersatz.

## Die echte Lücke

Sie ist **schmaler** als „Grammatik ist nicht kategorisierbar" und liegt an einer anderen Stelle als die
Idee vermutete:

1. **Zwischen der groben `Art` („Grammatik") und dem Unit-Freitext fehlt die Themen-Ebene.** Die eine ist
   fachgebunden und dreiwertig, der andere ist unvergleichbar. „present perfect" hat heute keinen Ort.
2. **Der einzige themenartige Träger an einer Übung ist kind-gebunden** (`Tag.ChildId` ist nicht nullable).
   Für eine Suche „über Bücher und Klassen hinweg" ist das strukturell das falsche Objekt — nicht ein
   fehlendes Feld, sondern eine falsche Skopierung.
3. **Die Suche erreicht die Übung, aber nichts an ihrer Unit.** Selbst wenn das Thema *nur* an der Unit
   hinge, käme die Übungssuche nicht daran: `ExerciseCatalogController` filtert über `SeriesUnitId` und
   transitiv über `Series.SubjectId`, aber über kein inhaltliches Feld der Unit.
4. **Der Vergleich (b) braucht einen Träger an der Unit**, nicht an der Übung: die Frage „in welcher
   Klassenstufe bringt Werk X das Thema?" muss auch für eine Unit beantwortbar sein, zu der noch niemand
   eine Übung gebaut hat.

Nicht die Lücke: das Freitextfeld. Es hat mit dem Briefing des KI-Creators einen echten, belegten
Verbraucher (`ProfileFacts.cs:63`) und bleibt.

## Offene Punkte

Alle in der Grill-Runde vom 2026-08-12 geschlossen (Nummern zeigen auf die Entscheidungen).

1. ~~Woran hängt das Thema — Unit, Übung oder beides?~~ → Entscheidung 2.
2. ~~Skopierung: global geteilt oder je Creator?~~ → Entscheidungen 3 und 4.
3. ~~Trägt ein Thema ein Fach?~~ → Entscheidung 4. Die Empfehlung der Ausformulierung („optional") wurde
   dabei **revidiert**: Pflicht.
4. ~~`VocabTag` erweitern oder eine eigene Tabelle?~~ → Entscheidung 1 (eigene Tabelle, nur Grammatik).
5. ~~Gehört die Vergleichs-Sicht (b) in diese Story?~~ → Entscheidung 5 (nein, Datenmodell ja).
6. ~~Sieht der Student das Thema?~~ → Entscheidung 9 (zurückgestellt).

In der Runde **neu aufgetaucht** und mitentschieden: der KI-Creator (Entscheidung 7) und die Wertigkeit des
Filters (Entscheidung 8).

## Entscheidungen

1. **Die neue Achse gilt nur für Grammatik und ist ein eigenes Objekt** (`GrammarTopic`, UI
   „Grammatik-Thema"). Grammatik ist die eine Achse, bei der ein geteiltes Vokabular konvergiert: die Menge
   ist endlich und lehrbuchunabhängig. Freie Themen („Growing up") sind werksspezifisch und treffen sich
   nie — sie zu kuratieren kostet Pflege ohne Treffer. `SeriesUnit.Topics` und `SeriesUnit.Grammar` bleiben
   **unverändert** Freitext. *Kosten:* zwei Dinge heißen „Thema", unterschieden nur durch das Beiwort; der
   distinkte Klassenname muss das tragen. Und ein generischer `CatalogTag` mit `Kind`-Spalte, der die drei
   bestehenden Tag-Tabellen aufräumen würde, ist damit ausgeschlossen (wäre eine eigene Story).
2. **Träger sind Übung *und* Unit, asymmetrisch.** Die Übung trägt das Thema, weil dort die Suche landet;
   die Unit trägt es zusätzlich, weil sonst „wann bringt Werk X das Passiv?" für eine Unit ohne Übungen
   unbeantwortbar bliebe (Lücke 4). Beim Anlegen einer Übung werden die Themen ihrer Unit **vorbelegt, nicht
   vererbt** — eine Vererbung machte jede Übung einer gemischten Unit fälschlich zu „present perfect".
   *Kosten:* zwei Join-Tabellen statt einer, und die Vorbelegung ist eine Regel mit eigenem Test.
3. **Pflege nach dem Owner-Muster von `Subject`** (B-13): anlegen darf jeder Creator, ändern und löschen nur
   der Anleger; ein geseedetes Thema ist für **niemanden** änderbar (fail-closed). Der Slug ist
   unveränderlich (Muster `InterestTag`), `Label`/`Synonyms`/`Color` sind pflegbar; gelöscht wird nur, was
   niemand benutzt. Begründung: das Thema trägt Suchsemantik in *fremden* Lehrplänen — wer es umbenennt,
   verschiebt die Bedeutung für andere. *Kosten:* ein Tippfehler in einem fremden Thema ist nicht
   reparierbar, man kann nur ein zweites anlegen; das offene Modell von `InterestTag` wird damit **nicht**
   fortgeschrieben, es gibt künftig zwei Pflege-Lesarten im Katalog.
4. **Das Fach ist Pflicht**, der Slug fachqualifiziert (`en-present-perfect`). Ein Grammatik-Phänomen ist
   sprachgebunden, und am Ort der Benutzung liegt immer ein Fach vor (`series_without_subject`). Ein
   nullables Fach fügte einen dritten Zustand hinzu („noch nicht gesetzt" vs. „gilt für alle"), den der
   Filter interpretieren müsste. *Kosten:* ein sprachübergreifendes Konzept („Konjunktiv" in Deutsch und
   Latein) braucht zwei Einträge, und ein Vergleich über Fachgrenzen ist nicht ausdrückbar.
5. **In dieser Story steckt nur der Filter (a).** Gebaut werden Datenmodell (beide Träger), Zuweisung im UI
   und der Filter an der Übungssuche; die Vergleichs-Sicht (b) bekommt **keinen** Bildschirm und wird eine
   Folge-Story mit `quelle: B-155`. Begründung: der Filter trägt den Nutzen sofort, die Story bleibt
   schätzbar, und eine Vergleichs-Ansicht lässt sich erst entwerfen, wenn echte Themen an echten Units
   hängen. *Kosten:* (b) bleibt liegen, und das Unit-Träger-Modell wird für einen Nutzen gebaut, der noch
   keine Oberfläche hat.
6. **Ein kleiner Grundbestand kommt in den Seed**, je Sprachfach und owner-los (Englisch: present perfect,
   simple past, Passiv, Relativsätze, if-clauses …; Französisch: passé composé, imparfait, subjonctif …;
   Mathe/Erdkunde nichts). Sonst erfindet der erste Nutzer die kanonischen Namen — genau der
   Konvergenzgewinn, um den es geht. Die Unveränderlichkeit aus Entscheidung 3 ist hier billig, weil dieses
   Projekt Datenbanken wegwirft (die Kette wird neu gefaltet). *Kosten:* eine inhaltliche Meinung im Seed,
   und in einer *bestehenden* DB ist ein Seed-Tippfehler weder umbenennbar noch löschbar.
7. **Der KI-Creator übernimmt die Themen seiner Unit** — dieselbe Vorbelegungsregel wie im Formular, nur
   ohne Mensch, der abwählt. Er ist der produktivste Autor im System; erzeugte er ungetaggtes Material, wäre
   die Suche gerade dort blind, wo der Bestand wächst, und niemand taggt hunderte Übungen nachträglich.
   *Kosten:* in einer gemischten Unit taggt der Agent breiter als nötig (die Passiv-Übung trägt auch
   „present perfect") — dieselbe Ungenauigkeit, die das Formular durch Abwählen vermeidet, hier ohne
   Korrektiv. Das Modell selbst entscheidet **nicht** mit; die deterministische Pipeline bekommt keine
   zusätzliche Modell-Entscheidung.
8. **Der Filter ist einwertig** (`?grammarTopicId=`), wie alle sieben bestehenden Facetten
   (`ExerciseFilterBar.tsx:59-112`). *Kosten:* „present perfect ODER simple past" braucht zwei Suchen; eine
   Mehrfachauswahl wäre ein eigener Wunsch — dann aber für alle Facetten, nicht nur für diese.
9. **Zurückgestellt: der Student sieht das Thema nicht.** Es ist kein Lösungsfeld, also unschädlich, aber es
   gibt keinen belegten Gewinn für das Kind. Nachträglich ist es ein additives Feld, also kein Bruch.
   *Kosten:* die Frage kommt wieder, sobald jemand „was lerne ich hier eigentlich?" in der Arcade
   beantworten will — dann als eigene kleine Story.

## Akzeptanzkriterien

1. **Anlegen ist idempotent:** `POST` eines Themas mit gleichem Label und Fach liefert das bestehende
   zurück (gleicher Slug), statt eine Dublette oder einen 409 zu erzeugen.
2. **Pflege ist owner-gegated:** `PATCH`/`DELETE` eines fremden Themas → `403 not_owner`; ein geseedetes
   Thema → `403` auch für den Seed-Vater; `DELETE` eines *benutzten* Themas wird mit eigenem Fehlercode
   abgewiesen, statt Verknüpfungen zu entfernen. Der Slug ist über `PATCH` nicht änderbar.
3. **Fachgrenze hält:** einer Übung ein Thema eines *anderen* Fachs zuzuweisen endet als
   `validation_error` (Muster „Unit muss zur Reihe gehören"), nicht als stiller Treffer.
4. **Der Filter findet über Werke hinweg:** `GET api/v1/creator/exercises?grammarTopicId=` liefert genau die
   Übungen mit diesem Thema — über Reihen und Klassenstufen hinweg —, mit unverändertem `X-Total-Count`,
   Paging und Sortierung.
5. **Vorbelegung statt Vererbung:** eine Übung in einer Unit mit zwei Themen erscheint mit beiden als
   Vorschlag; wird einer abgewählt, hängt am Ende **nur** der gewählte an der Übung (und die Unit behält
   beide).
6. **Der KI-Creator taggt:** eine vom Agenten in einer Unit mit Themen angelegte Übung trägt diese Themen
   (Test in den Agent-Tests, nicht nur im Backend).
7. **UI beidseitig** (Frontend-Konvention „Hin- und Rückweg"): Zuweisen im Anlege-Formular **und** im
   Bearbeiten-Dialog, wobei der PUT die Themen mitschickt (sonst löscht der Vollersatz sie); Themen-Chips im
   Unit-Formular; Facette in der `ExerciseFilterBar`, die als Query mitreist.
8. **Der Freitext bleibt unberührt:** „Grammatik der Unit" existiert unverändert und geht unverändert ins
   Briefing (`ProfileFacts.cs:63`) — belegt durch einen Test auf das Briefing.
9. **Der Seed trägt den Grundbestand:** eine frische DB zeigt je Sprachfach die Themen in der Auswahl, und
   sie sind für niemanden änderbar.
10. **E2E fährt den Nutzen:** Thema anlegen → zwei Übungen in **verschiedenen Reihen und Klassenstufen**
    damit versehen → über den Filter beide finden, eine dritte ohne das Thema nicht.

## Verlauf

- **2026-08-12** — angelegt (Quelle: Nutzer-Dialog beim Anlegen einer Übung über den neuen
  Lehrwerk-Weg). Prio P3 vorgeschlagen, noch nicht bestätigt.
- **2026-08-12** — `idee → ausformuliert`. Recherche verschob die Lücke: Übungen sind längst taggbar
  (`ExerciseTag`), aber `Tag.ChildId` ist nicht nullable — das Problem ist die **Skopierung**, nicht ein
  fehlendes Feld. Dazu gefunden: die grobe Achse „Art = Grammatik" existiert schon (`ExerciseCategory`,
  Seed), ein kindneutrales geteiltes Tag-Vokabular auch (`VocabTag`) — nur zeigt es auf `Vocabulary`.
  Sechs offene Punkte je mit Empfehlung; Prio bleibt P3.
- **2026-08-12** — `ausformuliert → gegrillt`. Neun Entscheidungen im Dialog; alle sechs offenen Punkte
  geschlossen, zwei Fragen kamen in der Runde neu hinzu (KI-Creator, Wertigkeit des Filters). Eine
  Empfehlung der Ausformulierung wurde dabei am Code widerlegt: „Fach optional" → **Pflicht**, weil
  `ExerciseCategory.SubjectId` nicht nullable ist und eine Übung ohnehin ein Fach voraussetzt. Prio bleibt
  P3, weiter unbestätigt.
