---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/test, rolle/creator]
aliases: [B-338-Fall unterscheidet nur ASCII-Groß/Klein]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf K, Teil 2 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-338]
wartet_auf: ""
---

# B-342 · Der Fall „doppeltes Wort nach dem Schlüssel" unterscheidet seine Eingaben nur in ASCII-Groß/Klein

[B-338](B-338-doppeltes-wort-schreibweise-ungetestet.md) sollte belegen, dass `RepeatedWordAsync` doppelte Inline-Wörter
nach `VocabKey.Generate` vergleicht. Der Fall (`backend/Pugling.Api.Tests/ExerciseItemsAndProgressTests.cs:432-433`)
schickt „Moon…"/„moon…" und „Mond"/„mond". Er tötet den rohen `Ordinal`-Vergleich, aber ein `OrdinalIgnoreCase`
bliebe grün – falsch für ß, Akzente und Leerraum. Auch die Prämisse der Story („`OrdinalIgnoreCase` brächte für
Moon/moon den alten 500 zurück") stimmt nicht: Moon/moon sind unter `OrdinalIgnoreCase` gleich.

Vorschlag: das zweite Paar in einer Faltung unterscheiden, die Groß/Klein nicht abdeckt (Rückseite „Straße"/„strasse"
oder ein Akzent).

## User Story

Als **Entwickler** möchte ich, dass der Fall für doppelte Wörter nach dem Store-Schlüssel rot wird, sobald jemand den
Vergleich auf Groß/Klein verengt.

## Ist-Stand (Lauf L, am Code belegt)

Siehe oben: „Moon…"/„moon…" und „Mond"/„mond" unterscheiden sich nur in ASCII-Groß/Klein.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Vorderseite mit Akzent („mõ…" gegen „mo…"), Rückseite mit ß („Straße" gegen „strasse").** Beides faltet nur der
   Schlüssel, nicht `OrdinalIgnoreCase`. *Kosten:* keine.

## Akzeptanzkriterien

1. Mutante „Vergleich roh und kleingeschrieben" in `RepeatedWordAsync` → der Fall fällt (`500`).

## Schätzung

`XS`, nur Test. Testweg: `ExerciseItemsAndProgressTests.UebungsPut_MitDoppeltemWort_Liefert400_UndSchreibtNichts`.

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf K, Teil 2 (`pugling-reviewer`, 🟡). Mutante aus Code gelesen.
- **2026-10-30** — Lauf L, Teil 2: gebaut. Mutante `(front + "\0" + back).ToLowerInvariant()` statt `VocabKey.Generate` in
  `RepeatedWordAsync` → **1 failed** (erwartet 1, „inline word twice, folded by the key: 500"). Dass der alte Fall unter
  dieser Mutante grün blieb, ist aus Code gelesen (Moon/moon sind kleingeschrieben gleich), nicht gemessen.
- **2026-10-30** — Review `pugling-reviewer`: sauber; die drei Faltungs-Mutanten (nur Kleinschreibung, Akzente ohne ß, ß ohne Akzente) fallen je über Vorder- oder Rückseite (aus Code gelesen).
- **2026-10-30** — Abnahme Lauf L, Teil 2: volle Suite **1028/1028**, Build ohne Warnung. E2E-Gesamtlauf **100 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet; danach nur Tests, Vertragskommentar und die neue Spec), danach die neue Spec und die drei Nachbar-Specs **4 passed**. Rollengang: Vater/Creator über die neue Spec `schlagwort-trotz-fremdem-synonym` (Schlagwort-Verwaltung, Facette ändern → Neuladen → gespeichert; rote Probe ohne den Fix **1 failed**); Sohn Regressionszeuge (seine Specs im Gesamtlauf grün, kein Pfad von ihm im Diff). Kein Delta-Review nach den Nachbesserungen: sie sind Testfälle und Kommentare, je mit gemessener Mutante (Selbst-Check). **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
