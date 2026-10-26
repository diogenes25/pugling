---
tags: [typ/story, status/idee, bereich/test, bereich/qualitaet]
aliases: [gesperrtGesamt >= 0, Spur, die nie erscheint]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau Sprint 11 (sechster Nachtlauf, docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-165]
wartet_auf: ""
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

## Verlauf

- **2026-10-26** — angelegt aus der Nachschau zu B-165 (sechster Nachtlauf, Sprint 11); am Code belegt.
