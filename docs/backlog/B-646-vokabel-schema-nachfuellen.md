---
tags: [typ/story, status/idee]
aliases: []
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Messung auf einer Kopie der echten pugling.db)"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-646 · Vokabel-Schema nachfüllen: Wortart, Nomen-/Verb-Angaben, vertauschte Sprachen

Gemessen am 2026-11-17 auf einer Kopie der echten DB (1.119 `Vocabularies`): die Pflichtfelder (`Key`, `Word`,
`Translation`, Sprachen) sind vollständig, die strukturierten Felder fast leer. `PartOfSpeech` steht bei 1.104
Einträgen auf `Other`; `Noun` ist bei 7, `Verb` bei 5 gesetzt (Handproben); `TranslationAlternatives`,
`PronunciationAudioUrl` und Tags sind nirgends gesetzt, `BaseFormId` einmal. Die Information steckt teils
unstrukturiert im Wort: 68 französische Einträge tragen den Artikel im `Word` (`la ville`), 192 englische das
`to`/`(to)`, 10 ein `the`/`a`. Latein (88, VIVA 1 L1–3) hat weder Genitiv noch Genus (`pater` statt
`pater, patris m.`) – das gehört zur Vokabel und fehlt Theodor schon beim Lernen. Ursache vermutlich: die
Ladeskripte (`raw/*/…-pugling/laden.py`, TSV-Spalten `wort`/`uebersetzung`/`hinweis`) kennen die Felder nicht.
Daneben ein Datenfehler: 29 Einträge mit `SourceLanguage = de` und englischer/französischer `Translation`
(ganze Sätze, z. B. „Entschuldigung, wie komme ich zum Bahnhof?") – laut Schema ist `SourceLanguage` die
Lernsprache, die Seiten sind also vertauscht. Gedacht: TSV um Wortart/Genus/Plural bzw. Genitiv erweitern (aus
den Seiten-Markdowns), Bestand per Creator-API nachziehen, die 29 Einträge richtigstellen. `TranslationAlternatives`
bleibt bewusst leer, solange die Wertung Varianten aus `,`/`;` gelten lässt (B-462). Für das heutige Abfragen
(`Word` ↔ `Translation`) folgenlos; nötig wird es für Artikel-/Konjugationsübungen und Filter nach Wortart.

**Anforderung des Vaters (2026-11-17):** Die Felder sollen **vollständig** gefüllt sein, weil weitere
Übungen auf den Vokabeln aufbauen. Konkret soll die Birkenbihl-Methode ausgebaut werden, und dafür braucht
**jedes Wort, das in einem Birkenbihl-Text vorkommt, eine Vokabel**. Stand auf der DB-Kopie: die einzige
Birkenbihl-Übung (#3 „Getting to know each other") hat 8 Wörter, **0** davon verknüpft (`vocabularyId: null`),
nur **1** im Speicher auffindbar. Unbelegter Verdacht für die Lücke: die Dekodierung
(`Services/Creator/BirkenbihlDecodingService.cs:49-62`) sucht nur die exakte Oberfläche – eine gebeugte Form
(`is`) findet ihre Grundform (`be`) nicht, `live` findet nicht `(to) live`, `ville` nicht `la ville`. Die
Vollständigkeit hängt also an zwei Dingen: gebeugte Formen als eigene Einträge mit `BaseFormId`/`Verb`
(so ist das Schema gedacht, `Models/VocabEntities.cs:3-5`), und Wörter ohne eingebauten Artikel/`to`, die
diese Angaben in `Noun`/`Verb` tragen.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Füllgrad-Messung des Vokabel-Schemas auf einer DB-Kopie, im Chat mit dem Vater).
- **2026-11-17** — Anforderung des Vaters ergänzt (Felder vollständig, jedes Birkenbihl-Wort braucht eine Vokabel), Birkenbihl-Abdeckung auf der DB-Kopie gemessen; P3→P2, Aufräumen→Wunsch.
