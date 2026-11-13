---
tags: [typ/story, status/idee, bereich/training, rolle/supervisor, rolle/student]
aliases: [Vater-Übungsrunde schreibt in den Lernstand des Kindes]
status: idee
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "B-433 Entscheidung 3 (zurückgestellt) und pugling-reviewer zu Sprint 69 (Backlog-Lauf 2026-11-13, docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-542 · Soll eine Übungsrunde des Vaters überhaupt in Verlauf und Lernstand des Kindes schreiben?

[B-433](B-433-vorschau-des-vaters-verbraucht-die-einfuehrung.md) hat die Einführung abgeschirmt: nur die eigene
Begegnung des Kindes setzt `PositionItemProgress.IntroducedAt`. Bewusst zurückgestellt (B-433, Entscheidung 3) blieb
die größere Frage, und der Review von Sprint 69 hat sie belegt: Spielt der Vater eine Position durch, schreibt eine
gewertete Antwort weiterhin über `ItemProgressService` in den **plan-übergreifenden** Lernstand des Kindes
(`ItemProgress`, `ItemProgressService.cs:37-49`, `:60`, `:65`) und einen `ItemReviewEvent` in **dessen** Verlauf
(`PositionPracticeController.cs:458-462`) – auch ein „falsch“ für eine durchgeklickte Kennenlernkarte. Dazu bewegt
eine Leitner-Bewertung des Vaters Kasten und `DueOn`, während `IntroducedAt` leer bleibt.

Gegenstimme: Der **Nachtrag** (der Vater trägt eine Übung nach, die das Kind auf Papier gemacht hat) lebt genau
davon, dass seine Runde zählt. Zu klären: Vorschau und Nachtrag trennen (eigener Modus), oder die Vater-Runde schreibt
den Lernstand nur, wenn sie ausdrücklich als Nachtrag gestartet wird.

## Verlauf

- **2026-11-13** — angelegt im Backlog-Lauf 2026-11-13 aus B-433 (E3) und dem `pugling-reviewer`-Befund zu Sprint 69.
