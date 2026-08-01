---
tags: [typ/referenz, bereich/qualitaet, bereich/doku]
aliases: [Vertragsdokument, OpenAPI-Artefakt]
---

# Das eingecheckte OpenAPI-Vertragsdokument

`v1.json` ist **erzeugt, nicht gepflegt.** Wer es von Hand ändert, verliert die Änderung beim nächsten
Testlauf. Geschrieben wird es von
[`ContractDocumentTests`](../../backend/Pugling.Api.Tests/ContractDocumentTests.cs) bei *jedem* Lauf von
`dotnet test`, gelesen wird es vom CI-Schritt „Vertragsdokument prüfen (OpenAPI)".

## Wozu es da ist

Damit eine Vertragsänderung **im Diff auftaucht**. Bis dahin war sie unsichtbar: wer ein Feld in einem
`Pugling.Contracts`-Record umbenannte, änderte eine Zeile in einem Record – und sonst bewegte sich nichts
im Repo. Jetzt bewegt sich diese Datei mit, und CI fragt beim nächsten Push, ob das Absicht war.

Das Tor **verbietet** nichts. Die API steht auf `v1` und darf sich bis zur Publikation frei ändern
([CLAUDE.md](../../CLAUDE.md) → API-Versionierung). Rot heißt nur: der committete Stand passt nicht mehr
zum Code – neu erzeugen und mitcommitten.

## Warum ohne Beispiele

Das Dokument ist **vertragsrein**: Schemata, Pfade, Statuscodes – ohne die verifizierten Swagger-Beispiele,
die der laufende Server ausliefert. Nicht aus Sparsamkeit, sondern weil es mit ihnen **nicht byte-stabil**
wäre: der Beispielkatalog wird beim Hoststart aus dem Quellbaum gelesen und von `DocsCaptureTests` im
selben Lauf neu geschrieben; xUnit gibt keine Reihenfolge her, also sähe ein Host den alten und ein
anderer den neuen Stand. Das Tor würde flappen statt zu bewachen.

Die Beispiele sind deshalb nicht ungedeckt – sie hängen am Tor **D4**, das
[`docs/api-examples/`](../api-examples/index.md) und `openapi-examples.generated.json` diff't.

## Fallstricke

- **Zeilenenden gehören in die Werte, nicht nur in die Datei.** Die `summary`-Felder tragen die
  XML-Doc-Kommentare wörtlich, samt ihrer Umbrüche. Unter Windows sind das `\r\n`, auf dem Linux-Runner
  `\n` – ohne Normalisierung *innerhalb* der JSON-Zeichenketten unterschiede sich das Dokument an
  hunderten Stellen zwischen den Plattformen, und das Tor wäre bei seinem ersten CI-Lauf rot gewesen.
  Dieselbe Fehlerklasse hat D4 schon einmal getroffen (`Environment.NewLine` in `Truncate()`).
- **`servers` trägt `http://localhost/`** – die Adresse des Testhosts, nicht die des Betriebs. Das ist
  deterministisch und damit unschädlich; wer die Datei als Client-Konfiguration liest, muss den Server
  aber selbst setzen.

## Verwendung

Ab [B-42](../backlog/B-42-openapi-typen-generieren.md) Schritt 2 erzeugt das Frontend seine Vertragstypen
aus dieser Datei (`openapi-typescript`), damit `tsc` bricht, wenn die Oberfläche ein entferntes Feld noch
benutzt. Der Client-Routen-Wächter
([B-40](../backlog/B-40-client-routen-waechter.md)) nutzt sie bewusst **nicht** – er liest das Dokument
lebend aus dem Testhost, weil ein Wächter gegen ein Abbild grün bleiben kann, während die echte API
abweicht.
