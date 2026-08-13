---
tags: [typ/story, status/geschaetzt, bereich/katalog, rolle/creator, rolle/supervisor]
aliases: [Grammatik-Themen als Tags, Grammatik-Taxonomie, Grammatik übungsübergreifend suchen]
status: geschaetzt
prio: P3
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: nein
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
10. **`GrammarTopic.SubjectId` löscht per `Cascade`** — ein Thema stirbt mit seinem Fach, es wandert **nicht**
    in die 409-Sperrliste von [B-144](B-144-fach-loeschen-trifft-reihen-lautlos.md). Begründung: B-144s Linie
    läuft an „kann diese Zeile ohne Fach existieren?", und ein fachloses Grammatik-Thema kann es nicht
    (das Fach ist Pflicht, Entscheidung 4); anders als `KeyResult` und `TimetableEntry`, die B-144 mit 409
    schützt, ist ein Thema außerdem **keine Kind-Habe**, deren Verlust jemandem wehtut. *Kosten:* Wer ein
    Fach löscht, löscht dessen Themen samt aller Zuordnungen an Übungen und Units mit — und die Zuordnung ist
    genau der Wert dieser Story. Ein Fach zu löschen ist damit teurer als heute, ohne dass eine Warnung das
    sagt: der Löschdialog von `CatalogAdmin` zählt fünf Folgen auf (B-144) und müsste eine sechste nennen.
    Das gehört in die Akzeptanzkriterien beim Bauen, nicht in eine Folge-Story. Nachgetragen am 2026-08-13
    auf Entscheid des Nutzers; die Schätzung hatte die Frage als Risiko benannt, weil `SchemaGuardTests`
    dafür eine bewusste Zeile verlangt.
11. **Die Story wird nicht geteilt** — gebaut wird der Umfang, wie er in den Entscheidungen 1–10 steht.
    Begründung: Die benannte Naht (Filter+Facette gegen Vokabular+Zuweisung+Agent) ließe das erste Stück ohne
    sichtbaren Nutzen zurück, und Teilen müsste die neun Grill-Entscheidungen neu schneiden. *Kosten:* `L` an
    der oberen Kante bleibt `L` an der oberen Kante — zeigt der Bau, dass es zu groß ist, greift der
    Teilen-Mechanismus des Bereichs (alte Id auf `verworfen` mit `ersetzt_durch`), und dann ist die Arbeit an
    den Entscheidungen zweimal zu leisten. Bestätigt am 2026-08-13.

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

## Schätzung

**Größe: L** — und zwar an der **oberen Kante**. Die nächste Vergleichsgröße ist
[B-63](B-63-lehrwerk-hierarchie.md) (`L`, Migration + Vertrag): dort wie hier eine neue Katalog-Ebene, eine
Migrationsfaltung, Vertragserweiterungen und beide Oberflächen. Der Unterschied zu B-63 ist, dass hier eine
**dritte** Fläche mitspielt, für die `wo` kein Wort hat: der KI-Creator (Entscheidung 7).

Was die Recherche an der Schätzung geändert hat: Der Agent-Anteil ist **eine** Stelle, nicht fünf — alle fünf
Strategien bauen ihre Nutzlast über den geteilten Helfer
[ExerciseStrategy.Payload](../../backend/Pugling.Agent.Creator/Drafting/ExerciseStrategy.cs) (`:68`). Und
Akzeptanzkriterium 8 braucht **keinen neuen** Test: `CreatorAgentTests.Der_Stoff_der_Unit_steht_im_Prompt()`
(`:461`) ist bereits der Wächter über den Unit-Freitext im Prompt und wird nur erweitert.

- **`migration: ja`** — nachgesehen, nicht vermutet: eine neue Entity plus **zwei** Join-Tabellen
  (Entscheidung 2). Die Kette wird neu gefaltet; `SchemaGuardTests` verlangt danach je eine **bewusste
  Zeile** in `Jeder_Fremdschluessel_Hat_Ein_Abgenommenes_Loeschverhalten` (`:192`) für **sechs** neue
  Fremdschlüssel und einen Eintrag in `Jede_Json_Spalte_Hat_Einen_ValueComparer` (`:403`) für `Synonyms`.
- **`vertragsbruch: nein`** — ebenfalls nachgesehen: `ExercisePayload<TConfig>`
  ([ExerciseAuthoringDtos.cs:12](../../backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs)) endet in
  lauter **optionalen** Parametern, ein angehängtes `GrammarTopicIds` ist damit rein additiv; `ExerciseResponse`
  und die Suchparameter gewinnen nur Felder. Kein bestehendes Feld ändert Namen oder Typ, kein Client muss
  angepasst werden, damit er weiter kompiliert.

**Risiken:**

- **Das Löschverhalten von `GrammarTopic.SubjectId` ist die eine Entscheidung, die diese Schätzung nicht
  allein treffen kann.** Das Fach ist Pflicht (Entscheidung 4), also gibt es genau zwei Wege, und G2 verlangt
  die bewusste Zeile: **Cascade** (das Thema stirbt mit dem Fach) oder **Aufnahme in die 409-Sperrliste** von
  [B-144](B-144-fach-loeschen-trifft-reihen-lautlos.md). Empfehlung: **Cascade** — B-144s Linie läuft an
  „kann diese Zeile ohne Fach existieren?", und ein fachloses Grammatik-Thema kann es nicht; anders als
  `KeyResult`/`TimetableEntry` ist es außerdem **keine Kind-Habe**, deren Verlust jemandem wehtut. Wer anders
  entscheidet, muss B-144s Meldungstext mitziehen.
- **Sechs neue Fremdschlüssel sind die größte G2-Erweiterung seit B-63.** Das Tor ist nach dem Falten bewusst
  kurz rot, bis jede Zeile eine *entschiedene* Löschregel trägt — nicht eine abgeschriebene.
- **`Synonyms` ist eine JSON-Spalte** (Muster
  [InterestTag](../../backend/Pugling.Api/Data/PuglingDbContext.cs) `:338-341`, mit
  `JsonValueComparer.For<List<string>>()`). Ohne Comparer gehen In-Place-Änderungen **still** verloren — Tor
  G7 fängt es, aber nur wenn man es nicht per Hand umgeht.
- **`Label` trägt kein `NOCASE`** — anders als `Publisher.Name` (`:222`) und `TextbookSeries.Name`. Die
  Idempotenz aus Akzeptanzkriterium 1 hängt darum am **Slug**, und `InterestSlug.From`
  (`SlugExtensions.cs:26`) faltet Groß-/Kleinschreibung und Diakritika ohnehin zusammen. Damit trägt der
  Slug die Dublettenabwehr, und das ist ausreichend — aber es ist genau die Lücke, die
  [B-141](B-141-interest-tag-label-dublette.md) für `InterestTag` offen führt. Wird B-141 anders entschieden,
  zieht diese Story nach.
- **Der PUT ist Vollersatz, und das ist der wahrscheinlichste Regressionsweg** (Akzeptanzkriterium 7):
  derselbe `ExercisePayload` dient POST *und* PUT. Ein Bearbeiten-Dialog, der die Themen nicht mitschickt,
  **löscht** sie — wörtlich die Klasse von [B-148](B-148-lehrbuch-formular-zerstoert-fachnamen.md) („das
  Lehrbuch-Formular zerstört den Fachnamen bei jedem Speichern") und der Grund, warum `frontend/CLAUDE.md`
  den Rückweg zur Pflicht macht.
- **Die Vorbelegungs-Regel lebt in zwei Clients und wird serverseitig nicht erzwungen** (Entscheidung 2:
  vorbelegen, nicht vererben). Formular und Agent müssen sie beide tragen; ein dritter Client könnte
  fälschlich vererben, ohne dass etwas widerspricht. Bewusste Lücke der Entscheidung, hier benannt.
- **Der Seed-Grundbestand ist eine inhaltliche Meinung** (Entscheidung 6) und in einer *bestehenden* DB
  danach weder umbenennbar noch löschbar (Entscheidung 3, fail-closed). Für dieses Projekt billig, weil
  Datenbanken weggeworfen werden — für eine Produktionsinstanz nicht.

**Angriffsplan** (Backend zuerst, dann Agent, dann Frontend):

1. `Models/CurriculumEntities.cs`: `GrammarTopic` (`Slug`, `Label`, `Synonyms`, `Color?`, `SubjectId`,
   `OwnerAdultId?`) plus die zwei Join-Entities. Dort, weil diese Datei den **geteilten, slug-idempotenten
   Katalog** trägt (`Publisher` `:13`, `TextbookSeries` `:29`, `SeriesUnit` `:67`) — nicht in
   `LearnEntities.cs`, die die übungsnahen Strukturen hält.
2. `PuglingDbContext.OnModelCreating`: Unique-Index auf `Slug` (fachqualifiziert, Entscheidung 4), String-Längen
   aus der Konventionsschleife, `Synonyms` mit `ValueComparer`, die sechs FKs mit entschiedenem Verhalten;
   Join-Tabellen nach dem `ChildInterest`-Muster (`:345-350`, beide Cascade, kein SQLite-Diamant).
   Migrationskette neu falten, Snapshot-Diff als Abnahme.
3. `Pugling.Contracts/Creator`: `GrammarTopicResponse`/`CreateGrammarTopicDto`/`UpdateGrammarTopicDto`
   (Namen global eindeutig halten), `GrammarTopicIds` additiv an `ExercisePayload`, Themen additiv an
   `ExerciseResponse`/`ExerciseSummary` und `SeriesUnitResponse`, `grammarTopicId` an den Suchparametern.
4. `GrammarTopicsController` unter `api/v1/creator/grammar-topics`: POST idempotent über den Slug (Muster
   `InterestTagsController` `:88-91`), Slug unveränderlich (`:110`), `PATCH`/`DELETE` owner-gegated nach dem
   **B-13-Muster** — inklusive der Reihenfolge *Eigentum vor Verwendung*, die
   `FachEigentumTests.FremderCreator_BekommtNotOwner_AuchWennDasFachBenutztIst` seit dem 2026-08-12 hält.
   Neuer `ApiErrors`-Code für „Thema in Benutzung".
5. Zuweisung an Übung und Unit inklusive **Fachgrenze** → `validation_error` (Muster „Unit muss zur Reihe
   gehören", `TextbooksController`).
6. `ExerciseCatalogController`: `grammarTopicId` als achte Facette (`:48-53` Parameter, Bedingung neben
   `:89-98`); `X-Total-Count`, Paging und Sortierung unverändert.
7. `Data/Seed.cs`: Grundbestand je Sprachfach, ohne Owner.
8. Agent: `ExerciseStrategy.Payload` (`:68`) übernimmt die Themen der Unit — **eine** Stelle für alle fünf
   Strategien.
9. `SchemaGuardTests`: die sechs G2-Zeilen und der G7-Eintrag.
10. Backend-Tests (siehe Testweg), dann `dotnet build Pugling.sln` wegen der Vertragsänderung.
11. Frontend: Themen-Chips im Unit-Formular (`VaterLehrwerke.tsx:603`-Umgebung), Zuweisung im
    Anlege-Formular **und** im Bearbeiten-Dialog mit mitgeschickten Themen (Risiko oben), Facette in
    `ExerciseFilterBar.tsx` (`:59-112`), die als Query mitreist; `lib/api.ts`-Methoden.
12. `Pugling.Client`: je eine einzeilige Methode für die neuen Endpunkte (kein HTTP-Plumbing duplizieren).

**Testweg**:

- **Neu `backend/Pugling.Api.Tests/GrammatikThemenTests.cs`**: Idempotenz des POST (AK 1); Owner-Gate mit
  fremdem Creator, Seed-Thema und *benutztem* Thema (AK 2); Slug-Unveränderlichkeit; Fachgrenze (AK 3);
  Filter über **zwei Reihen und zwei Klassenstufen** (AK 4). Zweiter Creator-Client nach dem Muster von
  `FachEigentumTests.ZweiterCreatorAsync`.
- **`SchemaGuardTests`**: Kettenlänge 1, die sechs FK-Zeilen, der `Synonyms`-Comparer, String-Längen.
- **`CreatorAgentTests`**: `Der_Stoff_der_Unit_steht_im_Prompt()` (`:461`) erweitern für AK 8 (der Freitext
  bleibt im Briefing), dazu ein neuer Fall für AK 6 (der Agent taggt aus der Unit) — mit `FakeChatClient`,
  ohne Ollama.
- **Frontend**: die Vorbelegungs-Regel als **reine Funktion** mit Test (Vorbild `seriesDerivation.ts`), damit
  AK 5 nicht nur im Bildschirm hängt; dazu ein Komponententest auf den Rückweg des Bearbeiten-Dialogs
  (Vorbild `textbookPatch.test.ts`, das genau die B-148-Falle hält).
- **Neu `frontend/e2e/grammatik-themen.spec.ts`** für AK 10 — eigene Datei, weil eine Spec beim ersten Rot
  alles Nachfolgende mitnimmt (`frontend/CLAUDE.md`, B-109).
- **`/smoke-test`** als Abschluss-Check gegen eine laufende Instanz.

**Zur Größe, ausdrücklich:** Die Story ist an der oberen Kante von `L`, und der Bereich kennt kein `XL`
(dann wird geteilt). Eine Naht liegt bereit — Entscheidung 5 hat sie halb gezogen, als sie die
Vergleichs-Sicht (b) ausgelagert hat: man könnte zusätzlich **den Filter samt Facette** (AK 4, 7-Teil,
10) von **Vokabular, Zuweisung und Agent** (AK 1–3, 5, 6, 8, 9) trennen. Ich habe **nicht** geteilt, weil
das erste Stück dann ohne sichtbaren Nutzen wäre und weil das Teilen die neun Entscheidungen der Grill-Runde
neu schneiden müsste — das ist eine Entscheidung des Menschen, nicht der Schätzung. Wenn der Bau zeigt, dass
es zu groß ist, ist der Weg im README beschrieben (alte Id auf `verworfen` mit `ersetzt_durch`).

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
- **2026-08-13** — `gegrillt → geschaetzt`. `L` / `beides` / `migration: ja` / `vertragsbruch: nein`, beide
  Flags **nachgesehen** statt vermutet: `ExercisePayload<TConfig>` endet in lauter optionalen Parametern
  (additiv, kein Bruch), und die Migration bringt sechs Fremdschlüssel plus eine JSON-Spalte, also je eine
  bewusste Zeile in G2 und G7. Zwei Annahmen hat die Recherche dabei korrigiert: der Agent-Anteil ist **eine**
  Stelle (`ExerciseStrategy.Payload:68`, geteilt von allen fünf Strategien), und AK 8 braucht **keinen** neuen
  Test — `CreatorAgentTests.Der_Stoff_der_Unit_steht_im_Prompt():461` ist schon der Wächter. Die eine
  Entscheidung, die die Schätzung nicht selbst treffen kann, ist als Risiko benannt: das Löschverhalten von
  `GrammarTopic.SubjectId` (Cascade oder Aufnahme in B-144s 409-Sperrliste). Größe an der oberen Kante von
  `L`, mit einer benannten Naht und der Begründung, warum **nicht** geteilt wurde.
