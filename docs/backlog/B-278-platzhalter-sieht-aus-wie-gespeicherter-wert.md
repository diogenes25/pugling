---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Platzhalter „Cornelsen“ im leeren Verlagsfeld]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zu B-267 in Lauf B (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-278 · Im leeren Verlagsfeld sieht der Platzhalter aus wie ein gespeicherter Wert

Seit [B-267](B-267-entkoppeltes-lehrbuch-zeigt-katalogwert-im-freitextfeld.md) zeigt das Freitextfeld nach dem Lösen
der Reihe, was gespeichert ist. Bei einem Buch, das nur über den Katalog verknüpft war, ist das *nichts*. Das leere
Feld trägt dann aber den grauen Platzhalter „Cornelsen“ (`frontend/src/vater/ChildMaterialSection.tsx`, Verlagsfeld).
Das liest sich wie „der Verlag ist noch da“, ausgerechnet wenn vorher Cornelsen aus dem Katalog dastand. Das
Klassenfeld (Platzhalter „8“) hat dasselbe in milderer Form.

Vorschlag des Reviewers: ein neutraler Platzhalter („z. B. Verlag“) oder keiner.

**In Lauf B bewusst nicht behoben:** 🟢-Hinweis, und der Zähler des Mini-Sprints stand bei 4 vor dem Delta-Review.

## Verlauf

- **2026-10-26** — angelegt aus dem `frontend-reviewer` zu B-267 in Lauf B ([Protokoll](../pm-sitzung-2026-10-26-tag.md)), Hinweis 4.
