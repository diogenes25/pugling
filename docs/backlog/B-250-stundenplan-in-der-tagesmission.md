---
tags: [typ/story, status/idee, bereich/training, rolle/student, rolle/supervisor]
aliases: [Stundenplan in der Tagesmission]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/knowunity-vergleich-plan.md#wo-knowunity-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-250 · Der Stundenplan gewichtet die Tagesmission

Der Supervisor kann je Kind einen Stundenplan pflegen (`TimetableEntry`: Wochentag → Fach, Pflege über
`TimetableController`), aber kein Service liest ihn – er ist reine Ablage. Knowunity baut den Plan um
den Stundenplan herum. Die Idee: die Tagesmission bzw. Reihenfolge in `overview` zieht Positionen nach
vorn, deren Fach morgen auf dem Stundenplan steht („morgen Englisch → heute Englisch-Vokabeln"). Das
ändert keine Pflicht, nur die Reihenfolge und Hervorhebung. Berührt die Learning Queue aus
[B-220](B-220-learning-queue-im-sohn-dashboard.md); beim Ausformulieren abgrenzen, damit nicht zwei
Sortierlogiken entstehen. Voraussetzung: die Position kennt ihr Fach (über die Reihe der Übung).

## Verlauf

- **2026-10-23** — angelegt (Quelle: Knowunity-Vergleich, Etappe 3 des Übernahmeplans).
