---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor]
aliases: [useGemerkterFehler Dep-Wechsel, alter Fehler bei neuer Suche]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 1)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-216]
wartet_auf: ""
---

# B-227 · Das Fehler-Gedächtnis überlebt auch den Start einer *anderen* Abfrage

B-216 hat die Regel „der letzte Fehler überlebt den Start der Wiederholung" in `useGemerkterFehler`
gefasst. Der Hook weiß aber nicht, ob dieselbe Abfrage wiederholt wird oder eine andere startet – und
`useAsync` setzt bei einem Wechsel der Abhängigkeiten ebenfalls `error = null, loading = true`.

## User Story

Als **Vater** möchte ich nach einer gescheiterten Übungssuche beim Ändern eines Filters sehen, dass eine
*neue* Suche läuft – nicht den alten Fehlertext mit einem „Lädt…"-Knopf, der zu einer Abfrage gehört, die
es nicht mehr gibt.

## Ist-Stand am Code

Nachgesehen am 2026-10-17 (Nachschau durch einen frischen Reviewer-Agenten, von mir gegengelesen).

- `frontend/src/lib/useGemerkterFehler.ts:22-29`: Rückgabe `fehler ?? (laedt ? letzter : null)` – ohne
  Kenntnis, welche Abfrage gerade lädt.
- `frontend/src/vater/VaterWizard.tsx` (Trefferbereich) und `frontend/src/vater/PlanPositions.tsx`
  (`AddPosition`): Nach einer gescheiterten Suche ändert der Vater einen Filter → `useAsync` startet neu
  (`error = null`, `loading = true`) → das Banner „Die Übungssuche ist gescheitert: <alter Fehler>" mit
  Knopf „Lädt…" bleibt stehen, und die Zählzeile sagt „Suche gescheitert" statt „Suche läuft…", weil
  `gescheitert` dort vor `zeilenVeraltet` ausgewertet wird (`VaterWizard.tsx:702`, `PlanPositions.tsx:534`).
- Vor `d834a8e` verschwand das Banner in genau diesem Fall (richtig); neu ist, dass es bleibt. Für
  `ListenAusfall` und die Shop-Karte ist das folgenlos – ihre Abfragen haben keine wechselnden Abhängigkeiten.
- Das Hook-Doc behauptete „den Start seiner **Wiederholung**"; nachgezogen auf das, was der Code tut, mit
  Verweis hierher.

## Die echte Lücke

Das Gedächtnis braucht einen Schlüssel, der sagt, *wofür* es gilt. Zwei Wege: `useGemerkterFehler(fehler,
laedt, schluessel)` räumt `letzter` bei einem Schlüsselwechsel (im Trefferbereich und im Planbau gibt es ihn
schon: `filterKey`); oder die Aufrufer prüfen `zeilenVeraltet` vor `gescheitert`. Der erste Weg behebt die
Ursache, der zweite nur die Zählzeile – das Banner bliebe.

## Offene Punkte

1. Schlüssel-Parameter am Hook (empfohlen) oder Reihenfolge in den Aufrufern? *Empfehlung: Schlüssel* –
   das Banner ist das Symptom, nicht die Zählzeile; und `ListenAusfall`/Shop lassen den Parameter einfach weg.

## Verlauf

- **2026-10-17** — angelegt aus der **Nachschau** zu Sprint 1 des Nachtlaufs (Commit `d834a8e`),
  `entgangen_bei: B-216`. Direkt `ausformuliert`: Ist-Stand mit `Datei:Zeile` belegt.
