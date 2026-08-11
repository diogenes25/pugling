---
tags: [typ/story, status/idee, bereich/backend, bereich/doku, rolle/creator]
aliases: [Einrückung im OpenAPI-Summary, para im Summary, Swagger-Codeblock]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachtlauf Sprint 3 (2026-08-10), gefunden beim Prüfen des eigenen OpenAPI-Diffs
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-147 · Ein `<para>` im `<summary>` trägt seine Quelltext-Einrückung ins OpenAPI-Dokument

## User Story

Als **Konsument der API** möchte ich die Beschreibung eines Endpunkts als Fließtext lesen — nicht als
eingerückten Block, der in einer Markdown-Anzeige womöglich als Code erscheint.

## Ist-Stand am Code

Gemessen am 2026-08-10 über `docs/openapi/v1.json`: von **323** Endpunkt-`summary`s beginnen **14** mit
Leerzeichen. Sie sind genau die, deren `/// <summary>` ein `<para>` enthält — der Generator übernimmt die
Einrückung des Quelltexts, statt sie zu normalisieren.

Beispiel (aus diesem Lauf, `SubjectsController.Delete`):

```text
"summary": "    Deletes a subject along with its exercise categories, unless a child's data points at it.\n    Two groups, split by what the delete would cost (B-144). ..."
```

Ohne `<para>` steht der Text bündig — die 309 übrigen Zusammenfassungen belegen das.

## Die echte Lücke

**Vier führende Leerzeichen sind in Markdown ein Codeblock.** Ob das sichtbar wird, hängt davon ab, ob die
anzeigende Oberfläche `summary` als Markdown rendert — das ist der Teil, der **noch nicht gemessen ist**
und beim Ausformulieren zuerst drankommt. Swagger UI rendert `description` als Markdown; ob es das mit
`summary` auch tut, wurde hier nicht geprüft.

Zwei mögliche Ausgänge, und sie führen zu ganz verschiedenen Stories:

- **Wird es gerendert**, sind 14 Endpunkt-Beschreibungen als Codeblock zu lesen — ein echter Defekt an der
  Oberfläche, die das Produkt *ist* (API-First).
- **Wird es nicht gerendert**, bleibt eine Unsauberkeit im Vertragsdokument: `docs/openapi/v1.json` ist
  eingecheckt und wird gelesen, und die Einrückung ist Rauschen im Diff.

## Verwandtschaft

Nachbar von `ConventionGuardTests.Vertrags_Dokumentation_Erreicht_Das_Dokument` (gelandet 2026-08-10):
das Tor prüft, dass ein `<para>` **innerhalb** eines Containers steht, der das Dokument erreicht. Hier
erreicht es das Dokument — nur in schlechterer Form. Dieselbe Fläche, eine Frage weiter.

**Kein `entgangen_bei`:** Der Zustand ist älter als jede der beteiligten Stories und gehört keiner
bestimmten Abnahme an; er entstand mit dem ersten `<para>` in einem `<summary>`.

## Offene Punkte

1. **Rendert die anzeigende Oberfläche `summary` als Markdown?** Zuerst messen — davon hängt ab, ob das ein
   Defekt oder eine Unsauberkeit ist. Empfehlung: an einem der 14 Endpunkte in der laufenden Swagger-UI
   nachsehen, nicht in der Dokumentation von Swagger UI nachlesen.
2. **Wo gehört die Behebung hin?** Drei Kandidaten, mit steigenden Kosten: die 14 Kommentare von Hand
   umformatieren (verrottet, der fünfzehnte kommt); ein Transformer am OpenAPI-Generator, der
   `summary` trimmt (eine Stelle, wirkt auf alle künftigen); oder `<para>` in `<summary>` ganz aufgeben und
   die Prosa nach `<remarks>` ziehen — was aber erst zu klären hätte, ob `<remarks>` das Dokument
   überhaupt erreicht. Empfehlung: der Transformer, wenn Punkt 1 „Defekt" ergibt.

## Verlauf

- **2026-08-10** — angelegt im Nachtlauf (Sprint 3) beim Lesen des eigenen OpenAPI-Diffs. **Bewusst nicht
  im Sprint behoben:** 12 der 14 Fälle liegen außerhalb seines Diffs, und die Frage, *ob* es sich sichtbar
  auswirkt, ist ungemessen — sie zu beantworten ist der nächste Schritt, nicht das Umformatieren.
  Zwei Fälle, die der Sprint selbst erzeugt hatte, tragen die Einrückung weiter; behoben wurde dort nur,
  was er **allein** verursacht hatte (zwei `<see cref>`, die als volle Methodensignatur im Fließtext
  landeten).
