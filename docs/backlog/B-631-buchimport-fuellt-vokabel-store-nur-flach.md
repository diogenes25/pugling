---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator, lerntechnik/vokabeln]
aliases: [Vokabel-Store nur Wort und Übersetzung, Wortart beim Buchimport, Genus und Stammformen aus dem Lehrwerk]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Frage: ist der Store mehr als eine 1:1-Übersetzung?)"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-631 · Der Buchimport füllt vom Vokabel-Store nur Wort und Übersetzung

Der Vokabel-Store ist als Lexikon gebaut (Wortart, `Noun` mit Artikel/Genus/Plural, `Verb` mit Stammformen,
Grundform-Verweis, Aussprache, Tags). Der Weg aus dem Lehrwerk (`lehrwerk-aufbereiten` → `laden.py`) schickt
aber je Wort nur `front`/`back`/`hint`, und der Server legt den Eintrag mit `PartOfSpeech = Other` an.
Gezählt an einer Kopie der echten `pugling.db` am 2026-11-17: von 387 Einträgen stehen 372 auf `Other`, und
dazu gehören alle 88 lateinischen. `Noun` ist 7-mal gefüllt, `Verb` 5-mal, `BaseFormId` einmal; Audio,
`TranslationAlternatives` und Vokabel-Tags sind leer. Was das Buch an Grammatik mitliefert, steckt als
Freitext im Hinweis („lang: nōmen · Neutrum“, „· Adverb“) oder fehlt ganz (Genus und Genitiv lateinischer
Nomen, Stammformen). Folgen: Die Artikel-Toleranz aus [B-437](B-437-artikel-toleranz-nur-bei-nomen.md)
behandelt jede Buchvokabel wie ein Nomen (vermutlich die Ursache hinter
[B-626](B-626-antwort-verlangt-mehr-als-die-vokabel.md)), und Übungen zu Genus, Plural oder Stammformen
lassen sich nicht aus dem Store erzeugen. Beim Ausformulieren klären: Was liest Opus beim Aufbereiten
zusätzlich vom Foto ab (neue TSV-Spalten?), wie kommt es in den Store (Store-Endpunkt statt Inline-Items?),
und was geschieht mit den 387 Bestandseinträgen. Die Alternativen sind wohl nicht nötig: `AnswerVariants`
zerlegt „a; b“ schon zur Laufzeit ([B-462](B-462-mehrdeutige-uebersetzung-verlangt-alle-bedeutungen.md),
[B-625](B-625-ein-eingabefeld-je-bedeutung.md)).

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater; Zahlen an einer DB-Kopie gezählt.
