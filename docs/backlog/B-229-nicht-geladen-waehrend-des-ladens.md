---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [PlanEditForm Ladefenster, Sonderoption während des Ladens]
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
entgangen_bei: [B-194]
wartet_auf: ""
---

# B-229 · Das Kind-Feld im Plan-Formular sagt „(nicht geladen)“, solange die Liste erst lädt

`PlanEditForm` (`frontend/src/vater/VaterPlanDetail.tsx`) montiert beim Klick auf „Bearbeiten" und lädt
die Kinderliste selbst. Solange `children.data === null`, ist die Optionsliste leer, und die Sonderoption
„<Name> (nicht geladen)" aus B-194 ist die einzige Option – für die Dauer des Abrufs auch im
**Erfolgsfall**. Wörtlich wahr, liest sich aber als Ausfall. Bei den sieben anderen `ReferenceSelect`-Stellen
laden die Listen auf Seitenebene, bevor das Formular aufgeht. Fix-Ideen: das Feld sperren, solange die erste
Ladung läuft (`children.loading && children.data === null`), oder die Liste in `VaterPlanDetail` laden und
als Prop reichen.

## Verlauf

- **2026-10-17** — angelegt aus der **Nachschau** zu Sprint 1 des Nachtlaufs (Commit `d834a8e`),
  `entgangen_bei: B-194`.
