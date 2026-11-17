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

# B-648 · Birkenbihl: jedes Wort eines Textes hat eine Vokabel

Teil (b) aus der Teilung von [B-646](B-646-vokabel-schema-nachfuellen.md). Ist-Stand (Dekodierung nur über die
exakte Oberfläche, CRUD-Anlage verankert keine Glossen, Übung #3 mit 0 von 8 verknüpft) und die Begründungen
stehen dort. **Baut auf [B-647](B-647-vokabeln-vollstaendig-ladeweg-und-bestand.md) auf:** Ohne strukturierte
Artikel- und Verbangaben findet die Suche keine Grundform.

## User Story

Als **Creator** möchte ich, dass jedes Wort eines Birkenbihl-Textes mit einer Vokabel verknüpft ist, und zwar
auch bei gebeugten Formen und Wörtern, die im Speicher mit Artikel oder `to` stehen. **Damit** kann die
Birkenbihl-Methode ausgebaut werden und auf den Vokabeln aufbauen.

## Entscheidungen

Aus B-646 übernommen: **1** (die Suche lässt Artikel und `to` beim Vergleichen weg), **4** (anlegen statt
abweisen, Übung ohne Glosse wird im Editor als unvollständig angezeigt und bleibt spielbar), **5** (gebeugte
Formen bei Bedarf, die Grundform schlägt die Suche vor, der Creator bestätigt).

## Akzeptanzkriterien

- Die Dekodierung findet `ville` → `la ville` und `live` → `(to) live`. Für `is` schlägt sie die Grundform `be`
  vor. Integrationstests in `Pugling.Api.Tests` (Nachbar `BirkenbihlExerciseTests`) decken alle drei Fälle ab.
- `POST …/sentences` verankert jedes Wort mit Glosse im Speicher. Anlegen und Ersetzen der ganzen Übung tun das
  auch, wie bei der Übersetzungsübung. Je ein Test, der vor dem Umbau rot ist.
- Bestätigt der Creator eine vorgeschlagene Grundform, entsteht die Form als eigener Eintrag mit `BaseFormId`
  und Verbangaben.
- Ein Wort ohne Glosse macht die Übung im Editor sichtbar unvollständig. Die Übung bleibt dem Kind spielbar.
- Übung #3 „Getting to know each other" ist nach einem Lauf auf einer Kopie der echten DB vollständig verknüpft
  (8 von 8).

## Verlauf

- **2026-11-17** — aus B-646 geteilt (Entscheidung 8), Stufe `gegrillt` mit den Entscheidungen 1, 4, 5 aus B-646.
