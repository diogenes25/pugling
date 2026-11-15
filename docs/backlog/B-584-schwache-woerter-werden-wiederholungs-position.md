---
tags: [typ/story, status/idee, bereich/training, bereich/auswertung, rolle/supervisor]
aliases: [Lücke wird Pflicht, Schwache Wörter zuweisen]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/rockettutor-vergleich-plan.md#wo-rockettutor-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-584 · Die schwachen Wörter eines Kindes werden mit einem Griff zur Wiederholungs-Position

RocketTutors Lehrer-Sicht zeigt die größten Wissenslücken und ist zugleich der Startpunkt einer
Zuweisung. Pugling erkennt die Lücke schon: der Lernstand filtert schwache Wörter (`onlyWeak`, Schwelle
50 %, `backend/Pugling.Api/Controllers/Student/ChildVocabularyProgressController.cs:47` und `:92`), das
Vater-Web zeigt sie (`frontend/src/lib/api.ts:588`), und Positionen üben per Default die schwächsten
zuerst (`PracticeOrder.WeakestFirst`, `Controllers/Supervisor/PlanPositionsController.cs:193`). Was nach
erster Sicht fehlt, ist der Schritt von der Liste zur **Pflicht**: der Vater sieht „diese 15 Wörter sitzen
nicht" und kann daraus nicht direkt eine Position mit eigenem Pflichtziel machen. Er müsste eine Übung
anlegen und zuweisen. Zu klären beim Ausformulieren: ob die Wiederholung eine neue Katalog-Übung braucht
oder eine bestehende Position auf eine Item-Auswahl eingeschränkt werden kann, wie sie mit dem
„ein aktiver Plan je Kind"-Guard zusammenspielt, und ob der offene `--mode weakness` des KI-Creators
dasselbe Ziel schon abdeckt. Berührt [B-39](B-39-supervisor-dashboard.md).

## Verlauf

- **2026-11-15** — angelegt (Quelle: RocketTutor-Vergleich, Etappe 5 des Übernahmeplans; Sammel-Story [B-581](B-581-rockettutor-vergleich-sammel-story.md)).
- **2026-11-15** — umnummeriert von B-544 auf B-584: die Id war auf `origin/main` schon vergeben (main belegt bis B-552, der Backlog-Lauf vom 2026-11-15 bis B-557). Inhalt unverändert.
