---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor]
aliases: [achter Fall ohne ReferenceSelect, children.error im Plan-Detail]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-09-01 zu B-172
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-194 · Die Kind-Auswahl im Plan-Detail hat die Regel von B-172 nicht geerbt

Der **achte** Fall derselben Falle — B-172 hat sieben umgestellt und ihren Umfang auf Fach/Verlag/Reihe/
Unit ausgezählt. Dieser hier lag außerhalb.

## User Story

Als **Vater** möchte ich beim Bearbeiten eines Plans sehen, dass die Kinderliste nicht geladen werden
konnte, statt ein leeres Feld für einen Plan zu sehen, der ein Kind hat.

## Ist-Stand am Code

`VaterPlanDetail.tsx:137-139` rendert `children.data?.map(...)` **roh**: keine Leer-Option, keine
Rückfall-Option für den geladenen Wert, und `children.error` wird in der ganzen Datei nirgends gerendert.
Fällt der Kinder-Abruf aus, steht das Feld leer — und der Ausfall ist unsichtbar. Genau die Klasse
„ungerenderter Fehlerzustand", die B-172s Akzeptanzkriterium 5 an fünf anderen Stellen geschlossen hat.

**Was es NICHT tut, und das ist nachgerechnet statt angenommen:** Es zerstört nichts. Ohne Optionen kann
kein `change` feuern, `form.childId` bleibt auf `plan.childId`, und `dto.childId` wird bei `:126` nie
gesetzt. `api.children()` ist ungepaged (`ChildrenController.cs:36` liefert `IEnumerable` ohne Paging) —
eine *teilweise* Liste kann es also gar nicht geben. Der Schaden ist damit „unsichtbarer Ausfall", nicht
„stiller Datenverlust" wie bei B-172.

**`entgangen_bei` bleibt leer, und das ist keine Nachlässigkeit:** Die Zeile liegt außerhalb dessen, was
B-172 angefasst und ausgezählt hat. Sie ist keiner Abnahme entgangen, sondern war nie im Umfang.

## Offene Punkte

1. `ReferenceSelect` einsetzen oder nur `children.error` rendern? Empfehlung: **beides gegen den Code
   prüfen** — der Baustein löst hier weniger als anderswo (es gibt keinen zerstörenden Pfad), aber
   Einheitlichkeit an acht von acht Stellen ist ein Wert für sich, weil die neunte sonst wieder
   danebengreift.

## Verlauf

- 2026-09-01 · Aufgenommen aus der **Nachschau** zu B-172 (Nachtlauf). Der Nachschau-Agent hat alle 103
  `<select>` des Frontends gesichtet; dies ist der einzige verbliebene **bearbeitende** Fall ohne die
  Regel. Die übrigen sind Anlege-, Filter- oder Auswahlfelder ohne geladenen Wert bzw. laufen auf
  Konstantenlisten — namentlich geprüft und im Protokoll benannt.
