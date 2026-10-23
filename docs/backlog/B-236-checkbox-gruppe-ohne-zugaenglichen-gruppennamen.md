---
tags: [typ/story, status/idee, bereich/frontend, a11y]
aliases: [Gruppenkopf ohne fieldset/legend, Freigabe-Gruppe ohne Gruppennamen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Skill `web-design-guidelines` zu B-176 (Nachtlauf 2026-10-23, Sprint 1)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-236 · Eine Gruppen-Überschrift über Kontrollkästchen ist sichtbar, aber für den Screenreader an nichts gebunden

B-176 hat die **Optik** der Gruppen-Überschriften repariert (`group-label`) und die Markup-Entscheidung aus
B-11 bewusst stehen lassen: „Freigabe (Startzustand)" ist ein `<span>`, weil es kein einzelnes Eingabefeld
beschriftet. Der zweite Blick (`web-design-guidelines`: „Use semantic HTML before ARIA", „Form controls need
label") zeigt die andere Hälfte: Die Überschrift ist an die Gruppe darunter **nicht gebunden**. Ein
Screenreader liest „Für andere Betreuer zuweisbar, Kontrollkästchen" – ohne zu sagen, dass es um die Freigabe
geht. Dasselbe gilt für „Schularten" (`VaterExerciseCreate.tsx:247`, ein `FieldLabel` ohne `htmlFor`, über
fünf Kästchen) und „Lern-Standards" (`:255`, ein `<label>` ohne Ziel).

Die semantische Form dafür ist `<fieldset><legend>` – dann trägt die Gruppe den Namen selbst, und die
Typografie-Regel aus B-176 müsste `legend` mit abdecken (oder `legend.group-label`).

## Offene Punkte

1. Welche Gruppenköpfe sind tatsächlich Köpfe einer **Gruppe von Controls** (fieldset/legend) und welche nur
   Überschrift über einem Wiederhol-Feld (`RepeatedTextFields` – dort trägt jedes Feld schon ein `aria-label`
   mit Scope)? Einzeln ansehen, nicht pauschal umstellen – dieselbe Mahnung wie in B-176.
2. `fieldset` bringt Browser-Standardoptik (Rahmen, Padding) mit – die muss im `index.css` zurückgesetzt
   werden, sonst verschiebt sich die Karte.

## Verlauf

- **2026-10-23** — angelegt aus dem `web-design-guidelines`-Lauf zu B-176 (Freigabe 7). **Bewusst nicht in
  B-176 mitgenommen:** dessen Ziel (die Überschrift sieht aus wie eine) ist ohne diese Story erfüllt, und
  die Bindung ist eine Markup-Entscheidung, die B-11 ausdrücklich anders getroffen hat. `unverifiziert`:
  wie Screenreader die drei Gruppen heute ansagen, ist nicht gemessen.
