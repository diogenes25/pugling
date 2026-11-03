---
tags: [typ/story, status/idee, bereich/backend, bereich/doku]
aliases: [list item ohne c und b]
status: idee
prio: P4
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Lauf Q, Teil 1 – Rohstrings des OpenAPI-Generators zu B-259/B-260 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-361 · In Listenpunkten verliert das Vertragsdokument `<c>` und `<b>`

Beim Lesen der Rohstrings des OpenAPI-Generators (Lauf Q, B-259/B-260) gefunden: in einem `<item>` einer `<list>` kommen
Inline-Elemente als nackter Text an. `UsageResponse.otherLearnersCount` (`backend/Pugling.Contracts/Creator/ExerciseCatalogDtos.cs`)
schreibt `<c>409 exercise_in_use</c>` und `<b>creator without own children</b>` – im Dokument steht „fails with 409
exercise_in_use" und „For a creator without own children", ohne Code- bzw. Fettsatz. Zu klären: wie viele `<list>` in
Contracts/Controllern das trifft (beim Ausformulieren gezählt: zwei), und ob Umformulieren (kein Inline-Element im Punkt)
oder ein Tor die richtige Antwort ist. Bewusst nicht in B-259 mitgenommen: anderes Verhalten des Generators, und der Text
bleibt lesbar.

## Verlauf

- **2026-11-03** — angelegt in Lauf Q, Teil 1 aus den Rohstrings zu B-259/B-260. Aus dem rohen Dokument gelesen.
