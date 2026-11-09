---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Schalter geteilte Übungen springt zurück]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-450 · „Geteilte Übungen anderer Väter anzeigen" springt nach dem Klick zurück

Fund 6 in Sprint 28 (Lauf AG), gefunden vom vollen E2E-Lauf: `e2e/typ-manifest-faellt-aus.spec.ts:40` – „Clicking the checkbox did not
change its state". Ursache, am Code analysiert: der Review-Fix zu B-428 hat den Schalter in die Adresse gelegt (`?shared=1`,
`frontend/src/vater/VaterExercises.tsx`, `setFilter`). Fach-Wechsel und Schalter-Klick gehen beide über `setSearchParams`; der zweite
Aufruf baut die Adresse aus den Werten des **alten** Renders (veraltete Closure – das gerade gewählte Fach fehlt), und React Router
rendert die Adress-Änderung verzögert, darum springt der kontrollierte Haken zurück.

Reparaturweg: `setParams((prev) => …)` in der funktionalen Form, die die aktuelle Adresse ändert statt sie aus Render-Werten neu zu bauen;
danach `typ-manifest-faellt-aus.spec.ts` und `creator-katalog-ueberblick.spec.ts` grün, eine Mutante „Render-Werte statt `prev`".
Blockiert die Abnahme von B-426, B-428, B-431 (`wartet_auf`).

## Verlauf

- **2026-11-09** — angelegt beim Ende von Lauf AG: Fund 6 überschritt den Zähler von Sprint 28 (Freigabe 3), der Lauf endete sofort,
  ohne Reparatur.
