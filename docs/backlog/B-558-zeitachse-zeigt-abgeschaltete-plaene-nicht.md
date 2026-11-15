---
tags: [typ/story, status/idee, bereich/auswertung, rolle/supervisor]
aliases: [Zeitachse ohne alte Pläne]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Backend-Review Sprint 79, Backlog-Lauf 2026-11-15 (docs/pm-sitzung-2026-11-15-backlog-lauf.md)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-558 · Die Zeitachse der Kinder vergisst einen Plan, sobald er abgeschaltet ist

Die Zeitachse (B-39, `GET supervisor/children/overview`) zählt je Tag nur Pläne, die **heute** aktiv sind
(`ChildrenDashboardService.TimelineAsync`, Filter `p.Active`) – dieselbe Regel wie die Tagesübersicht und die
Malus-Fairness. Weil je Kind nur ein Plan aktiv ist (`DeactivateSiblingPlansAsync`), schaltet jeder neue Plan den alten
ab, und dessen vergangene Wochen verschwinden aus der Zeitachse. Für „seine Kinder über die Zeit“ ist das zu wenig: der
Vater sieht nach einem Kapitelwechsel nur noch die Wochen seit dem Wechsel.

Bewusst nicht in Sprint 79 gebaut: das Sprint-Ziel (Überblick über die laufende Zeit) gilt ohne, und die Frage, was an
einem vergangenen Tag „aktiv“ war, braucht eine Entscheidung (der Plan trägt keine Aktivierungs-Historie; ein Plan, der
nie aktiv war – z. B. ein Entwurf des KI-Agenten –, darf nicht als verpasste Pflicht rot erscheinen). Mit B-539 (ein
aktiver Plan je Fach) wird die Lücke kleiner, nicht null.

Vorschlag zum Ausformulieren: für Tage vor heute alle Pläne zählen, die an dem Tag liefen **und** an dem Tag eine
gewertete Antwort oder einen Testversuch des Kindes haben; oder eine Aktivierungs-Historie (Migration).

## Verlauf

- **2026-11-15** — angelegt aus dem Backend-Review von Sprint 79 (Backlog-Lauf 2026-11-15); die Doku von `ChildPeriod`
  nennt die Regel seither ehrlich („plans that are active now“).
