---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor]
aliases: [halbe Reparatur units.error, alte Units waehrend des Wechsels]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-09-01 zu B-172
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-172]
wartet_auf: ""
---

# B-195 · Beim Reihenwechsel bleiben die Units der alten Reihe wählbar

Die Reparatur aus B-172 ist an dieser Stelle **halb**: Sie deckt den Fehlerfall, nicht das Ladefenster.

## User Story

Als **Vater** möchte ich beim Wechsel der Reihe keine Unit angeboten bekommen, die zur vorigen Reihe
gehört, damit mein Speichern nicht mit einer Fehlermeldung endet.

## Ist-Stand am Code

`ChildMaterialSection.tsx:291` schreibt `units.error ? [] : units.data ?? []`. Das entschärft die
veraltete Liste im **Fehlerfall** — aber nicht während des laufenden Abrufs:

- `useAsync` lässt `data` über den Wechsel stehen,
- und setzt `error` beim Start jedes Laufs auf `null`.

Während des Reihenwechsels bietet das Feld also weiter die Units der **alten** Reihe an, **wählbar**.

**Der Schaden ist serverseitig gedeckelt**, und auch das ist nachgerechnet: `TextbooksController.cs:146-147`
weist eine Unit aus einem fremden Werk mit `400` ab. Es ist also ein **vermeidbarer Fehlschlag**, keine
Zerstörung — darum P3 und nicht höher.

**Dieselbe Wurzel wie [B-164](B-164-useasync-paart-frischen-zustand-mit-alten-daten.md)** („frischer
Zustand mit alten Daten"), das die gemessene Breite trägt (110 `useAsync`-Aufrufstellen, 67 mit
Abhängigkeiten). Diese Story ist der eine Fall, der in **abgenommener Arbeit** sitzt und darum die
Wirkungszahl des Bereichs betrifft — B-164 selbst trägt kein `entgangen_bei`.

## Offene Punkte

1. Hier einzeln lösen oder auf B-164 warten? Empfehlung: **auf B-164 warten**, falls der zeitnah gebaut
   wird — sonst entstehen zwei Fassungen derselben Regel, und die veraltete gewinnt. Wenn nicht: hier ein
   Schlüsselvergleich wie in `VaterWizard` (`seitenSchluessel`/`zeilenVeraltet`, B-169), der genau für
   diesen Fall schon entschieden und getestet ist.

## Verlauf

- 2026-09-01 · Aufgenommen aus der **Nachschau** zu B-172 (Nachtlauf). Die Zeile stammt aus dem
  Abnahme-Commit `289b000`, darum `entgangen_bei: [B-172]`.
