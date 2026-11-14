---
tags: [typ/story, status/idee, bereich/klassenarbeit, bereich/auswertung, rolle/student, rolle/supervisor]
aliases: [Probeklausur, Notenprognose]
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

# B-249 · Eine Probeklausur über das Bündel einer Klassenarbeit, mit Notenprognose

Knowunity bietet vollständige, bewertete Probeklausuren, deren Ergebnis in den Plan zurückfließt. Wir
haben einen Abschlusstest **je Position** und an der `Klassenarbeit` das Feld `Grade` (1,0–6,0) für die
echte Note, aber keine Probe über das ganze Bündel einer Arbeit. Die Idee: das Kind schreibt vor dem
Termin eine Probeklausur aus den Übungen der Klassenarbeit (strikt one-at-a-time wie der Klausur-Modus),
der Server rechnet das Ergebnis in eine Note um, und nach dem Eintragen der echten Note sieht der Vater
Prognose und Ergebnis nebeneinander. Hängt an [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md)
(Bündel-Auflösung, Sichtbarkeit beim Kind). Zu klären: Notenschlüssel (fest, je Schulart, je
Klassenarbeit einstellbar), ob die Probe Münzen bringt und wie oft sie wiederholt werden darf, ohne dass
Auswendiglernen der Reihenfolge die Prognose entwertet.

## Verlauf

- **2026-10-23** — angelegt (Quelle: Knowunity-Vergleich, Etappe 2 des Übernahmeplans).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) (G-7): Der Notenschlüssel hat einen **festen Default** (ein üblicher Prozent-Schlüssel), den der Vater **je Klassenarbeit ändern** kann. Offen beim Ausformulieren: ob die Probe Münzen bringt und wie oft sie wiederholt werden darf, ohne dass das Auswendiglernen der Reihenfolge die Prognose entwertet. Hängt an B-248. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-22); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-11-05** — **Rest-Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05, [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): die Probeklausur bringt keine Münzen; beliebig oft wiederholbar, jede Runde neu gemischt, die Prognose zählt nur die erste Runde je Tag.
- **2026-11-13** — Backlog-Abgleich gegen den Code: besteht unverändert (kein Probeklausur-/Prognose-Endpunkt, kein Notenschlüssel an `KlassenarbeitEntities.cs:79-84`). Reihenfolge: nach [B-423](B-423-assistent-klassenarbeit-termin-und-stufenleiter.md) und B-248.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt – kein Probeklausur-/Prognose-Endpunkt und kein Notenschlüssel (`KlassenarbeitEntities.cs:79-84` trägt nur `ScheduledDate`/`Grade`/`GradeComment`); Reihenfolge nach B-423 und B-248 gilt weiter.
