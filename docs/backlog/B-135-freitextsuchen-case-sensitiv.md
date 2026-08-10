---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/katalog]
aliases: [Sechs Suchen ohne Schreibweisen-Toleranz, instr statt LIKE, SearchPattern ausrollen]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-128 (Nachtlauf 2026-08-09, Entscheidung 4)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-135 · Sechs weitere Freitextsuchen sind buchstabengenau

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

## Offene Punkte

1. **Welche der sechs sollen überhaupt tolerant suchen?** Nicht alle: `MediaAsset.Key`,
   `ShopArticle.ArticleNumber` und `InterestTag.Slug` sind Schlüssel, keine Prosa — dort ist ein exakter
   Treffer vertretbar, vielleicht sogar gewollt. Empfehlung: die vier Prosa-Felder (`Title`, `Text`,
   `Description`, `Label`) auf `SearchPattern` umstellen, die drei Schlüsselfelder bewusst lassen und den
   Grund als Kommentar hinschreiben. Kosten: die Suche verhält sich je Feld verschieden — das muss die
   Oberfläche nicht wissen, aber der nächste Leser des Codes schon.
2. **`ChildLearnProgressService.cs:153` ist ein anderer Fall.** Der Vergleich läuft **im Speicher**
   (`StringComparison.Ordinal`), nicht in SQL — dort ist die Lösung ein
   `StringComparison.OrdinalIgnoreCase`, nicht `SearchPattern`. Empfehlung: mitnehmen, aber als eigenen
   Punkt behandeln. Kosten: keine, außer dass die Story zwei Techniken statt einer trägt.
3. **Wächter oder nicht?** Ein Test könnte `\.Contains\(search` im Backend-Quelltext verbieten.
   Empfehlung: ja, mit Ausnahmeliste — das Repo hält solche Regeln sonst mechanisch, und diese ist
   unsichtbar genug, um sie sonst wieder zu verlieren. Kosten: ein quellentext-lesender Test ist ein
   grober Parser und braucht eine Rot-Liste (Erfahrung aus B-40).

## Akzeptanzkriterien

1. Jede der sechs Fundstellen ist entweder auf schreibweisen-tolerante Suche umgestellt oder trägt einen
   Kommentar, warum sie exakt bleibt.
2. Je umgestellter Fundstelle ein Integrationstest, der vorher rot war.
3. Ein Wächter meldet ein neu hinzukommendes `Contains(search)` in einer Query; seine Ausnahmeliste trägt
   je Eintrag einen Grund.
4. Keine Schemaänderung — die Kette bleibt, wie sie ist (`LIKE` braucht keine Collation).

## Verlauf

- **2026-08-09** — angelegt und zugleich ausformuliert im Nachtlauf (Sprint 1, beim Bau von B-128). Der
  Ist-Stand ist gemessen: acht Suchen gezählt, jede einzeln klassifiziert. **Bewusst nicht in B-128
  mitgenommen:** dessen Ziel (Verlags- und Reihensuche) ist ohne sie erfüllt, und drei der sechs Felder
  sind Schlüssel, bei denen die tolerante Suche erst zu *entscheiden* ist — das ist Punkt 1 und wäre in
  B-128 eine unbemerkte Nebenentscheidung geworden.
