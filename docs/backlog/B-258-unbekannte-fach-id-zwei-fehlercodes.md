---
tags: [typ/story, status/idee, bereich/backend, bereich/vertrag]
aliases: [invalid_reference vs validation_error, SubjectId unbekannt]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-145 (vierter Nachtlauf 2026-10-23, Entscheidung 3)
unverifiziert: true
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

## Verlauf

- **2026-10-23** — angelegt beim Grillen von B-145 im vierten Nachtlauf; ungeprüft.
