---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/doku, bereich/qualitaet]
aliases: [rohe HTML-Tags im Vertragsdokument, b-Tags in Summaries, OpenAPI-Beschreibungen]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: pugling-reviewer zu B-123 (2026-08-10), Fund 2 der zweiten Runde
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-138 · Rohe HTML-Tags stehen in 70 Beschreibungen des Vertragsdokuments

Beim Review von [B-123](B-123-lehrwerk-reihe-bearbeiten.md) an einer Stelle behoben und dabei gemessen,
dass es 70 sind. Ein Einzelfall-Fix lässt die Regel wie Zufall aussehen.

## User Story

Als **Konsument der API** möchte ich Beschreibungen lesen, die nicht mit `<b>` und `<em>` gespickt sind —
das Vertragsdokument ist bei API-First das Produkt, nicht ein Nebenprodukt des Quelltexts.

## Ist-Stand am Code

`ContractDocumentTests` schreibt `docs/openapi/v1.json` bei jedem Lauf; die `description` eines Schemas ist
der XML-Doc-Kommentar des Records. Der Generator übersetzt `<c>` in Backticks und **wirft `<para>` weg**,
lässt aber alles andere als rohen Text stehen.

Gezählt am 2026-08-10 im erzeugten Dokument:

| Tag | Vorkommen |
| --- | --- |
| `<b>` | 62 |
| `<i>` | 6 |
| `<em>` | 2 |
| `<para>` | 0 (wird verworfen) |

Eines der beiden `<em>` steht in der Summary von `POST /api/v1/creator/textbook-series`
(`TextbookSeriesController.cs`) — unmittelbar über der Stelle, an der B-123 dasselbe Problem lokal
behoben hat.

**Der Ersatz für `<para>` existiert und wird schon benutzt:** eine leere `///`-Zeile wird zu `\n\n` und
ergibt in Swagger einen echten Absatz. 38 Schemas machen das bereits (z. B. `UpdateMyAccountDto`); B-123
hat es übernommen, mit belegtem Ergebnis (zwei Absätze, kein Markup).

## Die echte Lücke

Nicht „hässliche Doku": Die Lücke ist, dass die **Zielsprache verwechselt** wird. Ein `///`-Kommentar in
`Pugling.Contracts` hat zwei Leser — den Entwickler in der IDE und den API-Konsumenten im
Vertragsdokument — und nur der erste sieht formatiertes XML. Für den zweiten ist `<b>` reiner Text, der
zusätzlich die Leerzeichen um sich herum schluckt (`SubjectId``<b>and</b>``SubjectName`).

Und es ist die Sorte Regel, die dieses Repo mechanisch hält: eine Konvention ohne Tor stellt sich in drei
Monaten wieder her.

## Offene Punkte

1. **Nur aufräumen oder ein Tor?** Empfehlung: beides, und das Tor ist der eigentliche Wert — eine Regel
   in `ConventionGuardTests`, die in `Pugling.Contracts`-Summaries nur `<c>`, `<see>`, `<paramref>` und
   `<summary>`/`<remarks>` selbst erlaubt. Kosten: eine Ausnahmeliste, falls irgendwo `<list>` oder
   `<code>` bewusst steht (vor dem Bau zählen).
2. **Gilt die Regel nur für `Contracts` oder auch für die Controller?** Die Summaries der **Actions**
   landen ebenfalls im Dokument (`operationId`-Beschreibungen) — das `<em>` aus dem Fund ist genau so
   eines. Empfehlung: dieselbe Regel für beide, weil beide dasselbe Dokument speisen. Kosten: der Wächter
   muss zwei Projekte lesen.
3. **Was passiert mit den Stellen, an denen `<b>` echte Betonung trägt?** 62-mal ist viel; ein Teil davon
   betont bewusst. Empfehlung: beim Aufräumen je Stelle entscheiden, ob die Betonung in die Wortwahl
   wandert („**must**" → „is required to") oder wegfällt — nicht mechanisch löschen.

## Akzeptanzkriterien

1. Im erzeugten `docs/openapi/v1.json` steht kein rohes `<b>`/`<i>`/`<em>`/`<strong>` mehr in einer
   `description` oder `summary`.
2. Ein Tor meldet ein neu hinzukommendes Tag; seine Ausnahmeliste trägt je Eintrag einen Grund.
3. Wo ein Absatz gewollt ist, steht eine leere `///`-Zeile — nicht `<para>` (der Generator verwirft es).
4. Die Suite bleibt grün; kein Verhalten ändert sich (Abnahmeform `Aufräumen`).

## Verlauf

- **2026-08-10** — angelegt aus dem `pugling-reviewer`-Befund zur zweiten B-123-Runde.
  Zahlen sind gemessen, und zwar **am geparsten JSON**, nicht per `grep`: `System.Text.Json`
  escapt `<` als `\u003C`, ein `grep` auf das Tag liefert darum immer 0 Treffer. Diese Falle
  hält die Story ausdrücklich fest, weil sie den Fund sonst unsichtbar macht.
  **Bewusst nicht in B-123 mitgenommen:** dort war es eine Datei, hier sind es 70 Stellen
  mit einer Abwägung je Stelle (Punkt 3) und einem Tor als eigentlichem Ziel.
