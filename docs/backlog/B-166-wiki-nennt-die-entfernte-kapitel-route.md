---
tags: [typ/story, status/idee, bereich/doku]
aliases: [Kapitel-Route im Wiki, tote Route in der Wissenskarte]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: beim Bauen von B-163 gefunden (docs/backlog/B-163-art-und-typ-tragen-dieselben-woerter.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-166 · Wiki und Endpunkt-Karte nennen die entfernte Kapitel-Route

## Ist-Stand (belegt)

Die `Chapter`-Entität ist **entfernt** — `grep -rn "class Chapter" backend/Pugling.Api/Models/` findet
nichts, und seit B-106 hängt jede Übung zwingend an einer Lehrwerk-`SeriesUnit`
(`backend/Pugling.Api/CLAUDE.md` → „Lern-Katalog"). Die lebende Route lautet:

```text
api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/<typ>
```

Drei Dateien führen dagegen weiter `subjects/{id}/chapters/{id}/…`:

| Datei | Vorkommen |
|---|---|
| `wiki/03-uebungstypen.md` | 2 (u. a. das „Vollständige Beispiel" in Abschnitt 4) |
| `docs/endpunkt-beziehungen.md` | mindestens 1 |
| `docs/backlog/B-80-tags-geben-fremde-konfiguration-preis.md` | mindestens 1 |

## Warum das mehr wiegt als ein Tippfehler

`docs/endpunkt-beziehungen.md` ist die Datei, die die Root-`CLAUDE.md` als **Einstieg** benennt („Erst die
Wissenskarte, dann breit suchen"). Eine tote Route dort führt jede Sitzung, die dem Rat folgt, an eine
Adresse, die 404 liefert — und das ausgerechnet an der Stelle, die Tokens sparen soll. Das Wiki-Beispiel
ist zusätzlich zum Kopieren gedacht.

Der Backlog-Eintrag B-80 ist ein **Zeitzeuge** und bleibt, wie er ist: dort war die Route zum Zeitpunkt
der Aufnahme richtig, und `## Verlauf` ist append-only.

## Offene Punkte

1. Wie weit reicht es wirklich? Empfehlung: nicht nur auf `chapters` greppen, sondern auch auf
   `chapterId` und `Kapitel` — der Begriff kann als Prosa überlebt haben, wo die Route schon stimmt.
   (Ein solcher Fall ist beim Finden schon aufgefallen und in B-163 mitkorrigiert: `README.md` nannte
   „Fächer, Kapitel" in der Aufzählung dessen, was die App kann.)
2. Ersetzen oder umschreiben? Empfehlung: **umschreiben**. Ein Beispiel braucht eine `seriesId` und eine
   `seriesUnitId`, die es vorher irgendwo herbekommt — eine reine Pfad-Ersetzung erzeugt ein Beispiel,
   das niemand nachvollziehen kann, weil die beiden Ids aus der Luft fallen.
3. Sollte ein Tor das halten? Empfehlung: **prüfen, nicht annehmen** — es gibt bereits einen
   Client-Routen-Wächter (`docs/codequalitaet-gates-plan.md`), und dessen Erfahrung war, dass halbe
   Parser eine Rot-Liste brauchen. Ein Wächter, der Markdown nach Routen absucht und gegen das
   OpenAPI-Dokument hält, ist attraktiv und genau darum vor dem Bauen zu vermessen.

## Verlauf

- 2026-08-13 · Aufgenommen. Beim Nachziehen der Typ-Label-Prosa für B-163 gefunden: dieselbe Datei trug
  neben dem alten Label auch eine Route auf eine Ebene, die es nicht mehr gibt. Das Label war in Scope,
  die Route nicht.
