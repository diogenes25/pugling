---
tags: [typ/story, status/idee, bereich/klassenarbeit, bereich/training, rolle/student, rolle/supervisor]
aliases: [Klausur-Countdown, Klassenarbeit treibt den Plan]
status: idee
prio: P2
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

# B-248 · Die Klassenarbeit wird beim Kind sichtbar und verdichtet den Plan vor dem Termin

Eine `Klassenarbeit` trägt ein `ScheduledDate` und ein Übungsbündel (direkt über `KlassenarbeitExercise`
und über Tags via `KlassenarbeitTag`), wirkt aber auf nichts: der Plan ändert sich vor dem Termin nicht,
und die Sohn-App zeigt die Arbeit nicht an (in `frontend/src/sohn/` kommt sie nur in einem Kommentar in
`SohnTest.tsx` vor). Knowunity baut den Tagesplan aus dem Prüfungstermin und verdichtet ihn, je näher er
rückt. Die Idee: (a) Countdown in der Sohn-Arcade („Englisch-Arbeit in 5 Tagen"), (b) eine
Bereitschaftsanzeige „wackelig → sicher" über die Items des Bündels, (c) die Positionen, deren Übungen
im Bündel liegen, werden in einem Fenster vor dem Termin stärker gewichtet – etwa durch eine zusätzliche
Tagesmission oder ein angehobenes Ziel. Der Vater setzt den Termin, die App erzwingt die Vorbereitung:
das ist die Kernidee in Reinform. Offen ist, ob die Verdichtung Pflicht (mit Malus) oder Angebot (mit
Bonus) sein soll und wer das Fenster festlegt.

## Verlauf

- **2026-10-23** — angelegt (Quelle: Knowunity-Vergleich, Etappe 1 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht; „wirkt auf nichts“ ist zu stark – Noten fließen in die Objective-Auswertung (`ObjectiveEvaluationService.cs:89-90`), es gibt den `repeat`-Endpunkt (`KlassenarbeitenController.cs:276`). Die Bündel-Auflösung teilt sie mit B-39.
- **2026-10-23** — **Zwei Entscheidungen des Nutzers** in der Grill-Runde G-7 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): (1) Die Verdichtung vor dem Termin ist **Pflicht mit Malus** – im Fenster entsteht eine zusätzliche Pflicht über das Bündel der Klassenarbeit, und Verpassen kostet Münzen wie jede andere Pflicht (Kernidee: der Vater setzt den Termin, die App erzwingt die Vorbereitung). (2) Das **Fenster hat einen festen Default und ist an der Klassenarbeit durch den Vater änderbar**; der Default-Wert selbst wird beim Ausformulieren vorgeschlagen (Anhaltspunkt 7 Tage). Beim Ausformulieren gegen den Code zu klären: wie die Zusatzpflicht entsteht (eigene Position im aktiven Plan, Mission oder angehobenes Ziel bestehender Positionen), Höhe des Malus und wie das Bündel aufgelöst wird (geteilt mit B-39).
- **2026-10-23** — **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-22); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an. Erteilt in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)).
- **2026-11-05** — Prioritäts-Beleg von außen: Baobab wirbt vor allem damit, genau das zu üben, was in der Arbeit drankommt. Vergleich in `docs/baobab-vergleich-plan.md` (Sammel-Story [B-374](B-374-baobab-vergleich-sammel-story.md)). Keine Änderung an den Entscheidungen.
