---
tags: [typ/story, status/idee, bereich/training, rolle/student]
aliases: [Sonderzeichen-Leiste falsche Sprache]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: "Nachtlauf 2026-11-18, Nachschau von B-661 (96a3491d, frontend-reviewer) – docs/pm-sitzung-2026-11-18.md, Runde 5"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-661]
---

# B-670 · Die Sonderzeichen-Leiste folgt der Sprache der Reihe, nicht der Sprache, in der das Kind tippt

Am Code bestätigt (Arbeitsstand 2026-11-18): `PositionPlayService.KeyboardLangOf` liefert je **Übung** die
`SourceLanguage` der Reihe (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:448`), unabhängig von der Richtung
der Karte; alle drei Spielpfade reichen das durch (`PositionPracticeController.cs:315`, `PositionTestsController.cs:54`,
`ExtraTrainingService.cs:433`). Bei einer Französisch-Karte „vorn nach hinten“ (Antwort deutsch) zeigt die Leiste die
französischen Zeichen statt ä/ö/ü/ß; bei Theodors umgedrehter Englisch-Karte erscheint gar keine. B-661 AK 1 sagt „je
getippter Sprache“; `TastaturSpracheTests` prüft nur `back-to-front` und die Übung ohne Reihe (`:56`, `:90`).

Geringer Schaden (deutsche Handy-Tastaturen haben Umlaute, nichts wird verraten). Nebenbei: `sonderzeichen.ts:23` erkennt
nur nackte Codes („fr“, nicht „fr-FR“) – in der echten DB stehen nackte Codes.

## Naheliegend

Je Karte am Server entscheiden (Zielsprache der Antwortseite), dazu ein `front-to-back`-Fall im Test.
