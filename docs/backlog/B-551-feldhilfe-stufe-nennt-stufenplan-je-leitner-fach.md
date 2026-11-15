---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Feldhilfe Stufe, Stufenplan je Leitner-Fach]
status: abgenommen
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 74 (Retro-Pflicht), Backlog-Lauf 2026-11-14 – docs/pm-sitzung-2026-11-14-backlog-lauf.md"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-16]
wartet_auf: ""
---

# B-551 · Die Feldhilfe „Stufe“ nennt einen Stufenplan je Leitner-Fach, den es nicht gibt

Die mit B-16 neue Feldhilfe `stage` (`frontend/src/lib/fieldHelp.ts:104-109`) schließt mit „Ein per API gesetzter
Stufenplan je Leitner-Fach geht dieser Einstellung vor.“ Der Stufenplan ist aber ein Plan **je Plantag**: `StageStep(int
DayNumber, int Stage)` (`backend/Pugling.Contracts/Common/StudyPlanBaseTypes.cs:17-18`), aufgelöst nach dem Tag seit
Planstart (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:73-80`); so beschreibt ihn auch
`docs/tutorial-supervisor.md` („Tag→Stufe-Fahrplan“, Abschnitt „Stufen-Fahrplan“). Mit dem Leitner-Fach hängt die
Box-Leiter zusammen (B-476), nicht der Stufenplan. Szenario: Ein Vater, der eine Position mit Fahrplan „ab Tag 5 Stufe 4“
bearbeitet, liest im Hilfetext, die Stufe werde je Fach übersteuert, und erwartet für Wörter in hohen Fächern eine andere
Stufe als für neue – tatsächlich wechselt sie für alle Wörter am selben Plantag.

## Verlauf

- **2026-11-14** — angelegt in der Nachschau zu Sprint 74 (Backlog-Lauf 2026-11-14), Beleg am Code `HEAD`.
- **2026-11-14** — Sprint 76, behoben (Frontend, Review-Auftrag): Feldhilfe `stage` nennt den Stufenplan je Plantag (Beispiel Assistent „Klassenarbeit“) und sagt ausdrücklich „nicht je Leitner-Fach“; Vitest-Fall in `fieldHelp.test.ts` (rot, wenn „Stufenplan je Leitner-Fach“ zurückkommt). Status unverändert.
- **2026-11-15** — **abgenommen** (Abschluss des Laufs). Gesamt-Review des Laufs (Backend + Frontend), dort als Fund aus dem Zusammenspiel behoben. Volle Suite 1659/1659 (Release), Vitest 1088/1088, `tsc -b` grün, volle Playwright-Suite nach dem letzten Code-Edit 153 passed (zwei Specs mit veralteter Erwartung danach angepasst und grün: 3 passed). Feldhilfe `stage` nennt den Stufenplan je Plantag; Vitest-Fall in `fieldHelp.test.ts` wird rot, sobald „Stufenplan je Leitner-Fach“ zurückkommt (vom Bau-Agenten gemessen).
