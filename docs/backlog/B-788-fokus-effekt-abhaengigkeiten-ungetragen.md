---
tags: [typ/story, status/idee, bereich/test, rolle/entwickler]
aliases: [Fokus-Effekt-Abhängigkeiten ungetragen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Nachtlauf B 2026-11-22, Review und Abhängigkeits-Mutanten zu B-603 (docs/pm-sitzung-2026-11-22-b.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-541]
---

# B-788 · Zwei Abhängigkeiten des Fokus-Effekts im Test trägt kein Fall

Der Fokus-Effekt in `frontend/src/sohn/SohnTest.tsx:92-101` hängt an
`[item, cursor, revealed, error, result, action.busy]`. Gemessen beim Bau von B-603 (Lauf B, 2026-11-22): Mutanten, die
genau eine Abhängigkeit aus dem Dep-Array streichen, gegen `SohnTest.test.tsx` plus `SohnPractice.test.tsx`:

| gestrichen | neue Tests (B-603) | HEAD-Tests |
|---|---|---|
| `revealed` | 1 rot | – |
| `action.busy` | **0 rot** (87/87) | **0 rot** (35/35) |
| `cursor` | **0 rot** (87/87) | **0 rot** (35/35) |

Dazu `SohnPractice.tsx` `[phase, idx]`: ohne `idx` 2 rot, ohne `phase` 13 rot.

Die Lücke stammt also nicht aus B-603 (`waitFor`), sondern bestand schon vorher. Schwerer wiegt:
`SohnTest.test.tsx:416-417` verspricht im Kommentar zu AK 3 „Rot, wenn er … nicht an `action.busy` hängt“, und dieses Rot
kommt nicht. Das ist eine Zusicherung, die nur als Kommentar existiert (Regel in der Root-`CLAUDE.md`).

Offen für die Ausformulierung:
- Ob `action.busy` und `cursor` im Dep-Array überhaupt nötig sind. Vielleicht deckt eine andere Abhängigkeit jeden
  echten Wechsel ab. Dann wären sie Ballast, und der Kommentar wäre falsch.
- Oder ob es einen Ablauf gibt, in dem nur sie wechseln. Dann fehlt ein Fall, der sie trägt.
