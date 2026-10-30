---
tags: [typ/story, status/idee, bereich/backend, bereich/test, rolle/creator]
aliases: [B-338-Fall unterscheidet nur ASCII-Groß/Klein]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf K, Teil 2 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: true
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

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf K, Teil 2 (`pugling-reviewer`, 🟡). Mutante aus Code gelesen.
