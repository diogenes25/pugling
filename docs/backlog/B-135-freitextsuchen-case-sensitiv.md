---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/katalog]
aliases: [Sieben Suchen ohne Schreibweisen-Toleranz, instr statt LIKE, SearchPattern ausrollen]
status: abgenommen
prio: P3
art: Defekt
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-128 (Nachtlauf 2026-08-09, Entscheidung 4)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-135 · Sieben weitere Freitextsuchen sind buchstabengenau

Abgespalten von [B-128](B-128-katalogsuche-case-sensitiv.md) (Entscheidung 4). Dort wurde gemessen, was
`Contains` in SQLite wirklich tut — und dass es nicht nur die Katalogsuche betrifft.

## User Story

Als **Creator** möchte ich überall in der App suchen können, ohne die Schreibweise zu treffen, mit der
jemand den Eintrag angelegt hat — nicht nur bei Verlagen und Lehrwerken.

## Ist-Stand am Code

Die Ursache steht seit B-128 an einer Stelle dokumentiert
(`backend/Pugling.Api/Services/Shared/SearchPattern.cs`): EF bildet `string.Contains` auf SQLites
`instr()` ab, das byte-genau ist und **keine** Spalten-Collation heranzieht. `NOCASE` heilt darum nur
Gleichheitsvergleiche.

Gezählt am 2026-08-09 (`grep` über `Controllers/` und `Services/`, jede Fundstelle einzeln angesehen):
**acht** Freitextsuchen im Backend. Zwei sind seit B-128 auf `LIKE` umgestellt, die übrigen **sechs**
nicht:

| Datei:Zeile | Suchfelder | Anmerkung |
| --- | --- | --- |
| `Controllers/Creator/ClozeTextsController.cs:36` | `Title`, `Text`, `Key` | Freitext, klarer Fall |
| `Controllers/Creator/ExerciseCatalogController.cs:83` | `Title`, `Description` | Freitext, klarer Fall |
| `Controllers/Creator/InterestTagsController.cs:53` | `Slug`, `Label` | `Slug` ist abgeleitet kleingeschrieben |
| `Controllers/Creator/MediaAssetsController.cs:79` | `Description`, `Key` | `Key` ist ein Schlüssel, kein Text |
| `Controllers/Supervisor/ShopController.cs:69` | `Title`, `ArticleNumber` | Artikelnummer eher exakt gewollt |
| `Services/Student/ChildLearnProgressService.cs:153` | in-memory `StringComparison.Ordinal` | kein SQL — eigene Bauart |

Der Vokabelspeicher (`VocabularyStoreController.cs:92-97`) sucht über `Word`/`Translation`, die
`NOCASE` tragen — was ihm laut der B-128-Messung **nichts** nützt: auch dort ist die Suche
buchstabengenau. Sein `Key` hat ohnehin keine Collation.

## Die echte Lücke

Nicht „sechs vergessene Stellen": Die Lücke ist, dass ein plausibler und überall gleich aussehender
Ausdruck (`x.Contains(search)`) auf SQLite etwas anderes tut, als er verspricht — und dass man das an
keiner Stelle sieht. B-128 hat die Erklärung an eine Hilfsklasse geschrieben; solange die übrigen sechs
`Contains` benutzen, steht die Erklärung neben dem Fehler statt in ihm.

## Der Ist-Stand, am 2026-08-10 nachgemessen

Zwei Korrekturen gegenüber der Ausformulierung — beide beim Grillen am Code geprüft:

- **`ChildLearnProgressService.cs:153` ist nicht betroffen — die Zeile 153.** Sie steht auf
  `StringComparison.OrdinalIgnoreCase`, nicht `Ordinal`, und `git log -L 153,153` zeigt: seit ihrer
  ersten Fassung (`9e58a04`). Offener Punkt 2 beruhte insoweit auf einem Lesefehler. **Die Datei ist
  trotzdem betroffen**, nur an anderer Stelle — siehe die letzte Tabellenzeile. Genau diese Unterscheidung
  ist der Wert des Funds: in einer Datei stehen zwei fast gleich aussehende Suchen, von denen die eine im
  Speicher läuft (`Matches`, faltet selbst, bleibt richtig) und die andere als SQL.
- **Der Vokabelspeicher gehört dazu.** `VocabularyStoreController.cs:92-93` stand nur im Fließtext, nicht
  in der Tabelle — er ist aber eine der Fundstellen, nicht ein Nebensatz. Dazu seine beiden
  Schmalfilter `?word=`/`?translation=`, die im Vertrag ausdrücklich „substring filter" heißen (nur
  `partOfSpeech` sagt „exact"): die Trennlinie liegt zwischen Substring und Exakt, nicht zwischen den
  Parametern eines Endpunkts.
- **Eine siebte Fundstelle kam beim Bauen dazu** — gefunden vom Wächter aus Entscheidung 2, bevor er
  fertig war, und in **keiner** Inventur enthalten: `ChildLearnProgressService.ItemsAsync`.

Damit sind es **sieben SQL-Suchen**:

| Datei | Suchfelder |
| --- | --- |
| `Controllers/Creator/ClozeTextsController.cs:36` | `Title`, `Text`, `Key` |
| `Controllers/Creator/ExerciseCatalogController.cs:83` | `Title`, `Description` |
| `Controllers/Creator/InterestTagsController.cs:53` | `Slug`, `Label` |
| `Controllers/Creator/MediaAssetsController.cs:79` | `Description`, `Key` |
| `Controllers/Creator/VocabularyStoreController.cs:92` | `Word`, `Translation`, `Key` (+ `?word=`/`?translation=`) |
| `Controllers/Supervisor/ShopController.cs:69` | `Title`, `ArticleNumber` |
| `Services/Student/ChildLearnProgressService.cs:294` (`ItemsAsync`) | `Word`, `Translation` — `joined` ist ein `IQueryable`, also SQL |

## Offene Punkte

1. ~~**Welche der sechs sollen überhaupt tolerant suchen?**~~ → Entscheidung 1 (alle, alle Felder).
2. ~~**`ChildLearnProgressService.cs:153` ist ein anderer Fall.**~~ → gegenstandslos, siehe Ist-Stand oben.
3. ~~**Wächter oder nicht?**~~ → Entscheidung 2 (ja, in der engen Form).

## Entscheidungen

1. **Alle sechs Fundstellen werden tolerant, und zwar über *alle* ihre Felder — keine Feld-Ausnahmeliste.**
   Das weicht von der Empfehlung der Ausformulierung ab (dort: nur die vier Prosa-Felder), und zwar aus
   drei Gründen. (a) Es ist je Endpunkt **ein** Eingabefeld über einer OR-Kette mehrerer Spalten. Bliebe
   eine Spalte exakt, hinge das Verhalten der Suchbox daran, welche Spalte zufällig trifft — für den
   Nutzer unsichtbar und in keiner Oberfläche erklärbar. (b) Die drei vermeintlichen „Schlüssel" sind
   keine Systemschlüssel: `ShopArticle.ArticleNumber` ist laut seinem eigenen Hilfetext ausdrücklich
   „dein eigenes Kürzel, um den Artikel wiederzufinden" (`fieldHelp.shopArticleNumber`), also
   Nutzer-Freitext; `MediaAsset.Key`, `Vocabulary.Key` und `InterestTag.Slug` sind abgeleitet und
   kleingeschrieben — eine tolerante Suche trifft sie **besser**, nie schlechter. (c) Eine Regel ohne
   Ausnahmen braucht keine Ausnahmeliste, die veraltet. Kosten: eine *exakte* Suche ist danach nirgends
   mehr möglich. Zugesichert hat sie kein Endpunkt (kein DTO, keine Doku, kein Test), aber wer sie später
   braucht, braucht einen eigenen Parameter statt eines Nebeneffekts.
2. **Ein Wächter, aber die enge Form — und mit seiner Grenze im eigenen Text.** Ein quellentext-lesender
   Test über `Controllers/` und `Services/` meldet `.Contains(` mit den Bezeichnern, die dieses Repo für
   den Suchbegriff tatsächlich benutzt (`search`, `term`), samt Ausnahmeliste mit Grund je Eintrag.
   Begründung: die Regel ist **unsichtbar** — derselbe Ausdruck sieht überall gleich aus und tut auf
   SQLite etwas anderes, als er verspricht; genau die Sorte, die dieses Repo mechanisch hält statt an
   Disziplin. Kosten: ein halber Parser (die Lehre aus B-40). Er fängt die Form, die hier geschrieben
   wird, nicht jede denkbare — und das steht als Grenze im Test selbst, damit niemand ihn für
   vollständig hält.

## Akzeptanzkriterien

1. Alle **sieben** Fundstellen suchen schreibweisen-tolerant über `SearchPattern` — keine trägt eine
   Feld-Ausnahme.
2. Je umgestellter Fundstelle ein Integrationstest, der vorher rot war (mit genannter Zahl).
3. Ein Wächter meldet ein neu hinzukommendes `Contains(search)`/`Contains(term)` in `Controllers/` oder
   `Services/`; seine Ausnahmeliste trägt je Eintrag einen Grund, und sein Kommentar benennt, was er
   **nicht** fängt.
4. Keine Schemaänderung — die Kette bleibt, wie sie ist (`LIKE` braucht keine Collation).
5. Der Sonderfall bleibt sichtbar: dass `SearchPattern` nur ASCII faltet („STRASSE" findet „Straße"
   nicht), steht bereits an der Klasse und wird nicht dupliziert.

## Schätzung

**Größe: M** — sechs Fundstellen, sechs rote Proben, ein Wächter mit Ausnahmeliste. Über B-01 (`S`), unter
einer DB-Umbau-Etappe (`L`); vergleichbar mit dem vokabel-basierten Batch-Pfad im `MediaSelector`.

- **`wo: backend`** — die Oberfläche schickt denselben `?search=`-Parameter wie bisher, nur die Antwort
  wird größer.
- **`migration: nein`** — `LIKE` braucht keine Collation; genau das ist der Punkt der B-128-Messung.
- **`vertragsbruch: nein`** — kein DTO, keine Route, kein Fehlercode ändert sich.
- **Risiken:** 1. **Escaping.** `SearchPattern.Contains` neutralisiert `%`, `_` und den Escape selbst — wer
  eine Fundstelle umstellt und `EF.Functions.Like` **ohne** das dritte Argument aufruft, macht aus einer
  Suche nach „50%" einen Treffer auf alles. Der Escape-Parameter gehört an jeden der sechs Aufrufe.
  2. **Der Wächter darf nicht sich selbst melden** (er enthält den gesuchten String als Literal) und nicht
  `SearchPattern.Contains` — beides gehört in die Ausnahmeliste, sonst ist er ab Zeile eins rot.
  3. `ExerciseCatalogController` legt den Begriff in eine lokale Variable `term`; ein Wächter, der nur
  `search` kennt, übersähe genau diese Fundstelle — sie war schon in der ersten Messung dieses Laufs
  beinahe durchgerutscht.
- **Angriffsplan:** 1. Je Fundstelle `Contains` → `EF.Functions.Like(spalte, pattern, SearchPattern.Escape)`,
  Muster wörtlich aus `PublishersController.cs:47-49`. 2. Je Fundstelle ein Integrationstest
  (Groß-/Kleinschreibung über Kreuz), **vor** dem Fix laufen lassen und die Zahl notieren. 3. Wächter in
  `ConventionGuardTests` im Muster der bestehenden quellentext-lesenden Tore.
- **Testweg:** `backend/Pugling.Api.Tests` — je Fundstelle ein Fall in der Testklasse des betroffenen
  Controllers, im Muster der Suchtests aus B-128; der Wächter als Fall in `ConventionGuardTests`.

## Verlauf

- **2026-08-09** — angelegt und zugleich ausformuliert im Nachtlauf (Sprint 1, beim Bau von B-128). Der
  Ist-Stand ist gemessen: acht Suchen gezählt, jede einzeln klassifiziert. **Bewusst nicht in B-128
  mitgenommen:** dessen Ziel (Verlags- und Reihensuche) ist ohne sie erfüllt, und drei der sechs Felder
  sind Schlüssel, bei denen die tolerante Suche erst zu *entscheiden* ist — das ist Punkt 1 und wäre in
  B-128 eine unbemerkte Nebenentscheidung geworden.
- **2026-08-10** — gegrillt und geschätzt im Nachtlauf (autonom nach Freigabe 1, `art: Defekt`). Der
  Ist-Stand wurde dabei am Code **nachgemessen und in zwei Punkten korrigiert** (siehe eigenen Abschnitt):
  `ChildLearnProgressService` war nie betroffen, der Vokabelspeicher schon. Entscheidung 1 weicht von der
  Empfehlung der Ausformulierung ab — alle Felder statt nur die Prosa-Felder, begründet und mit Kosten.
  Größe **M**, `wo: backend`, `migration: nein`, `vertragsbruch: nein`.
- **2026-08-10** — gebaut und **abgenommen** (Nachtlauf, Sprint 1). **Rote Probe: 9 von 11 rot** vor dem
  Fix, die zwei grünen waren die bestehenden Verlags-Tests. Sieben Fundstellen umgestellt — eine mehr als
  die Inventur kannte: `ChildLearnProgressService.ItemsAsync` hat der neue Wächter gefunden, **bevor er
  fertig war**. Sie ist der lehrreichste Fall der Story: in derselben Datei stehen zwei fast gleich
  aussehende Suchen, von denen eine im Speicher läuft und faltet, die andere als SQL und nicht.
  `pugling-reviewer`: **kein Blocker**, sechs Funde, alle im selben Zug behoben — darunter zwei, die diese
  Story betrafen: die `?word=`/`?translation=`-Filter waren ungetestet umgestellt (jetzt eigener Fall) und
  ein Nachbartest in `VocabExerciseAuthoringTests` hielt eine ordinale Zusicherung über einen jetzt
  toleranten Filter fest. Der Wächter selbst war schwächer als gedacht: sein Regex ließ `searchTerm`
  durch — den wahrscheinlichsten Namen überhaupt —, jetzt geschärft und gegengeprüft.
  **Rollengang im Browser** (Freigabe 6): Vokabelspeicher, Suche nach „GOODBYE" — **(0) Treffer vor dem
  Fix, (1) danach**, gleiche DB, gleiche Oberfläche. Suite **801/801**.
