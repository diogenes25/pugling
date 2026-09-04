---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [useExerciseTypes schluckt den Fehler, Typ-Manifest ohne error]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zu Sprint 1 des Nachtlaufs 2026-09-04 (zu B-188)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-215 · Das Typ-Manifest schluckt seinen Ausfall — und niemand kann ihn anzeigen

[B-188](B-188-wizard-verschweigt-drei-weitere-ausfaelle.md) hat die drei Optionslisten des Assistenten
wahrheitsfähig gemacht. Die **vierte** kam nicht mit, weil sie strukturell anders gebaut ist.

`frontend/src/lib/exerciseTypes.ts` fängt den Fehlschlag des Manifest-Abrufs ab und setzt eine leere Liste
(`.catch(() => setAll([]))`), begründet als „lesbar statt kaputt". Der Hook hat damit **gar keinen**
Fehlerkanal — ein Konsument *kann* den Ausfall nicht anzeigen, egal wie sorgfältig er ist.

Sichtbar wird das im Assistenten als „Typ"-Pulldown, das nur noch „– alle Typen –" anbietet, und in
Beschriftungen, die auf die rohen Schlüssel zurückfallen (`Essay` statt „Aufsatz"). Dieselbe Klasse wie die
in B-188 behobene `categories`-Liste, nur eine Ebene tiefer.

## Offene Punkte

1. Bekommt der Hook ein `error` neben seiner leeren Liste, oder bleibt die Schluck-Entscheidung und wird
   nur besser dokumentiert? *Empfehlung: `error` ergänzen und die leere Liste behalten* — dann kann jeder
   Konsument selbst wählen, ob er anzeigt, und die alte Zusage („lesbar statt kaputt") bleibt gültig.
2. Wer zeigt es dann an? Das Manifest wird an mehreren Stellen gelesen. *Empfehlung: erst zählen, dann
   entscheiden — dieselbe Gefälle-Frage wie in B-174/B-188 (was ist handlungsleitend, was nur eine
   verlorene Auswahlmöglichkeit).*

## Verlauf

- **2026-09-04** — vom `frontend-reviewer` beim Review von B-188 benannt und von mir am Code gegengelesen.
  Bewusst **nicht** in B-188 mitgebaut: Der Hook ist geteilt, ein `error` dort trifft jeden Konsumenten und
  wäre damit ein Umbau außerhalb des Sprint-Diffs. `unverifiziert: true`, weil der sichtbare Schaden
  hergeleitet und nicht vorgeführt ist.
