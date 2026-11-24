---
tags: [typ/story, status/abgenommen, bereich/backend]
aliases: []
status: abgenommen
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
nachgeschaut: "2026-11-24"
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
- **2026-11-23** — Review `pugling-reviewer` (nur gelesen): Übersetzungen sinngetreu, Diff nur Kommentare (gegen HEAD
  ohne Kommentare verglichen); 🟡 ohne Zählung (Freigabe 11: nur Kommentar): fünf übersehene Stellen (`SampleJson.cs:57`,
  `ComboTests.cs:41, 51`, `ApiVersioningTests.cs:38`, `PositionReportTests.cs:48`) und „curriculum“ gegen das Glossar
  (`docs/translate.md`: Lehrplan = study plan) – nachgezogen, zwei 🟢-Formulierungen geglättet. Meine Suche nach deutschen
  Funktionswörtern hatte die kurzen Zeilen ohne solche Wörter nicht gefunden.
- **2026-11-23** — abgenommen (Nachtlauf E, Sprint 125), Commit `617f50fd`. Beleg statt Rollengang (kein Rollenpfad
  berührt): Suite 2173/2173 (Release) vor den Nachträgen, danach die betroffenen Klassen 199/199 und Build grün; Diff nur
  in Kommentaren.
- **2026-11-24** — Nachschau (Sprint 126): `grep` auf deutsche Funktionswörter in `//`- und `///`-Zeilen des
  Testprojekts. Übrig sind nur Zitate deutscher Produkttexte (`CreatorAgentTests`, `OpenApiExampleCatalogConcurrencyTests`,
  `PositionPracticeFlowTests`), die nach Entscheidung 2 erlaubt sind. Kein Befund.
- **2026-11-24** — Nachschau (Nachtlauf H, Retro Sprint 130): Prüfpunkt „bleibt deutsche Kommentar-Prosa im Testprojekt?“ – `grep` über `//`/`///`-Zeilen aller `backend/Pugling.Api.Tests/*.cs` mit den Funktionswörtern der/die/das/und/nicht/wird/eine/keine/bei/wenn: 16 Treffer in 9 Dateien, jeder gelesen, alle sind Zitate deutscher Produkttexte oder Beispielwörter in englischer Prosa (z. B. `AnswerGraderTests.cs:4` „die Stadt“, `CreatorAgentTests.cs:1702` „nicht sichtbar“) – kein Befund, keine Entgleitung.
- **2026-11-24** — Nachschau (Nachtlauf G, Retro Sprint 127): Prüfpunkt die sieben in der Story genannten Dateien
  (`PlanPositionCrudTests`, `CreatorAgentTests`, `InterestTaxonomyTests`, `ReihenNamensDubletteTests`,
  `KindInteressenTests`, `CreatorProfileTests`, `ComboTests`) – alle `//`-/`///`-Zeilen nach deutschen Funktionswörtern
  („und|nicht|ist|wird|eine|wenn|weil|oder“) gegrept: 0 Treffer. Die Umlaut-Treffer im übrigen Testprojekt
  (`AnswerVariantsTests`, `BeideRichtungenTests`, …) stehen nur in zitierten deutschen Beispielwörtern („die Straße“,
  „für“), die erlaubt sind. Kein Fund.
- **2026-11-24** — Nachschau (Nachtlauf I, Retro Sprint 133), kein Befund. Prüfpunkte: die acht Stellen `PlanPositionCrudTests.cs:418`, `ComboTests.cs:17, 41`, `SampleJson.cs:57`, `ApiVersioningTests.cs:38`, `PositionReportTests.cs:48`, `SpeedBonusTests.cs:74`, `AssignAgentTests.cs:121` einzeln gelesen – englisch; dazu `grep` über `//`-Zeilen in `Pugling.Api.Tests/*.cs` mit den Funktionswörtern der/die/das/nicht/wird/und außerhalb von Anführungszeichen: 0 Treffer.
