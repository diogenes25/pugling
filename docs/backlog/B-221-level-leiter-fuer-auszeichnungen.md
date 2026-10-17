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
