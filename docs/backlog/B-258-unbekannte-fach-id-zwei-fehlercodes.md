---
tags: [typ/story, status/in-arbeit, bereich/backend, bereich/vertrag]
aliases: [invalid_reference vs validation_error, SubjectId unbekannt]
status: in-arbeit
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: ja
quelle: B-145 (vierter Nachtlauf 2026-10-23, Entscheidung 3)
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-258 · Eine unbekannte `SubjectId` meldet je nach Ressource zwei verschiedene Fehlercodes

Dieselbe Regel – „die `SubjectId` zeigt auf kein Fach" – meldet `TextbookSeriesController.cs:115` als
`invalid_reference`, `TextbooksController.cs:62,95` (Lehrbuch des Kindes) als `validation_error`. Ein Client,
der nach dem Code verzweigt, muss beide kennen. Zu klären beim Ausformulieren: welcher Code der richtige ist
(`invalid_reference` ist der spezifischere), wo sonst noch eine unbekannte Referenz als `validation_error`
gemeldet wird (`TextbooksController.cs:138,143` für Reihe und Unit sehen gleich aus), und ob das Frontend
einen der beiden Codes liest. Aus B-145 abgetrennt, weil es eine Vertragsfrage ist und nicht aus dem
Umbenennen folgt; `art: Defekt`, weil ein uneinheitlicher Code den Vertrag für den Client falsch macht.

## User Story

Als **Client der API** (Frontend, KI-Creator, Fremd-Client) möchte ich, dass eine Id, die auf nichts zeigt, überall
denselben Code meldet, damit ich nach dem Code verzweigen kann, ohne zu wissen, mit welcher Ressource ich sprach.

## Ist-Stand (Lauf J, am Code belegt)

- `invalid_reference`: `TextbookSeriesController.cs:115,117` (Fach, Verlag der Reihe), `ExerciseControllerBase`
  (`CategoryValid`).
- `validation_error` für dieselbe Regel: `TextbooksController.cs:70,103` (Fach), `:146` (Reihe), `:151` (Unit),
  `GrammarTopicsController.cs:85` (Fach), `SeriesUnitsController.cs:191` (Grammatikthemen).
- **Kein Leser:** das Frontend verzweigt an keiner dieser Stellen nach einem der beiden Codes (`grep` über
  `frontend/src` ohne `contract.ts`: nur Kommentare in `pin.ts`, `PlanPositions.tsx`); `Pugling.Client`/Agent ebenso
  nicht. Kein Test prüfte den Code an diesen Stellen.

## Entscheidungen

1. (Nutzer, 2026-10-28) Einheitlich `invalid_reference`, an allen sechs Fundstellen. Kleiner Vertragsbruch, angenommen.
2. (Lauf J, im Rahmen von 1) Nur der Code wechselt, Status (`400`) und Meldungstext bleiben. Die Übungs-Validierung
   (gleiche Regel, anderer Mechanismus) liegt außerhalb der genannten Fundstellen → [B-329](B-329-uebung-unbekannte-referenz-validation-error.md).

## Akzeptanzkriterien

1. An jeder der sechs Fundstellen meldet eine unbekannte Id `400 invalid_reference`.
2. Ein Test benennt die Fundstelle, die abweicht.

## Schätzung

`XS`, Backend, kein Schema. Testweg: `UnbekannteReferenzTests` (neu), volle Suite.

## Verlauf

- **2026-10-23** — angelegt beim Grillen von B-145 im vierten Nachtlauf; ungeprüft.
- **2026-10-28** — **Freigabe des Nutzers (Autonomer Modus, Opt-in)**, im Dialog nach Lauf I („Nimm die von dir
  vorgeschlagenen Entscheidungen“) auf den Vorschlag im [Protokoll](../pm-sitzung-2026-10-28-nacht.md#ende-lauf-i):
  eine unbekannte Referenz meldet einheitlich `invalid_reference` (der spezifischere Code), an allen Stellen, nicht nur
  den beiden der Story – Fundstellen dort: `TextbooksController.cs:70,103,146,151`, `GrammarTopicsController.cs:85`,
  `SeriesUnitsController.cs:191`. Kleiner Vertragsbruch (`vertragsbruch: ja`), bewusst angenommen. Noch zu prüfen beim
  Ausformulieren: ob das Frontend einen der beiden Codes liest.
- **2026-10-28** — Lauf J: ausformuliert, geschätzt, gebaut (Freigabe 9). Rote Probe: `UnbekannteReferenzTests`
  **1 failed** (erwartet 1), Liste nennt alle sechs Stellen mit `validation_error`. Mutante „Unit-Stelle zurück“
  **1 failed**, Liste nennt genau `series unit POST grammarTopicIds`. Volle Suite **1004/1004**.
