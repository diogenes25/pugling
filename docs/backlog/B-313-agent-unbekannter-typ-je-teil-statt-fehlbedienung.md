---
tags: [typ/story, status/idee, bereich/agent, rolle/creator]
aliases: [plan --types Tippfehler, unbekannter Übungstyp je Teil]
status: idee
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf F Teil 5 in Lauf G, Teil 1 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-313 · Soll ein unbekannter Übungstyp in `plan`/`exam` eine Fehlbedienung sein?

`plan --types Vocabulary,Clozee` erzeugt laut Code die gültigen Teile, legt einen Plan mit weniger Positionen an und
endet mit Exit 1: der unbekannte Typ wird **je Teil** gefangen (`CreatorPipeline.cs:26-30`, gefangen in
`PlanPlanner.cs:164` und `ExamPlanner.cs:79`). Der Commit von B-308 verspricht „Fehlbedienung = Exit 2“, und der
Kommentar in `PlanPlanner.cs:126` („All four checks come before the first model call“) zählt die Typ-Schlüssel nicht
mit. Im Review von B-308 wurde der Fall bewusst je Teil gelassen – es fehlt nur die festgehaltene Entscheidung.

**Die Frage an den Nutzer:** Tippfehler in `--types` vorab als Fehlbedienung abweisen (billig: `request.Types` gegen
`pipeline.SupportedTypes` vor dem Briefing prüfen), oder den Teil-Plan bewusst anlegen und nur melden?

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf F, Teil 5 (`45cbe7b`), Fund 3 (🟢)
  ([Protokoll](../pm-sitzung-2026-10-27-nacht.md), Lauf G Teil 1). `Frage`: der Nachtlauf entscheidet das nicht.
