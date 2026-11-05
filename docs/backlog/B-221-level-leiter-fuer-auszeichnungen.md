---
tags: [typ/story, status/idee, bereich/gamification, bereich/backend, rolle/student, rolle/supervisor]
aliases: [Level-Leiter, Badge-Leiter, Auszeichnungs-Stufen]
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

# B-221 · Eine Level-Leiter für Auszeichnungen statt einzelner Badges

Auszeichnungen sind bei uns je Kind frei konfigurierbar (`metric`, `threshold`, Emoji), aber ohne
Leiter: jeder Badge steht für sich. Khan Academy zerlegt einen Kurs in Levels und vergibt Badges bei
5, 10, 15, 25, 50, 75 und 100, jedes Level mit einer „Level up"-Feier. Die Idee: eine geseedete
Badge-Serie mit festen Schwellen je Metrik, die der `GamificationService` wie heute idempotent belohnt,
plus eine Darstellung der nächsten Stufe in der Sohn-Arcade. Knüpft an
[B-36](B-36-motivations-animationen-teilziele.md) an, das die Feier der Teilziele trägt.

## Verlauf

- **2026-10-17** — angelegt (Quelle: Khan-Academy-Vergleich, Etappe 4 des Übernahmeplans).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) (G-6): eine **feste, systemweit definierte Leiter** je Metrik gilt automatisch für jedes Kind, auch für neue Kinder und bestehende Datenbanken, und ist also kein Seed nur für leere DBs (vgl. `Seed.cs:689-705`). Die frei konfigurierbaren Abzeichen des Vaters bleiben daneben bestehen. Die Schwellen schlägt der Lauf beim Ausformulieren vor (Anhaltspunkt 5/10/25/50/100) und nennt sie im Verlauf. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-21); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-11-05** — **Rest-Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05, [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): Schwellen 5/10/25/50/100 je Metrik.
