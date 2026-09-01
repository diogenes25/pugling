---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/creator]
aliases: [B-173 AK 2 nicht gebaut, Loeschen ohne Meldung]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-09-02 zu Sprint 1 des Nachtlaufs
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-173]
wartet_auf: ""
---

# B-196 · Das Löschen einer Reihe meldet nichts — und lässt die Bestätigung von vorhin stehen

**Das ist ein Akzeptanzkriterium, das abgenommen wurde, ohne gebaut zu sein.** B-173s AK 2 nennt das
Löschen ausdrücklich, und ihre Entscheidung 1 begründet den Meldungsplatz auf Seitenebene sogar damit,
dass er „auch die Sortier-Variante (Umbenennen) und das Löschen abdeckt".

## User Story

Als **Creator** möchte ich nach dem Löschen einer Reihe eine Bestätigung sehen, die zum Löschen gehört,
damit ich nicht die Erfolgsmeldung von vorhin für sie halte.

## Ist-Stand am Code

`VaterLehrwerke.tsx:251-256`:

```ts
if (await action.run(() => api.deleteTextbookSeries(series.id))) onChanged();
```

Kein `okText`, kein `onMessage(…)`. Ein geglücktes Löschen erzeugt **gar keine** Meldung — weder in der
Zeile (die ohnehin verschwindet) noch am Seitenplatz.

**Und der zweite Teil ist der schlimmere:** `remove()` **räumt** den Seitenplatz auch nicht. Nach „Reihe A
speichern → Reihe B löschen" steht das grüne „Gespeichert." von A über der gerade verschwundenen Zeile B
und liest sich als Bestätigung des Löschens. Das ist die Fehlerfamilie von B-173 selbst, nur in die
andere Richtung: **eine Meldung, die zu einer anderen Handlung gehört als der, die man gerade getan hat.**

## Die echte Lücke

Der Seitenplatz ist zustandsbehaftet, die Regel „der Erfolg reist, Fehler bleiben lokal" ist
zustandsfrei formuliert. Wo eine Handlung gar keine Meldung erzeugt, bleibt die vorige stehen.

## Offene Punkte

1. Nur das Löschen der Reihe, oder alle stummen Erfolgspfade derselben Fläche? Empfehlung: **erst zählen**
   — `ChildMaterialSection.tsx:231` (Buch entfernen) ist derselbe Fall und war schon vor dem Sprint stumm.
2. Zusammen mit [B-197](B-197-meldungsplatz-wird-beim-absenden-nicht-geraeumt.md) bauen? Empfehlung: **ja**
   — beide brauchen dieselbe Verallgemeinerung des Meldungsplatzes, und getrennt entstünden zwei Muster.

## Verlauf

- 2026-09-02 · Aufgenommen aus der **Nachschau** zu Sprint 1 (zweiter Nachtlauf). Der Fund ist ernster als
  ein gewöhnlicher Defekt: Er belegt, dass die Abnahme von B-173 ein eigenes Akzeptanzkriterium nicht
  geprüft hat. Der Rollengang (43/43 E2E) hat es nicht gefangen, weil keine Spec eine Reihe löscht.
