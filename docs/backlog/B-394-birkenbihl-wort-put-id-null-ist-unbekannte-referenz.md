---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Wort-PUT meldet Id 0 als unbekannte Referenz]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-w
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-387]
---

# B-394 · Der Birkenbihl-Wort-`PUT` meldet `vocabularyId = 0` als `invalid_reference`, der CRUD-Weg als `validation_error`

B-387 entstand aus „zwei Wege zum selben Feld, zwei Antworten" und hat sie für die unbekannte Id angeglichen – für `0` und negative Ids
nicht: `SetWord` (`backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs`, `PUT …/birkenbihl/{exerciseId}/words/{wordId}`,
um `:586-592`) schlägt die Id einfach nach und antwortet `invalid_reference`, `POST`/`PUT` der Übung seit B-387 `validation_error`
(Regel aus B-329: `≤ 0` ist Formfehler). Kein Test deckt `SetWord` mit `0`. Die Summary am Birkenbihl-Validator behauptete
„wie der Wort-Endpunkt" – seit Lauf W auf die unbekannte Id eingeschränkt.

Zu klären beim Ausformulieren: `≤ 0` → `validation_error` auch in `SetWord` (vermutlich), mit einem Fall in `UnknownReferenceCodeTests`.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf W, Sprint 8 (Retro Sprint 9; frischer `pugling-reviewer`, `git show cae15a1`,
  🟢). **Entgangen bei B-387:** das Verhalten von `SetWord` ist älter, aber B-387 versprach die Angleichung und ihre Summary behauptete
  sie. Increment **dieses** Laufs: **Limit 2**, Lauf W endet nach Sprint 9.
