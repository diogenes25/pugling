---
tags: [typ/story, status/idee, bereich/test, rolle/entwickler]
aliases: [Kinderzeile per Namen, createChild-Rennen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-10-23-nacht.md (dritter Lauf, rote Probe zu B-35)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-255 · E2E-Specs suchen die Kinderzeile per Namen, den zwei Tabellen tragen

Die Vater-Startseite (`VaterDashboard.tsx`) zeigt den Namen eines Kindes zweimal: in der Tagesübersicht
(`:54`, Name als Text in der ersten Spalte) und in der Kinder-Tabelle (`:79-81`, Id vorn, Name als Link).
Eine Spec, die `getByRole("row", { name: /<Kind>/ })` nimmt und die erste Zelle als Id liest, bekommt je nach
Ladereihenfolge den Namen statt der Id – gemessen beim Bau von `e2e/karten-umdrehen.spec.ts` (B-35): zwei
Läufe rot an zwei verschiedenen Stellen, mit dem Link-Filter dreimal grün. Dasselbe Muster steht ungefiltert
in `e2e/bilder.spec.ts` (`createChild`); ob es dort je fällt und welche weiteren Specs es teilen, ist nicht
geprüft. Außerhalb des Diffs von Sprint 1, darum eigene Story.

## Verlauf

- **2026-10-23** — angelegt beim Bau von B-35; ungeprüft für die übrigen Specs.
