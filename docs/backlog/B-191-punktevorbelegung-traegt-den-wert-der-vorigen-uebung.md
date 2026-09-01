---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor]
aliases: [pointsFrom current heisst zweierlei, Punkte der vorigen Uebung wandern mit]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-09-01 zu B-45
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-45]
wartet_auf: ""
---

# B-191 · Die Punkte-Vorbelegung trägt den Wert der zuvor angeklickten Übung mit

Die Fehlerfamilie dieses Repos, wörtlich: **eine Bedingung, die zwei Situationen zusammenzieht.**

## User Story

Als **Vater** möchte ich, dass die vorgeschlagene Zielbelohnung zu der Übung gehört, die ich gerade
gewählt habe, damit ich meinem Kind nicht versehentlich das Fünffache für die falsche Aufgabe zusage.

## Ist-Stand am Code

`PlanPositions.tsx:184-186` — `pointsFrom(ex, current)` liefert `current` zurück, wenn die Übung keine
Empfehlung trägt. **`current` steht dabei für zwei verschiedene Dinge:**

- „Der Vater hat diese Zahl selbst getippt" — der Fall, für den die Regel gedacht ist.
- „Die **vorher angeklickte** Übung hat diese Zahl empfohlen" — der Fall, den niemand gewogen hat.

**Ablauf, an dem es sichtbar wird:**

1. Übung A anklicken (`rewardPoints = 100`) → das Feld steht auf 100.
2. Übung B anklicken (`rewardPoints = 0`, also **keine** Empfehlung) → `pointsFrom` gibt `current` = 100
   zurück, das Feld bleibt auf 100.
3. Hinzufügen → die Position zu B trägt **100 Münzen** Zielbelohnung, die weder B empfiehlt noch jemand
   getippt hat. Der Server allein hätte 20 gesetzt.

Der Kommentar `PlanPositions.tsx:177-179` und die Testkonstante `GETIPPT` in `PlanPositions.test.ts:146`
schreiben die Annahme „selbst getippt" fest — und **können die beiden Fälle nicht unterscheiden**. Genau
darum ist die Suite grün.

## Die echte Lücke

Es fehlt der Zustand, der „getippt" von „geerbt" trennt. Dasselbe Muster wie `Testable` als Typ- statt
Tages-Aussage (B-114) oder „leer" für „nichts gekauft" und „Laden gescheitert" (B-111).

## Offene Punkte

1. Woran erkennt man „selbst getippt"? Empfehlung: ein `touched`-Flag am Feld, wie es der Assistent für
   den Feinschliff schon führt (`VaterWizard.tsx`, `touchedFineTune`) — kein neues Muster, sondern ein
   bereits im Repo entschiedenes.
2. Soll ein Übungswechsel eine getippte Zahl überschreiben? Empfehlung: **nein** — eine bewusste Eingabe
   ist die stärkere Aussage. Zu bestätigen.
3. Deckt der Testweg beide Richtungen? Empfehlung: der Testfall muss **zwei** Übungen nacheinander
   anklicken; ein Fall mit nur einer kann diesen Defekt nicht fangen — das ist der Grund, warum er
   durchkam.

## Verlauf

- 2026-09-01 · Aufgenommen aus der **Nachschau** zu B-45 (Pflichthandlung der Retrospektive, Nachtlauf).
  Ablauf und Fundorte vom Nachschau-Agenten am Code belegt und von mir gegengelesen. `entgangen_bei:
  [B-45]` — der Code stammt aus dem Abnahme-Paar `48f06b1`/`5590ef3`.
