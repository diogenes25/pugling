---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator]
aliases: [Grammar-Entwurf verrät Lösung]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-375
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-458 · Ein KI-Entwurf vom Typ Grammar darf die Lösung schon in der Aufgabe nennen

Beim Lauf des KI-Creators mit `gemma4:e2b` gegen das Access-Lehrwerk (B-375, 2026-11-10) enthielt ein
Grammar-Entwurf die Aufgabe „They were playing football … While they ______ (play) football …“ mit der Lösung
`were playing`. Die Lösung stand damit schon im Aufgabentext. Für Cloze gibt es eine Regel, die genau das
verwirft („die Lösung steht im Text und ist damit verraten“, `ClozeStrategy`). Für `GrammarStrategy` fehlt
eine solche Regel offenbar, darum ging der Entwurf durch. Ein zweiter Entwurfsfehler im selben Lauf (eine
fragwürdige Zeitform als einzige Lösung) ist mechanisch nicht zu fangen. Den fängt nur der Testmodus des Vaters.

## Verlauf

- **2026-11-10** — angelegt (Fund beim Live-Lauf in B-375).
