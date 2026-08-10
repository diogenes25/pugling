---
tags: [typ/story, status/idee, bereich/backend, bereich/katalog, rolle/creator]
aliases: [Zwei Interessen-Tags mit demselben Label, vierte slug-idempotente Ressource]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-136 (Nachtlauf 2026-08-10, Entscheidung 3)
unverifiziert: true
entgangen_bei: []
---

# B-141 · Zwei Interessen-Tags dürfen dasselbe Label tragen

Dieselbe Defektklasse wie B-124 → B-133 → [B-136](B-136-verlag-umbenennen-erzeugt-namensdublette.md), zum
**vierten** Mal — jetzt bei der Ressource, die B-136 beim Grillen als Kandidatin benannt hat.
`InterestTagsController.Create` gibt bei einem Slug-Treffer bedingungslos die vorhandene Zeile zurück,
`Update` prüft Slug gegen Slug. Der Slug friert beim Umbenennen ein, also können danach zwei Tags dasselbe
`Label` tragen — und ein Label ist genau das, was in jedem Auswahlfeld steht. Der Kommentar am Controller
sagt es sogar selbst („otherwise two tags share a label in every picker") und erzwingt es trotzdem nicht.

**Warum nicht in B-136 mitgenommen** (dessen Ziel ist ohne diese Story erfüllt): Es ist **kein
gleichgelagerter Fall**, und beide Unterschiede sind teuer.

1. **`InterestTag.Label` trägt keine `NOCASE`-Collation.** Die tragen nur `Publisher.Name`,
   `TextbookSeries.Name` und `Vocabulary.Word`/`Translation` (`Data/PuglingDbContext.cs:222,238,292-293`,
   beim Grillen von B-136 nachgezählt). Eine schreibweisen-tolerante Namensprüfung braucht dort eine
   Schemaänderung — und die faltet die Migrationskette neu. Aus dem `S` der Verlags-Story würde ein `M`
   mit `migration: ja`.
2. **`Create` nimmt einen ausdrücklichen Slug entgegen** (`CreateInterestTagDto.Slug`), und der
   Update-Kommentar hält als **bewusste Entscheidung** fest, dass ein Label darum legitim vom Slug
   abweichen darf: „as strong as Create's rule and no stronger". Ob Label-Eindeutigkeit hier überhaupt
   gewollt ist, ist damit erst zu entscheiden — bei Verlag und Reihe stellte sich die Frage nicht.

Beim Ausformulieren zu klären: ob die Eindeutigkeit fachlich gewollt ist (Punkt 2), und falls ja, ob sie
schreibweisen-tolerant sein muss oder ein `Ordinal`-Vergleich reicht — Letzteres käme ohne Migration aus
und wäre die deutlich billigere Hälfte. Ebenfalls zu prüfen: ob damit die vierte Wiederholung den
geteilten Helfer bzw. Wächter rechtfertigt, den B-136 ausdrücklich zurückgestellt hat.

## Verlauf

- **2026-08-10** — angelegt beim Grillen von [B-136](B-136-verlag-umbenennen-erzeugt-namensdublette.md)
  (Entscheidung 3) im Nachtlauf. Der Ist-Stand ist am Code belegt (Controller und Collationen einzeln
  nachgesehen); `unverifiziert: true` bleibt trotzdem stehen, weil die **fachliche** Frage aus Punkt 2
  nicht erhoben ist.
