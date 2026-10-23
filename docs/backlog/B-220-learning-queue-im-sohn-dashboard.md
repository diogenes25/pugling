---
tags: [typ/story, status/idee, bereich/frontend, bereich/training, rolle/student]
aliases: [Learning Queue, Lern-Warteschlange, Heute-Woche-Überfällig]
status: idee
prio: P2
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

# B-220 · Eine Learning Queue in der Sohn-Arcade: heute, diese Woche, überfällig

Die Sohn-Arcade zeigt über `overview` die Tagesmission. Khan Academys „Reimagined"-Dashboard ordnet
alles nach Fälligkeit in Tages-, Wochen- und Unit-Missionen, und Überfälliges rutscht automatisch in die
Tagesliste. Die Idee: eine Warteschlange aus Pflichtpositionen, Missionen und (nach
[B-218](B-218-mischwiederholung-ueber-alle-positionen.md)) der Mischwiederholung, gruppiert nach
„heute", „diese Woche" und „überfällig", damit das Kind sieht, was es **jetzt** kostet, wenn es nichts
tut. Zu klären ist, ob die Gruppierung ein neuer Server-Endpunkt ist oder aus `overview` und den
Missionen clientseitig entsteht.

## Verlauf

- **2026-10-17** — angelegt (Quelle: Khan-Academy-Vergleich, Etappe 3 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): **zum Teil schon da, eine Annahme falsch.** `SohnHome.tsx:61-150` zeigt die Positionen des Tages mit Takt-Pill, „offen“ und „N fällig“, die Missionen gruppieren nach „Heute“/„Diese Woche“ (`GamificationPanels.tsx:7-11`). „Überfällig“ gibt es für Pflichtziele nicht – eine abgelaufene Periode wird als Malus gebucht und ist nicht nachholbar (`PositionProgressService.cs:34-46`); sinnvoll nur für Leitner-Karten (`DueCount`), Objectives (`Overdue`) und Klassenarbeiten. „Was es kostet“ braucht `PenaltyCoins` in `PositionStatus` (heute nur im Supervisor-DTO) – additiv.
- **2026-10-23** — **Zwei Entscheidungen des Nutzers** in der Grill-Runde G-5 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): (1) Die Queue **zeigt, was Nichtstun kostet**: je Pflicht „bis heute Abend, sonst −N Münzen“. Dazu kommen die fälligen Karten, die Mischwiederholung (B-218) und der Klassenarbeits-Countdown (B-248) in einer Liste. `PenaltyCoins` kommt additiv in den Student-Vertrag. „Überfällig“ gilt nur für Karten, Objectives und Klassenarbeiten, nicht für Pflichten. (2) Die Queue ist ein **Server-Endpunkt** (API-First, eine Sortierlogik), den B-250 (Stundenplan) später mitnutzt. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-20); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an. Erst nach B-218, die Countdown-Zeile erst nach B-248.
