---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/creator]
aliases: [rot und gruen gleichzeitig, zwei eigene StatusBanner]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-09-03 zu B-204 (Nachtlauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-212 · Auf der Lehrwerke-Seite stehen rot und grün gleichzeitig — der Weg ist nur ein anderer

## User Story

Als **Creator** möchte ich auf der Lehrwerke-Seite nicht gleichzeitig eine Fehlermeldung und eine
Erfolgsmeldung sehen, weil ich dann nicht weiß, welche zu meiner letzten Handlung gehört.

## Ist-Stand am Code

`VaterLehrwerke.tsx:526` (`SeriesForm`) und `:820` (`UnitForm`) rendern je einen **eigenen**
`StatusBanner` aus ihrer `action.message`. Der trägt nicht nur Eingabeprüfungen, sondern auch
Server-Fehler — `SeriesForm:425` ruft `action.runFor(updateTextbookSeries)`, das etwa an einer
Namensdublette scheitert.

Der Weg: Reihe A aufklappen, Speichern scheitert (rot unter ihrem Formular) → an Reihe B „Löschen", das
gelingt → grün am Platz der Seite (`:210`). Beides steht gleichzeitig im Blickfeld, mit demselben
Argument wie beim vierten Weg: **kein Aufrufer kann einen fremden lokalen Platz räumen.**

**Was B-204 tatsächlich geschlossen hat:** den Platz in `SeriesRow` (Kommentar `:326-333`, `remove` meldet
seit `:269` nach oben). Der Kommentar behauptet darüber hinaus „**ein** Platz, und B-197s Regel gilt
konstruktiv" — nachgemessen gilt das nur für `SeriesRow`; die Datei trägt weiterhin **drei**
`action.message`-Banner (`:526`, `:820`, `:931`).

## Offene Punkte

1. Beide Formulare nach oben melden lassen, oder ist ein lokaler Platz an einem **Formular** (anders als
   an einer Zeile) richtig? *Empfehlung: erst die Frage beantworten, dann bauen* — ein Formular hat, anders
   als eine Zeile, einen eigenen Fokusbereich; das ist ein möglicher Grund für eine bewusste Ausnahme, und
   dann gehört er als solcher aufgeschrieben statt stillschweigend zu gelten.
2. Den Kommentar `:326-333` in jedem Fall korrigieren — er behauptet heute mehr, als die Datei hält.

## Verlauf

- **2026-09-03** — aus der **Nachschau** zu B-204 (Nachtlauf). `entgangen_bei` bleibt **leer**: die beiden
  Banner sind älter als B-204s Diff, und dessen Ziel (der Platz in `SeriesRow`) ist erfüllt. Zuzurechnen
  ist B-204 nur der zu weit gefasste Kommentar.
