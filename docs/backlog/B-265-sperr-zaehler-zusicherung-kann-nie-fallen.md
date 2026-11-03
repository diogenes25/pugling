---
tags: [typ/story, status/abgenommen, bereich/test, bereich/qualitaet]
aliases: [gesperrtGesamt >= 0, Spur, die nie erscheint]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau Sprint 11 (sechster Nachtlauf, docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-165]
wartet_auf: ""
nachgeschaut: "2026-11-03"
---

# B-265 · Die „Spur“ der gesperrten Lesevorgänge ist eine Zusicherung, die nie fallen kann

`backend/Pugling.Api.Tests/OpenApiExampleCatalogConcurrencyTests.cs:304`:
`Assert.True(gesperrtGesamt >= 0, $"Locked reads across all 10 runs: {gesperrtGesamt}.")`. Der Kommentar (`:303`)
nennt sie eine Spur, mit der sich ein künftiges Flackern von einer Inhalts-Regression unterscheiden lasse. xUnit gibt
die Meldung aber nur beim Fehlschlag aus – die Zahl erscheint in keinem Lauf, die versprochene Unterscheidung gibt es
nicht. Seit `a84aa2b` (B-165) unverändert.

Grenzfall in derselben Probe, nur strukturell belegt: bekommt der Lese-Thread unter Volllast in keinem Durchgang
Rechenzeit (0 ok, 0 gesperrt), wird `ok > gesperrt` (`:298`) rot und meldet fälschlich „final path was held open“.

Vorschlag: Ausgabe über `ITestOutputHelper`/`TestContext.Current.SendDiagnosticMessage` oder die Zeile streichen;
den Verhungern-Fall eigens benennen. **Nicht in diesem Lauf behoben:** außerhalb des Sprint-11-Diffs.

## User Story

Als **Entwickler**, dessen Suite einmal an `AtomaresSchreiben_KeineLeseFehler` rot wird, möchte ich in der Meldung die
Zahlen jedes Fensters lesen, damit ich ein Flackern von einem nicht mehr atomaren Schreiben unterscheiden kann.

## Ist-Stand (Lauf O, am Code belegt, Stand `cc19020`)

- `OpenApiExampleCatalogConcurrencyTests.cs:304`: `Assert.True(gesperrtGesamt >= 0, …)` – kann nie fallen, die Meldung
  erscheint darum nie. Der Verhungern-Fall (0 ok, 0 gesperrt) fällt an `ok > gesperrt` mit „final path was held open".

## Die echte Lücke

Die „Spur" war eine Zusicherung über den Ausgangszustand; die Unterscheidung, die sie versprach, gab es nicht.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Die Spur-Zeile fällt weg;** die Zahlen jedes Fensters stehen in der Meldung der echten Zusicherung (siehe B-274).
2. **Verhungern bekommt eine eigene Zusicherung** mit eigener Meldung („no attempt in any window"), über alle Fenster.

## Akzeptanzkriterien

1. Keine Zusicherung der Probe kann im Ausgangszustand nicht fallen.
2. Eine rote Probe nennt die Zahlen aller zehn Fenster.

## Schätzung

`XS`, nur Tests. Testweg: `OpenApiExampleCatalogConcurrencyTests`, Mutante „unsicherer Schreiber".

## Verlauf

- **2026-10-26** — angelegt aus der Nachschau zu B-165 (sechster Nachtlauf, Sprint 11); am Code belegt.
- **2026-11-03** — Lauf O, Teil 1: gebaut (mit B-274). AK 1 → die Spur-Zeile ist gestrichen, die übrigen Zusicherungen
  fallen je unter einer benannten Bedingung; AK 2 → Mutante „unsicherer Schreiber" **1 failed** mit der Meldung „13639 reads
  were denied and only 925 succeeded across all windows … (#1 ok 301/locked 1392, …, #10 ok 10/locked 1742)".
- **2026-11-03** — Abnahme Lauf O, Teil 1: volle Suite **1048/1048**, Build ohne Warnung, E2E **103 passed** (`exit=0`, nach dem letzten Edit; der Dev-Server lädt den Beispielkatalog über `Load`). Rollengang: der Entwickler ist die Rolle – die Proben selbst sind sein Weg, ihre Mutanten je benannt; Creator, Vater und Sohn Regressionszeugen (kein Pfad von ihnen im Diff, ihre Specs grün). **Abgenommen**, Commit siehe Protokoll ([Lauf O](../pm-sitzung-2026-11-02-nacht.md)).
- **2026-11-03** — Nachschau in Lauf P, Teil 1 (frischer `pugling-reviewer`, read-only): **sauber** (🟢 → [B-358](B-358-beispielkatalog-proben-restkanten.md), Punkt 2: unter Linux nennt die Meldung an `:282` nur das aktuelle Fenster). Geprüft: die alte Spur-Zusicherung `>= 0` ist weg; unsicherer Schreiber rot an `:282` (Linux) bzw. `:300` (Windows, 925 gegen 13 639); `:299` fängt eine Harness-Mutante (Leser-Schleife nie betreten), ist also nicht leer.
