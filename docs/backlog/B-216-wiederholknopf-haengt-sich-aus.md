---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [Erneut suchen verschwindet unter dem Finger, Fehlerbanner haengt aus]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zu Sprint 1 des Nachtlaufs 2026-09-04 (zu B-188)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-216 · Zwei Wiederhol-Knöpfe verschwinden im Moment des Klicks unter dem Finger

`useAsync.reload()` setzt `error` auf `null` und `loading` auf `true` **im selben Schritt**
(`frontend/src/lib/useAsync.ts:47-48`). Ein Fehlerbanner, das allein an `error` hängt, hängt sich damit in
genau dem Moment aus, in dem sein eigener Knopf gedrückt wird.

Betroffen sind zwei Stellen:

- `frontend/src/vater/WizardTrefferbereich` (B-162) — „Erneut suchen"
- `frontend/src/sohn/SohnShop.tsx:325-332`

Drei Folgen, in dieser Reihenfolge unangenehm:

1. **Der Fokus fällt auf `<body>`.** Der gedrückte Knopf ist weg. Das ist wörtlich der Grund, aus dem im
   selben Bildschirm „Auswahl leeren" montiert bleibt und nur gesperrt wird (`VaterWizard.tsx:504-506`) —
   hier wird es andersherum gemacht.
2. **Keine Rückmeldung während der Wiederholung.** Banner weg, nichts an seiner Stelle. Scheitert es
   erneut, blitzt es nur wieder auf; dauert es lange, sieht es aus, als hätte es geklappt.
3. **`disabled={laedt}` ist an diesen Knöpfen toter Code** — die Kombination „Fehler steht *und* Abfrage
   läuft" kann `useAsync` nie erzeugen, solange das Banner an `error` allein hängt.

**Die Reparatur ist schon vorgeführt:** `ListenAusfall` (B-188) hält den letzten Fehler fest, solange
geladen wird, und beschriftet den Knopf zu „Lädt…" um. Bewacht von `VaterWizard.test.tsx` („bleibt während
der Wiederholung stehen", „räumt sich ab, wenn die Wiederholung geklappt hat").

## Offene Punkte

1. Beide Stellen auf `ListenAusfall` umstellen, oder nur das Gedächtnis-Muster übernehmen? *Empfehlung:
   nur das Muster* — der Trefferbereich trägt mehr als ein Banner (Zählzeile, Leer-Satz, Platzhalter), und
   ihn in einen fremden Baustein zu pressen wäre eine Vereinheitlichung ohne Gewinn.
2. Gehört das Gedächtnis stattdessen in `useAsync` selbst (ein `letzterFehler`, das ein `reload` überlebt)?
   *Empfehlung: erst zählen.* Es wäre ein querschnittliches Feld an 105 Aufrufstellen für heute drei Fälle —
   dieselbe Frage, die [B-164](B-164-useasync-paart-frischen-zustand-mit-alten-daten.md) offen hat, und die
   gehört entschieden, bevor sie ein zweites Mal einzeln beantwortet wird.

## Verlauf

- **2026-09-04** — vom `frontend-reviewer` beim Review von B-188 benannt, am Code gegengelesen. Die
  **dritte** Stelle (`ListenAusfall`) ist im selben Sprint behoben, weil sie im Diff lag; diese zwei liegen
  außerhalb und laufen darum als eigene Story (README, „Ein Fund beim Bauen…"). Kein `entgangen_bei`: Beide
  sind älter als der Sprint.
