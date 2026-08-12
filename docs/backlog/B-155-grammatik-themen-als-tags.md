---
tags: [typ/story, status/ausformuliert, bereich/katalog, rolle/creator, rolle/supervisor]
aliases: [Grammatik-Themen als Tags, Grammatik-Taxonomie, Grammatik übungsübergreifend suchen]
status: ausformuliert
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
  (`- Grammatik der Unit: {Unit.Grammar}`). Kein Suchpfad, kein Filter, keine Auswertung.
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
  Schulart, Typ, Art, Freitext).

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
- `InterestTag` ([InterestEntities.cs:16](../../backend/Pugling.Api/Models/InterestEntities.cs)) ist das
  Vorbild für „slug-idempotent, geteilt, jeder darf verwenden".

**Eine grobe Grammatik-Achse gibt es bereits — eine Ebene zu grob.**

- `ExerciseCategory` („Art") hängt am **Fach** ([LearnEntities.cs:40-46](../../backend/Pugling.Api/Models/LearnEntities.cs))
  und trägt im Seed genau die Werte `Vokabeln` / `Grammatik` / `Leseverstehen`
  ([Seed.cs:997-999](../../backend/Pugling.Api/Data/Seed.cs)). Filterbar ist sie schon (`categoryId`,
  UI „Art-Filter", `ExerciseFilterBar.tsx:104`).
- Auf Aufgaben-Ebene trägt außerdem jede Grammatik-Aufgabe einen optionalen Regelhinweis als Freitext:
  `GrammarTask(Prompt, Answer, RuleHint)` — [ExerciseConfigs.cs:124](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs).

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

1. **Woran hängt das Thema — Unit, Übung oder beides?**
   *Empfehlung: beides, aber asymmetrisch.* Die Übung trägt es (dort landet die Suche), die Unit trägt es
   zusätzlich für den Vergleich (b) — siehe Lücke 4. Beim Anlegen einer Übung werden die Themen ihrer Unit
   **vorbelegt, nicht abgeleitet**: eine Ableitung machte jede Übung einer gemischten Unit fälschlich zu
   „present perfect". *Kosten:* zwei Join-Tabellen statt einer und eine Vorbelegungsregel im Anlege-Formular.
2. **Skopierung: global geteilt oder je Creator?**
   *Empfehlung: global geteilt und slug-idempotent, Muster `InterestTag`.* Eine Liste je Creator macht den
   Vergleich zwischen Lehrwerken unmöglich — das ist der halbe Zweck der Story. *Kosten:* Wildwuchs und
   Dubletten („Present Perfect" / „present perfect") werden zur echten Gefahr; der Slug fängt die
   Schreibweise, nicht die Synonymie. Wer umbenennen darf, braucht das Owner-Muster von `Subject` (B-13).
3. **Trägt ein Thema ein Fach?**
   *Empfehlung: ja, optional* (Muster `ExerciseCategory`, aber `SubjectId` nullable). „present perfect" ist
   englisch; ohne Fachbezug zeigt die Auswahlliste am Englisch-Werk auch die Deutsch-Themen. *Kosten:* ein
   Thema, das in zwei Fächern gilt („Konjunktiv"), braucht zwei Einträge oder ein leeres Fach — beides
   erklärbar, aber nicht schön.
4. **`VocabTag` erweitern oder eine eigene Tabelle?**
   *Empfehlung: eigene Tabelle.* `VocabTag` heißt und bedeutet „Vokabel-Schlagwort"; ihm eine dritte
   Bedeutung zu geben ist genau die Begriffs-Drift, die dieses Repo schon zweimal teuer bezahlt hat
   (`Father`→`Adult`, Lernziel→`KeyResult`). *Kosten:* eine vierte Tag-Tabelle im Repo — die Alternative
   wäre ein generischer `CatalogTag` mit `Kind`-Spalte, der aber die drei bestehenden Tabellen erst dann
   aufräumt, wenn man sie migriert, und das ist eine eigene Story.
5. **Gehört die Vergleichs-Sicht (b) in diese Story?**
   *Empfehlung: nein — Datenmodell ja, Bildschirm nein.* Der Filter (a) ist der Nutzen, der sofort trägt;
   die Vergleichs-Sicht wird eine Folge-Story, sobald Themen an Units hängen. *Kosten:* (b) bleibt liegen,
   und das Datenmodell wird für einen Nutzen gebaut, der noch keine Oberfläche hat.
6. **Sieht der Student das Thema?**
   *Empfehlung: zurückstellen.* Es ist kein Lösungsfeld, also unschädlich — aber es ist auch kein
   erkennbarer Gewinn für das Kind, und jede zusätzliche Nutzlast an der Positions-Ansicht will begründet
   sein.

## Akzeptanzkriterien

Entwurf — 6 hängt an Entscheidung 1, die Formulierung von 2 an Entscheidung 3.

1. Ein Grammatik-Thema lässt sich anlegen und ist **idempotent über den Slug**: ein zweites Anlegen mit
   demselben Namen liefert das bestehende Thema zurück, statt eine Dublette in den geteilten Katalog zu
   schreiben.
2. Eine Übung trägt 0..n Themen. `GET api/v1/creator/exercises?grammarTopicId=` liefert genau die Übungen
   mit diesem Thema — **über Fächer, Reihen und Klassenstufen hinweg** —, mit unverändertem
   `X-Total-Count`, Paging und Sortierung.
3. Der Filter ist im Vater-Web als weitere Facette in `ExerciseFilterBar` bedienbar und **reist als Query
   mit** (Frontend-Konvention „eine Auswahl reist als Query").
4. Das Freitextfeld „Grammatik der Unit" existiert unverändert und geht unverändert in das Briefing des
   KI-Creators (`ProfileFacts.cs:63`) — belegt durch einen Test, der das Briefing prüft.
5. Ein E2E fährt den Nutzen: Thema anlegen → zwei Übungen in **verschiedenen Reihen und Klassenstufen**
   damit versehen → über den Filter beide finden, und eine dritte ohne das Thema nicht.
6. Eine Unit trägt 0..n Themen, und für ein Thema ist je Reihe die Klassenstufe abfragbar, in der es
   vorkommt (Grundlage der Vergleichs-Sicht; ohne eigenen Bildschirm).

## Verlauf

- **2026-08-12** — angelegt (Quelle: Nutzer-Dialog beim Anlegen einer Übung über den neuen
  Lehrwerk-Weg). Prio P3 vorgeschlagen, noch nicht bestätigt.
- **2026-08-12** — `idee → ausformuliert`. Recherche verschob die Lücke: Übungen sind längst taggbar
  (`ExerciseTag`), aber `Tag.ChildId` ist nicht nullable — das Problem ist die **Skopierung**, nicht ein
  fehlendes Feld. Dazu gefunden: die grobe Achse „Art = Grammatik" existiert schon (`ExerciseCategory`,
  Seed), ein kindneutrales geteiltes Tag-Vokabular auch (`VocabTag`) — nur zeigt es auf `Vocabulary`.
  Sechs offene Punkte je mit Empfehlung; Prio bleibt P3.
