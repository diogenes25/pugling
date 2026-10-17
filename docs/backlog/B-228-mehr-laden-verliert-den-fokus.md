---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Mehr laden Fokus, gefüllte Verlaufskarte ohne Fokusregel]
status: idee
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

# B-228 · „Mehr laden“ in der gefüllten Verlaufskarte verliert den Fokus

B-216 hat an drei Stellen den Fokus beim Wiederholen auf den Fehlerkasten gelegt, weil ein `disabled`-Knopf
ihn in Chromium verliert. Die **gefüllte** Verlaufskarte des Sohn-Shops (`frontend/src/sohn/SohnShop.tsx`,
Banner über den Zeilen und Knopf „Mehr laden" darunter) bekam im selben Sprint zwar das Fehler-Gedächtnis
(Reviewer-Fund), aber weder `tabIndex` am Banner noch den Fokus-Umzug am Knopf – der gesperrte „Mehr laden"
lässt den Fokus auf `<body>` fallen, genau der Verlust, den Entscheidung 5 von B-216 an den anderen Stellen
behebt. Kein Kriterium verlangte es dort; für „Mehr laden" ohne Fehler ist der Verlust älter als der Sprint.

## Verlauf

- **2026-10-17** — angelegt aus der **Nachschau** zu Sprint 1 des Nachtlaufs (Commit `d834a8e`),
  `entgangen_bei: B-216`.
