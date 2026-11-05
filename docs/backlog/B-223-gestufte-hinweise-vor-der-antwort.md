---
tags: [typ/story, status/idee, bereich/training, bereich/backend, rolle/student, rolle/creator]
aliases: [Gestufte Hinweise, Hint-Stufen, Hilfe vor der Antwort]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/khan-academy-vergleich-plan.md#wo-khan-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-223 · Gestufte Hinweise vor der Antwort, die Punkte kosten

Ein Vokabel-Item trägt einen statischen `Hint`, der mit der Karte ausgespielt wird; danach kennt der
Student-Pfad nur Antwort und Reveal. Khan Academy gibt über Khanmigo Strategiehinweise, ohne die Lösung
zu verraten. Die Idee: ein gestufter Hinweisweg am Item (etwa Anfangsbuchstabe, dann Wortlänge, dann
ein weiterer Buchstabe), den das Kind aktiv anfordert und der die Review-Punkte der Karte mindert. Der
Server muss den Hinweis so herausgeben, dass die Lösungsfeld-Regel (kein `Answer`/`Solution` an den
Studenten) unverletzt bleibt, und die Minderung muss im `ScoringService` nachvollziehbar sein.

## Verlauf

- **2026-10-17** — angelegt (Quelle: Khan-Academy-Vergleich, Etappe 6 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): **teilweise vorhanden** – ein Tipp auf Wunsch (`SohnPractice.tsx:60,305`, kostenlos, clientseitig) und die Wortlänge über `answerLength`/`LetterBoxes` (`:324`). Die Spalte `HintsUsed` existiert, wird aber von keinem Pfad beschrieben (`StudyPlanEntities.cs:188-194`). Beim Ausformulieren: auf diesen Bestand aufsetzen statt einen neuen Mechanismus zu bauen.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-5 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): Hinweise **kosten Punkte, über die vorhandene Spalte `HintsUsed`**. Die Hinweise sind gestuft (Anfangsbuchstabe, dann ein weiterer Buchstabe); der Server zählt sie und mindert die Punkte der Karte nachvollziehbar im `ScoringService`. Ein neuer Mechanismus neben dem vorhandenen Tipp entsteht nicht, der Tipp wird zur ersten Stufe. Die Lösungsfeld-Regel bleibt unverletzt. Offen beim Ausformulieren: ob es im Abschlusstest Hinweise gibt. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-18); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-11-05** — **Rest-Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05, [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): im Abschlusstest gibt es keine Hinweise – er misst.
