---
tags: [typ/story, status/abgenommen, bereich/katalog, rolle/creator]
aliases: [Grammar-Entwurf verrät Lösung]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-375
entgangen_bei: []
nachgeschaut: ""
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
- **2026-11-10** — gebaut und abgenommen in `50e9b07`, als Teil von
  [B-460](B-460-ki-aufgaben-programm-entscheidet.md). `DraftRules.AnswerNotInPrompt` prüft die Lösung als ganzes
  Wort im Aufgabentext; runde Klammern (Grundform, Auswahl) zählen nicht. Test
  `Eine_Grammatikaufgabe_mit_der_Loesung_im_Text_wird_abgelehnt`, Proben M6/M7 rot. Kein Live-Lauf mit dem Typ
  Grammar: Der Lehrplan erzeugt per Vorgabe nur Vocabulary und Cloze.
