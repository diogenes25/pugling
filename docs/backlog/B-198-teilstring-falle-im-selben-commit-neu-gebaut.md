---
tags: [typ/story, status/ausformuliert, bereich/frontend]
aliases: [Buch hinterlegt ohne exact, B-189 wiederholt]
status: ausformuliert
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-02 zu Sprint 1 des Nachtlaufs
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-151]
wartet_auf: ""
---

# B-198 · Die Teilstring-Falle aus B-189 wurde im selben Commit neu gebaut

## User Story

Als **Entwickler** möchte ich, dass eine gerade diagnostizierte Testfalle nicht drei Dateien weiter erneut
entsteht, damit der volle Suite-Lauf nicht wieder aus einem Grund rot wird, der nichts über das Produkt
sagt.

## Ist-Stand am Code

`frontend/e2e/bestaetigung-ueberlebt.spec.ts:83`:

```ts
await expect(page.getByText("Buch hinterlegt.")).toBeVisible();
```

Ohne `{ exact: true }`. `getByText` vergleicht als **Teilstring**, und `ChildMaterialSection.tsx:65`
rendert „Noch kein **Buch hinterlegt.**" — solange `books.reload()` nach dem Anlegen läuft, hält
`useAsync` die alte, leere Seite; der Leer-Text steht also neben dem Banner. Zwei Treffer,
`strict mode violation`.

**Die Klickfolge ist exakt die aus [B-189](B-189-teilstring-zusicherung-bricht-im-vollen-lauf.md)**: frisch
angelegtes Kind → leere Bücherliste → „Buch hinterlegen".

**Was daran mehr ist als ein vergessenes Flag:** Commit `e479d83` hat diese Falle eine Datei weiter
(`kind-lehrbuch-fach.spec.ts:51`) **diagnostiziert und behoben** — und sie im selben Commit hier neu
eingebaut. `6688ff5` hat danach gezielt beide „Gespeichert."-Zusicherungen dieser Datei mit
`{ exact: true }` nachgezogen und Zeile 83 übersehen.

**Warum der 43/43-Beleg der Abnahme das nicht ausschließt:** B-189s eigene Messung sagt, dass genau dieses
Konstrukt **allein grün** ist und erst unter der Last des vollen Laufs rot wird. Der grüne Lauf ist also
kein Gegenbeweis, sondern derselbe Zufall, der B-189 wochenlang verborgen hat.

## Offene Punkte

1. Nur diese Zeile, oder alle `getByText` ohne `exact` in `e2e/` zählen? Empfehlung: **zählen** — die Menge
   ist auszählbar, und die Zahl entscheidet, ob daraus ein Wächter wird. B-189s Entscheidung 2 hat einen
   Wächter verworfen, weil Teilstring meistens gewollt ist; drei Wiederholungen wären ein Grund, das noch
   einmal anzusehen.

## Verlauf

- 2026-09-02 · Aufgenommen aus der **Nachschau** zu Sprint 1 (zweiter Nachtlauf).
