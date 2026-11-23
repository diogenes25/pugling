---
tags: [typ/story, status/idee, bereich/backend]
aliases: []
status: idee
prio: P3
art: Aufräumen
groesse: ""
wo: backend
migration: offen
vertragsbruch: offen
quelle: "Review von B-861 im Nachtlauf 2026-11-23-c (docs/pm-sitzung-2026-11-23-c.md, pugling-reviewer 🟡)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-863 · Deutsche `//`- und `///`-Absätze in `Pugling.Api.Tests`

B-861 hat die deutschen `/* */`-Blöcke übersetzt; der Reviewer fand daneben deutsche `//`- und `///`-Absätze, alle in
`Pugling.Api.Tests`: `PlanPositionCrudTests.cs:418-439, 448-450, 471-475`, `CreatorAgentTests.cs:725-745, 3001-3034`,
`InterestTaxonomyTests.cs:86-91, 103, 125`, `ReihenNamensDubletteTests.cs:74-75, 86, 109, 126-128`,
`KindInteressenTests.cs:291-292`, `CreatorProfileTests.cs:114, 133`, `ComboTests.cs:17`. Die Regel „Code-Doku auf
Englisch“ gilt ausdrücklich auch für die Tests. Gelesen vom Reviewer, nicht von mir nachgezählt.

## Verlauf

- **2026-11-23** — angelegt aus dem Review von B-861 (Nachtlauf 2026-11-23-c), bewusst nicht im Sprint 120.
