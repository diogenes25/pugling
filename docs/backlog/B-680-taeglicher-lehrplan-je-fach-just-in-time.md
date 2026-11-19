---
tags: [typ/story, status/idee, bereich/lehrplan, bereich/klassenarbeit, rolle/creator, rolle/student]
aliases: [Täglicher Lehrplan, Just-in-time-Plan, Privatlehrer-Plan]
status: idee
prio: P1
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/leitbild-privatlehrer.md
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-680 · Der Lehrer-Agent erzeugt jeden Tag den Plan je Fach aus Klausur, Stoff und Lernstand

Heute ist ein `StudyPlan` eine feste Sammlung von Positionen, die der Vater oder der KI-Creator (`plan`, `assign`) auf
Zuruf anlegt; täglich wandern nur Tagesdosis, Leitner-Fälligkeit und Tagesmission. Das Leitbild vom 2026-11-19 verlangt
**Privatunterricht**: einmal am Tag legt ein Lehrer-Agent je Kind und Fach fest, was heute dran ist – ausgerichtet auf
den nächsten Klausurtermin und dessen Stoff, auf den Lernstand des Kindes und das Ziel (die Notenstufe, B-682). Der
Umfang ergibt sich aus dem, was bis zum Ziel noch fehlt. Gibt es keine Klausur, läuft ein freier Plan, damit das Kind
im Stoff bleibt. Die tägliche Pflicht mit dem Stick bleibt der Grundgedanke.

Zu klären beim Ausformulieren: Läuft der Planer im Server (Job) oder als Agent gegen die API? Was ersetzt die heutige
Sicherung „KI-Plan entsteht inaktiv“, wenn niemand mehr freigibt? Wie wird eine Pflicht, die erst heute entsteht,
gegen den Malus fair (Ankündigung, Frist)? Verhältnis zu B-248 (Klausur verdichtet den Plan), B-618 (Tagesbudget über
Fächer) und B-630 (Rückmeldungen im KI-Creator). Vermutlich eine Karte (`/wayfinder`), keine Einzel-Story.

## Verlauf

- **2026-11-19** — angelegt aus dem Leitbild-Gespräch mit dem Vater ([Leitbild](../leitbild-privatlehrer.md)).
