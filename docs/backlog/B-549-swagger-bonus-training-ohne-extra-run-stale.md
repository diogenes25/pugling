---
tags: [typ/story, status/idee, bereich/gamification, bereich/api, rolle/student]
aliases: [extra_run_stale fehlt in der Swagger-Doku]
status: idee
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Sprint 73 (Retro-Pflicht), Backlog-Lauf 2026-11-14 – docs/pm-sitzung-2026-11-14-backlog-lauf.md"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-545]
wartet_auf: ""
---

# B-549 · Die Swagger-Doku des Bonus-Trainings kennt die verworfene Runde nicht

B-545 verwirft eine Bonus-Runde, deren Übung sich seit dem Start geändert hat – beim Fortsetzen
(`ExtraTrainingService.StartAsync`: neue Runde, neue `runId`) und bei der Abgabe (`409 extra_run_stale`). Die
Endpunkt-Doku in `backend/Pugling.Api/Controllers/Student/ExtraTrainingController.cs` sagt das nicht: Start
(`:31-34`) verspricht weiter „resumes its open run, which keeps its form and cards“, und die Abgabe (`:48-51`) nennt
als `409` nur `extra_run_completed` und `extra_cap_reached` – weder `extra_run_stale` noch das schon vorher fehlende
`extra_training_locked`. Die Story hatte den Code ausdrücklich „in den `<response>`-Docs von Start/Abgabe“ geplant
(B-545, Schätzung). Fehlerszenario: Ein Client nach Swagger (Agent über `Pugling.Client.StudentApi.SubmitExtraRunAsync`)
behandelt einen `409` bei der Abgabe als „schon abgegeben“ und behält die Antworten, oder hält nach einem Start mit
neuer `runId` an der alten fest. Die Arcade ist nicht betroffen (sie kennt den Code, `chromeText.tsx`, `SohnBonus.tsx`).

## Verlauf

- **2026-11-14** — angelegt aus der Nachschau zu Sprint 73 (Backlog-Lauf 2026-11-14), Stellen am Stand `HEAD` gelesen.
