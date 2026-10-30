---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [B-331 Schlüssel-Zweig nur mit identischen Strings getestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf K, Teil 1 (docs/pm-sitzung-2026-10-29-nacht.md)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-331, B-141]
wartet_auf: ""
nachgeschaut: "2026-10-30"
---

# B-338 · Dass doppelte Wörter nach dem Store-Schlüssel verglichen werden, hält kein Fall mit zwei Schreibweisen

## Ist-Stand (Lauf K, am Code belegt)

[B-331](B-331-doppeltes-wort-im-uebungs-payload-500.md) vergleicht neue Inline-Wörter per `VocabKey.Generate`
(`ExerciseControllers.cs` `RepeatedWordAsync`). Der einzige Fall dafür schickt zwei **zeichengleiche** Strings. Die Mutante
„roher Vergleich" (`Front + "\0" + Back`) oder ein `OrdinalIgnoreCase` blieb grün – und brächte für „Moon"/„moon" den alten
`500` mit halb geschriebener Übung zurück. Dieselbe Familie bei [B-141](B-141-interest-tag-label-dublette.md): der exakte
Vergleich in `InterestTagService.EnsureAsync` (Entscheidung 3) hatte keinen Fall mit anderer Schreibweise (🟢 der Nachschau).

## Entscheidungen (autonom, Freigabe 1 – `Defekt`)

1. Nur Fälle, kein Code: das Verhalten ist richtig. Eine sechste Form mit „Moon…"/„moon…" und „Mond"/„mond"; ein Interesse
   in Großbuchstaben trifft den umbenannten Tag nicht. *Kosten:* keine.

## Akzeptanzkriterien

1. Die Mutante „roher Vergleich" fällt an der neuen Form; die Mutante „`EnsureAsync` case-insensitiv" fällt am neuen Fall.

## Schätzung

`XS`, Backend; Integrationstests.

## Verlauf

- **2026-10-29** — angelegt aus der Nachschau (frischer `pugling-reviewer`, `git show 0fcff19`) in Lauf K, Teil 2 (🟡 + 🟢);
  im Sprint gebaut. Beide Fälle sind mit dem Ist-Code **grün** (erwartet). Mutante „roher Vergleich" **1 failed** („inline
  word twice, folded by the key: 500", Z. 442); Mutante „`EnsureAsync` case-insensitiv" **1 failed** (Z. 111).
- **2026-10-29** — Abnahme Lauf K, Teil 2: volle Suite **1017/1017**, Vitest **584/584**, `tsc -b` sauber, E2E **100 passed** (`exit=0`, nach der letzten Code-Änderung gestartet). Rollengang als E2E-Gesamtlauf (Browser-Extension nicht genutzt): Sohn über seine Arcade-Specs (Übung, Klausur, Buchstaben ordnen) plus die Komponentenfälle mit Knoten-Identität; Creator über `schnellanlage-ohne-dublette`; Vater Regressionszeuge. Was ein echter Screenreader ansagt, prüft keine Maschine – **Gerätecheck offen**: einmal mit NVDA/VoiceOver eine Übungsrunde und eine fortgesetzte Klausurfrage anhören. Reviews: `frontend-reviewer`, Delta-Review (ohne 🔴/🟡). **Abgenommen**, Commit siehe Protokoll ([Lauf K](../pm-sitzung-2026-10-29-nacht.md)).
- **2026-10-30** — Nachschau in Lauf L, Teil 1 (frischer `pugling-reviewer`): **entgangen** → [B-342](B-342-doppeltes-wort-faltung-nur-gross-klein-getestet.md) (🟡: der Fall unterscheidet nur ASCII-Groß/Klein, `OrdinalIgnoreCase` bliebe grün; die Prämisse der Story stimmt nicht). Geprüft: `RepeatedWordAsync` gegen die neue Form und ihre Eingaben; Suchreihenfolge von `EnsureAsync` (Slug, Label, Synonym) und der Großschreib-Fall über `RenamedTagAsync`/`UniqueName`; der Label-Fall von `InterestTagLabelTests` unterscheidet genau in Groß/Klein und trägt.
