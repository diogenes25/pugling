---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/backend, rolle/supervisor]
aliases: [pointsGoalMet ohne Obergrenze im UI, Vererbungszweig wird nie genommen]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-09-01 zu B-45
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-45]
wartet_auf: ""
---

# B-192 · Die Obergrenze der Punkte-Empfehlung schützt den Weg nicht, den der Vater geht

## User Story

Als **Vater** möchte ich, dass eine unsinnig hohe Empfehlung aus einer fremden Katalog-Übung auch dann
begrenzt wird, wenn ich den Lehrplan über die Oberfläche baue — nicht nur über die API.

## Ist-Stand am Code

B-45 begrenzt die geerbte Empfehlung serverseitig auf `is > 0 and <= 200`
(`PlanPositionsController.cs:205`). Der Zweig greift aber **nur, wenn `pointsGoalMet` `null` ist**.

- `PlanPositions.tsx:185` (`pointsFrom`) hat **keine Obergrenze**.
- `PlanPositions.tsx:248` (`settingsToDto`) schickt `pointsGoalMet` **immer als Zahl**, nie `null`.

**Folge:** Das Vater-Web nimmt den Vererbungszweig des Servers nie. Eine fremde Übung mit
`rewardPoints = 100000` belegt das Feld mit 100000 vor, das wird ausdrücklich gepostet, und der Server
nimmt es unbegrenzt an — so ausdrücklich zugesichert in `PlanPositionCrudTests.cs:335-340`. Begrenzt ist
allein der API-First-Pfad.

**Mildernd, und darum P3:** Die Zahl steht sichtbar im Feld; der Vater kann sie sehen und ändern. Der
Kommentar `PlanPositions.tsx:182` nennt die Asymmetrie sogar („dort zusätzlich") — die Story B-45 führt
sie aber nirgends als offenen Punkt, und ihr Abnahme-Commit beschreibt das Loch als geschlossen.

## Die echte Lücke

Eine Regel, die an einem Zustand hängt (`null`), den der einzige reale Aufrufer nie erzeugt. Der Test
belegt die Regel, nicht ihre Wirksamkeit.

## Offene Punkte

1. Grenze im Frontend spiegeln, oder das Feld `null` lassen, wenn der Vater nichts getippt hat?
   Empfehlung: **`null` lassen** — dann gilt genau eine Regel an genau einer Stelle, und sie ist die
   serverseitige. Hängt an B-191 (dort entsteht das `touched`-Flag, das „nichts getippt" überhaupt
   erkennbar macht).
2. Soll der Server auch einen **ausdrücklich geschickten** Wert begrenzen? Empfehlung: nachdenken statt
   reflexhaft ja — eine bewusste Vatereingabe von 500 ist womöglich gewollt („der Stick" ist Produktkern).
   Das ist eine Produktentscheidung, kein Defekt.

## Verlauf

- 2026-09-01 · Aufgenommen aus der **Nachschau** zu B-45 (Nachtlauf). `entgangen_bei: [B-45]`.
  Offener Punkt 2 ist ausdrücklich eine Frage an den Menschen und wird nicht autonom entschieden.
