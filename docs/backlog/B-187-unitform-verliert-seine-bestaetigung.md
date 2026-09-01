---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/creator]
aliases: [UnitForm schliesst mit dem Banner, dritter Fall von B-151]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zum Sprint 1 des Nachtlaufs 2026-09-01
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-187 · Auch das Unit-Formular schließt sich mit seiner eigenen Bestätigung

Der dritte Fall derselben Wurzel wie [B-151](B-151-gespeichert-banner-verschwindet-mit-dem-formular.md) —
gefunden vom `frontend-reviewer` beim Review von B-151/B-173, **außerhalb** von deren Diff.

## User Story

Als **Creator** möchte ich nach dem Speichern einer Unit eine Bestätigung sehen, die lange genug steht, um
sie zu lesen, damit ich nicht raten muss, ob die Stoffnotiz angekommen ist.

## Ist-Stand am Code

- `VaterLehrwerke.tsx:634` setzt `"Gespeichert."` beim Ändern einer bestehenden Unit.
- Danach `onDone()` → `UnitPanel` setzt `setEditing(null)`, das Formular hängt aus — und mit ihm der
  `StatusBanner` in `VaterLehrwerke.tsx:732`.
- Genau die Kette, die B-151 für `TextbookForm` und `ProfileForm` behoben hat.

**Warum nicht in B-151 mitgenommen:** Die Zeilen liegen außerhalb des Diffs jener Story
(`UnitPanel` wurde im selben Lauf nur für B-184 an einer Platzhalter-Zeile angefasst). Eine geschätzte
Story während des Bauens wachsen zu lassen ist genau das, was B-151 bei ihrer eigenen Entstehung aus
B-148 vermieden hat.

**`entgangen_bei` bleibt leer und das ist keine Nachlässigkeit:** Der Zustand ist älter als jede Story
dieses Sprints und lag außerhalb ihres Diffs — er ist keiner Abnahme entgangen, sondern nie geprüft
worden.

## Die echte Lücke

Dieselbe wie in B-151: Die Bestätigung lebt in dem Element, dessen Verschwinden sie bestätigt.

## Offene Punkte

1. Denselben Schnitt wie B-151 nehmen (`onDone(message)` → Meldungsplatz an `UnitPanel`), oder eine Ebene
   höher an `VaterLehrwerke` andocken, wo seit B-173 schon ein Platz steht?
   Empfehlung: **an `UnitPanel`** — der Unit-Bereich ist ein eigener Akkordeon-Platz, und eine Meldung am
   Seitenrand wäre bei aufgeklappten Units weit weg. Zu prüfen beim Ausformulieren.
2. Trägt `UnitForm` beim Anlegen (`"Unit hinzugefügt."`) dasselbe Problem? Empfehlung: nachsehen, nicht
   vermuten — beim Lehrbuch-Formular war genau dieser Weg heil, weil das Anlege-Formular dauerhaft montiert
   ist.

## Verlauf

- 2026-09-01 · Aufgenommen aus dem `frontend-reviewer`-Review zu Sprint 1 des Nachtlaufs. Die drei
  Fundorte sind vom Reviewer benannt und von mir gegengelesen; `unverifiziert` bleibt darum aus. Bewusst
  **nicht** in B-151 hineingezogen: außerhalb ihres Diffs.
