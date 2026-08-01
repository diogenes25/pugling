---
tags: [typ/story, status/idee, bereich/qualitaet, bereich/tests]
aliases: [Beispielkatalog-Rennen, openapi-examples IOException]
status: idee
prio: P3
art: Defekt
quelle: docs/backlog/B-42-openapi-typen-generieren.md
unverifiziert: true
---

# B-57 · Im Testlauf lesen und schreiben zwei Stellen gleichzeitig dieselbe Katalogdatei

`openapi-examples.generated.json` wird im **selben** Testlauf gelesen und geschrieben:
`OpenApiExampleCatalog.Load` öffnet sie beim Hoststart mit `File.OpenRead` (also `FileShare.Read`),
`DocsCaptureTests.WriteOpenApiExamples` überschreibt sie mit `File.WriteAllText`. xUnit parallelisiert über
Collections, es gibt also keine Reihenfolge – und damit ein Zeitfenster, in dem beide zugreifen. Das kann in
**beide** Richtungen als `IOException` enden, und zwar für jede Fabrik, die die Beispiele anlässt (das ist die
Vorgabe, also fast alle).

Bisher ist es nicht aufgefallen; das Fenster ist schmal. Ein Flake, der nur selten zuschlägt, ist aber
teurer als einer, der immer zuschlägt – er kostet beim nächsten Auftreten eine Untersuchung, die schon einmal
gemacht wurde.

Der Befund ist **vorbestehend**. Er kam beim Review von [B-42](B-42-openapi-typen-generieren.md) Schritt 1
zur Sprache, weil dort dieselbe Datei aus einem anderen Grund im Weg stand: das eingecheckte
Vertragsdokument darf die Beispiele nicht enthalten, sonst ist es nicht byte-stabil (Naht 2 in
[docs/testabdeckung-plan.md](../testabdeckung-plan.md)). Die Byte-Stabilität ist gelöst, das **Rennen um die
Datei** nicht.

**Zu prüfen beim Ausformulieren:** Ist es überhaupt beobachtbar (gezielt provozieren, nicht auf Glück warten)?
Und was ist die richtige Antwort – tolerantes Öffnen (`FileShare.ReadWrite`), atomares Schreiben über eine
temporäre Datei mit `File.Move`, oder den Katalog gar nicht mehr aus dem Quellbaum lesen? Die dritte Variante
würde auch den Grund für den Schalter `OpenApi:ExamplesEnabled` entschärfen.

## Verlauf

- **2026-08-01** — geerntet aus dem Review zu [B-42](B-42-openapi-typen-generieren.md) Schritt 1 (E3),
  ungeprüft: die `IOException` ist beobachtet, ihre Häufigkeit und die richtige Antwort sind offen.
