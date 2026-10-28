---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Reload nach Plan-Löschen zeigt Study plan not found]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf H, Teil 2 in Lauf I (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-326 · Üben und Klausur nehmen nach einem Reload die ungeprüfte Plan-Id

`frontend/src/sohn/SohnTest.tsx:37/70` und `SohnPractice.tsx:49` lesen weiter `useSohn().planId`, das
[B-254](B-254-weg-zeigt-englischen-planfehler.md) nur für Startseite und Weg über `useChosenPlan` prüft. Wird nach dem
Löschen oder Deaktivieren des Plans `/sohn/test/<id>` bzw. `/sohn/practice/<id>` neu geladen (Reload, Zurück,
Lesezeichen), steht wieder „Study plan not found. (Ref: …)“ da – auch in der deutschen Arcade englisch (404 ohne deutsche
Fassung). Normal führt der Weg über die Startseite, die die Id korrigiert. Außerhalb der AKs von B-254, darum
`entgangen_bei` leer.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau (frischer `frontend-reviewer`, `git show f96d358`) in Lauf I
  ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); aus Code gelesen, Mutante hergeleitet, nicht gemessen.
