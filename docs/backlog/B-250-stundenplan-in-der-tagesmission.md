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
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht (`Timetable` außer im eigenen Controller nur im `SubjectsController` referenziert). Haken: das Fach hängt nur optional an der Reihe (`CurriculumEntities.cs:42`) – Positionen ohne Fach fallen aus der Gewichtung.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) (G-7): Der Stundenplan bewirkt **nur Reihenfolge und Hervorhebung**. Positionen, deren Fach morgen auf dem Plan steht, rücken in der Queue (B-220) nach vorn. An der Pflicht ändert er nichts. Die Sortierung sitzt im Queue-Endpunkt aus B-220, sodass keine zweite Sortierlogik entsteht. Positionen ohne Fach an der Reihe fallen aus der Gewichtung. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-20); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt: `Timetable` wird außer in `TimetableController`/`TimetableEntities` nur in `SubjectsController` referenziert, kein Service liest ihn; Fach an der Reihe weiter optional (`Models/CurriculumEntities.cs:42`). Voraussetzung B-220 (Queue-Endpunkt) ist noch `idee`, ein Queue-Endpunkt existiert nicht.
