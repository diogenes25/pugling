---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [useChosenPlan gemerkt-Zweig ohne Test]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf H, Teil 2 in Lauf I (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-254]
wartet_auf: ""
---

# B-324 · Dass der gemerkte Plan vor dem spielbaren gewinnt, prüft kein Test

[B-254](B-254-weg-zeigt-englischen-planfehler.md) AK 2: `useChosenPlan` (`frontend/src/sohn/useChosenPlan.ts:16-18`) wählt
gemerkt → spielbar → erster. Den ersten Zweig trägt kein Fall: in `SohnProgress.test.tsx` liegt die gemerkte Id (99) nie
in der Planliste, `SohnHome` hat keinen Test, keine E2E-Spec bedient „Plan wählen“. Mutante: `plans.data.find((p) =>
p.id === planId) ??` streichen → alles grün. Folge: das Kind wählt auf der Startseite einen nicht spielbaren Plan,
`setPlanId(4)` wird vom Effekt sofort auf den spielbaren zurückgesetzt – der Wähler wäre tot.

Vorschlag: ein Fall mit gemerkter, nicht spielbarer Id **in** der Liste; erwartet `overview` mit dieser Id und kein
`setPlanId`.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau (frischer `frontend-reviewer`, `git show f96d358`) in Lauf I
  ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); aus Code gelesen, Mutante hergeleitet, nicht gemessen.
