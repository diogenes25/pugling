---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [useChosenPlan gemerkt-Zweig ohne Test]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf H, Teil 2 in Lauf I (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: false
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

## User Story

Als Kind möchte ich, dass der Plan, den ich auf der Basis wähle, gewählt bleibt – auch wenn ein anderer gerade
spielbar ist.

## Ist-Stand am Code

`useChosenPlan.ts:16-18` wählt richtig (gemerkt → spielbar → erster); nur der erste Zweig hat keinen Fall. Testlücke,
kein Verhaltensfehler – die Mutante „Zweig gestrichen“ ist der Beleg.

## Akzeptanzkriterien

1. Ein Fall, in dem die gemerkte Id ein nicht spielbarer Plan des Kindes ist und ein anderer spielbar ist und vorn
   steht: geladen wird der gemerkte, `setPlanId` wird nicht gerufen.

## Entscheidungen

1. Im bestehenden `SohnProgress.test.tsx` (derselbe Hook, dieselben Mocks) statt einer `SohnHome`-Testdatei. Kosten:
   die Startseite selbst bleibt ohne eigenen Fall; sie teilt den Hook.

## Schätzung

XS, `frontend`, keine Migration, kein Vertragsbruch.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau (frischer `frontend-reviewer`, `git show f96d358`) in Lauf I
  ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); aus Code gelesen, Mutante hergeleitet, nicht gemessen.
- **2026-10-28** — in Lauf I, Teil 2 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) unter Freigabe 1 ausformuliert, gegrillt, geschätzt (XS) und gebaut. Kein Code-Fix (Testlücke). Mutante `find((p) => p.id === planId) ??` gestrichen → **1 failed**; Kontrolle **3/3**. Der spielbare Plan steht vorn, damit auch „erster“ und „gemerkt“ auseinanderfallen.
- **2026-10-28** — Review (`frontend-reviewer`, Mutanten gemessen): Fall rot gegen „Zweig gemerkt gestrichen“ und gegen „`setPlanId` immer“; Setup trennt gemerkt (99, nicht spielbar), spielbar und erster (5). Vitest **569/569**, `tsc -b` sauber, volle E2E **99 passed** (alle `exit=0`, E2E nach der letzten Änderung gestartet); Browser-Extension nicht genutzt. **`abgenommen`.**
