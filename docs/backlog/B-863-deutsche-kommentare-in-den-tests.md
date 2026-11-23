---
tags: [typ/story, status/in-arbeit, bereich/backend]
aliases: []
status: in-arbeit
prio: P3
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Review von B-861 im Nachtlauf 2026-11-23-c (docs/pm-sitzung-2026-11-23-c.md, pugling-reviewer 🟡)"
unverifiziert: false
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

## Ist-Stand am Code (nachgezählt 2026-11-23)

Gesucht über `//`- und `///`-Zeilen mit deutschen Funktionswörtern und Umlauten außerhalb von Anführungszeichen, jeder
Treffer gelesen: deutsche Kommentar-Prosa in 10 Dateien – die sieben der Liste oben, dazu `PositionPracticeFlowTests.cs:242-243`,
`ComboTests.cs:17, 71`, `SpeedBonusTests.cs:74` und `AssignAgentTests.cs:121`; `CreatorProfileTests.cs` hat zwei Stellen
(114, 133). Bleiben dürfen Zitate deutscher Produkt- und Doku-Texte („hat kein Fach“, `FreitextsucheCaseTests.cs:147`,
`OpenApiExampleCatalogConcurrencyTests.cs:33-34`) und deutsche Testnamen – die Regel betrifft Doku, nicht Inhalt.

## Entscheidungen

Autonom gegrillt (Aufräumen, Freigabe 1, Nachtlauf E).

1. **Übersetzt wird jede deutsche Kommentar-Prosa im Testprojekt, wörtlich im Sinn, nicht umgeschrieben.** Begründung:
   die Regel der Root-`CLAUDE.md` gilt ausdrücklich für die Tests, und diese Kommentare tragen oft das Warum und die rote
   Probe eines Tests – eine Umformulierung könnte genau das verlieren. Kosten: ein Diff über zehn Dateien ohne
   Verhaltensänderung. Verworfen: auch die anderen vier Backend-Projekte jetzt mitzunehmen – dort sind die Treffer der
   Suche Zitate deutscher Produkttexte, kein Befund. Schließt aus: deutsche Prosa, die der nächste Leser für Inhalt hält.
2. **Kein Tor dafür.** Eine Sprach-Erkennung über Kommentare träfe die erlaubten Zitate; ein rotes Tor mit
   Ausnahmeliste je Zitat kostet mehr, als es fängt (zwei Funde in drei Monaten, beide vom Reviewer). Kosten: die Regel
   bleibt Disziplin.

## Schätzung

**Größe: S** – reine Kommentar-Übersetzung, `migration: nein`, `vertragsbruch: nein`. Beleg: Build, Suite grün, Diff nur in
Kommentarzeilen.

## Verlauf

- **2026-11-23** — angelegt aus dem Review von B-861 (Nachtlauf 2026-11-23-c), bewusst nicht im Sprint 120.
- **2026-11-23** — ausformuliert, gegrillt, geschätzt und gebaut (Nachtlauf E, Sprint 125): zehn Dateien übersetzt;
  `git diff -U0` zeigt außer Kommentarzeilen nur drei Zeilenend-Kommentare (`// Feature aus` u. a.), keinen Code.
