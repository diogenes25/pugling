---
tags: [typ/story, status/ausformuliert, bereich/frontend]
aliases: [label-row ohne Typografie, Gruppen-Ueberschrift als Fliesstext]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-08-13 zu B-11
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-11]
---

# B-176 · Eine Gruppen-Überschrift verliert ihre Optik, sobald sie kein `<label>` mehr ist

Der a11y-Schritt war richtig, seine sichtbare Folge ist mitgekommen und niemandem aufgefallen.

## Ist-Stand am Code (selbst nachgeprüft)

`frontend/src/index.css:224` stylt die Feld-Überschrift ausschließlich über das Element:

```css
.field label { font: 700 12px/1 var(--font-body); color: var(--muted); text-transform: uppercase; … }
```

`.label-row` (`:229`) setzt **nur** `display:flex; align-items:center; gap:6px` — keine Typografie.

In `frontend/src/vater/VaterExerciseCreate.tsx:278` wurde die Gruppen-Überschrift im Review-Fix `2d42f13`
von `<label>` auf `<span className="label-row">` umgestellt, mit einer im Code stehenden und **korrekten**
Begründung: „Gruppen-Überschrift, darum `span` statt `label`: sie beschriftet kein Eingabefeld." Die Folge:
„Freigabe (Startzustand)" rendert als **Fließtext**, während „Schularten" und „Lern-Standards" in derselben
Karte als Überschrift erscheinen.

Dasselbe Muster steht an weiteren Stellen — ein `label-row`-`span`, der Text **direkt** enthält statt eines
`<label>`: `ClozeTexts.tsx:244,272`, `exerciseConfig.tsx:438,719`. Davon zu unterscheiden ist die in
`frontend/CLAUDE.md` **dokumentierte** Verwendung (`label-row` als Hülle um `<label className="checkline">` +
`<InfoHint>`) — dort trägt das innere `<label>` die Optik, und alles ist in Ordnung. Die Fundstellen sind
darum einzeln anzusehen und nicht pauschal umzustellen; eine Zahl schreibe ich hier bewusst nicht hin
(siehe [B-175](B-175-zwei-gerottete-zahlen-in-kommentaren.md)).

## Fehlerfamilie

Korrekt für den bedachten Fall — die Zugänglichkeit —, schmal beim Rest: die Optik hing am Elementnamen,
und mit dem Elementnamen ging sie weg. Kein Test deckt Typografie, und im Rollengang liest man eine
Überschrift, die wie Fließtext aussieht, als Fließtext.

## Angriffsplan (Vorschlag)

Eine Klasse für Gruppen-Überschriften, die dieselbe Typografie trägt (z. B. `.field > .label-row` oder eine
eigene `.group-label`), und die betroffenen Stellen darauf ziehen. Damit ist die Optik an die **Rolle**
gebunden statt an das Element — was der eigentliche Fehler war.

**Testweg**: keiner auf Testebene (Typografie ist nicht sinnvoll zusicherbar). Beleg ist der Blick auf die
Karte „Übung anlegen": drei Gruppen, drei gleich aussehende Überschriften.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-11, dort als kosmetischer Fund gemeldet. Von mir
  gegengeprüft: `.label-row` trägt tatsächlich keine Typografie, und die betroffene Zeile stammt aus dem
  Review-Fix der Story selbst.
