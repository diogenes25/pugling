---
tags: [typ/story, status/gegrillt]
aliases: []
status: gegrillt
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-646
grund: ""
ersetzt_durch: []
---

# B-647 · Vokabeln vollständig: Definition, Ladeweg und Bestand

Teil (a) aus der Teilung von [B-646](B-646-vokabel-schema-nachfuellen.md). Ist-Stand, echte Lücke und die
Begründung jeder Entscheidung stehen dort.

## User Story

Als **Creator** möchte ich, dass jede Vokabel im Speicher ihre Wortart und, je nach Wortart, Artikel, Genus,
Plural oder Genitiv bzw. die Verbangaben strukturiert trägt, **und dass das auch für jede neu geladene Unit
gilt**. **Damit** können Übungen, die auf Vokabeln aufbauen (Birkenbihl, Artikel, Konjugation, Filter nach
Wortart), auf diese Daten zählen.

## Entscheidungen

Aus B-646 übernommen: **1** (Artikel/`to` bleiben im `Word`, zusätzlich strukturiert), **2** (Definition von
„vollständig"), **3** (Opus liest den Bestand aus den Seiten-Markdowns, Vater prüft eine Stichprobe), **6**
(`NounInfo.Genitive`), **7** (Übersetzungssätze bis B-649 als `Phrase`).

## Akzeptanzkriterien

- `?incomplete=true` folgt Entscheidung 2: Nomen ohne Artikel/Genus (sprachabhängig) bzw. ohne Plural, im
  Latein ohne Genitiv/Genus. Verben ohne festgelegtes `IsBaseForm`, gebeugte Formen ohne `BaseFormId`. Jede
  andere Wortart, solange sie `Other` ist. Tests in `Pugling.Api.Tests` decken je Wortart einen vollständigen
  und einen unvollständigen Fall ab.
- `NounInfo.Genitive` existiert im Vertrag, im Client und im Vokabel-Editor des Frontends (anlegen **und**
  zurücklesen).
- Speichern weist einen Widerspruch ab: Beginnt `Word` mit einem Artikel der Lernsprache, muss `Noun.Article`
  derselbe sein.
- `laden.py` und das TSV-Format kennen Spalten für Wortart, Artikel, Genus, Plural, Genitiv und
  Infinitiv/Grundform. Eine neu geladene Unit hat 0 Treffer in `?incomplete=true`.
- Auf einer Kopie der echten DB: Nach dem Nachfüll-Lauf liefert `?incomplete=true` für en, fr und la **0**
  Treffer. Die 29 Übersetzungssätze tragen `Phrase`. `ItemProgress` und `PositionItemProgress` sind
  zeilengleich vor und nach dem Lauf.
- Der Vater hat eine Stichprobe je Buch geprüft. Erst danach läuft der Nachfüll-Lauf gegen die echte DB.

## Verlauf

- **2026-11-17** — aus B-646 geteilt (Entscheidung 8), Stufe `gegrillt` mit den Entscheidungen 1–3, 6, 7 aus B-646.
