---
tags: [typ/story, status/idee, bereich/werkzeug, rolle/creator]
aliases: [kacheln.ps1 Drehung, Fotos auf dem Kopf, Übersichtsbogen]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ".claude/skills/lehrwerk-aufbereiten"
migration: ""
vertragsbruch: ""
quelle: Lauf des Skills lehrwerk-aufbereiten an Découvertes Série bleue 2 (2026-11-11)
unverifiziert: true
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-474 · `kacheln.ps1` kann nicht drehen, und für die Sichtung von 56 Fotos fehlt ein Übersichtsbogen

Beim zweiten Buch lag das Buch bei 13 von 56 Fotos auf dem Kopf oder quer (Inhaltsverzeichnis, Unit-Seiten). Das
Handy schreibt dafür keine EXIF-Drehung, weil es flach über dem Tisch gehalten wurde; `kacheln.ps1` wertet nur
EXIF aus und liefert kopfstehende Kacheln. Umgangen mit einem Pillow-Einzeiler, der gedrehte Kopien nach
`gedreht/` schreibt und erst dann kachelt.

Zweiter Fund im selben Lauf: Schritt 1 des Skills („jedes Foto einmal ansehen“) kostet bei 56 Fotos 56 volle
Bilder. Ein Übersichtsbogen (12 Miniaturen je Blatt, mit Nummer beschriftet) hat für Seitenart und Drehung
gereicht – fünf Bilder statt 56.

Dritter Fund: Im Blitzlicht sind die obersten zwei bis fünf Zeilen jeder linken Vokabelseite blass. Eine
Ausschnitt-Vergrößerung (Bereich angeben, zweifach hochskaliert) hat jede dieser Zeilen geklärt; die
3-Zeilen-Kachel allein nicht.

Naheliegende Abhilfe: `-Drehung 0|90|180|270`, `-Uebersicht` (Kontaktbogen) und `-Ausschnitt x,y,b,h` in
`kacheln.ps1`, dazu je ein Satz in `SKILL.md` Schritt 1.

## Verlauf

- **2026-11-11** — angelegt (Lauf an Découvertes Série bleue 2).
